using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using PlistCS;
#if UNITY_IPHONE
using UnityEditor.iOS.Xcode.Extensions;
using UnityEditor.iOS.Xcode;
#endif

public class PluginInjector
{
#if !UNITY_TVOS && UNITY_IOS

	private static string injectorFileName = "config.plist";
    private static string pluginsPath = Application.dataPath + "/HCSDK/Plugins/iOS";

	[PostProcessBuild (998)]
	public static void OnPostprocessBuild (BuildTarget target, string buildPath)
	{
		AddAllPlugins (buildPath);
    }

	private static void AddAllPlugins (string buildPath)
	{
        List<PluginInfo> plugins = GetAllPlugins ();

        for (int i = 0; i != plugins.Count; i++)
        {
            AddPluginToXcode (buildPath, plugins[i]);
        }
	}

	private static List<PluginInfo> GetAllPlugins ()
	{
		List<PluginInfo> plugins = new List<PluginInfo> ();

		string [] pluginDirectories = Directory.GetDirectories (pluginsPath, "*", SearchOption.TopDirectoryOnly);

        for (int i = 0; i != pluginDirectories.Length; i++)
        {
            DirectoryInfo di = new DirectoryInfo (pluginDirectories[i]);

            FileInfo[] fis = di.GetFiles (injectorFileName, SearchOption.TopDirectoryOnly);
            if (fis.Length > 0 && fis[0].Name == injectorFileName)
            {
                plugins.Add (ParsePluginInfo (di, fis[0].FullName));
            }
        }

		return plugins;
	}

    private static void AddPluginToXcode(string buildPath, PluginInfo plugin)
    {
        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(PBXProject.GetPBXProjectPath(buildPath)));

#if UNITY_2019_3_OR_NEWER
        string targetProject = proj.GetUnityFrameworkTargetGuid();
        
        proj.AddHeadersBuildPhase(targetProject);
        proj.AddFrameworksBuildPhase(targetProject);
#else
        string targetProject = proj.TargetGuidByName("Unity-iPhone");
#endif

        Queue<string> directories = new Queue<string>();
        directories.Enqueue(plugin.pluginName);

        while (directories.Count > 0)
        {
            string directory = directories.Dequeue();
            DirectoryInfo di = new DirectoryInfo(pluginsPath + "/" + directory);
            DirectoryInfo[] dis = di.GetDirectories("*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i != dis.Length;i++)
            {
                if (dis[i].Extension != ".framework" && dis[i].Extension!=".bundle")
                {
                    directories.Enqueue(directory + "/" + dis[i].Name);
                }
                else
                {
                    string guid = proj.AddFile(dis[i].FullName, directory + "/" + dis[i].Name, PBXSourceTree.Absolute);
                    proj.AddFileToBuild(targetProject, guid);

                    if (dis[i].Extension == ".framework")
                    {
                        List<string> addPaths = new List<string>();
                        List<string> removePaths = new List<string>();

                        addPaths.Add(dis[i].Parent.FullName);

                        proj.UpdateBuildProperty(targetProject, "FRAMEWORK_SEARCH_PATHS", addPaths, removePaths);
                    }
                }
            }
            
            if (!Directory.Exists(buildPath + "/" + directory))
            {
                Debug.Log("PluginsInjector :: AddPluginToXcode :: " + (buildPath + "/" + directory));

                Directory.CreateDirectory(buildPath + "/" + directory);
            }


            FileInfo[] fis = di.GetFiles("*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i != fis.Length; i++)
            {
                if (fis[i].Extension != ".DS_Store" && fis[i].Extension != ".meta" && fis[i].Extension != ".plist" && fis[i].Extension != ".md")
                {
                    Debug.Log("PluginsInjector :: AddPluginToXcode :: " + fis[i].FullName + " " + (directory + "/" + fis[i].Name));
                    string guid=proj.AddFile(fis[i].FullName, directory+"/"+fis[i].Name, PBXSourceTree.Absolute);

                    if (fis[i].Extension == ".h")
                    {
#if UNITY_2019_3_OR_NEWER
                        proj.AddPublicHeaderToBuild(targetProject, guid);
#else
                        proj.AddFileToBuild(targetProject, guid);
#endif
                    }
                    else
                    {
                        proj.AddFileToBuild(targetProject, guid);
                    }

                    if (fis[i].Extension == ".a")
                    {
                        List<string> addPaths = new List<string>();
                        List<string> removePaths = new List<string>();

                        addPaths.Add(fis[i].DirectoryName);

                        proj.UpdateBuildProperty(targetProject, "LIBRARY_SEARCH_PATHS", addPaths, removePaths);
                    }
                }
            }
        }
        AddAllConfigStuff(proj, targetProject, plugin);

        File.WriteAllText(PBXProject.GetPBXProjectPath(buildPath), proj.WriteToString());
    }

    private static PluginInfo ParsePluginInfo(DirectoryInfo pluginDirectory, string configFile)
    {
        PluginInfo plugin = new PluginInfo();
        plugin.path = pluginDirectory.FullName;
        plugin.pluginName = pluginDirectory.Name;

        ParseConfigFile(configFile, plugin);

        return plugin;
    }

    private static void ParseConfigFile(string configFile, PluginInfo plugin)
    {
        Dictionary<string, object> plist = (Dictionary<string, object>)Plist.readPlist (configFile);

        if (plist.ContainsKey("requiredFrameworks"))
        {
            List<object> requiredFrameworks = plist["requiredFrameworks"] as List<object>;
            for (int i = 0; i != requiredFrameworks.Count; i++)
            {
                plugin.requiredFrameworks.Add(requiredFrameworks[i].ToString());
            }
        }

        if (plist.ContainsKey("weakLinkFrameworks"))
        {
            List<object> weak = plist["weakLinkFrameworks"] as List<object>;
            for (int i = 0; i != weak.Count; i++)
            {
                if (!plugin.weakFrameworks.Contains(weak[i] + ".framework"))
                {
                    plugin.weakFrameworks.Add(weak[i] + ".framework");
                }
            }
        }

        if (plist.ContainsKey("dynamicLibraries"))
        {
            List<object> dylibs = plist["dynamicLibraries"] as List<object>;
            for (int i = 0; i != dylibs.Count; i++)
            {
                plugin.dylibs.Add(dylibs[i].ToString());
            }
        }

        if (plist.ContainsKey("linkerFlags"))
        {
            List<object> flags = plist["linkerFlags"] as List<object>;
            for (int i = 0; i != flags.Count; i++)
            {
                plugin.flags.Add(flags[i].ToString());
            }
        }
    }

    private static void AddAllConfigStuff(PBXProject proj, string target,  PluginInfo plugin)
    {
        for (int i = 0; i != plugin.requiredFrameworks.Count; i++)
        {
            proj.AddFrameworkToProject(target, plugin.requiredFrameworks[i], plugin.weakFrameworks.Contains(plugin.requiredFrameworks[i]));
        }

        for (int i = 0; i != plugin.dylibs.Count; i++)
        {
            proj.AddFrameworkToProject(target, plugin.dylibs[i], plugin.weakFrameworks.Contains(plugin.dylibs[i]));
        }

        List<string> removeFlags = new List<string>();
        proj.UpdateBuildProperty(target, "OTHER_LDFLAGS", plugin.flags, removeFlags);
    }

    private class PluginInfo
	{
        public PluginInfo()
        {
            requiredFrameworks = new List<string>();
            weakFrameworks = new HashSet<string>();
            dylibs = new List<string>();
            flags = new List<string>();
        }

		public string path;
        public string pluginName;
        public List<string> requiredFrameworks;
        public HashSet<string> weakFrameworks;
        public List<string> dylibs;
        public List<string> flags;
	}

#endif
}

