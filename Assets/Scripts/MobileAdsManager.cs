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

    [Header("Banner Layout")]
    [SerializeField] private bool reserveBannerSpaceInSafeArea = false;

    [Header("Optional Test Device IDs")]
    [SerializeField] private List<string> testDeviceIds = new();

    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool isInitialized;
    private bool isInitializing;
    private bool isLoadingRewarded;
    private bool isShowingRewarded;
    private bool rewardEarned;

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
            bool success = rewardEarned;
            rewardEarned = false;
            isShowingRewarded = false;

            Action<bool> callback = rewardResultCallback;
            rewardResultCallback = null;

            ad.Destroy();
            LoadRewarded();

            callback?.Invoke(success);
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning("Rewarded fullscreen failed: " + error);

            rewardEarned = false;
            isShowingRewarded = false;

            Action<bool> callback = rewardResultCallback;
            rewardResultCallback = null;

            ad.Destroy();
            LoadRewarded();

            callback?.Invoke(false);
        };
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

        safeAreaFitter.SetExtraBottomInsetPx(Mathf.Max(0f, bannerHeightPx));
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
}