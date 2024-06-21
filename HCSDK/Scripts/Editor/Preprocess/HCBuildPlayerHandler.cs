#if UNITY_2018_3_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
using PlistCS;
using UnityEditor.Build.Reporting;

public class HCBuildPlayerHandler : MonoBehaviour
{
    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
    }

    private static void BuildPlayer(BuildPlayerOptions options)
    {
        try
        {
            Debug.Log("HCBuildPlayerHandler :: BuildPlayer");

            CheckRequiredKeys();

            CheckPushManual();

            BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("exception while building:", ex.Message, "ok");
                
                Exception log = ex.InnerException == null ? ex : ex.InnerException;
            Debug.LogException(log);
            Debug.LogErrorFormat("{0} in BuildHandler: '{1}'", log.GetType().Name, ex.Message);
        }
    }

    private static void CheckPushManual()
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
    private static void CheckRequiredKeys()
    {
        string localConfig = Application.dataPath+"/HCSDK/Configuration/Resources/iOS/iOSConfiguration.xml";
        #if UNITY_EDITOR_WIN
        localConfig = localConfig.Replace('/','\\');
        #endif

    #if UNITY_ANDROID
        localConfig = Application.dataPath+"/HCSDK/Configuration/Resources/Android/AndroidConfiguration.xml";
        #if UNITY_EDITOR_WIN
        localConfig = localConfig.Replace('/','\\');
        #endif

    #endif

        Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist (localConfig);
        
        CheckKeysFromList(plist, HCRequiredKeys.multiplatformKeys);
        
    #if UNITY_IOS
        CheckKeysFromList(plist, HCRequiredKeys.iosKeys);
    #elif UNITY_ANDROID
        CheckKeysFromList(plist, HCRequiredKeys.androidKeys);
    #endif
    }

    private static void CheckKeysFromList(Dictionary<string, object> plist, string[] keys)
    {
        for (int i = 0; i != keys.Length; i++)
        {
            if (!plist.ContainsKey(keys[i]))
            {
                string message = "Missing required key " + keys[i] + " in HCSDK configuration. Contact with id team.";
                
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("HCSDK configuration problem", message, "ok");
                }

                throw new BuildFailedException(message);
            }
        }
    }
}
#endif