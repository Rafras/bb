using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace BoomBit.HyperCasual
{
    using Coredian.Firebase;

    using UnityEngine.Networking;

    public class AnalyticsTracker: MonoBehaviour
    {
        
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool _IsTablet();
        
        [DllImport("__Internal")]
        private static extern bool _IsLimitedAdTracking();
#elif UNITY_ANDROID && !UNITY_EDITOR
        private static bool _IsTablet()
        {
            AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = activityClass.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaObject metrics = new AndroidJavaObject("android.util.DisplayMetrics");

            try
            {
                activity.Call<AndroidJavaObject>("getWindowManager").Call<AndroidJavaObject>("getDefaultDisplay").Call("getMetrics", metrics);
            }
            catch (Exception ex)
            {
                return false;
            }

            float XDPI = metrics.Get<float>("xdpi");
            float YDPI = metrics.Get<float>("ydpi");

            var wInches = (float)Screen.height / YDPI;
            var hInches = (float)Screen.width / XDPI;

            double screenDiagonal = Math.Sqrt(Math.Pow(wInches, 2) + Math.Pow(hInches, 2));

            return (screenDiagonal >= 7.0);
        }
#else
        private static bool _IsTablet() { return true; }
#endif

        private HashSet<int> trackedNumbers = new HashSet<int>(){5,10,25,50,75,100,200};
        
        public void TrackDisplay(AdType adType, string placement="")
        {
            if (adType==AdType.banner) return;
            
            if(Core.GetService<IFirebaseService>() != null)
                Core.GetService<IFirebaseService>().LogEvent("ad", new Dictionary<string, object>(){{"ad_type", adType.ToString()}, {"ad_location", placement}});

            if (adType == AdType.rewarded)
            {
                if(Core.GetService<IFirebaseService>() != null)
                    Core.GetService<IFirebaseService>().LogEvent("ad_video", new Dictionary<string, object>(){{"ad_location", placement}});
            }
        }

        public static bool IsLimitedAdTracking()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _IsLimitedAdTracking();
#endif
            return false;
        }
        
        public void TrackFill(AdType adType)
        {
            if (adType==AdType.banner) return;

            if(Core.GetService<IFirebaseService>() != null)
                Core.GetService<IFirebaseService>().LogEvent("ad_fill", new Dictionary<string, object>(){{"ad_type", adType.ToString()}});
        }

        public void TrackAdFail(AdType adType, string placement)
        {
            if (adType == AdType.banner) return;
            StartCoroutine(TrackAdFailCoroutine(adType, placement));
        }

        private IEnumerator TrackAdFailCoroutine(AdType adType, string placement)
        {
            yield return new WaitForEndOfFrame();
            UnityWebRequest request = UnityWebRequest.Get("https://www.google.com");
            yield return request.SendWebRequest();
            if (request.isHttpError || request.isNetworkError) {
                Debug.LogWarning("No internet connection");
            }
            else
            {
                if(Core.GetService<IFirebaseService>() != null)
                    Core.GetService<IFirebaseService>().LogEvent("ad_fail", new Dictionary<string, object>(){{"ad_type", adType.ToString()}, {"ad_location", placement}});
            }
            request.Dispose();
        }

        public void TrackNoTablet()
        {
            if (!_IsTablet())
            {
                if(Core.GetService<IFirebaseService>() != null)
                    Core.GetService<IFirebaseService>().LogEventOnce("no_tablet");
            }
        }
        
    }

    public enum AdType
    {
        interstitial,
        rewarded,
        banner,
    }
}
