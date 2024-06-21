using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoomBit.HyperCasual
{
    public static class AndroidUtils
    {
#if UNITY_ANDROID && !UNITY_EDITOR
	    public static AndroidJavaObject CreateJavaMapFromDictionary(IDictionary<string, object> dict)
	    {
		    var javaMap = new AndroidJavaObject("java.util.HashMap");
		    var putMethodId = AndroidJNIHelper.GetMethodID(
			    javaMap.GetRawClass(), "put", "(Ljava/lang/Object;Ljava/lang/Object;)Ljava/lang/Object;");

		    var args = new object[2];
		    foreach (var kvp in dict)
		    {
			    using (var k = new AndroidJavaObject("java.lang.String", kvp.Key))
			    {
				    var valueClassName = "";
				    if (kvp.Value is String) valueClassName = "java.lang.String";
				    else if (kvp.Value is Boolean) valueClassName = "java.lang.Boolean";
				    else if (kvp.Value is Int32) valueClassName = "java.lang.Integer";
				    else if (kvp.Value is Int64) valueClassName = "java.lang.Long";
				    else if (kvp.Value is Single) valueClassName = "java.lang.Float";
				    else if (kvp.Value is Double) valueClassName = "java.lang.Double";
				    
				    using (var v = new AndroidJavaObject(valueClassName, kvp.Value))
				    {
					    args[0] = k;
					    args[1] = v;
					    AndroidJNI.CallObjectMethod(javaMap.GetRawObject(), putMethodId, AndroidJNIHelper.CreateJNIArgArray(args));
				    }
			    }
		    }

		    return javaMap;
	    }
	    
	    public static AndroidJavaObject CreateAndroidBundleFromDictionary(IDictionary<string, object> dict)
	    {
		    var javaBundle = new AndroidJavaObject("android.os.Bundle");
		    
		    foreach (var kvp in dict)
		    {
				    if (kvp.Value is String)
					    javaBundle.Call("putString", kvp.Key, kvp.Value);
				    else if (kvp.Value is Boolean)
					    javaBundle.Call("putBoolean", kvp.Key, (bool)kvp.Value);
				    else if (kvp.Value is Int32)
					    javaBundle.Call("putInt", kvp.Key, (int)kvp.Value);
				    else if (kvp.Value is Int64)
					    javaBundle.Call("putLong", kvp.Key, (long)kvp.Value);
				    else if (kvp.Value is Single)
					    javaBundle.Call("putFloat", kvp.Key, (float)kvp.Value);
				    else if (kvp.Value is Double)
					    javaBundle.Call("putDouble", kvp.Key, (double)kvp.Value);
		    }

		    return javaBundle;
	    }

		public static string GetProp(string key)
		{
			var pluginClass = new AndroidJavaClass("com.tes.aidemandroidnative.Utils");
			var _plugin = pluginClass.CallStatic<AndroidJavaObject>("instance");
			return _plugin.CallStatic<String>("GetAndroidSystemProperty", key);
		}
		
#endif

		/// <summary>
		/// Returns the store on which the app was published
		/// </summary>
		/// <returns>Current storefront</returns>
		public static Storefront GetCurrentStorefront()
		{
			return Storefront.GooglePlayStore;
		}

		public enum Storefront
		{
			GooglePlayStore
			//SamsungGalaxyStore,
			//HuaweiAppGallery
		}
    }
}
