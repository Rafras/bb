using System.Runtime.InteropServices;

namespace Moloco.Plugins
{
    #if UNITY_IOS
    class MolocoPrivacyIOS
    {
        [DllImport ("__Internal")]
        private static extern void moloco_setHasUserConsent(bool val);

        public static void setHasUserConsent(bool val) {
            moloco_setHasUserConsent(val);
        }
        
        [DllImport ("__Internal")]
        private static extern void moloco_setIsAgeRestrictedUser(bool val);

        public static void setIsAgeRestrictedUser(bool val) {
            moloco_setIsAgeRestrictedUser(val);
        }
        
        [DllImport ("__Internal")]
        private static extern void moloco_setIsDoNotSell(bool val);

        public static void setIsDoNotSell(bool val) {
            moloco_setIsDoNotSell(val);
        }
    }
    #endif
}