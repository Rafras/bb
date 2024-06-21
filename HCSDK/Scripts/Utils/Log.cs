using UnityEngine;

namespace BoomBit.HyperCasual.Utils
{
    internal static class Log
    {
        public static void Info(string message)
        {
            Debug.Log(message);
        }
        
        public static void InfoFormat(string format, params object[] parameters)
        {
            Debug.Log(string.Format(format, parameters));
        }
        
        public static void Warning(string message)
        {
            Debug.Log(message);
        }
        
        public static void WarningFormat(string format, params object[] parameters)
        {
            Debug.Log(string.Format(format, parameters));
        }
        
        public static void Error(string message)
        {
            Debug.Log(message);
        }
        
        public static void ErrorFormat(string format, params object[] parameters)
        {
            Debug.Log(string.Format(format, parameters));
        }
    }
}