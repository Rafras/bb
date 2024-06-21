// ReSharper disable StringLiteralTypo
namespace BoomBit.HyperCasual
{
	using UnityEngine;
	using Coredian.Firebase;
	using System.Collections.Generic;
	using System;
	using System.Collections;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using AppsFlyerSDK;

	using Coredian;
	using Coredian.Native;
	using AppsFlyerConnector;
#if CORE_IN_APP_PURCHASES
	using Coredian.InAppPurchases;
	using Coredian.InAppPurchases.Android;

	using ProductType = Coredian.InAppPurchases.ProductType;
#endif

	public class AppsFlyerProvider : MonoBehaviour, IAppsFlyerConversionData, IAppsFlyerUserInvite, IAppsFlyerPurchaseValidation
	{
		public const string UsercentricsTemplateId = "Gx9iMF__f";
		
		private List<object> alreadyBoughtPurchases = null;
		
		private bool hasConsent = false;

		private bool isOneTimeOptOut = false;

		private bool coreIapEventsBound;

		private bool useCustomUserId;
		
		private string customUserId;

		private bool sdkStarted;
		
		public event Action ReceivedConversionData;

		public bool HasConversionData { get; private set; } = false;

		public Dictionary<string, object> ConversionData { get; private set; }

		public void Initialize (string appId, string devKey)
		{
			Debug.Log("START APPS FLYER INITIALIZE ");

			if (HCSDK.GetLocalParameter("USE_CUSTOM_USER_ID").ToUpper() == "TRUE") useCustomUserId = true;

#if UNITY_IOS && !UNITY_EDITOR
			if (HCSDK.GetLocalParameter("HC_USE_AF_SKADNETWORK").ToUpperInvariant() == "FALSE")
			{
				AppsFlyer.disableSKAdNetwork(true);
			}

			if (HCSDK.GetLocalParameter("HC_WAIT_FOR_ATT").ToUpperInvariant()=="TRUE")
			{
				AppsFlyer.waitForATTUserAuthorizationWithTimeoutInterval(60);
			}
#endif
			ChangeConsent(hasConsent);
			AppsFlyer.initSDK(devKey, appId, this);

			InitAppsFlyerPurchaseConnector();
			if (!useCustomUserId && ConsentAllowToStart())
			{
				StartAppsFlyer();
			}

			if (!hasConsent)
			{
				if (Core.GetService<IFirebaseRemoteConfigService>() != null)
					Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += GetConfiguration;
			}

			Debug.Log("END APPS FLYER INITIALIZE ");
		}

		public void SetCustomUserId(string customerId)
		{
			Log.InfoFormat("AppsFlyerProvider :: SetCustomUserId=>{0}", customerId);
			customUserId = customerId;
			
			AppsFlyer.setCustomerUserId(customerId);
			if (useCustomUserId && !sdkStarted && ConsentAllowToStart())
			{
				StartAppsFlyer();
			}
		}

		private void GetConfiguration(IDictionary<string, string> obj)
		{
			if (ConsentAllowToStart())
			{
				if (!useCustomUserId) StartAppsFlyer();
				else if (!string.IsNullOrEmpty(customUserId)) SetCustomUserId(customUserId);
			}
		}

		private bool ConsentAllowToStart()
		{
			if (hasConsent) return true;

			if (Core.GetService<IFirebaseRemoteConfigService>() != null)
			{
				if (Core.GetService<IFirebaseRemoteConfigService>()
						.RemoteConfig.TryGetValue("AF_HC_ONE_TIME_OPT_OUT", out var oneTimeOptOut))
				{
					isOneTimeOptOut = oneTimeOptOut.ToUpper() == "TRUE";
				}
			}

			return isOneTimeOptOut;
		}

		private void StartAppsFlyer()
		{
			if (!useCustomUserId)
			{
				if (HCSDK.GetLocalParameter("USE_CUSTOM_USER_ID").ToUpper() != "TRUE")
				{
					if (Core.GetService<IFirebaseService>() != null)
					{
						if (!string.IsNullOrEmpty(Core.GetService<IFirebaseService>().AppInstanceId))
						{
							AppsFlyer.setCustomerUserId(Core.GetService<IFirebaseService>().AppInstanceId);
						}
					}
				}
			}

			Log.InfoFormat("AppsFlyerProvider :: StartAppsFlyer");
			AppsFlyer.startSDK();
			AppsFlyerAdRevenue.setIsDebug(HCSDK.GetParameter("HCSDK_Test_AdRevenue").ToUpperInvariant() == "TRUE");
			AppsFlyerAdRevenue.start();

			sdkStarted = true;
			
			StartCoroutine(GetConversionTimeout());

			SetupUninstall();
			if (Core.IsInitialized) SendAdditionalData();
			
			Core.InitializationSucceeded += OnCoreInitializationSucceeded;
			Core.WillDeinitialize += OnCoreWillDeinitialize;
			BindCoreIapEvents();
		}
		
		private void InitAppsFlyerPurchaseConnector()
		{
			
#if UNITY_ANDROID
			try
			{
				using var arrayClass = new AndroidJavaClass("com.android.billingclient.api.BillingClientStateListener");
			}
			catch (Exception e)
			{
				Log.WarningFormat("AppsFlyerPurchaseConnector :: BillingClientStateListener Not Exist, tracking In App Purchases will not be possible.");
				return;
			}
#endif
			AppsFlyerPurchaseConnector.init(this, AppsFlyerConnector.Store.GOOGLE);
			AppsFlyerPurchaseConnector.setIsSandbox(
				HCSDK.GetParameter("HCSDK_Test_Purchase").ToUpperInvariant() == "TRUE");
			AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions, AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases);
			AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
			AppsFlyerPurchaseConnector.build();
			AppsFlyerPurchaseConnector.startObservingTransactions();
		}
		
		public void SetConsent(bool hasConsent)
		{
			this.hasConsent = hasConsent;
		}

		public void ChangeConsent(bool changeConsent)
		{
			hasConsent = changeConsent;
		}

		public void AttributeAndOpenStore(string appId, string campaign, Dictionary<string, string> parameters)
		{
			AppsFlyer.attributeAndOpenStore(appId, campaign, parameters, this);
		}

		private void SetupUninstall()
		{
#if !UNITY_EDITOR
			if (Core.GetService<IFirebaseMessagingService>() != null)
			{
				if (Core.GetService<IFirebaseMessagingService>().PushToken == null)
				{
					Core.GetService<IFirebaseMessagingService>().TokenReceived += SetUninstallToken;
				}
				else
				{
					SetUninstallToken(Core.GetService<IFirebaseMessagingService>().PushToken);
				}
			}
#endif
		}
		
		private void SetUninstallToken(string token)
		{
#if UNITY_ANDROID && !UNITY_EDITOR
			AppsFlyer.updateServerUninstallToken(token);
#endif
		}
		
		public void onConversionDataSuccess(string conversionData)
		{
			try
			{
				Dictionary<string, object> conversionDict = AppsFlyer.CallbackStringToDictionary(conversionData);
				ConversionData = conversionDict;
				if (conversionDict != null)
				{
					if (conversionDict.ContainsKey("af_status") && (conversionDict["af_status"] != null))
					{
						if ((string)conversionDict["af_status"] == "Non-organic" &&
						    conversionDict.ContainsKey("media_source") &&
						    (conversionDict["media_source"] != null))
						{
							if (Core.GetService<IFirebaseService>() != null)
								Core.GetService<IFirebaseService>()
									.SetUserPropertyString("ad_network", conversionDict["media_source"].ToString());
							PlayerPrefs.SetString("HCSDK_ad_source", conversionDict["media_source"].ToString());
						}
						else
						{
							if (Core.GetService<IFirebaseService>() != null)
								Core.GetService<IFirebaseService>().SetUserPropertyString("ad_network", "organic");
							PlayerPrefs.SetString("HCSDK_ad_source", "organic");
						}
					}

					if (conversionDict.ContainsKey("campaign") && (conversionDict["campaign"] != null))
					{
						if (Core.GetService<IFirebaseService>() != null)
							Core.GetService<IFirebaseService>()
								.SetUserPropertyString("campaign", conversionDict["campaign"].ToString());
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("AppsFlyerProvider :: onConversionDataSuccess :: "+ex.Message);
			}
			
			HasConversionData = true;
			OnReceivedConversionData();

			AnonymizeUserWithoutConsent();
		}

		public void onConversionDataFail(string error)
		{
			if(Core.GetService<IFirebaseService>() != null)
				Core.GetService<IFirebaseService>().SetUserPropertyString("ad_network", "unknown");
			HasConversionData = true;
			OnReceivedConversionData();

			AnonymizeUserWithoutConsent();
		}


		private void AnonymizeUserWithoutConsent()
		{
			Log.InfoFormat("AppsFlyer AnonymizeUserWithoutConsent");
			if (!hasConsent)
			{
				Log.WarningFormat("AppsFlyer Anonymize user, you are missing AppsFlyer Consent");
				AppsFlyer.anonymizeUser(true);
			}
		}
		
		public void onAppOpenAttribution(string attributionData)
		{
			Log.InfoFormat("AppsFlyerProvider :: onAppOpenAttribution :: {0}", attributionData);
		}

		public void onAppOpenAttributionFailure(string error)
		{ 
			Log.InfoFormat("AppsFlyerProvider :: onAppOpenAttributionFailure :: {0}", error);
		}

		public void onInviteLinkGenerated(string link)
		{
			Log.InfoFormat("AppsFlyerProvider :: onInviteLinkGenerated :: {0}", link);
		}
		
		public void onInviteLinkGeneratedFailure(string error)
		{
			Log.InfoFormat("AppsFlyerProvider :: onInviteLinkGeneratedFailure :: {0}", error);
		}

		public void onOpenStoreLinkGenerated(string link)
		{
			Log.InfoFormat("AppsFlyerProvider :: onOpenStoreLinkGenerated :: {0}", link);
#if UNITY_IOS
			StoreNativeView.ShowAppInStore(link);
#endif
		}

		protected virtual void OnReceivedConversionData()
		{
			ReceivedConversionData?.Invoke();
		}


		private IEnumerator GetConversionTimeout()
		{
			yield return new WaitForSecondsRealtime(10f);
			if (!HasConversionData) onConversionDataFail("timeout");
		}


		private void OnCoreInitializationSucceeded()
		{
			SendAdditionalData();
			BindCoreIapEvents();
		}

		private void OnCoreWillDeinitialize()
		{
			UnbindCoreIapEvents();
		}

		private void BindCoreIapEvents()
		{
#if CORE_IN_APP_PURCHASES
			if (!coreIapEventsBound)
			{
				if (Core.GetService<IInAppPurchasesService>() == null) return;
				Core.GetService<IInAppPurchasesService>().PurchaseUpdated += TrackPurchase;
				coreIapEventsBound = true;
			}
#endif
		}

		private void UnbindCoreIapEvents()
		{
#if CORE_IN_APP_PURCHASES
			if (coreIapEventsBound)
			{
				Core.GetService<IInAppPurchasesService>().PurchaseUpdated -= TrackPurchase;
				coreIapEventsBound = false;
			}
#endif
		}

		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		private void SendAdditionalData()
		{
			try
			{
				Dictionary<string, string> additionalData = new Dictionary<string, string>();
				additionalData.Add(
					"hcsdk",
					HCSDKVersion.Version + " " + (Type.GetType("BoomBit.HyperCasual.IronSourceProvider") != null ? "IS" : "AL"));

				var iapVersion = HCCoreModules.Modules.ContainsKey("inapp_purchases")
					? HCCoreModules.Modules["inapp_purchases"].Split('/')[0]
					: "";
				var iapGpVersion = HCCoreModules.Modules.ContainsKey("inapp_purchases_android_gp")
					? HCCoreModules.Modules["inapp_purchases_android_gp"].Split('/')[0]
					: "";

				additionalData.Add("iap", iapVersion + "," + iapVersion + "," + iapGpVersion);

				var cpVersion = HCCoreModules.Modules.ContainsKey("decoreator_cross_promotion")
					? HCCoreModules.Modules["decoreator_cross_promotion"].Split('/')[0]
					: "";

				var xpigVersion = HCCoreModules.Modules.ContainsKey("cross_promo_xpig")
					? HCCoreModules.Modules["cross_promo_xpig"].Split('/')[0]
					: "";

				additionalData.Add("cp", cpVersion + "," + xpigVersion);

				var adsVersion = HCCoreModules.Modules.ContainsKey("advertisements")
					? HCCoreModules.Modules["advertisements"].Split('/')[0]
					: "";

				additionalData.Add("ads", adsVersion);

				additionalData.Add("med", IronSource.pluginVersion());
				
				AppsFlyer.setAdditionalData(additionalData);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("AppsFlyerProvider :: SendAdditionalData Problem with ");
				Log.ErrorFormat("Error while sending AppsFlyer Additional Metadata.\n{0}\n{1}", e.Message, e.StackTrace);
			}
		}
		
#if CORE_IN_APP_PURCHASES
		private void TrackPurchase(IPurchase purchase)
		{
			if (purchase.Sandboxed && (HCSDK.GetParameter("HCSDK_Test_Purchase").ToUpperInvariant()!="TRUE")) return;
			if (purchase.State != PurchaseState.Succeeded) return;

			if (purchase.Product.Type == ProductType.Subscription)
			{
				PlayerPrefs.SetInt("HC_HAVE_SUBSCRIPTION", 1);
			}
			else
			{
				PlayerPrefs.SetInt("HC_HAVE_PURCHASE", 1);
			}
		}
#endif
		
		/// <inheritdoc />
		public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
		{
			Log.InfoFormat("AppsFlyer Purchase Connector :: didReceivePurchaseRevenueValidationInfo :: {0}", validationInfo);
#if !CORE_IN_APP_PURCHASES
			PlayerPrefs.SetInt("HC_HAVE_PURCHASE", 1);
#endif
		}

		/// <inheritdoc />
		public void didReceivePurchaseRevenueError(string error)
		{
			Log.InfoFormat("AppsFlyer Purchase Connector :: didReceivePurchaseRevenueError :: {0}", error);
		}
	}
}
