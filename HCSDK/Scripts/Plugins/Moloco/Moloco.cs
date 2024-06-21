using UnityEngine;
using System;

namespace MolocoSdk 
{
    using Coredian;

    public static class MolocoSDK
    {   
        private static readonly PlatformDelegate platformDelegate;

        static MolocoSDK()
        {
            #if UNITY_ANDROID
                platformDelegate = new AndroidDelegate();
            #endif

            #if UNITY_IOS
                platformDelegate = new IOSDelegate();
            #endif
        }

        public static void SetPrivacy(PrivacySettings privacySettings) 
        {
            platformDelegate.SetPrivacy(privacySettings);
        }
    }

    public class PrivacySettings 
    {
        public Boolean? IsUserConsent = null;
        public Boolean? IsAgeRestrictedUser = null;
        public Boolean? IsDoNotSell = null;
    }

    interface PlatformDelegate {
        void SetPrivacy(PrivacySettings privacySettings);
    }
}
