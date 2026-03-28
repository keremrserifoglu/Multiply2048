using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

public class MobileAdsManager : MonoBehaviour
{
    public static MobileAdsManager I { get; private set; }

    public enum RewardFlow
    {
        LimitedCredits,
        GameOverShuffle
    }

    [Header("Init")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private SafeAreaFitter safeAreaFitter;

    [Header("Optional Test Device IDs")]
    [SerializeField] private List<string> testDeviceIds = new();

    [Header("Banner Layout")]
    [SerializeField] private bool reserveBannerSpaceInSafeArea = false;
    [SerializeField] private float extraBannerPaddingDp = 40f;
    [SerializeField] private float minimumBannerInsetDp = 60f;

    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool isInitialized;
    private bool isInitializing;
    private bool isLoadingRewarded;
    private bool isShowingRewarded;
    private bool rewardEarned;

    private bool rewardedClosed;
    private bool rewardCompletionSent;
    private Coroutine rewardedCloseRoutine;

    private Action<bool> rewardResultCallback;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsRewardedLoading => isLoadingRewarded;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeSdk();
        }
    }

    public void InitializeSdk()
    {
        if (isInitialized || isInitializing)
        {
            return;
        }

        isInitializing = true;
        ApplyRequestConfiguration();

        MobileAds.Initialize(_ =>
        {
            isInitializing = false;
            isInitialized = true;

            LoadBottomBanner();
            LoadRewarded();
        });
    }

    private void ApplyRequestConfiguration()
    {
        RequestConfiguration requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = testDeviceIds
        };

        MobileAds.SetRequestConfiguration(requestConfiguration);
    }

    public void LoadBottomBanner()
    {
        if (!isInitialized)
        {
            return;
        }

        DestroyBottomBanner();

        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        bannerView = new BannerView(GetBannerAdUnitId(), adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            float heightPx = bannerView != null ? bannerView.GetHeightInPixels() : 0f;
            ApplyBannerInsetPx(heightPx);
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            Debug.LogWarning("Banner failed to load: " + error);
            ApplyBannerInsetPx(0f);
        };

        bannerView.LoadAd(new AdRequest());
    }

    public void DestroyBottomBanner()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        ApplyBannerInsetPx(0f);
    }

    public void ShowRewarded(Action<bool> onCompleted)
    {
        ShowRewarded(RewardFlow.LimitedCredits, onCompleted);
    }

    public void ShowRewarded(RewardFlow flow, Action<bool> onCompleted)
    {
        if (!isInitialized)
        {
            InitializeSdk();
            onCompleted?.Invoke(false);
            return;
        }

        if (isShowingRewarded)
        {
            onCompleted?.Invoke(false);
            return;
        }

        if (!IsRewardedReady)
        {
            LoadRewarded();
            onCompleted?.Invoke(false);
            return;
        }

        RewardedAd adToShow = rewardedAd;
        rewardedAd = null;

        isShowingRewarded = true;
        rewardEarned = false;
        rewardedClosed = false;
        rewardCompletionSent = false;
        rewardResultCallback = onCompleted;

        adToShow.Show(_ =>
        {
            rewardEarned = true;
        });
    }

    public void LoadRewarded()
    {
        if (!isInitialized)
        {
            return;
        }

        if (isLoadingRewarded)
        {
            return;
        }

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            return;
        }

        isLoadingRewarded = true;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            isLoadingRewarded = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded failed to load: " + error);
                return;
            }

            rewardedAd = ad;
            RegisterRewardedEvents(ad);
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            rewardedClosed = true;
            isShowingRewarded = false;

            if (rewardedCloseRoutine != null)
                StopCoroutine(rewardedCloseRoutine);

            rewardedCloseRoutine = StartCoroutine(FinishRewardedAfterClose(ad));
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Rewarded fullscreen failed: " + error);
            CompleteRewardFlow(false, ad);
        };
    }

    private System.Collections.IEnumerator FinishRewardedAfterClose(RewardedAd ad)
    {
        yield return null;
        yield return new WaitForSecondsRealtime(0.35f);

        CompleteRewardFlow(rewardEarned, ad);
    }

    private void ApplyBannerInsetPx(float bannerHeightPx)
    {
        ResolveSafeAreaFitter();
        if (safeAreaFitter == null)
        {
            return;
        }

        if (!reserveBannerSpaceInSafeArea)
        {
            safeAreaFitter.SetExtraBottomInsetPx(0f);
            return;
        }

        float minInsetPx = DpToPx(minimumBannerInsetDp);
        float extraPaddingPx = DpToPx(extraBannerPaddingDp);
        float finalInsetPx = Mathf.Max(bannerHeightPx, minInsetPx) + extraPaddingPx;

        safeAreaFitter.SetExtraBottomInsetPx(finalInsetPx);
    }
    
    private void ResolveSafeAreaFitter()
    {
        if (safeAreaFitter != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        safeAreaFitter = FindFirstObjectByType<SafeAreaFitter>(FindObjectsInactive.Include);
#else
        safeAreaFitter = FindObjectOfType<SafeAreaFitter>(true);
#endif
    }

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return "ca-app-pub-3940256099942544/2435281174";
#else
        return "ca-app-pub-3940256099942544/6300978111";
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return "ca-app-pub-3940256099942544/1712485313";
#else
        return "ca-app-pub-3940256099942544/5224354917";
#endif
    }

    private void CompleteRewardFlow(bool success, RewardedAd ad)
    {
        if (rewardCompletionSent)
            return;

        rewardCompletionSent = true;
        rewardEarned = false;
        rewardedClosed = false;
        isShowingRewarded = false;

        Action<bool> callback = rewardResultCallback;
        rewardResultCallback = null;

        if (rewardedCloseRoutine != null)
        {
            StopCoroutine(rewardedCloseRoutine);
            rewardedCloseRoutine = null;
        }

        if (ad != null)
            ad.Destroy();

        LoadRewarded();

        GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            callback?.Invoke(success);
        });
    }

    private float DpToPx(float dp)
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) dpi = 160f;

        return dp * (dpi / 160f);
    }
}