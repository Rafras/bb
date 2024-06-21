#if UNITY_IOS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using PlistCS;
using System.IO;
using MiniJSON;
using HCExtension;
using UnityEditor.Build.Content;

public class HCPostprocess
{
    
#if !UNITY_TVOS
    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
    {
        var pbxprojPath = PBXProject.GetPBXProjectPath(buildPath);
        var pbxproj = new PBXProject();
        using (var pbxprojReader = File.OpenText(pbxprojPath))
        {
            pbxproj.ReadFromStream(pbxprojReader);
        }

        CheckSkAdNetworkFile();
        
#if UNITY_2019_3_OR_NEWER
        FixMainTarget(pbxproj);

        var targetGuid = pbxproj.GetUnityFrameworkTargetGuid();
        var target2Guid = pbxproj.GetUnityMainTargetGuid();
#else
        var targetGuid = pbxproj.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
        GenerateCorrectPlist(buildPath);
        MakeInjection(buildPath);

        SetupBuildProperties(pbxproj, targetGuid);

        pbxproj.WriteToFile(pbxprojPath);
    }
#endif

    private static void CheckSkAdNetworkFile()
    {
        string networksIdentifiersFile = Application.dataPath+@"/HCSDK/Configuration/Resources/iOS/SKAdNetworkIdentifiers.csv";
        if (File.Exists(networksIdentifiersFile))
        {
            File.Delete(networksIdentifiersFile);
        }
    }
    
#if UNITY_2019_3_OR_NEWER
    private static void FixMainTarget(PBXProject pbxproj)
    {
        var targetGuid = pbxproj.GetUnityMainTargetGuid();
        pbxproj.SetBuildProperty(targetGuid, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");

#if UNITY_2020_3_OR_NEWER
        pbxproj.SetBuildProperty(pbxproj.GetUnityFrameworkTargetGuid(), "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
#endif
    }
#endif

    private static void GenerateCorrectPlist(string buildPath)
    {
        string plistPath = Path.Combine (buildPath, "Info.plist");
        string localConfig = Application.dataPath+"/HCSDK/Configuration/Resources/iOS/iOSConfiguration.xml";

        if (!File.Exists (plistPath)) {
            Debug.LogWarning ("Couldn't find Info.plist in build output.");
            return;
        }
        if (!File.Exists (localConfig)) {
            Debug.LogWarning ("Couldn't find " + localConfig + " in project.");
            return;
        }

        // modify plist
        Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist (plistPath);

        Dictionary<string, object> configuration = (Dictionary<string, object>)Plist.readPlist (localConfig);
        
        string applicationIdentifier = PlayerSettings.applicationIdentifier;

        if (plist.ContainsKey ("NSAppTransportSecurity")) plist.Remove ("NSAppTransportSecurity");
        plist.Add ("NSAppTransportSecurity", new Dictionary<string, object> () { { "NSAllowsArbitraryLoads", true } });

        if (plist.ContainsKey ("UIViewControllerBasedStatusBarAppearance")) plist.Remove ("UIViewControllerBasedStatusBarAppearance");

        if (plist.ContainsKey("UIApplicationExitsOnSuspend")) {
            plist.Remove("UIApplicationExitsOnSuspend");
        }

        if (configuration.ContainsKey("AdMob_app_id"))
        {
            if (!plist.ContainsKey("GADApplicationIdentifier")) plist.Add("GADApplicationIdentifier", configuration ["AdMob_app_id"]);
            else plist["GADApplicationIdentifier"] = configuration ["AdMob_app_id"];
        }
        
        if (configuration.ContainsKey("AppLovin_id"))
        {
            if (!plist.ContainsKey("AppLovinSdkKey")) plist.Add("AppLovinSdkKey", configuration ["AppLovin_id"]);
            else plist["AppLovinSdkKey"] = configuration ["AppLovin_id"];
        }

        if (!plist.ContainsKey("GADIsAdManagerApp")) plist.Add("GADIsAdManagerApp", true);
        else plist["GADIsAdManagerApp"] = true;
        
        if (!plist.ContainsKey ("NSLocationAlwaysUsageDescription")) plist.Add ("NSLocationAlwaysUsageDescription", "Advertisement would like to use your location.");
        if (!plist.ContainsKey ("NSLocationWhenInUseUsageDescription")) plist.Add ("NSLocationWhenInUseUsageDescription", "Advertisement would like to use your location.");
        if (!plist.ContainsKey ("NSBluetoothAlwaysUsageDescription")) plist.Add ("NSBluetoothAlwaysUsageDescription", "Advertisement would like to use your bluetooth.");
        if (!plist.ContainsKey ("NSBluetoothPeripheralUsageDescription")) plist.Add ("NSBluetoothPeripheralUsageDescription", "Advertisement would like to use your bluetooth.");
        if (!plist.ContainsKey ("NSCalendarsUsageDescription")) plist.Add ("NSCalendarsUsageDescription", "Advertisement would like to use your calendar.");
        if (!plist.ContainsKey ("NSMotionUsageDescription")) plist.Add ("NSMotionUsageDescription", "Advertisement would like to have interactive ad controls.");
        if (!plist.ContainsKey ("NSCameraUsageDescription")) plist.Add ("NSCameraUsageDescription", "Advertisement would like to use camera.");
        if (!plist.ContainsKey ("NSPhotoLibraryUsageDescription")) plist.Add ("NSPhotoLibraryUsageDescription", "Advertisement would like to store a photo.");
        
        if (configuration.ContainsKey("supported_localizations"))
        {
            object[] supportedLocalizations = configuration["supported_localizations"].ToString().Split(';');
            List<object> localizations=new List<object>(supportedLocalizations);
            
            if (plist.ContainsKey("CFBundleLocalizations"))
            {
                plist.Remove("CFBundleLocalizations");
            }
            plist.Add("CFBundleLocalizations", localizations);
        }

        if (!plist.ContainsKey("UIBackgroundModes")) plist.Add("UIBackgroundModes", new List<object>() { "remote-notification" });

        Plist.writeXml (plist, plistPath);
    }
    
    private static void SetupBuildProperties(PBXProject pbxproj, string targetGuid)
    {
        pbxproj.SetBuildProperty(targetGuid, "CLANG_ENABLE_MODULES", "YES");
        pbxproj.UpdateBuildProperty(targetGuid, "OTHER_LDFLAGS", null, new List<string>{ "-all_load" });
        pbxproj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
        pbxproj.SetBuildProperty(pbxproj.GetUnityMainTargetGuid(), "ENABLE_BITCODE", "NO");
        pbxproj.SetBuildProperty(targetGuid, "GCC_ENABLE_OBJC_EXCEPTIONS", "YES");
#if !UNITY_2019_3_OR_NEWER
        pbxproj.SetBuildProperty(targetGuid, "SWIFT_VERSION", "5.0");
        pbxproj.SetBuildProperty(targetGuid, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");
#endif
    }
    
    private static void CopyAndReplaceFile(string srcPath, string dstPath)
    {
        if (File.Exists(dstPath))
            File.Delete(dstPath);

        FileInfo fi = new FileInfo(dstPath);

        Debug.Log("Directory: " + fi.Directory.FullName);
        if (!fi.Directory.Exists) fi.Directory.Create();
        
        File.Copy(srcPath, dstPath);
    }
    
    private static void CopyAndReplaceDirectory(string srcPath, string dstPath)
    {
        if (Directory.Exists(dstPath))
            Directory.Delete(dstPath, true);
        if (File.Exists(dstPath))
            File.Delete(dstPath);

        Directory.CreateDirectory(dstPath);

        foreach (var file in Directory.GetFiles(srcPath))
            File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));

        foreach (var dir in Directory.GetDirectories(srcPath))
            CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)));
    }
    
    private static string ExecShell(string cmd, string stdin = null)
    {
        var escapedCmd = cmd.Replace("\"", "\\\"");    
        var process = new System.Diagnostics.Process()
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"" + escapedCmd + "\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        if (stdin != null) 
        {
            process.StandardInput.AutoFlush = true;
            process.StandardInput.Write(stdin);
            process.StandardInput.Close();
        }
        string result = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.Close();

        if (error.Trim().Length > 0)
        {
            Debug.LogError("ExecShell\ncommand: \"" + escapedCmd + "\"\nerror: " + error);
        }

        return result;
    }
    
    private static string GenObjectId(Dictionary<string, object> pbxprojJson)
    {
        var objects = (Dictionary<string, object>)pbxprojJson["objects"];
        string objectId = System.Guid.NewGuid().ToString("N").Substring(0, 24);
        // technically it can cause an infinite loop,
        // but good luck with generating more than 2 duplicate UUIDs, even trimmed
        while (objects.ContainsKey(objectId))
        {
            objectId = System.Guid.NewGuid().ToString("N").Substring(0, 24);
        }
        return objectId;
    }

    private static void AddShellScriptBuildPhase(Dictionary<string, object> pbxprojJson, string targetGuid, string script, List<string> inputPaths=null)
    {
        if (inputPaths==null) inputPaths= new List<string>();
        
        var objectId = GenObjectId(pbxprojJson);
        var objects = (Dictionary<string, object>)pbxprojJson["objects"];
        var phase = new Dictionary<string, object>{
            { "isa", "PBXShellScriptBuildPhase" },
            { "buildActionMask", "2147483647" },
            { "runOnlyForDeploymentPostprocessing", "0" },
            { "files", new List<string>() },
            { "inputPaths", inputPaths },
            { "outputPaths", new List<string>() },
            { "shellPath", "/bin/sh" },
            { "shellScript", script }
        };
        objects[objectId] = phase;
        var target = (Dictionary<string, object>)objects[targetGuid];
        var buildPhases = (List<object>)target["buildPhases"];
        buildPhases.Add(objectId);
    }

    private static void MakeInjection(string path)
    {
        string[] appController = new[]
        {
            Path.Combine(path, "Classes/AppController.mm"),
            Path.Combine(path, "Classes/UnityAppController.mm"),
        };

        string preprocessorPath = path + "/Classes/Preprocessor.h";
        string text = File.ReadAllText(preprocessorPath);
        text = text.Replace("UNITY_USES_REMOTE_NOTIFICATIONS 0", "UNITY_USES_REMOTE_NOTIFICATIONS 1");
        File.WriteAllText(preprocessorPath, text);

        InjectCode(appController, "", "#import \"PMInjection.h\"\n");
        
        InjectCode(appController, "- (BOOL)application:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions\n{",
                   "\n\n\t[[PMInjection sharedManager] Injection:application didFinishLaunchingWithOptions:launchOptions];\n");

        InjectCode(appController, "- (void)application:(UIApplication*)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData*)deviceToken {",
                   "\n\t[[PMInjection sharedManager] Injection:application didRegisterForRemoteNotificationsWithDeviceToken:deviceToken];\n\n");
            
        InjectCode(appController, "- (BOOL)application:(UIApplication*)app openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options {",
                   "\n\t[[PMInjection sharedManager] Injection:app openURL:(NSURL*)url options:options];\n\n");
        
        InjectCode(appController, "- (void)application:(UIApplication *)application didReceiveRemoteNotification:(NSDictionary *)userInfo fetchCompletionHandler:(void (^)(UIBackgroundFetchResult result))handler {",
                   "\n\tif (userInfo[@\"CONFIG_STATE\"]) {\n\t\t[[NSUserDefaults standardUserDefaults] setBool:YES forKey:@\"CONFIG_STALE\"];\n\t}\n");
    }

#region Code injection utils

    private static bool InjectAndReplaceCode(string[] fileNames, string injection, string partToReplace)
    {
        bool injected = false;
        foreach (var fileName in fileNames)
        {
            if (File.Exists(fileName))
                injected |= InjectAndReplaceCode(fileName, injection, partToReplace);
        }
        return injected;
    }

    private static bool InjectAndReplaceCode(string fileName, string injection, string partToReplace)
    {
        if (!File.Exists(fileName))
            return false;

        string fileContent = File.ReadAllText(fileName);

        fileContent = fileContent.Replace(partToReplace, injection);

        File.WriteAllText(fileName, fileContent);

        return true;
    }

    private static bool InjectCode(string[] fileNames, string anchor, string injection, bool before = false)
    {
        bool injected = false;
        foreach (var fileName in fileNames)
        {
            if (File.Exists(fileName))
                injected |= InjectCode(fileName, anchor, injection, before);
        }
        return injected;
    }

    private static bool InjectCode(string fileName, string anchor, string injection, bool before)
    {
        if (!File.Exists(fileName))
            return false;

        string[] afterParts = anchor.Split(' ');

        var fileContent = File.ReadAllText(fileName);

        var beforePos = 0;
        var afterPos = 0;
        if (afterParts.Length > 0) while (true)
            {
                var start = true;
                var match = true;
                foreach (var part in afterParts)
                {
                    var nextPos = fileContent.IndexOf(part, afterPos);

                    if (nextPos < 0)
                        return false;

                    if (!start)
                    {
                        var whitespace = fileContent.Substring(afterPos, nextPos - afterPos);

                        if (whitespace.Trim().Length > 0)
                        {
                            match = false;
                            break;
                        }
                    }
                    else
                    {
                        beforePos = nextPos;
                    }
                    start = false;
                    afterPos = nextPos + part.Length;
                }
                if (match)
                    break;
            }

        bool injected = false;
        if (before)
        {
            if (beforePos >= 0 && !fileContent.Substring(0, beforePos).EndsWith(injection))
            {
                fileContent = fileContent.Substring(0, beforePos) + injection + fileContent.Substring(beforePos);
                injected = true;
            }
        }
        else
        {
            if (afterPos >= 0 && !fileContent.Substring(afterPos).StartsWith(injection))
            {
                fileContent = fileContent.Substring(0, afterPos) + injection + fileContent.Substring(afterPos);
                injected = true;
            }
        }
        File.WriteAllText(fileName, fileContent);
        return injected;
    }

    #endregion
}
#endif
