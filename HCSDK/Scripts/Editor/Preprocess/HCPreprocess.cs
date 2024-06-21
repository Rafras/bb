using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
using PlistCS;

#if !UNITY_2018_3_OR_NEWER

public class HCPreprocess : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }
    
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("HCPreprocess :: OnPreprocessBuild");
        
        #if !no_Firebase && UNITY_IOS
        FirebaseHelper.CheckFirebaseConfiguration();
        #elif !no_Firebase && UNITY_ANDROID
	    FirebaseHelper.CheckFirebaseConfigurationAndroid
        #endif

        CheckPushManual();
    }
    
   private  void CheckPushManual()
    {
        string localConfig = Application.dataPath+"/HCSDK/Configuration/Resources/iOS/iOSConfiguration.xml";
        
        if (!File.Exists (localConfig)) {
            Debug.LogWarning ("Couldn't find " + localConfig + " in project.");
            throw new BuildFailedException("Couldn't find " + localConfig + " in project.");
        }
        
        Dictionary<string, object> configuration = (Dictionary<string, object>)Plist.readPlist (localConfig);
        
        string defFile = Application.dataPath + "/HCSDK/Plugins/iOS/PMInjection/PMInjectionDefinitions.h";

        using (StreamWriter sw = new StreamWriter(defFile))
        {
            sw.WriteLine ();

            if (configuration.ContainsKey("push_manual") && configuration["push_manual"].ToString() == "TRUE")
            {
                sw.WriteLine("#define _HC_PUSH_MANUAL");
            }

            sw.Close();
        }
    }
}

#endif
