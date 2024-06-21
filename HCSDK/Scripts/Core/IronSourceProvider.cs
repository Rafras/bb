using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
// ReSharper disable StringLiteralTypo
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BoomBit.HyperCasual
{
	using Coredian.Firebase;

	using AppsFlyerSDK;

	using Coredian;
	using Coredian.AdvertisingId;
	using Coredian.Privacy;
	using Coredian.Utility;

	using MolocoSdk;

	// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
	public class IronSourceProvider : MonoBehaviour, IAdsProvider
	{
		public const string UsercentricsTemplateId = "9dchbL797";
		private const string CustomSegmentNamePrefix = "ironsource_custom_segment_name_";
		private const string CustomSegmentValuePrefix = "ironsource_custom_segment_value_";
		private const float ResetAdsCacheTime = 5f;
		
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void _SetLDUForFAN();
		[DllImport("__Internal")]
		private static extern void _SetFANAdvertiserTrackingEnabled(bool isTrackingEnabled);
#else
		// ReSharper disable once UnusedMember.Local
		private static void _SetLDUForFAN(){}
		
		// ReSharper disable once UnusedParameter.Local
		private static void _SetFANAdvertiserTrackingEnabled(bool isTrackingEnabled){}
#endif
		
		private bool wasRewarded;
		private bool isVideoShowed;

		private bool isBannerVisible;
		private bool isBannerCached;
		private bool bannerShowed;
		private bool isBannerCaching;
		private bool interstitialCaching;

		private bool consent;

		private bool isAdShow;
		private bool whileCachingInterstitial;

		private float lastInterstitialCacheTime = 0f;
		private float lastBannerCacheTime = 0f;

		private string lastInterstitialPlacement = "";
		private string lastRewardedPlacement = "";
		
		private AdImpressionTracker impressionTracker;
		private const string AdMediationName = "IronSource";
		
		private float retryInterstitialAttempt = 0;

		private bool ironSourceInitialized;

		private void InitEventHandlers()
		{
			IronSourceEvents.onImpressionDataReadyEvent += ImpressionSuccessEvent;

			IronSourceEvents.onSdkInitializationCompletedEvent += CompleteIronSourceInitialization;

			IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
			IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
			IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
			IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += VideoAvailabilityChangedEvent;
			IronSourceEvents.onRewardedVideoAdStartedEvent += VideoStartEvent;
			IronSourceEvents.onRewardedVideoAdEndedEvent += VideoEndEvent;
			IronSourceEvents.onRewardedVideoAdShowFailedEvent += OnVideoFailEvent;

			IronSourceEvents.onInterstitialAdReadyEvent += OnInterstitialAdReady;
			IronSourceEvents.onInterstitialAdClosedEvent += OnInterstitialAdClosed;
			IronSourceEvents.onInterstitialAdOpenedEvent += OnInterstitialAdOpened;
			IronSourceEvents.onInterstitialAdClickedEvent += OnInterstitialAdClicked;
			IronSourceEvents.onInterstitialAdShowFailedEvent += OnInterstitialAdShowFailed;
			IronSourceEvents.onInterstitialAdLoadFailedEvent += OnInterstitialAdLoadFailed;
			IronSourceEvents.onInterstitialAdShowSucceededEvent += OnInterstitialAdShowSucceeded;

			IronSourceEvents.onBannerAdLoadedEvent += BannerAdLoadedEvent;
			IronSourceEvents.onBannerAdLoadFailedEvent += BannerAdLoadFailedEvent;
			IronSourceEvents.onBannerAdClickedEvent += BannerAdClickedEvent;
			IronSourceEvents.onBannerAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
			IronSourceEvents.onBannerAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
			IronSourceEvents.onBannerAdLeftApplicationEvent += BannerAdLeftApplicationEvent;
		}

		//Invoked once the banner has loaded
		void BannerAdLoadedEvent()
		{
			if (BannerShowEvent != null) BannerShowEvent();
			
			isBannerVisible = true;
			isBannerCached = true;
			isBannerCaching = false;
            if (!bannerShowed)
            {
                isBannerVisible = false;
                HideBannerAd();
            }
            else
            {
                if (BannerShowEvent != null) BannerShowEvent();
            }
		}
		//Invoked when the banner loading process has failed.
		//@param description - string - contains information about the failure.
		void BannerAdLoadFailedEvent (IronSourceError error) {
			IronSource.Agent.destroyBanner();
			isBannerVisible = false;
			isBannerCached = false;
			isBannerCaching = false;
			OnBannerNotShown();
			if (bannerShowed) StartCoroutine(WaitBeforeBannerCache());
		}

		private IEnumerator WaitBeforeBannerCache()
		{
			yield return new WaitForEndOfFrame();
			LoadBanner();
		}
		
		// Invoked when end user clicks on the banner ad
		void BannerAdClickedEvent () {
			ClickOnBanner();
		}
		//Notifies the presentation of a full screen content following user click
		void BannerAdScreenPresentedEvent () {
		}
		//Notifies the presented screen has been dismissed
		void BannerAdScreenDismissedEvent() {
		}
		//Invoked when the user leaves the app
		void BannerAdLeftApplicationEvent() {
		}
		
		private IEnumerator WaitBeforeHideBanner()
		{
			yield return new WaitForEndOfFrame();
			OnBannerHide();
		}

		private void OnBannerHide(){
			Log.Info("IronSourceProvider OnBannerHide");
			if (BannerHideEvent!=null) BannerHideEvent();
		}

		private void OnBannerNotShown(){
			if (BannerIsNotShownEvent!=null) BannerIsNotShownEvent();
		}

		private void ClickOnBanner()
		{
			if (ClickOnBannerEvent!=null) ClickOnBannerEvent("IronSource");
		}

		public event Action BannerShowEvent;
		
		public event Action BannerHideEvent;

		public event Action BannerIsNotShownEvent;

		public event Action<string> ClickOnBannerEvent;

        public void InvokeBannerShowEvent()
        {
            if (BannerShowEvent != null) BannerShowEvent();
        }

		private void OnInterstitialAdReady()
		{
			retryInterstitialAttempt = 0;
			whileCachingInterstitial = false;
			OnInterstitialCachedSuccessfulEvent();
		}

		private void OnInterstitialAdLoadFailed(IronSourceError error)
		{
			whileCachingInterstitial = false;
			OnInterstitialCachedFailEvent();
			
			retryInterstitialAttempt++;
			float retryDelay = Mathf.Pow(2, retryInterstitialAttempt / 2f);
			
			StartCoroutine(WaitForLoadInterstitial(retryDelay));
		}

		private void OnInterstitialAdOpened()
		{
			OnInterstitialDisplayEvent();
		}

		private void OnInterstitialAdClosed()
		{
			Log.Info("IronSourceProvider :: OnInterstitialAdClosed");
			if (isAdShow)
			{
				isAdShow = false;
				OnEndOfAd();
				LoadInterstitial();
			}
		}

		private void OnInterstitialAdShowSucceeded()
		{
			Log.Info("IronSourceProvider :: OnInterstitialAdShowSucceeded");
		}

		private void OnInterstitialAdShowFailed(IronSourceError error)
		{
			if (isAdShow)
			{
				isAdShow = false;

				OnAdFail();
				LoadInterstitial();
			}
		}

		private void OnInterstitialAdClicked()
		{
			Log.Info("IronSourceProvider :: OnInterstitialAdClicked");
			OnInterstitialClickedEvent();
		}
		
		public event Action InterstitialCachedSuccessfulEvent;
		public event Action InterstitialCachedFailEvent;

		public event Action InterstitialClickedEvent;
		public event Action<string> InterstitialDisplayEvent;

		public event Action GetInterstitialRequestEvent;

		private void OnGetInterstitialRequestEvent()
		{
			if (GetInterstitialRequestEvent != null)
				GetInterstitialRequestEvent();
		}

		private void OnInterstitialCachedSuccessfulEvent()
		{
			if (InterstitialCachedSuccessfulEvent != null)
				InterstitialCachedSuccessfulEvent();
		}

		private void OnInterstitialCachedFailEvent()
		{
			if (InterstitialCachedFailEvent != null)
				InterstitialCachedFailEvent();
		}

		private void OnInterstitialClickedEvent()
		{
			if (InterstitialClickedEvent != null)
				InterstitialClickedEvent();
		}

		private void OnInterstitialDisplayEvent()
		{
			if (InterstitialDisplayEvent != null)
				InterstitialDisplayEvent(lastInterstitialPlacement);
		}

		
		public event Action AdFailEvent;

		public event Action EndOfAdEvent;

//		public event Action EndOfMoreGamesEvent;

		private void OnEndOfAd()
		{
			if (EndOfAdEvent != null) EndOfAdEvent();
		}

		private void OnAdFail()
		{
			if (AdFailEvent != null) AdFailEvent();
		}

		void RewardedVideoAdOpenedEvent()
		{
			Log.Info("IronSourceProvider :: RewardedVideoAdOpenedEvent");

			OnVideoDisplayEvent();
		}

		void RewardedVideoAdClosedEvent()
		{
			Log.Info("IronSourceProvider :: RewardedVideoAdClosedEvent");
			isVideoShowed = false;

	#if UNITY_IOS
			StartCoroutine(WaitBeforeRewardVideoClose());
	#else
			RewardedVideoEnded();
	#endif
		}

		void RewardedVideoEnded()
		{
			StartCoroutine(WaitBeforeVideoEnd());
		}

		void VideoAvailabilityChangedEvent(bool available)
		{
			Log.Info("IronSourceProvider :: VideoAvailabilityChangedEvent :: " + available);
			if (available) 
			{
				OnVideoCachedSuccessfulEvent();
			}
			else 
			{
				OnVideoCachedFailEvent();
			}
		}

		void VideoStartEvent()
		{
			Log.Info("IronSourceProvider :: VideoStartEvent");
		}

		void VideoEndEvent()
		{
			Log.Info("IronSourceProvider :: VideoEndEvent");
		}

		void RewardedVideoAdRewardedEvent(IronSourcePlacement placement)
		{
			Log.Info("IronSourceProvider :: RewardedVideoAdRewardedEvent :: " + placement.getRewardName());
			wasRewarded = true;
		}

		void OnVideoFailEvent(IronSourceError error)
		{
			Log.Info("IronSourceProvider :: VideoFailEvent");
			isVideoShowed = false;
			StartCoroutine(WaitBeforeFailVideo());
			PlayerPrefs.SetInt("hcsdk_showed_video", PlayerPrefs.GetInt("hcsdk_showed_video", 0)-1);
		}
		
		
		public void Initialize(bool autoInterstitialCaching)
		{
			Log.Info("START Init IronSource");

			interstitialCaching = autoInterstitialCaching;
			string appKey = HCSDK.GetLocalParameter("IronSource_id");
			IronSourceAdQuality.Start(appKey);
			
			GameObject go=new GameObject("AdImpressionTracker", typeof(AdImpressionTracker));
			impressionTracker = go.GetComponent<AdImpressionTracker>();
			impressionTracker.Initialize();
			go.transform.parent = transform;
			
			SetSegment();
			
			IronSourceConfig.Instance.setClientSideCallbacks(true);

			InitEventHandlers();
			isBannerVisible = false;
			isBannerCaching = false;
			bannerShowed = false;
			
			ChangeConsent(consent);
			SetFanAdvertisingTracking();
			IronSource.Agent.setUserId(AppsFlyer.getAppsFlyerId());

			if (HCSDK.GetParameter("IS_TEST_SUITE_ON").ToUpper() == "TRUE")
			{
				IronSource.Agent.setMetaData("is_test_suite", "enable");
			}

			IronSource.Agent.shouldTrackNetworkState(HCSDK.GetParameter("IS_NETWORK_CHANGE_STATUS_OFF") != "TRUE");

			IronSource.Agent.init(appKey);

			Log.Info("END Init IronSource");
		}

		private void CompleteIronSourceInitialization()
		{
			Log.Info("IronSourceProvider :: CompleteIronSourceInitialization");
			ironSourceInitialized = true;
			if (interstitialCaching)
			{
				StartCoroutine(WaitBeforeFirstCache());
			}
		}
		
		private void SetFanAdvertisingTracking()
		{
			var advertisingIdService = Core.GetService<IAdvertisingIdService>();
			if (advertisingIdService != null)
			{
				_SetFANAdvertiserTrackingEnabled(advertisingIdService.GetCachedConsentStatus() == ConsentStatus.Granted);
			}
		}

		private void ImpressionSuccessEvent(IronSourceImpressionData impressionData)
		{
			if (impressionData.revenue == null) return;
			
			if (HCSDK.GetParameter("HCSDK_DISABLE_SENDING_IMPRESSION_DATA_IS") != "TRUE")
			{
				if (!ThreadUtility.IsMainThread) ThreadUtility.ExecuteOnMainThread(()=>impressionTracker.TrackImpression(impressionData.impressionDict, AdMediationName, impressionData.revenue));
				else impressionTracker.TrackImpression(impressionData.impressionDict, AdMediationName, impressionData.revenue);
			}

			
			Dictionary<string, object> parameters = new Dictionary<string, object>
			{
					{ "ad_platform", "ironSource" }, { "ad_source", impressionData.adNetwork },
					{ "ad_format", impressionData.adUnit },
					{ "ad_unit_name", impressionData.placement },
					{ "currency", "USD" },
					{ "value", impressionData.revenue ?? 0f }
				};

			if(Core.GetService<IFirebaseService>() != null)
			{
				Core.GetService<IFirebaseService>().LogEvent("ad_impression", parameters);
			}
			
			Dictionary<string, string> afRevenueParameters = new Dictionary<string, string>
			{
					{ "ad_format", impressionData.adUnit }, { "ad_unit_name", impressionData.instanceName }
				};
			AppsFlyerAdRevenue.logAdRevenue(impressionData.adNetwork, AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeIronSource, impressionData.revenue ?? 0f, "USD", afRevenueParameters);
		}

		private bool IsInterstitialStillCaching()
		{
			if (whileCachingInterstitial && (Time.realtimeSinceStartup - lastInterstitialCacheTime) >= ResetAdsCacheTime)
			{
				whileCachingInterstitial = false;
			}
			return whileCachingInterstitial;
		}
		
		private bool IsBannerStillCaching()
		{
			if (isBannerCaching && (Time.realtimeSinceStartup - lastBannerCacheTime) >= ResetAdsCacheTime)
			{
				isBannerCaching = false;
			}
			return isBannerCaching;
		}

		private void SetSegment()
		{
			IronSourceSegment segment = new IronSourceSegment();

            Dictionary<string,string> customParams = new Dictionary<string,string> ();

            SetCustomSegment(customParams, "1");
			SetCustomSegment(customParams, "2");
			SetCustomSegment(customParams, "3");
			
			customParams.Add ("RAMMemory", SystemInfo.systemMemorySize.ToString());
			
			if (PlayerPrefs.HasKey("HCSDK_ad_source"))
			{
				string adSource = PlayerPrefs.GetString("HCSDK_ad_source").Replace("-", "");
				adSource=adSource.Replace("_", "");
				adSource=adSource.Replace(" ", "");
				
				customParams.Add ("AdSource", adSource);
			}

			segment.customs = customParams;

			if (PlayerPrefs.GetInt("HC_HAVE_PURCHASE", 0) == 1 || PlayerPrefs.GetInt("HC_HAVE_SUBSCRIPTION", 0) == 1)
			{
				segment.isPaying = 1;
			}

			IronSourceEvents.onSegmentReceivedEvent += segmentName =>
													   {
														   Log.InfoFormat(
															   "IronSourceProvider :: onSegmentReceivedEvent :: {0}", 
															   segmentName);
													   };
			
			IronSource.Agent.setSegment(segment);
		}

		private void SetCustomSegment(Dictionary<string,string> customParams, string customSegmentName)
		{
			if (HCSDK.GetParameter(CustomSegmentNamePrefix+customSegmentName) != ""
				&& HCSDK.GetParameter(CustomSegmentValuePrefix+customSegmentName) != "")
			{
				customParams.Add(HCSDK.GetParameter(CustomSegmentNamePrefix+customSegmentName),
								 HCSDK.GetParameter(CustomSegmentValuePrefix+customSegmentName));
			}
		}

		private IEnumerator WaitBeforeFirstCache()
		{
			yield return new WaitForEndOfFrame();
			LoadInterstitial();
		}
		
		private IEnumerator WaitForLoadInterstitial(float time)
		{
			yield return new WaitForSecondsRealtime(time);

			LoadInterstitial();
		}
		
		public bool IsBannerShowed => isBannerVisible && isBannerCached;

		public void ShowBanner()
		{
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			if(IsBannerStillCaching())
			{
				return;
			}
			bannerShowed = true;

			if (isBannerCached)
			{
				isBannerVisible = true;

                if (BannerShowEvent != null) BannerShowEvent();

				IronSource.Agent.displayBanner();
			}
			else
			{
				LoadBanner();
			}
		}

		private void LoadBanner()
		{
			if (!ironSourceInitialized)
			{
				StartCoroutine(DelayLoadBanner(1f));
				return;
			}
			isBannerCaching = true;
			isBannerCached = false;
			lastBannerCacheTime = Time.realtimeSinceStartup;
#if UNITY_IOS
			IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
#else
			IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, IronSourceBannerPosition.BOTTOM);
#endif
		}

		private IEnumerator DelayLoadBanner(float delay)
		{
			yield return new WaitForSecondsRealtime(delay);
			if (!isBannerCached && !isBannerCaching) LoadBanner();
		}
		
		public void HideBanner()
		{
			if (!isBannerCaching && isBannerCached)
			{
				HideBannerAd();
			}
			if (isBannerCaching)
				bannerShowed = false;
		}

		private void HideBannerAd()
		{
			if (!ironSourceInitialized) return;
			isBannerVisible = false;

			IronSource.Agent.hideBanner();
			StartCoroutine(WaitBeforeHideBanner());
		}

		public event Action VideoSuccessEvent;

		public event Action<string> VideoFailEvent;

		public event Action VideoCanceledEvent;

		private void OnVideoCanceled()
		{
			if (VideoCanceledEvent != null) VideoCanceledEvent();
		}

		private void OnVideoSuccess()
		{
			if (VideoSuccessEvent != null) VideoSuccessEvent();
		}

		private void OnVideoFail(string placement)
		{
			if (VideoFailEvent != null) VideoFailEvent(placement);
		}
		
		public event Action VideoCachedSuccessfulEvent;
		public event Action VideoCachedFailEvent;
		public event Action VideoClickEvent;
		public event Action<string> VideoDisplayEvent;
		public event Action GetVideoRequestEvent;

		protected virtual void OnGetVideoRequestEvent()
		{
			if (GetVideoRequestEvent != null)
				GetVideoRequestEvent();
		}

		protected virtual void OnVideoCachedSuccessfulEvent()
		{
			if (VideoCachedSuccessfulEvent != null)
				VideoCachedSuccessfulEvent();
		}


		protected virtual void OnVideoCachedFailEvent()
		{
			if (VideoCachedFailEvent != null)
				VideoCachedFailEvent();
		}

		protected virtual void OnVideoClickEvent()
		{
			if (VideoClickEvent != null)
				VideoClickEvent();
		}

		protected virtual void OnVideoDisplayEvent()
		{
			if (VideoDisplayEvent != null)
				VideoDisplayEvent(lastRewardedPlacement);
		}

		public bool IsVideoAvailable(string placement)
		{
			if (!ironSourceInitialized) return false;
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			return IronSource.Agent.isRewardedVideoAvailable() && !IronSource.Agent.isRewardedVideoPlacementCapped( placement );
		}

		public void ShowVideo(string placement)
		{
			if (!ironSourceInitialized) return;
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			Log.Info("IronSourceProvider :: ShowVideoForCoin :: "+placement);

			lastRewardedPlacement = placement;
			
			if ( IsVideoAvailable( placement ) )
			{
				isVideoShowed = true;
				OnGetVideoRequestEvent();
				wasRewarded = false;
				
				PlayerPrefs.SetInt("hcsdk_showed_video", PlayerPrefs.GetInt("hcsdk_showed_video", 0)+1);
				
				IronSource.Agent.showRewardedVideo ( placement );
				Log.Info("IronSourceProvider :: showRewardedVideo OK");
			}
			else
			{
				Log.Info("IronSourceProvider :: showRewardedVideo FAIL");
				StartCoroutine(WaitBeforeFailVideo());
			}
		}

		public bool IsVideoShowed()
		{
			return isVideoShowed;
		}

		public bool IsAdShowed
		{
			get
			{
				return isAdShow;
			}
		}


		public void  ShowAdWithLocation (string locationName)
		{
			if (!ironSourceInitialized) return;
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			if (isAdShow == false)
			{
				if ( !IronSource.Agent.isInterstitialPlacementCapped(locationName) && IronSource.Agent.isInterstitialReady() )
				{
					lastInterstitialPlacement = locationName;
					isAdShow=true;
					IronSource.Agent.showInterstitial( locationName );
				}
				else 
				{
					LoadInterstitial();
				}
			}
		}


		public bool IsInterstitialReady(string placement)
		{
			if (!ironSourceInitialized) return false;
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			if (!IronSource.Agent.isInterstitialPlacementCapped(placement) && IronSource.Agent.isInterstitialReady())
			{
				return true;
			}
			else
			{
				if (!IronSource.Agent.isInterstitialReady())
				{
					LoadInterstitial();
				}

				return false;
			}
		}

		public void StartCachingInterstitial()
		{
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			if (!IronSource.Agent.isInterstitialReady())
			{
				LoadInterstitial();
			}
		}
		
		private void LoadInterstitial()
		{
			if (!ironSourceInitialized) return;
			if (Core.GetService<IAdvertisingIdService>().GetCachedConsentStatus() !=
				ConsentStatus.Granted)
			{
				Log.Warning("Advertising identifier consent not granted!");
			}

			if (!IsInterstitialStillCaching())
			{
				Log.Info("IronSourceProvider :: LoadInterstitial");
				OnGetInterstitialRequestEvent();
				whileCachingInterstitial = true;
				lastInterstitialCacheTime = Time.realtimeSinceStartup;
				IronSource.Agent.loadInterstitial();
			}
		}

		void OnApplicationPause(bool isPaused)
		{
			IronSource.Agent.onApplicationPause(isPaused);
		}

		private IEnumerator WaitBeforeFailVideo()
		{
			yield return new WaitForEndOfFrame();
			
			OnVideoFail(lastRewardedPlacement);
		}

		private IEnumerator WaitBeforeVideoEnd()
		{
			yield return new WaitForSecondsRealtime(1f);
			if (wasRewarded)
				OnVideoSuccess();
			else
			{
				PlayerPrefs.SetInt("hcsdk_canceled_video", PlayerPrefs.GetInt("hcsdk_canceled_video", 0)+1);
				OnVideoCanceled();
			}
		}

		// ReSharper disable once UnusedMember.Local
		private IEnumerator WaitBeforeRewardVideoClose()
		{
			yield return new WaitForEndOfFrame();
			
			Log.Info("IronSourceProvider :: WaitBeforeRewardVideoClose");

			RewardedVideoEnded();
		}

		public void SetConsent(bool hasConsent)
		{
			consent = hasConsent;
		}

		private void ChangeConsent(bool changeConsent)
		{
			SdkConsentHandler.SetConsent();

			consent = changeConsent;

			IronSourceAdQuality.SetUserConsent(changeConsent);
			IronSource.Agent.setConsent(consent);
			IronSource.Agent.setMetaData("do_not_sell", consent ? "false" : "true");
		}
	}
}

