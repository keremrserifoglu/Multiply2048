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
    private bool isLoadingRewarded;
    private bool rewardEarned;
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
        MobileAds.RaiseAdEventsOnUnityMainThread = true;
        if (initializeOnStart)
        {
            InitializeSdk();
        }
    }

    public void InitializeSdk()
    {
        if (isInitialized) return;

        MobileAds.Initialize(_ =>
        {
            isInitialized = true;
            LoadBottomBanner();
            LoadRewarded();
        });
    }

    public void LoadBottomBanner()
    {
        if (!isInitialized) return;

        DestroyBottomBanner();

        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        bannerView = new BannerView(GetBannerAdUnitId(), adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner loaded event fired.");

                float heightPx = bannerView != null ? bannerView.GetHeightInPixels() : 0f;
                if (heightPx <= 0f)
                {
                    heightPx = ConvertDpToPx(50f);
                    Debug.LogWarning($"GetHeightInPixels() returned zero, fallback is used: {heightPx}px");
                }

                Debug.Log($"Banner real height: {heightPx}px");
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

        bannerView.LoadAd(new AdRequest());
    }

    public void ShowRewarded(Action<bool> onCompleted)
    {
        if (!isInitialized)
        {
            onCompleted?.Invoke(false);
            return;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            LoadRewarded();
            onCompleted?.Invoke(false);
            return;
        }

        rewardResultCallback = onCompleted;
        rewardEarned = false;

        rewardedAd.Show(_ =>
        {
            rewardEarned = true;
        });
    }

    public void LoadRewarded()
    {
        if (!isInitialized || isLoadingRewarded) return;

        isLoadingRewarded = true;
        rewardedAd = null;

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingRewarded = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning($"Rewarded failed to load: {error}");
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedEvents(ad);
            });
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
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
                Debug.LogWarning($"Rewarded fullscreen error: {error}");

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

        if (safeAreaFitter == null) return;

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
        return "ca-app-pub-7230005206464633/6664349066";
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