using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using System;

public class GoogleMobileAdsScript : MonoBehaviour
{
    #region Public Method
    public void ShowBanner()
    {
        if (bannerView != null)
        {
            bannerView.Show();
        }
        else
        {
            RequestBanner();
        }
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void ShowAutoInterstitial()
    {
        countChangeScene++;
        print("countChangeScene : " + countChangeScene);

        if (countChangeScene % interstitialPerSceneNumberRate == 0)
        {
            // ShowAd
            ShowInterstitial();
        }
    }

    public void ShowInterstitial()
    {
        if (interstitialAd != null)
        {
            if (interstitialAd.IsLoaded())
            {
                interstitialAd.Show();
            }
        }
        else
        {
            RequestInterstitial();
        }
    }

    public void ShowRewardAd()
    {
        if (rewardedAd != null)
        {
            if (rewardedAd.IsLoaded())
            {
                rewardedAd.Show();
            }
        }
        else
        {
            RequestRewardedAd();
        }
    }

    public void RewardVideo(string type, double amount)
    {
        // put ur rewarded video here

    }
    #endregion

    #region Variables
    public static GoogleMobileAdsScript USE;

    [Header("Android Ads ID")]
    public string androidBannerUnitID = "ca-app-pub-3940256099942544/6300978111";
    public string androidInterstitialUnitID = "ca-app-pub-3940256099942544/1033173712";
    public string androidRewardUnitID = "ca-app-pub-3940256099942544/5224354917";

    [Header("iOS Ads ID")]
    public string iOSBannerUnitID = "ca-app-pub-3940256099942544/2934735716";
    public string iOSInterstitialUnitID = "ca-app-pub-3940256099942544/4411468910";
    public string iOSRewardUnitID = "ca-app-pub-3940256099942544/1712485313";

    [Header("Banner Ad Extra Config")]
    public bool bannerUse = false;
    public AdPosition adPosition = AdPosition.Bottom;
    public string bannerScenes = "";

    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    [Space]
    public bool rewardedUse;

    [Header("Interstitial Per Scene Number")]
    public bool interstitialUse;
    public bool autoInterstitial = true;
    public int interstitialPerSceneNumberRate = 15;
    public static int countChangeScene = 0;
    #endregion

    #region Init
    private void Awake()
    {
        if (USE == null)
        {
            USE = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        if (bannerScenes.Length < sceneCount)
        {
            int count = sceneCount - bannerScenes.Length;
            for (int i = 0; i < count; i++)
            {
                bannerScenes += "0";
            }
        }

        SceneManager.activeSceneChanged += ChangedActiveScene;

        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus => { });

        if (bannerUse)
        {
            RequestBanner();
        }

        if (interstitialUse)
        {
            RequestInterstitial();
        }

        if (rewardedUse)
        {
            RequestRewardedAd();
        }
    }

    private void ChangedActiveScene(Scene current, Scene next)
    {
        string currentName = current.name;

        if (currentName == null)
        {
            // Scene1 has been removed
            currentName = "Replaced";
        }

        //Debug.Log("Scenes: " + currentName + ", " + next.name);

        if (interstitialUse && autoInterstitial)
        {
            ShowAutoInterstitial();
        }

        if (bannerUse)
        {
            if (bannerScenes.Substring(next.buildIndex, 1) == "1")
            {
                ShowBanner();
            }
            else
            {
                HideBanner();
            }
        }
    }
    #endregion

    #region BannerAd
    private void RequestBanner()
    {
#if UNITY_ANDROID
        string adUnitId = androidBannerUnitID;
#elif UNITY_IPHONE
        string adUnitId = iOS_BannerUnitID;
#else
        string adUnitId = "unexpected_platform";
        return;
#endif

        // Create a 320x50 banner at the top of the screen.
        bannerView = new BannerView(adUnitId, AdSize.Banner, adPosition);

        // Called when an ad request has successfully loaded.
        bannerView.OnAdLoaded += HandleOnBannerAdLoaded;
        // Called when an ad request failed to load.
        bannerView.OnAdFailedToLoad += HandleOnBannerAdFailedToLoad;
        // Called when an ad is clicked.
        bannerView.OnAdOpening += HandleOnBannerAdOpened;
        // Called when the user returned from the app after an ad click.
        bannerView.OnAdClosed += HandleOnBannerAdClosed;
        // Called when the ad click caused the user to leave the application.
        bannerView.OnAdLeavingApplication += HandleOnBannerAdLeavingApplication;

        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();

        // Load the banner with the request.
        bannerView.LoadAd(request);
    }

    private void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    private void HandleOnBannerAdLoaded(object sender, EventArgs args)
    {
        print("HandleAdLoaded event received");
    }

    private void HandleOnBannerAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        print("HandleFailedToReceiveAd event received with message: "
                            + args.Message);

        DestroyBannerAd();
    }

    private void HandleOnBannerAdOpened(object sender, EventArgs args)
    {
        print("HandleAdOpened event received");
    }

    private void HandleOnBannerAdClosed(object sender, EventArgs args)
    {
        print("HandleAdClosed event received");

        DestroyBannerAd();
    }

    private void HandleOnBannerAdLeavingApplication(object sender, EventArgs args)
    {
        print("HandleAdLeavingApplication event received");

        DestroyBannerAd();
    }
    #endregion

    #region InterstitialAd
    private void RequestInterstitial()
    {
#if UNITY_ANDROID
        string adUnitId = androidInterstitialUnitID;
#elif UNITY_IPHONE
        string adUnitId = iOS_InterstitialUnitID;
#else
        string adUnitId = "unexpected_platform";
        return;
#endif

        // Initialize an InterstitialAd.
        interstitialAd = new InterstitialAd(adUnitId);

        // Called when an ad request has successfully loaded.
        interstitialAd.OnAdLoaded += HandleOnInterstitialAdLoaded;
        // Called when an ad request failed to load.
        interstitialAd.OnAdFailedToLoad += HandleOnInterstitialAdFailedToLoad;
        // Called when an ad is shown.
        interstitialAd.OnAdOpening += HandleOnInterstitialAdOpened;
        // Called when the ad is closed.
        interstitialAd.OnAdClosed += HandleOnInterstitialAdClosed;
        // Called when the ad click caused the user to leave the application.
        interstitialAd.OnAdLeavingApplication += HandleOnInterstitialAdLeavingApplication;

        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the interstitial with the request.
        interstitialAd.LoadAd(request);
    }

    private void DestroyIntrestitialAds()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }

    private void HandleOnInterstitialAdLoaded(object sender, EventArgs args)
    {
        print("HandleAdLoaded event received");
    }

    private void HandleOnInterstitialAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        print("HandleFailedToReceiveAd event received with message: "
                            + args.Message);

        DestroyIntrestitialAds();
    }

    private void HandleOnInterstitialAdOpened(object sender, EventArgs args)
    {
        print("HandleAdOpened event received");
    }

    private void HandleOnInterstitialAdClosed(object sender, EventArgs args)
    {
        print("HandleAdClosed event received");

        DestroyIntrestitialAds();
    }

    private void HandleOnInterstitialAdLeavingApplication(object sender, EventArgs args)
    {
        print("HandleAdLeavingApplication event received");

        DestroyIntrestitialAds();
    }
    #endregion

    #region RewardedAd
    private void RequestRewardedAd()
    {
#if UNITY_ANDROID
        string adUnitId = androidRewardUnitID;
#elif UNITY_IPHONE
        string adUnitId = iOS_RewardUnitID;
#else
        string adUnitId = "unexpected_platform";
        return;
#endif

        rewardedAd = new RewardedAd(adUnitId);

        // Called when an ad request has successfully loaded.
        rewardedAd.OnAdLoaded += HandleRewardedAdLoaded;
        // Called when an ad request failed to load.
        rewardedAd.OnAdFailedToLoad += HandleRewardedAdFailedToLoad;
        // Called when an ad is shown.
        rewardedAd.OnAdOpening += HandleRewardedAdOpening;
        // Called when an ad request failed to show.
        rewardedAd.OnAdFailedToShow += HandleRewardedAdFailedToShow;
        // Called when the user should be rewarded for interacting with the ad.
        rewardedAd.OnUserEarnedReward += HandleUserEarnedReward;
        // Called when the ad is closed.
        rewardedAd.OnAdClosed += HandleRewardedAdClosed;

        // Create an empty ad request.
        AdRequest request = new AdRequest.Builder().Build();
        // Load the rewarded ad with the request.
        rewardedAd.LoadAd(request);
    }

    private void DestroyRewardedAd()
    {
        rewardedAd = null;
    }

    private void HandleRewardedAdLoaded(object sender, EventArgs args)
    {
        print("HandleRewardedAdLoaded event received");
    }

    private void HandleRewardedAdFailedToLoad(object sender, AdErrorEventArgs args)
    {
        print(
            "HandleRewardedAdFailedToLoad event received with message: "
                             + args.Message);

        DestroyRewardedAd();
    }

    private void HandleRewardedAdOpening(object sender, EventArgs args)
    {
        print("HandleRewardedAdOpening event received");
    }

    private void HandleRewardedAdFailedToShow(object sender, AdErrorEventArgs args)
    {
        print(
            "HandleRewardedAdFailedToShow event received with message: "
                             + args.Message);

        DestroyRewardedAd();
        RequestRewardedAd();
    }

    private void HandleRewardedAdClosed(object sender, EventArgs args)
    {
        print("HandleRewardedAdClosed event received");

        DestroyRewardedAd();
        RequestRewardedAd();
    }

    private void HandleUserEarnedReward(object sender, Reward args)
    {
        string type = args.Type;
        double amount = args.Amount;
        print(
            "HandleRewardedAdRewarded event received for "
                        + amount.ToString() + " " + type);

        RewardVideo(type, amount);

        DestroyRewardedAd();
        RequestRewardedAd();
    }
    #endregion
}