using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using HCExtension;
using UnityEngine.Networking;

public class WhitelistMechanism : MonoBehaviour
{
	private static WhitelistMechanism instance;
	

#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern bool _HasAppByScheme(string schemeName);
	#endif
	
    public void Initialize()
    {
	    instance = this;
    }

    public static bool IsFbInstalled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
		if (instance.HasApp("com.facebook.orca") ||
	        instance.HasApp("com.facebook.mlite") ||
	        instance.HasApp("com.facebook.lite") ||
	        instance.HasApp("com.facebook.katana"))
	    {
		    return true;
	    }
	    else return false;
#elif UNITY_IOS && !UNITY_EDITOR
	    if (_HasAppByScheme("fb-messenger-api") || _HasAppByScheme("fb")) return true;
	    else return false;
#else
	    return false;
#endif
    }
    
    private bool HasApp(string productName) 
    {
        bool isAppInstalled = false;
        
        #if UNITY_ANDROID && !UNITY_EDITOR
		AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
		AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
		AndroidJavaObject launchIntent = null;
		
		try {
			launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage",productName);
		}
		catch(Exception ex) {
			isAppInstalled = false;
		}
		if(launchIntent == null) {
			isAppInstalled = false;
		}
		else {
			isAppInstalled = true;
		}
		#endif
		
		return isAppInstalled;
    }
}
