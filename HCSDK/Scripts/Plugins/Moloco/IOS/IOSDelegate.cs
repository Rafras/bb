using Moloco.Plugins;

namespace MolocoSdk 
{
     #if UNITY_IOS
    class IOSDelegate : PlatformDelegate
    {
        public void SetPrivacy(PrivacySettings privacySettings) {
            if (privacySettings.IsUserConsent.HasValue)
            {
                MolocoPrivacyIOS.setHasUserConsent(privacySettings.IsUserConsent.Value);
            }
            
            if (privacySettings.IsAgeRestrictedUser.HasValue)
            {
                MolocoPrivacyIOS.setIsAgeRestrictedUser(privacySettings.IsAgeRestrictedUser.Value);
            }
            
            if (privacySettings.IsDoNotSell.HasValue)
            {
                MolocoPrivacyIOS.setIsDoNotSell(privacySettings.IsDoNotSell.Value);
            }
        } 
    }
    #endif
}
