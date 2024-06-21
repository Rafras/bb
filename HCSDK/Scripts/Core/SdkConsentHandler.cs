namespace BoomBit.HyperCasual
{
	using System.Runtime.InteropServices;

	using Coredian;
	using Coredian.Privacy;

    using MolocoSdk;

	using UnityEngine;

	/// <summary>
    /// Class that handles all SDK networks requiring special treatment.
    /// </summary>
    public static class SdkConsentHandler
    {
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void _SetLDUForFAN();

		[DllImport("__Internal")]
		private static extern void _SetBigoAdsConsent(bool hasConsent);
#else
		private static void _SetLDUForFAN(){}
		private static void _SetBigoAdsConsent(bool hasConsent){}
#endif
		private const string facebookAudienceNetworkTemplateId = "ax0Nljnj2szF_r";
		private const string bigoAdsTemplateId = "WIP";
		
		//Use IS template Id unitil we have correct
        private const string molocoTemplateId = "9dchbL797";

		public static void SetConsent()
        {
            SetMolocoConsent();
			SetFacebookAudienceNetworkConsent();
			
			//Bigo Ads doesn't required consent on iOS, only on Android but only when Bigo Ads is initialized,
			//we don't have any option to determine when Bigo Ads is initialized by Iron Source
			//SetBigoAdsConsent();
        }

		private static void SetMolocoConsent()
        {
            var consent = HasConsent(molocoTemplateId);

			Log.InfoFormat("{0} :: {1} => {2}", nameof(SdkConsentHandler),
						   nameof(SetMolocoConsent), consent);
            PrivacySettings privacy = new PrivacySettings { IsUserConsent = consent, IsDoNotSell = !consent };
            MolocoSDK.SetPrivacy(privacy);
        }

        private static void SetFacebookAudienceNetworkConsent()
        {
			if (!HasConsent(facebookAudienceNetworkTemplateId)) return;
			
#if UNITY_ANDROID && !UNITY_EDITOR
			using (var arrayClass = new AndroidJavaClass("java.lang.reflect.Array"))
			using (var arrayObject = arrayClass.CallStatic<AndroidJavaObject>("newInstance",
				new AndroidJavaClass("java.lang.String"), 0))
			{
				using (var AdSettings = new AndroidJavaClass("com.facebook.ads.AdSettings"))
				{
					AdSettings.CallStatic("setDataProcessingOptions", arrayObject, 0, 0);
				}
			}
#elif UNITY_IOS
            _SetLDUForFAN();
#endif
        }
        
        private static bool HasConsent(string templateId)
        {
            return Core.GetService<IConsentService>().GetConsentStatus(templateId) == ConsentStatus.Granted;
        }
        
        private static void SetBigoAdsConsent()
        {
#if UNITY_ANDROID
			AndroidJavaClass consentOptions = new AndroidJavaClass("sg.bigo.ads.ConsentOptions");
			AndroidJavaObject gdpr = consentOptions.GetStatic<AndroidJavaObject>("GDPR");
			AndroidJavaObject ccpa = consentOptions.GetStatic<AndroidJavaObject>("CCPA");
			
			using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using AndroidJavaObject currentActivityObject =
				unityClass.GetStatic<AndroidJavaObject>("currentActivity");
			{
				using (var bigoAds = new AndroidJavaClass("sg.bigo.ads.BigoAdSdk"))
				{
					bigoAds.CallStatic("setUserConsent", currentActivityObject, gdpr, HasConsent(bigoAdsTemplateId));
					bigoAds.CallStatic("setUserConsent", currentActivityObject, ccpa, HasConsent(bigoAdsTemplateId));
				}
			}
#elif UNITY_IOS
			_SetBigoAdsConsent(HasConsent(bigoAdsTemplateId));
#endif
		}
        
    }
}
