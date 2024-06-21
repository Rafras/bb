using System.IO;
using System.Text;
using System.Text.RegularExpressions;

#if UNITY_EDITOR && UNITY_ANDROID
namespace BoomBit.HyperCasual.Editor
{
	using System;

	using UnityEngine;
    using UnityEditor;
    using System.Xml;
	using System.Linq;
	using System.Collections.Generic;

	using Coredian.BuildTools.Android.Editor.Configuration;
	using Coredian.Serialization.Json;

	public class AndroidManifestTool
    {
	    public static string MainActivityClassName = "com.boombit.MainActivity";
	    
        private static XmlDocument manifest;
	    private static Dictionary<string, object> plist;

	    private const string XmlnsAndroid = "http://schemas.android.com/apk/res/android";
	    private const string XmlnsTools = "http://schemas.android.com/tools";

	    private const string AppXpath = "manifest/application";

	    private const string BbSdkProjectPath = "/Plugins/Android/HCSDK.androidlib";
        private const string ManifestPath = BbSdkProjectPath + "/AndroidManifest.xml";
        
		private const string PlistPath = "/HCSDK/Configuration/Resources/Android/AndroidConfiguration.xml";
		private const string GoogleServicesJsonPath = "/Core/Settings/google-services.json";

		[MenuItem("HCSDK/Update Android Manifest (force)")]
	    public static void UpdateManifest()
	    {
			plist = (Dictionary<string, object>) PlistCS.Plist.readPlist(Application.dataPath + PlistPath);
	        
			plist["appId"] = PlayerSettings.applicationIdentifier;
			PlistCS.Plist.writeXml(plist, Application.dataPath + PlistPath);
			
			SetupBbSdkProject();

	        SetupManifest();
	        
	        SetupApplication();
	        
            SetupOther();

            SetupIronSource();

			SaveManifest();
        }
		
		private static void SetupBbSdkProject()
	    {
		    var fullProjectPath = Application.dataPath + BbSdkProjectPath;
		    if (Directory.Exists(fullProjectPath)) return;
		    
		    Directory.CreateDirectory(fullProjectPath);

		    var propsPath = BbSdkProjectPath + "/project.properties";
		    using (var writer = new StreamWriter(Application.dataPath + propsPath))
		    {
			    writer.WriteLine("android.library=true");
		    }
		    AssetDatabase.ImportAsset("Assets" + propsPath);
	    }

		private static void SetupManifest()
	    {
		    manifest = new XmlDocument();
		    manifest.AppendChild(manifest.CreateXmlDeclaration("1.0", "utf-8", "no"));

		    var manifestNode = AddElement(
			    "manifest",
			    new Attribute("xmlns:android", null, XmlnsAndroid),
			    new Attribute("xmlns:tools", null, XmlnsTools),
			    new Attribute("package", null, "com.boombit.sdk.common.lib"));
		   
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "android.permission.ACCESS_NETWORK_STATE"));
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "android.permission.ACCESS_WIFI_STATE"));
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "android.permission.INTERNET"));
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "android.permission.VIBRATE"));
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "android.permission.WAKE_LOCK"));
		    // AddElement(manifestNode, "uses-permission",
			   //  new Attribute("name", "com.android.vending.BILLING"));

			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", "android.permission.ACCESS_COARSE_LOCATION"),
			// 	new Attribute("node", XmlnsTools, "remove"));
			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", "android.permission.READ_PHONE_STATE"),
			// 	new Attribute("node", XmlnsTools, "remove"));
			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", "android.permission.READ_EXTERNAL_STORAGE"),
			// 	new Attribute("node", XmlnsTools, "remove"));
			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", "android.permission.WRITE_EXTERNAL_STORAGE"), 
			// 	new Attribute("node", XmlnsTools, "remove"));

			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", "com.google.android.c2dm.permission.RECEIVE"));
			// AddElement(manifestNode, "uses-permission",
			// 	new Attribute("name", PlayerSettings.applicationIdentifier + ".permission.C2D_MESSAGE"));
			// AddElement(manifestNode, "permission",
			//     new Attribute("name", PlayerSettings.applicationIdentifier + ".permission.C2D_MESSAGE"),
			//     new Attribute("protectionLevel", "signature"));
		    
		    // AddElement(manifestNode, "supports-screens",
			   //  new Attribute("smallScreens", "true"),
			   //  new Attribute("normalScreens", "true"),
			   //  new Attribute("largeScreens", "true"),
			   //  new Attribute("xlargeScreens", "true"),
			   //  new Attribute("anyDensity", "true"));
	    }

        private static void SetupApplication()
        {
	        AddElement( AppXpath, new Attribute("networkSecurityConfig", "@xml/network_security_config"));
        }

	    private static void SetupMainActivity()
	    {
		    var activity = AddElement(AppXpath, "activity",
			    new Attribute("name", MainActivityClassName),
			    new Attribute("screenOrientation", "sensor"),
			    new Attribute("launchMode", "singleInstance"),
			    new Attribute("exported", "true"));

		    AddElement(activity, "meta-data",
			    new Attribute("name", "unityplayer.UnityActivity"),
			    new Attribute("value", "true"));

		    AddElement(activity, "meta-data",
			    new Attribute("name", "unityplayer.ForwardNativeEventsToDalvik"),
			    new Attribute("value", "true"));

		    AddElement(activity, "intent-filter",
			    new Element("action",
				    new Attribute("name", "android.intent.action.MAIN")),
			    new Element("category",
				    new Attribute("name", "android.intent.category.LAUNCHER")));

		    AddElement(activity, "intent-filter",
			    new Element("action",
				    new Attribute("name", PlayerSettings.applicationIdentifier + ".MESSAGE")),
			    new Element("category",
				    new Attribute("name", "android.intent.category.DEFAULT")));

		    AddElement(activity, "intent-filter",
			    new Element("action",
				    new Attribute("name", "android.intent.action.VIEW")),
			    new Element("category",
				    new Attribute("name", "android.intent.category.DEFAULT")),
			    new Element("category",
				    new Attribute("name", "android.intent.category.BROWSABLE")),
			    new Element("data",
				    new Attribute("scheme", "newpluginmanager")),
			    new Element("data",
				    new Attribute("scheme", PlayerSettings.applicationIdentifier),
				    new Attribute("host", "connect")));

			if (plist.ContainsKey("unique_app_name"))
			{
				AddElement(
					activity,
					"intent-filter",
					new Element(
						"action",
						new Attribute("name", "android.intent.action.VIEW")),
					new Element(
						"category",
						new Attribute("name", "android.intent.category.DEFAULT")),
					new Element(
						"category",
						new Attribute("name", "android.intent.category.BROWSABLE")),
					new Element(
						"data",
						new Attribute("scheme", (string)plist["unique_app_name"])));
			}
		}

	    private static void SetupOther()
	    {
		    AddElement(AppXpath, "activity",
				new Attribute("name", "com.ironsource.sdk.controller.ControllerActivity"),
				new Attribute("configChanges", "orientation|screenSize"),
				new Attribute("hardwareAccelerated", "true"));
		    
		    AddElement(AppXpath, "activity",
			    new Attribute("name", "com.ironsource.sdk.controller.InterstitialActivity"),
			    new Attribute("configChanges", "orientation|screenSize"),
			    new Attribute("hardwareAccelerated", "true"),
			    new Attribute("theme", "@android:style/Theme.Translucent"));
		    
		    AddElement(AppXpath, "activity",
			    new Attribute("name", "com.ironsource.sdk.controller.OpenUrlActivity"),
			    new Attribute("configChanges", "orientation|screenSize"),
			    new Attribute("hardwareAccelerated", "true"),
			    new Attribute("theme", "@android:style/Theme.Translucent"));

			AddElement(AppXpath, "provider",
					   new Attribute("authorities", "${applicationId}.IronsourceLifecycleProvider"),
					   new Attribute("name", "com.ironsource.lifecycle.IronsourceLifecycleProvider"));
			
			 AddElement(AppXpath, "activity",
				new Attribute("name", "com.google.games.bridge.NativeBridgeActivity"),
				new Attribute("theme", "@android:style/Theme.Translucent.NoTitleBar.Fullscreen"));

            AddElement(AppXpath, "activity",
                new Attribute("name", "com.adcolony.sdk.AdColonyInterstitialActivity"),
                new Attribute("configChanges", "keyboardHidden|orientation|screenSize"),
                new Attribute("hardwareAccelerated", "true"));

            AddElement(AppXpath, "activity",
                new Attribute("name", "com.adcolony.sdk.AdColonyAdViewActivity"),
                new Attribute("configChanges", "keyboardHidden|orientation|screenSize"),
                new Attribute("hardwareAccelerated", "true"));

            AddElement(AppXpath, "meta-data",
			    new Attribute("name", "unityplayer.SkipPermissionsDialog"),
			    new Attribute("value", "true"));
			AddElement(AppXpath, "uses-library",
				new Attribute("name", "org.apache.http.legacy"), new Attribute("required", "false"));
			AddElement(AppXpath, "meta-data",
			    new Attribute("name", "com.google.android.gms.nearby.connection.SERVICE_ID"),
			    new Attribute("value", ""));
		    AddElement(AppXpath, "meta-data",
			    new Attribute("name", "com.google.android.gms.ads.APPLICATION_ID"),
			    new Attribute("value", (string)plist["AdMob_app_id"]));
		}

		private static void SetupIronSource()
	    {
		    AddElement(AppXpath, "meta-data",
			    new Attribute("name", "applovin.sdk.key"),
			    new Attribute("value", plist["AppLovin_id"].ToString()));
	    }

	    private static void SaveManifest()
	    {
		    var settings = new XmlWriterSettings
		    {
			    Indent = true,
			    IndentChars = "    "
		    };
		    using (var writer = XmlWriter.Create(Application.dataPath + ManifestPath, settings))
		    {
			    manifest.Save(writer);
				writer.Close();
		    }
		    AssetDatabase.ImportAsset("Assets" + ManifestPath);
	    }
		
	    private static XmlElement AddNodes(XmlElement parentNode, params Node[] subNodes)
	    {
		    foreach (var subNode in subNodes)
		    {
			    if (subNode.GetType() == typeof(Attribute))
			    {
				    var attr = (Attribute)subNode;
				    if (string.IsNullOrEmpty(attr.Xmlns))
					    parentNode.SetAttribute(attr.Name, attr.Value);
				    else
					    parentNode.SetAttribute(attr.Name, attr.Xmlns, attr.Value);
			    }
			    else if (subNode.GetType() == typeof(Element))
			    {
				    var element = (Element)subNode;
				    AddElement(parentNode, element.Name, element.Nodes);
			    }
		    }

		    return parentNode;
	    }

	    private static XmlElement AddElement(XmlNode parentNode, string elementName, params Node[] subNodes)
	    {
		    var newElement = manifest.CreateElement(elementName);

		    if (subNodes != null)
			    AddNodes(newElement, subNodes);
	        
		    parentNode.AppendChild(newElement);

		    return newElement;
	    }

	    private static XmlElement AddElement(string xpath, string elementName, params Node[] subNodes)
	    {
		    return AddElement(manifest.SelectSingleNode(xpath), elementName, subNodes);
	    }

	    private static XmlElement AddElement(string fullXpath, params Node[] subNodes)
	    {
		    var components = fullXpath.Split('/');
		    
		    if (components.Length > 1)
		    {
			    return AddElement(string.Join("/", components.Take(components.Length - 1).ToArray()), components.Last(), subNodes);
		    }
		    else if (components.Length == 1)
		    {
			    return AddElement(manifest, components.First(), subNodes);
		    }
		    else
		    {
			    return null;
		    }
	    }

	    private class Node
	    {
			public string Name { get; protected set; }
			public string Xmlns { get; protected set; }
	    }

	    private class Element : Node
	    {
		    public Node[] Nodes { get; private set; }

		    public Element(string name, params Node[] nodes)
		    {
			    Name = name;
			    Nodes = nodes;
		    }
	    }
	    
		private class Attribute : Node
		{
			public string Value { get; private set; }
	
			public Attribute(string name, string value)
			{
				Name = name;
				Xmlns = XmlnsAndroid;
				Value = value;
			}
			
			public Attribute(string name, string xmlns, string value)
			{
				Name = name;
				Xmlns = xmlns;
				Value = value;
			}
		}
    }
}
#endif
