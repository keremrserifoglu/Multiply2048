using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public class MobileAdsManager : MonoBehaviour
{
    public static MobileAdsManager I { get; private set; }

    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private SafeAreaFitter safeAreaFitter;

    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool isInitialized;
    private bool isInitializing;
    private bool isLoadingRewarded;
    private bool rewardEarned;

    public bool IsRewardedReady => rewardedAd != null && rewardedAd.CanShowAd();
    public bool IsRewardedLoading => isLoadingRewarded;

    private Action<bool> rewardResultCallback;

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
        Debug.Log("Initializing Mobile Ads SDK...");

        MobileAds.Initialize(_ =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isInitializing = false;
                isInitialized = true;

                Debug.Log("Mobile Ads SDK initialized.");

                LoadBottomBanner();
                LoadRewarded();
            });
        });
    }

    public void LoadBottomBanner()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Banner load skipped because ads are not initialized.");
            return;
        }

        DestroyBottomBanner();

        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        bannerView = new BannerView(GetBannerAdUnitId(), adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner loaded.");

                float heightPx = bannerView != null ? bannerView.GetHeightInPixels() : 0f;
                if (heightPx <= 0f)
                {
                    heightPx = ConvertDpToPx(50f);
                    Debug.LogWarning($"Banner height returned zero. Fallback is used: {heightPx}px");
                }

                ApplyBannerInsetPx(heightPx);
            });
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogWarning("Banner failed to load: " + error);
                ApplyBannerInsetPx(0f);
            });
        };

        Debug.Log("Loading banner...");
        bannerView.LoadAd(new AdRequest());
    }

    public void ShowRewarded(Action<bool> onCompleted)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Rewarded could not be shown because ads are not initialized.");
            onCompleted?.Invoke(false);
            return;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("Rewarded ad is not ready yet.");
            LoadRewarded();
            onCompleted?.Invoke(false);
            return;
        }

        rewardResultCallback = onCompleted;
        rewardEarned = false;

        Debug.Log("Showing rewarded ad...");

        rewardedAd.Show(_ =>
        {
            Debug.Log("Reward callback received.");
            rewardEarned = true;
        });
    }

    public void LoadRewarded()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Rewarded load skipped because ads are not initialized.");
            return;
        }

        if (isLoadingRewarded)
        {
            return;
        }

        isLoadingRewarded = true;
        rewardedAd = null;

        Debug.Log("Loading rewarded ad...");

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingRewarded = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("Rewarded failed to load: " + error);
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedEvents(ad);

                Debug.Log("Rewarded loaded.");
            });
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Rewarded closed.");

                Action<bool> callback = rewardResultCallback;
                bool success = rewardEarned;

                rewardResultCallback = null;
                rewardEarned = false;
                rewardedAd = null;

                LoadRewarded();
                callback?.Invoke(success);
            });
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogWarning("Rewarded fullscreen failed: " + error);

                Action<bool> callback = rewardResultCallback;

                rewardResultCallback = null;
                rewardEarned = false;
                rewardedAd = null;

                LoadRewarded();
                callback?.Invoke(false);
            });
        };
    }

    private void ApplyBannerInsetPx(float bannerHeightPx)
    {
        if (safeAreaFitter == null)
        {
            safeAreaFitter = FindFirstObjectByType<SafeAreaFitter>(FindObjectsInactive.Include);
        }

        if (safeAreaFitter == null)
        {
            return;
        }

        safeAreaFitter.SetExtraBottomInsetPx(Mathf.Max(0f, bannerHeightPx));
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

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return "YOUR_IOS_BANNER_ID";
#else
        return "ca-app-pub-7230005206464633/9290512404";
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return "YOUR_IOS_REWARDED_ID";
#else
        return "ca-app-pub-7230005206464633/6429641198";
#endif
    }

    private float ConvertDpToPx(float dp)
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f)
        {
            dpi = 160f;
        }

        return dp * (dpi / 160f);
    }
}