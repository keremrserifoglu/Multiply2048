using System;
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

    [Header("General")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool useTestIds = true;
    [SerializeField] private SafeAreaFitter safeAreaFitter;

    [Tooltip("Keep this false if you do not want the whole UI to jump when the banner loads.")]
    [SerializeField] private bool reserveBannerSpaceInSafeArea = false;

    [Header("Android Ad Unit Ids")]
    [SerializeField] private string androidBannerId = "ca-app-pub-3940256099942544/9214589741";
    [SerializeField] private string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("iOS Ad Unit Ids")]
    [SerializeField] private string iosBannerId = "ca-app-pub-3940256099942544/2435281174";
    [SerializeField] private string iosRewardedId = "ca-app-pub-3940256099942544/1712485313";

    private BannerView bannerView;
    private RewardedAd rewardedAd;
    private bool isInitialized;
    private bool isShowingRewarded;
    private Action rewardResultCallback;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        ResolveSafeAreaFitter();
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
        if (isInitialized)
        {
            return;
        }

        MobileAds.Initialize(_ =>
        {
            isInitialized = true;
            LoadBottomBanner();
            LoadRewarded();
        });
    }

    public void LoadBottomBanner()
    {
        if (!isInitialized)
        {
            InitializeSdk();
            return;
        }

        DestroyBottomBanner();

        string adUnitId = GetBannerAdUnitId();
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        bannerView = new BannerView(adUnitId, adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            ApplyBannerInset(adaptiveSize.Height);
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            Debug.LogWarning($"Banner failed to load: {error}");
            ApplyBannerInset(0f);
        };

        bannerView.OnAdPaid += adValue =>
        {
            Debug.Log($"Banner paid event: {adValue.Value} {adValue.CurrencyCode}");
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

        ApplyBannerInset(0f);
    }

    public bool IsRewardedReady()
    {
        return rewardedAd != null && rewardedAd.CanShowAd();
    }

    public void ShowRewarded(RewardFlow flow, Action onCompleted)
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

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            LoadRewarded();
            onCompleted?.Invoke(false);
            return;
        }

        isShowingRewarded = true;
        rewardResultCallback = onCompleted;

        bool rewardEarned = false;

        rewardedAd.Show(_ =>
        {
            rewardEarned = true;
        });

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            CompleteRewardFlow(rewardEarned);
        };

        rewardedAd.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"Rewarded fullscreen failed: {error}");
            CompleteRewardFlow(false);
        };
    }

    public void LoadRewarded()
    {
        if (!isInitialized)
        {
            return;
        }

        rewardedAd = null;

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"Rewarded failed to load: {error}");
                return;
            }

            rewardedAd = ad;
            RegisterRewardedEvents(rewardedAd);
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded opened.");
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            LoadRewarded();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"Rewarded closed with error: {error}");
            LoadRewarded();
        };
    }

    private void CompleteRewardFlow(bool success)
    {
        if (!isShowingRewarded)
        {
            return;
        }

        isShowingRewarded = false;

        Action callback = rewardResultCallback;
        rewardResultCallback = null;

        callback?.Invoke(success);
    }

    private void ApplyBannerInset(float bannerHeightDp)
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

        float density = GetDisplayDensity();
        float bannerHeightPx = bannerHeightDp * density;
        safeAreaFitter.SetExtraBottomInsetPx(bannerHeightPx);
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

    private float GetDisplayDensity()
    {
        if (Screen.dpi > 0f)
        {
            return Screen.dpi / 160f;
        }

        return Application.isMobilePlatform ? 2f : 1f;
    }

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return iosBannerId;
#else
        return androidBannerId;
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        return iosRewardedId;
#else
        return androidRewardedId;
#endif
    }
}