using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class MobileAdsManager : MonoBehaviour
{
    public static MobileAdsManager I { get; private set; }

    [Header("General")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private SafeAreaFitter safeAreaFitter;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private RectTransform mainMenuScoresArea;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private float extraBottomPaddingPx = 24f;
    [SerializeField] private float extraTopPaddingPx = 8f;

    [Header("Android Ad Unit Ids")]
    [SerializeField] private string androidBannerId = "ca-app-pub-3940256099942544/9214589741";
    [SerializeField] private string androidRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("iOS Ad Unit Ids")]
    [SerializeField] private string iosBannerId = "ca-app-pub-3940256099942544/2435281174";
    [SerializeField] private string iosRewardedId = "ca-app-pub-3940256099942544/1712485313";

    private BannerView bannerView;
    private RewardedAd rewardedAd;

    private bool isInitialized;
    private bool isLoadingRewarded;
    private bool rewardEarned;
    private Action<bool> rewardResultCallback;

    private Vector2 bottomBarInitialAnchoredPosition;
    private bool bottomBarInitialPositionCached;

    private Vector2 mainMenuScoresInitialAnchoredPosition;
    private bool mainMenuScoresInitialPositionCached;

    private Vector2 topBarInitialAnchoredPosition;
    private bool topBarInitialPositionCached;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_2023_1_OR_NEWER
        if (safeAreaFitter == null) safeAreaFitter = FindFirstObjectByType<SafeAreaFitter>(FindObjectsInactive.Include);
#else
        if (safeAreaFitter == null) safeAreaFitter = FindObjectOfType<SafeAreaFitter>(true);
#endif
    }

    private void Start()
    {
        if (bottomBar != null)
        {
            bottomBarInitialAnchoredPosition = bottomBar.anchoredPosition;
            bottomBarInitialPositionCached = true;
        }

        if (mainMenuScoresArea != null)
        {
            mainMenuScoresInitialAnchoredPosition = mainMenuScoresArea.anchoredPosition;
            mainMenuScoresInitialPositionCached = true;
        }

        if (topBar != null)
        {
            topBarInitialAnchoredPosition = topBar.anchoredPosition;
            topBarInitialPositionCached = true;
        }

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
            return;
        }

        DestroyBottomBanner();

        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerView = new BannerView(GetBannerAdUnitId(), adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner loaded event fired.");
            ApplyBannerInset(adaptiveSize.Height);
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            Debug.LogWarning("Banner failed event fired: " + error);
            ApplyBannerInset(0f);
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
        if (!isInitialized || isLoadingRewarded)
        {
            return;
        }

        isLoadingRewarded = true;
        rewardedAd = null;

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
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
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Action<bool> callback = rewardResultCallback;
            bool success = rewardEarned;

            rewardResultCallback = null;
            rewardEarned = false;
            rewardedAd = null;

            LoadRewarded();
            callback?.Invoke(success);
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"Rewarded fullscreen failed: {error}");

            Action<bool> callback = rewardResultCallback;
            rewardResultCallback = null;
            rewardEarned = false;
            rewardedAd = null;

            LoadRewarded();
            callback?.Invoke(false);
        };
    }

    private void ApplyBannerInset(float bannerHeightDp)
    {
        Debug.Log($"ApplyBannerInset called. bannerHeightDp={bannerHeightDp}, safeAreaFitter={(safeAreaFitter != null ? safeAreaFitter.name : "NULL")}");

#if UNITY_2023_1_OR_NEWER
    if (safeAreaFitter == null) safeAreaFitter = FindFirstObjectByType<SafeAreaFitter>(FindObjectsInactive.Include);
#else
        if (safeAreaFitter == null) safeAreaFitter = FindObjectOfType<SafeAreaFitter>(true);
#endif
        if (safeAreaFitter == null)
        {
            Debug.LogWarning("SafeAreaFitter is still null.");
            return;
        }

        float bannerHeightPx = ConvertDpToPx(bannerHeightDp);
        float effectiveBottomOffset = bannerHeightPx > 0f ? bannerHeightPx + extraBottomPaddingPx : 0f;

        Debug.Log($"bannerHeightPx={bannerHeightPx}, effectiveBottomOffset={effectiveBottomOffset}");

        safeAreaFitter.SetExtraBottomInsetPx(effectiveBottomOffset);

        if (bottomBar != null)
        {
            if (!bottomBarInitialPositionCached)
            {
                bottomBarInitialAnchoredPosition = bottomBar.anchoredPosition;
                bottomBarInitialPositionCached = true;
            }

            bottomBar.anchoredPosition = new Vector2(
                bottomBarInitialAnchoredPosition.x,
                bottomBarInitialAnchoredPosition.y + effectiveBottomOffset
            );
        }

        if (mainMenuScoresArea != null)
        {
            if (!mainMenuScoresInitialPositionCached)
            {
                mainMenuScoresInitialAnchoredPosition = mainMenuScoresArea.anchoredPosition;
                mainMenuScoresInitialPositionCached = true;
            }

            mainMenuScoresArea.anchoredPosition = new Vector2(
                mainMenuScoresInitialAnchoredPosition.x,
                mainMenuScoresInitialAnchoredPosition.y + effectiveBottomOffset
            );
        }

        if (topBar != null)
        {
            if (!topBarInitialPositionCached)
            {
                topBarInitialAnchoredPosition = topBar.anchoredPosition;
                topBarInitialPositionCached = true;
            }

            topBar.anchoredPosition = new Vector2(
                topBarInitialAnchoredPosition.x,
                topBarInitialAnchoredPosition.y - extraTopPaddingPx
            );
        }
    }

    private float ConvertDpToPx(float dp)
    {
#if UNITY_EDITOR
        return dp * 3f;
#else
        if (Screen.dpi > 0f)
        {
            return dp * (Screen.dpi / 160f);
        }

        return Application.isMobilePlatform ? dp * 2f : dp;
#endif
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