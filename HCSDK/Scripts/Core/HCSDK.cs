using System.Collections.Generic;
using System.Security.Cryptography;    
using UnityEngine;
using PlistCS;
using System;
using System.Collections;
using System.Threading.Tasks;
using Coredian;
using Coredian.Native;
using System.Runtime.CompilerServices;
using Coredian.CrossPromo;
using Coredian.Privacy;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace BoomBit.HyperCasual
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using AppsFlyerSDK;

    using Coredian.AdvertisingId;
    using Coredian.Facebook;
    using Coredian.Serialization.Json;
    using Coredian.Firebase;
#if GAME_SERVICES
    using Coredian.GameServices;
#endif

    using UnityEngine.Networking;

    using ConsentType = Coredian.Privacy.ConsentType;

    public class HCSDK : MonoBehaviour
    {
        private const string UndefinedConsentMessage = "Consent is undefined! Configure the Consent Config Node.";
        
#if GAME_SERVICES
        private IGameServicesService GameCenter => Core.GetService<IGameServicesService>();
#endif

        private static Type mediationProvider = typeof(IronSourceProvider);
        private const string providerTag = "IS";
        
        private static bool wasDestroyed = false;
        private bool isPaused = false;
        private static HCSDK instance = null;
        private Dictionary<string, object> configuration;
        private IAdsProvider adsProvider = null;
        private AppsFlyerProvider appsFlyer = null;
        private AnalyticsTracker analyticsTracker;
        private WhitelistMechanism whitelistMechanism = null;
        private DeviceRegionMechanism deviceRegionMechanism = null;
        private bool isFirstSession = false;
        private bool noAds = false;
        private bool shouldWaitForAttPopup = false;
        private string customUserId = null;

        private IConsentService consentService
        { 
            get { return  Core.GetService<IConsentService>(); }
        }
        
        private bool adsInitialized;
        private bool fiamConsent;
        private bool fConsent;
        private InterstitialMechanism interstitialMechanism;
        private RewardedMechanism rewardedMechanism;
        private IAd currentlyDisplayedMoreGames;

        private GameObject fpsCounterGO = null;
        private bool reviewPopupShowed = false;

        /// <summary>
        /// Called when a rewarded ad has been viewed successfully and the user has earn an award.
        /// </summary>
        public static event Action VideoSuccessEvent;
        
        /// <summary>
        /// Called when a rewarded ad fail to show.
        /// </summary>
        public static event Action VideoFailEvent;
        
        /// <summary>
        /// Called when a rewarded ad was cancelled while before end.
        /// </summary>
        public static event Action VideoCancelEvent;
        
        /// <summary>
        /// Called when a rewarded ad was displayed,
        /// </summary>
        public static event Action VideoDisplayEvent;
        
        /// <summary>
        /// Called when a rewarded ad was clicked.
        /// </summary>
        public static event Action VideoClickEvent;

        /// <summary>
        /// Called when an interstitial ad was closed.
        /// </summary>
        public static event Action InterstitialCloseEvent;
        
        /// <summary>
        /// Called when an interstitial ad was displayed.
        /// </summary>
        public static event Action InterstitialDisplayEvent;
        
        /// <summary>
        /// Called when an interstitial ad was clicked.
        /// </summary>
        public static event Action InterstitialClickEvent;
        
        /// <summary>
        /// Called when an interstitial ad fail.
        /// </summary>
        public static event Action InterstitialFailEvent;

        /// <summary>
        /// Called when HCSDK start initialization of ads mediation.
        /// </summary>
        public static event Action InitializationStartEvent;

        /// <summary>
        /// Called when app is installed and opened with the App Link generated from AppsFlyer.
        /// Returns Dictionary that contains all conversion data.
        /// </summary>
        public static event Action<Dictionary<string, object>> ReceivedConversionData;
        
        
        /// <summary>
        /// Indicates if app already have conversion Data.
        /// </summary>
        public static bool HasConversionData {
            get
            {
                if (instance == null || instance.appsFlyer == null) return false;
                else return instance.appsFlyer.HasConversionData;
            }
        }
        
        /// <summary>
        /// Returns conversion Data if any.
        /// </summary>
        public static Dictionary<string, object> ConversionData
        {
            get
            {
                if (HasConversionData) return instance.appsFlyer.ConversionData;

                return null;
            }
        }
        
        /// <summary>
        /// Called when a banner was displayed. 
        /// </summary>
        public static event Action BannerDisplayEvent;

        public static event Action BackFromBackgroundEvent;

        [Obsolete("HCSDK.LoginInGCSuccessfullEvent is deprecated. Use Coredian.GameServices.LogInSuccessful instead.", false)]
        public static event Action LoginInGCSuccessfullEvent;
        
        [Obsolete("HCSDK.LoginInGCFailEvent is deprecated. Use Coredian.GameServices.LogInFailed instead.", false)]
        public static event Action LoginInGCFailEvent;
        
        [Obsolete("HCSDK.LoggedOutGC is deprecated. Removed from first-party API.", true)]
        public static event Action LoggedOutGC;
        
        public static event Action FbInitComplete;
        
        private static bool initializationComplete = false;
        public static bool InitializationComplete
        {
            get { return initializationComplete; }
        }

        public static HCSDK Instance
        {
            get
            {
                if (wasDestroyed) return null;

                if (instance == null)
                {
                    StartHCSDK();
                }

                return instance;
            }
        }

        /// <summary>
        /// Start the HCSDK. Should be invoked before any other HCSDK methods.
        /// </summary>
        public static void StartHCSDK()
        {
            if (wasDestroyed) return;
            if (!Application.isPlaying) return;
            if (instance != null) return;
            
            GameObject go = new GameObject("HCSDK", typeof(HCSDK));
            instance = (HCSDK) go.GetComponent<HCSDK>();
            DontDestroyOnLoad(go);

            instance.LoadConfiguration();

            go=new GameObject("InterstitialMechanism", typeof(InterstitialMechanism));
            go.transform.parent = instance.transform;
            instance.interstitialMechanism=(InterstitialMechanism) go.GetComponent<InterstitialMechanism>();
            instance.interstitialMechanism.Initialize();
            
            go=new GameObject("RewardedMechanism", typeof(RewardedMechanism));
            go.transform.parent = instance.transform;
            instance.rewardedMechanism=(RewardedMechanism) go.GetComponent<RewardedMechanism>();
            instance.rewardedMechanism.Initialize();
            
            if (PlayerPrefs.GetInt("HCSDK_FIRST_SESSION", 1) == 1)
            {
                Instance.isFirstSession = true;
                PlayerPrefs.SetInt("HCSDK_FIRST_SESSION", 0);
            }
            
            if (PlayerPrefs.GetInt("HCSDK_NO_ADS", 0) == 1)
            {
                Instance.noAds = true;
            }

            instance.InitWhitelist();
            instance.InitDeviceRegion();

            go=new GameObject("AnalyticsTracker", typeof(AnalyticsTracker));
            go.transform.parent = instance.transform;
            instance.analyticsTracker=(AnalyticsTracker) go.GetComponent<AnalyticsTracker>();

            instance.InitGameCenter();

            if (Core.IsInitialized)
            {
                instance.OnCoreInitialization();
            }

            Core.InitializationSucceeded += instance.OnCoreInitialization;
            Core.WillDeinitialize += instance.OnCoreDeinitialization;
        }

        private void OnCoreInitialization()
        {
            Log.Info("HCSDK :: OnCoreInitialization");
            
            instance.consentService.ConsentChanged += instance.OnConsentChanged;
            instance.CheckIfProvidersShouldWaitForAtt();

            if (Core.GetService<IFirebaseRemoteConfigService>() != null)
            {
                if (Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigInitialized)
                {
                    Log.Info("HCSDK :: OnCoreInitialization :: IsRemoteConfigInitialized");
                    OnFirebaseRemoteConfigInitializedSuccessfulEvent();
                }
                else
                {
                    Core.GetService<IFirebaseRemoteConfigService>().FirebaseRemoteConfigInitializedSuccessful +=
                        OnFirebaseRemoteConfigInitializedSuccessfulEvent;
                }
            }

            if (Core.GetService<IFirebaseCrashlyticsService>() != null)
            {
                Core.GetService<IFirebaseCrashlyticsService>().SetCustomKey("HCSDK Version", HCSDKVersion.Version);

                foreach (var VARIABLE in HCCoreModules.Modules)
                {
                    Core.GetService<IFirebaseCrashlyticsService>().SetCustomKey(VARIABLE.Key, VARIABLE.Value);
                }
            }

            CheckAndInitProviders();

#if UNITY_EDITOR
            instance.InitTesterGUIEditor();
#else
            instance.InitTesterGUI();
#endif

            if (Core.GetService<FacebookService>().IsSdkInitialized) OnFbInitComplete();
            else Core.GetService<FacebookService>().SdkInitialized += OnFbInitComplete;
        }
        

        private void OnCoreDeinitialization()
        {
           consentService.ConsentChanged -= OnConsentChanged;
        }
        
        private void OnFirebaseRemoteConfigInitializedSuccessfulEvent()
        {
            Core.GetService<IFirebaseRemoteConfigService>().FirebaseRemoteConfigInitializedSuccessful -=
                OnFirebaseRemoteConfigInitializedSuccessfulEvent;
            Log.InfoFormat("HCSDK :: OnFirebaseRemoteConfigInitializedSuccessfulEvent");

            Log.InfoFormat("HCSDK :: FetchRemoteConfig");

            Core.GetService<IFirebaseRemoteConfigService>().FetchRemoteConfig();
        }
        
        private void OnConsentChanged(ConsentType consentType)
        {
            Log.InfoFormat("HCSDK :: OnConsentChanged type=>{0}", consentType);
            CheckAndInitProviders();
        }
        
        private static void OnFbInitComplete()
        {
            Core.GetService<IFacebookService>().SdkInitialized -= OnFbInitComplete;
            FbInitComplete?.Invoke();
        }

        /// <summary>
        /// Get the parameter from local configuration (iOSConfiguration or AndroidConfiguration).
        /// </summary>
        /// <param name="key">Name of the key from configuration.</param>
        /// <returns>Parameter value, or an empty string if it does not exist.</returns>
        public static string GetLocalParameter(string key)
        {
            if (wasDestroyed || (Instance.configuration == null)) return "";
            if (!Instance.configuration.ContainsKey(key)) return "";
            
            return Instance.configuration[key].ToString();
        }
        private static string GetRemoteParameter(string key)
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() == null || !Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded)
            {
                return "";
            }
            else
            {
                if(Core.GetService<IFirebaseRemoteConfigService>().RemoteConfig.TryGetValue(key, out string value))
                    return value;
                else
                    return "";
            }
        }
        
        /// <summary>
        /// Get the parameter from local or remote configuration. Always try remote configuration first.
        /// </summary>
        /// <param name="key">Name of the key from configuration.</param>
        /// <returns>Parameter value, or an empty string if it does not exist.</returns>
        public static string GetParameter(string key)
        {
            if (GetRemoteParameter(key) != "") return GetRemoteParameter(key);
            return GetLocalParameter(key);
        }

        public static void SetCustomUserId(string customUserId)
        {
            Instance.customUserId = customUserId;
            if (Instance.appsFlyer != null) Instance.appsFlyer.SetCustomUserId(customUserId);
        }

        private void WaitForFetchSuccessfulConfig(IDictionary<string, string> obj)
        {
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded -= instance.WaitForFetchSuccessfulConfig;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed -= instance.WaitForFetchFailConfig;
            StartCoroutine(WaitBeforeInitAdsProvider());
        }

        private void WaitForFetchFailConfig()
        {
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded -= instance.WaitForFetchSuccessfulConfig;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed -= instance.WaitForFetchFailConfig;
            StartCoroutine(WaitBeforeInitAdsProvider());
        }
        
        private IEnumerator WaitBeforeInitAdsProvider()
        {
            adsInitialized = true;
            yield return new WaitForEndOfFrame();

            float waitTime;
            if (ShouldWaitBeforeIS(out waitTime))
            {
                yield return new WaitForSecondsRealtime(waitTime);
            }

            if (GetLocalParameter("wait_for_deeplink") == "TRUE")
            {
                if (appsFlyer.HasConversionData) InitAds();
                else appsFlyer.ReceivedConversionData += InitAds;   
            }
            else
            {
                InitAds();
            }
        }

        private bool ShouldWaitBeforeIS(out float waitTime)
        {
            waitTime = 6f;
            if (isFirstSession)
            {
                if (configuration.ContainsKey("wait_for_deeplink") && configuration["wait_for_deeplink"].ToString()=="TRUE")
                {
                    string deeplinkWaitTimeString = null;
                    if (configuration.ContainsKey("deeplink_wait_time"))
                    {
                        deeplinkWaitTimeString = configuration["deeplink_wait_time"].ToString();
                    }
                    else if (configuration.ContainsKey("deepling_wait_time"))
                    {
                        deeplinkWaitTimeString = configuration["deepling_wait_time"].ToString();
                    }

                    if (deeplinkWaitTimeString != null)
                    {
                        if (!float.TryParse(deeplinkWaitTimeString, out waitTime))
                        {
                            waitTime = 6f;
                        }
                    }

                    return true;
                }
            }
            
            return false;
        }

        public static void ShowFPSCounter(float x = 0.8f, float y = 0.9f)
        {
            if (instance.fpsCounterGO == null)
            {
                instance.fpsCounterGO = new GameObject("fpsCounter");
                
                FPSCounter counter = instance.fpsCounterGO.AddComponent<FPSCounter>();
                counter.SetPosition(x, y);
                
            }
        }

        public static void HideFPSCounter()
        {
            Destroy(instance.fpsCounterGO);
            instance.fpsCounterGO = null;
        }

        /// <summary>
        /// Disable any ads served by HCSDK.
        /// </summary>
        /// <param name="pernament">Ads will be disabled after restarting the app if set to true.</param>        
        public static void DisableAds(bool pernament = true)
        {
            if(pernament) PlayerPrefs.SetInt("HCSDK_NO_ADS", 1);
            Instance.noAds = true;
        }
        
        /// <summary>
        /// Indicates if an interstitial ad is ready.
        /// </summary>
        /// <param name="placement">Placement of the ad.</param>
        /// <returns><c>true</c>, if an interstitial ad is ready, <c>false</c> otherwise.</returns>
        public static bool IsInterstitialReady(string placement)
        {
            Instance.CheckIfConsentDefined();
            if (wasDestroyed || Instance.interstitialMechanism==null) return false;

            return Instance.interstitialMechanism.IsInterstitialReady(placement);
        }

        /// <summary>
        /// Indicates if a rewarded ad is ready.
        /// </summary>
        /// <param name="placement">Placement of the ad.</param>
        /// <returns><c>true</c>, if a rewarded ad is ready, <c>false</c> otherwise.</returns>
        public static bool IsRewardedReady(string placement, bool withCP = false)
        {
            Instance.CheckIfConsentDefined();
            if (wasDestroyed || Instance.rewardedMechanism==null) return false;
            return Instance.rewardedMechanism.IsRewardedReady(placement, withCP);
        }
        
        /// <summary>
        /// Indicates if a rewarded ad is displayed.
        /// </summary>
        /// <returns><c>true</c>, if a rewarded ad is showed, <c>false</c> otherwise.</returns>
        public static bool IsRewardedShowed()
        {
            if (wasDestroyed || Instance.rewardedMechanism==null) return false;
            return Instance.rewardedMechanism.IsRewardedShowed();
        }

        public static void ShowMoreGames()
        {
            Instance.currentlyDisplayedMoreGames = Core.GetService<CrossPromoService>().GetAd(AdUnit.MoreGames);
            if (Instance.currentlyDisplayedMoreGames != null)
            {
                Instance.currentlyDisplayedMoreGames.Shown += () => BannerMechanism.Instance.MoreGamesShowed();
                Instance.currentlyDisplayedMoreGames.Hidden += () => BannerMechanism.Instance.AfterMoreGames();
                Instance.currentlyDisplayedMoreGames.Show();
            }
        }
        /// <summary>
        /// Show an interstitial ad.
        /// </summary>
        /// <param name="placement">Placement of the ad.</param>
        public static void ShowInterstitial(string placement)
        {
            Instance.CheckIfConsentDefined();
#if UNITY_EDITOR
            if (instance == null)
            {
                return;
            }
#endif
            if (Instance.noAds) return;
            if (wasDestroyed || Instance.interstitialMechanism == null) return;
            
            if (Instance.interstitialMechanism.IsInterstitialReady(placement)) BannerMechanism.Instance.FullscreenAdShowed();            

            Instance.interstitialMechanism.ShowInterstitial(placement);
        }
        
        /// <summary>
        /// Show a rewarded ad.
        /// </summary>
        /// <param name="placement">Placement of the ad.</param>
        public static void ShowRewarded(string placement)
        {
            Instance.CheckIfConsentDefined();
            if (wasDestroyed || Instance.rewardedMechanism == null) return;
            
            if (Instance.rewardedMechanism.IsRewardedReady(placement, true)) BannerMechanism.Instance.FullscreenAdShowed();
            
            Instance.rewardedMechanism.ShowRewarded(placement);
        }
        
        /// <summary>
        /// Show banner ad on bottom center of the screen.
        /// </summary>
        public static void ShowBanner()
        {
#if UNITY_EDITOR
            if (instance == null)
            {
                return;
            }
#endif
            Instance.CheckIfConsentDefined();
            
            if (Instance.noAds) return;
            if (wasDestroyed || BannerMechanism.Instance == null) return;
            BannerMechanism.Instance.ShowBanner();
        }

        /// <summary>
        /// Hide banner ad.
        /// </summary>
        public static void HideBanner()
        {
#if UNITY_EDITOR
            if (instance == null)
            {
                return;
            }
#endif
            if (Instance.noAds) return;
            if (wasDestroyed || BannerMechanism.Instance == null) return;
            BannerMechanism.Instance.HideBanner();
        }
        
        /// <summary>
        /// Start caching interstitial ad. Works only when <c>manual_interstitial_caching</c> is set to <c>true</c>.
        /// </summary>
        public static void StartCachingInterstitial()
        {
#if UNITY_EDITOR
            if (instance == null)
            {
                return;
            }
#endif
            if (Instance.noAds) return;
            if (wasDestroyed || Instance.adsProvider == null) return;
            Instance.adsProvider.StartCachingInterstitial();
        }

        /// <summary>
        /// Get AppId(iOS) or bundle(Android) of app.
        /// </summary>
        /// <returns></returns>
        public static string GetAppId()
        {
            if (instance.configuration.ContainsKey("appId"))
            {
                return instance.configuration["appId"].ToString();
            }

            return "";
        }

        public static void SetReviewMode(bool reviewMode)
        {
            if (reviewMode && !instance.reviewPopupShowed)
            {
                if (GetParameter("HCSDK_SHOW_IDFA_POPUP").ToUpperInvariant() != "FALSE")
                {
                    instance.reviewPopupShowed = true;
                    instance.ShowIdfaPopupInReview();
                }
            }
        }

        /// <summary>
        /// Get device region.
        /// </summary>
        /// <returns>Device region in ISO2</returns>
        public static string GetDeviceRegion()
        {
            return Instance.deviceRegionMechanism.GetDeviceLocalization();
        }

        public static async Task ShowConsent()
        {
            _ = await instance.consentService.AskForConsentAsync(ConsentType.All);
        }
        
        public static async Task OpenConsentManagement()
        {
            _ = await Core.GetService<IConsentService>().OpenConsentManagementAsync();
        }
        
        public static void TrackNoRewardedVideo(string placement)
        {
            if (wasDestroyed || Instance.analyticsTracker == null) return;
            Instance.analyticsTracker.TrackAdFail(AdType.rewarded, placement);
        }


        public static void ShowWifiSettings()
        {
#if !UNITY_EDITOR && UNITY_IOS
            Application.OpenURL("App-Prefs:root=WIFI");
#elif !UNITY_EDITOR && UNITY_ANDROID
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivityObject =
                    unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var intentObject = new AndroidJavaObject(
                        "android.content.Intent", "android.settings.WIFI_SETTINGS"))
                    {
                        currentActivityObject.Call("startActivity", intentObject);
                    }
#endif
        }


#if GAME_SERVICES
        [Obsolete("HCSDK.GameCenterLogIn is deprecated. Use Coredian.GameServices.LogIn instead.", false)]
        public static void GameCenterLogIn() 
        {
            if(instance != null && instance.GameCenter != null)
                instance.GameCenter.LogIn();
        }

        [Obsolete("HCSDK.GameCenterLogOut is deprecated. Removed from first-party API.", true)]
        public static void GameCenterLogOut() 
        {
        }

        [Obsolete("HCSDK.GameCenterIsSignedIn is deprecated. Use Coredian.GameServices.IGameServicesService.IsLoggedIn instead.", false)]
        public static bool GameCenterIsSignedIn()
        {

            if (instance != null && instance.GameCenter != null)
            {
                return instance.GameCenter.IsLoggedIn;
            }

            return false;
        }
#endif

        void OnDestroy()
        {
            wasDestroyed = true;
        }

        private async void ShowIdfaPopupInReview() 
        {
            try
            {
                if((Core.GetService<IAdvertisingIdService>()?.GetCachedConsentStatus() ?? ConsentStatus.Denied) != ConsentStatus.Denied)
                    _ = await Core.GetService<IConsentService>().AskForConsentAsync(ConsentType.AdvertisingIdentifier);
            }
            catch(Exception)
            {
                Debug.LogError("Error while showing popup");
            }
        }
        
        private void LoadConfiguration()
        {
            string configurationPath = "iOS/iOSConfiguration";
            #if UNITY_ANDROID
            configurationPath = "Android/AndroidConfiguration";
            #endif

            TextAsset confAsset=Resources.Load<TextAsset>(configurationPath);
            configuration = Plist.readPlist(confAsset.bytes) as Dictionary<string, object>;
        }

        private void InitAds()
        {
            
#if HCSDK_DIAGNOSTICS
            // starts measuring the display time between the start of the mediation and the display of the ad on the device
            // TimeAverage.StartCountingTime();
            Log.Info("Starts counting time.");
            Coredian.Diagnostics.Performance.StartExecution("counting_time");

#endif
            appsFlyer.ReceivedConversionData -= InitAds;

            if (consentService == null) return;
            if (initializationComplete) return;

            initializationComplete = true;
            if (InitializationStartEvent != null) InitializationStartEvent();
            
            bool manualInterstitialCaching = configuration.ContainsKey("manual_interstitial_caching") && configuration["manual_interstitial_caching"].ToString() == "TRUE";
            
            adsProvider = CreateProviderObject();
            if (consentService.GetConsentStatus(IronSourceProvider.UsercentricsTemplateId) == ConsentStatus.Granted)
            {
                adsProvider.SetConsent(true);
            }
            else
            {
                adsProvider.SetConsent(false);
            }

            SetAdEvents();

            if (BannerMechanism.Instance != null)
            {
                BannerMechanism.Instance.AdsProvider = Instance.adsProvider;
            }
            
            interstitialMechanism.AdsProvider = adsProvider;
            rewardedMechanism.AdsProvider = adsProvider;
            
            adsProvider.Initialize(!manualInterstitialCaching);
        }

        private IAdsProvider CreateProviderObject()
        {
            GameObject go = new GameObject(mediationProvider.Name, mediationProvider);
            go.transform.parent = transform;
            return go.GetComponent(mediationProvider) as IAdsProvider;
        }

        private void InitBannerMechanism()
        {
            var go = new GameObject("BannerMechanism", typeof(BannerMechanism));
            go.transform.parent = transform;
            BannerMechanism.Instance.BannerShown += () =>
                                                    {
                                                        BannerDisplayEvent?.Invoke();
                                                    };
        }

        private void InitAppsFlyer()
        {
            GameObject go=new GameObject("AppsFlyerProvider", typeof(AppsFlyerProvider));
            go.transform.parent = transform;
            appsFlyer = go.GetComponent(typeof(AppsFlyerProvider)) as AppsFlyerProvider;
            appsFlyer.ReceivedConversionData += OnReceivedConversionData;
            
            if (consentService.GetConsentStatus(AppsFlyerProvider.UsercentricsTemplateId) == ConsentStatus.Granted)
            {
                appsFlyer.SetConsent(true);
            }
            else
            {
                appsFlyer.SetConsent(false);
            }
            appsFlyer.Initialize(configuration["appId"].ToString(), configuration["AppsFlyer_devKey"].ToString());

            if (customUserId != null)
            {
                appsFlyer.SetCustomUserId(customUserId);
            }
        }
        
        
        private void InitWhitelist()
        {
            GameObject go=new GameObject("WhitelistMechanism", typeof(WhitelistMechanism));
            go.transform.parent = transform;
            whitelistMechanism = go.GetComponent(typeof(WhitelistMechanism)) as WhitelistMechanism;
            whitelistMechanism.Initialize();
        }

        private void InitDeviceRegion()
        {
            GameObject go=new GameObject("DeviceRegionMechanism", typeof(DeviceRegionMechanism));
            go.transform.parent = transform;
            deviceRegionMechanism = go.GetComponent(typeof(DeviceRegionMechanism)) as DeviceRegionMechanism;
        }

        private void InitGameCenter()
        {
#if GAME_SERVICES
            GameCenter.LogInSuccessful += LoginInGCSuccessfullEvent;
            GameCenter.LogInFailed += LoginInGCFailEvent;
#endif
        }
        
        private void InitTesterGUI()
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() == null) return;
            
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed += TesterGUIFetchedFailEvent;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += TesterGUIFetchedSuccessfulEvent;
        }

        private void TesterGUIFetchedFailEvent()
        {
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed -= TesterGUIFetchedFailEvent;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded -= TesterGUIFetchedSuccessfulEvent;
        }

        private void TesterGUIFetchedSuccessfulEvent(IDictionary<string, string> config)
        {
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed -= TesterGUIFetchedFailEvent;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded -= TesterGUIFetchedSuccessfulEvent;

            if (GetParameter("TESTER_GUI_ON").Equals("TRUE"))
            {
                //Debug.Log("DEGIE TESTER GUI LOADED");
                GameObject go=new GameObject("TesterGUI", typeof(TesterGUI));
                go.transform.parent = instance.transform;
            }
        }

        private void InitTesterGUIEditor()
        {
            GameObject go=new GameObject("TesterGUI", typeof(TesterGUI));
            go.transform.parent = instance.transform;
        }

        private void SetAdEvents()
        {
            interstitialMechanism.InterstitialCloseEvent += ()=>
            {
                if (InterstitialCloseEvent!=null) InterstitialCloseEvent();
            };
            interstitialMechanism.InterstitialDisplayEvent += (string placament)=> 
            {
                analyticsTracker.TrackDisplay(AdType.interstitial, placament);
                if (InterstitialDisplayEvent!=null) InterstitialDisplayEvent();
            };
            interstitialMechanism.InterstitialClickEvent += ()=>
            {
                if (InterstitialClickEvent!=null) InterstitialClickEvent();
            };

            interstitialMechanism.InterstitialFailEvent +=()=>
            {
                if (InterstitialFailEvent!=null) InterstitialFailEvent();
            };
            rewardedMechanism.VideoCachedSuccessfulEvent += () =>
            {
                analyticsTracker.TrackFill(AdType.rewarded);
            };
            rewardedMechanism.VideoCanceledEvent += ()=>
            {
                if (VideoCancelEvent!=null) VideoCancelEvent();
                analyticsTracker.TrackFill(AdType.rewarded);
            };
            rewardedMechanism.VideoClickEvent += ()=> 
            {
                if (VideoClickEvent!=null) VideoClickEvent();
            };
            rewardedMechanism.VideoSuccessEvent += ()=>
            {
                if (VideoSuccessEvent!=null) VideoSuccessEvent();
            };
            rewardedMechanism.VideoFailEvent += (string placement)=>
            {
                analyticsTracker.TrackAdFail(AdType.rewarded, placement);
                if (VideoFailEvent!=null) VideoFailEvent();
            };
            rewardedMechanism.VideoDisplayEvent += (string placement)=>
            {
                analyticsTracker.TrackDisplay(AdType.rewarded, placement);
                if (VideoDisplayEvent!=null) VideoDisplayEvent();
            };

            adsProvider.BannerShowEvent += ()=>
            {
                analyticsTracker.TrackDisplay(AdType.banner);
                BannerDisplayEvent?.Invoke();
            };
        }

        private void OnBackFromBackgroundEvent()
        {
            if (BackFromBackgroundEvent != null)
                BackFromBackgroundEvent();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (isPaused == true && pauseStatus == false)
            {
                OnBackFromBackgroundEvent();
            }

            isPaused = pauseStatus;
        }
        
        private void CheckAndInitProviders()
        {
            Log.InfoFormat("HCSDK :: CheckAndInitProviders :: GetConsentStatus=>{0}, IfIdfaReady=>{1} ", consentService.GetConsentStatus(AppsFlyerProvider.UsercentricsTemplateId),
                           CheckIfIdfaReady());
            if (consentService.GetConsentStatus(AppsFlyerProvider.UsercentricsTemplateId) != ConsentStatus.Unknown && appsFlyer == null && CheckIfIdfaReady())
            {
                Log.InfoFormat("HCSDK :: CheckAndInitProviders :: InitAppsFlyer");
                InitAppsFlyer();
            }

            if (GameObject.Find("BannerMechanism") == null)
            {
                InitBannerMechanism();
            }

            if (!adsInitialized && appsFlyer!=null && CheckIfIdfaReady())
            {
                if (Application.isEditor)
                {
                    initializationComplete = true;
                    adsInitialized = true;
                    if (InitializationStartEvent != null) InitializationStartEvent();
                }
                else
                {
                    if (Core.GetService<IFirebaseRemoteConfigService>() != null && instance.configuration.ContainsKey("initialize_after_remote_config") &&
                        instance.configuration["initialize_after_remote_config"].ToString() == "TRUE")
                    {
                        if (Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded) instance.StartCoroutine(instance.WaitBeforeInitAdsProvider());
                        else
                        {
                            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += instance.WaitForFetchSuccessfulConfig;
                            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingFailed += instance.WaitForFetchFailConfig;
                        }
                    }
                    else
                    {
                        instance.StartCoroutine(instance.WaitBeforeInitAdsProvider());
                    }
                }
            }

            analyticsTracker.TrackNoTablet();
        }

        private void CheckIfProvidersShouldWaitForAtt()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (HCSDK.GetLocalParameter("wait_for_att_popup").ToUpperInvariant() == "TRUE")
            {
                shouldWaitForAttPopup = true;
            }
            else if (HCSDK.GetLocalParameter("wait_for_att_popup").ToUpperInvariant() == "" ||
                     HCSDK.GetLocalParameter("wait_for_att_popup").ToUpperInvariant() == "FROMOS14.5")
            {
                try
                {
                    System.Version iosVersion;

                    Debug.Log(Device.systemVersion);
                    string[] elems = Device.systemVersion.Split('.');
                    if (elems.Length > 3)
                    {
                        iosVersion = new System.Version(elems[0]+"."+elems[1]+"."+elems[2]);
                    }
                    else iosVersion = new System.Version(Device.systemVersion);

                    System.Version minAttVersion = new System.Version("14.5");

                    if (iosVersion.CompareTo(minAttVersion) >= 0)
                    {
                        shouldWaitForAttPopup = true;                    
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("HCSDK :: Problem with parsing version string :: "+e.Message);
                }
            }
#endif
        }
        
        private bool CheckIfIdfaReady()
        {
            if (shouldWaitForAttPopup)
            {
                if ((Core.GetService<IAdvertisingIdService>()?.GetCachedConsentStatus() == ConsentStatus.Granted) ||
                    (Core.GetService<IAdvertisingIdService>()?.GetCachedConsentStatus() == ConsentStatus.Denied))
                {
                    return true;
                }
                else
                {
                    return false;
                }   
            }
            
            return true;
        }

        private void OnReceivedConversionData()
        {
            if (appsFlyer.ConversionData != null) ReceivedConversionData?.Invoke(appsFlyer.ConversionData);
        }

        private void CheckIfConsentDefined()
        {
            if (Core.GetService<IConsentService>()?.GetConsentStatus(IronSourceProvider.UsercentricsTemplateId) ==
                ConsentStatus.Unknown ||
                Core.GetService<IAdvertisingIdService>()?.GetCachedConsentStatus() ==
                ConsentStatus.Unknown)
            {
                Log.Warning(UndefinedConsentMessage);
            }
        }
    }
}
