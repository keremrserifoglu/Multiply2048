using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public class MobileAdsManager : MonoBehaviour
{
    public static MobileAdsManager I { get; private set; }

    [Header("Init")]
    [SerializeField] private bool initializeOnStart = true;

    [Header("Testing")]
    [SerializeField] private bool useTestBannerId = true;
    [SerializeField] private bool useTestDevice = true;
    [SerializeField] private string androidTestDeviceId = "105BB3D8317B32D820233396FC8FC1E7";

    [Header("UI")]
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
            Debug.Log("InitializeSdk skipped. Already initialized or initializing.");
            return;
        }

        isInitializing = true;

        Debug.Log("Initializing Mobile Ads SDK...");

        if (useTestDevice && !string.IsNullOrWhiteSpace(androidTestDeviceId))
        {
            List<string> testDeviceIds = new List<string> { androidTestDeviceId };

            RequestConfiguration requestConfiguration = new RequestConfiguration
            {
                TestDeviceIds = testDeviceIds
            };

            MobileAds.SetRequestConfiguration(requestConfiguration);
            Debug.Log("AdMob test device registered: " + androidTestDeviceId);
        }
        else
        {
            Debug.Log("AdMob test device registration skipped.");
        }

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
        Debug.Log("LoadBottomBanner() entered.");

        if (!isInitialized)
        {
            Debug.LogWarning("Banner load skipped because ads are not initialized.");
            return;
        }

        DestroyBottomBanner();

        int widthDp = GetAdaptiveBannerWidthDp();
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(widthDp);
        string unitId = GetBannerAdUnitId();

        Debug.Log("Creating banner...");
        Debug.Log("Banner unit id: " + unitId);
        Debug.Log("Banner width dp: " + widthDp);

        bannerView = new BannerView(unitId, adaptiveSize, AdPosition.Bottom);

        bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner loaded.");

                if (bannerView != null)
                {
                    Debug.Log("Banner response info: " + bannerView.GetResponseInfo());
                }

                float heightPx = bannerView != null ? bannerView.GetHeightInPixels() : 0f;
                if (heightPx <= 0f)
                {
                    heightPx = ConvertDpToPx(50f);
                    Debug.LogWarning("Banner height returned zero. Fallback height is used: " + heightPx + "px");
                }

                Debug.Log("Applying banner inset px: " + heightPx);
                ApplyBannerInsetPx(heightPx);
            });
        };

        bannerView.OnBannerAdLoadFailed += error =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogWarning("Banner failed to load: " + error);

                if (error != null)
                {
                    Debug.LogWarning("Banner failure response info: " + error.GetResponseInfo());
                }

                ApplyBannerInsetPx(0f);
            });
        };

        bannerView.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner impression recorded.");
            });
        };

        bannerView.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner clicked.");
            });
        };

        Debug.Log("Loading banner ad request...");
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
        Debug.Log("LoadRewarded() entered.");

        if (!isInitialized)
        {
            Debug.LogWarning("Rewarded load skipped because ads are not initialized.");
            return;
        }

        if (isLoadingRewarded)
        {
            Debug.Log("Rewarded load skipped because loading is already in progress.");
            return;
        }

        isLoadingRewarded = true;
        rewardedAd = null;

        Debug.Log("Loading rewarded ad...");
        Debug.Log("Rewarded unit id: " + GetRewardedAdUnitId());

        RewardedAd.Load(GetRewardedAdUnitId(), new AdRequest(), (ad, error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                isLoadingRewarded = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("Rewarded failed to load: " + error);

                    if (error != null)
                    {
                        Debug.LogWarning("Rewarded failure response info: " + error.GetResponseInfo());
                    }

                    return;
                }

                rewardedAd = ad;
                RegisterRewardedEvents(ad);

                Debug.Log("Rewarded loaded.");
                Debug.Log("Rewarded response info: " + ad.GetResponseInfo());
            });
        });
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Rewarded fullscreen opened.");
            });
        };

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

        ad.OnAdPaid += value =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Rewarded OnAdPaid fired. Value: " + value.Value + " " + value.CurrencyCode);
            });
        };

        ad.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Rewarded impression recorded.");
            });
        };

        ad.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Rewarded clicked.");
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
            Debug.LogWarning("SafeAreaFitter not found. Banner inset cannot be applied.");
            return;
        }

        float appliedInset = Mathf.Max(0f, bannerHeightPx);
        safeAreaFitter.SetExtraBottomInsetPx(appliedInset);

        Debug.Log("SafeAreaFitter bottom inset set to: " + appliedInset + "px");
    }

    public void DestroyBottomBanner()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying existing banner.");
            bannerView.Destroy();
            bannerView = null;
        }

        ApplyBannerInsetPx(0f);
    }

    private int GetAdaptiveBannerWidthDp()
    {
        float scale = MobileAds.Utils.GetDeviceScale();
        if (scale <= 0f)
        {
            scale = 1f;
        }

        int widthDp = Mathf.RoundToInt(Screen.width / scale);

        if (widthDp <= 0)
        {
            widthDp = 320;
        }

        return widthDp;
    }

    private string GetBannerAdUnitId()
    {
#if UNITY_IOS
        return useTestBannerId
            ? "ca-app-pub-3940256099942544/2435281174"
            : "YOUR_IOS_BANNER_ID";
#else
        return useTestBannerId
            ? "ca-app-pub-3940256099942544/9214589741"
            : "ca-app-pub-7230005206464633/9290512404";
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