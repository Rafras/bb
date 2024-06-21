namespace BoomBit.HyperCasual
{
	using System.Runtime.InteropServices;

	using UnityEngine;

	public class IronSourceAdQuality
	{
		
#if UNITY_IOS && !UNITY_EDITOR
		[DllImport("__Internal")]
		private static extern void _HCIsAdQualityStart(string appKey);
		[DllImport("__Internal")]
		private static extern void _HCIsAdQualitySetUserconsent(bool consent);
#endif
		
		public static void Start(string ironSourceAppKey)
		{
#if !UNITY_EDITOR			
#if UNITY_ANDROID
			using var arrayClass = new AndroidJavaClass("com.ironsource.adqualitysdk.sdk.IronSourceAdQuality");
			using var ironSourceAdQuality = arrayClass.CallStatic<AndroidJavaObject>("getInstance");
			using var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			using AndroidJavaObject currentActivityObject =
				unityClass.GetStatic<AndroidJavaObject>("currentActivity");
			{
				ironSourceAdQuality.Call("initialize", currentActivityObject, ironSourceAppKey);
			}
#elif UNITY_IOS 
			_HCIsAdQualityStart(ironSourceAppKey);
#endif
#endif
		}

		public static void SetUserConsent(bool consent)
		{

#if !UNITY_EDITOR			
#if UNITY_ANDROID
			using var arrayClass = new AndroidJavaClass("com.ironsource.adqualitysdk.sdk.IronSourceAdQuality");
			using var ironSourceAdQuality = arrayClass.CallStatic<AndroidJavaObject>("getInstance");
			{
				ironSourceAdQuality.Call("setUserConsent", consent);
			}
#elif UNITY_IOS 
			_HCIsAdQualitySetUserconsent(consent);
#endif
#endif
		}
		
	}
}
