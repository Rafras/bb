using UnityEngine.Networking;

namespace HC.Editor
{
	using UnityEditor;
	using UnityEngine;
	using System.Collections.Generic;
	using UnityEditor.Callbacks;
	using BoomBit.HyperCasual;

	using System.IO;

	[InitializeOnLoad]
	public static class SchemeUpdater
	{
		private static UnityWebRequest www=null;
		private static string whitelistPath="";

		static SchemeUpdater()
		{
			UpdateWhitelist(); 
		}

		[PostProcessSceneAttribute]
		public static void OnPostprocessScene()
		{
			Debug.Log("SchemeUpdater :: OnPostprocessScene");
			UpdateWhitelist();
		}

		private static void UpdateWhitelist()
		{
			#if UNITY_IOS
			if (www!=null) return;

			GetWhitelistConfigUrl();

			EditorApplication.update+=Update;
			www=new UnityWebRequest(GetWhitelisctConfigForApp());
			www.downloadHandler = new DownloadHandlerBuffer();
			www.SendWebRequest();
			#endif
		}

		private static string GetWhitelistConfigUrl()
		{
			whitelistPath=Application.dataPath+@"/HCSDK/Configuration/Resources/Whitelist/default.csv";
			return "https://interactiveicons.pluginmanagerconfig1.info/Schemes/com.hc.csv";
		}

		private static string GetWhitelisctConfigForApp()
		{
			return "https://interactiveicons.pluginmanagerconfig1.info/Schemes/"+PlayerSettings.applicationIdentifier+".csv";
		}

		private static void Update()
		{
			
			if (www==null) return;
			if (www.isDone)
			{
				if(www.error == null) 
				{
					using (StreamWriter sw=new StreamWriter(whitelistPath))
					{
						sw.Write(www.downloadHandler.text);
					}
				} 
				else if (www.url == null)
				{
					Debug.LogWarning("SchemeUpdater :: Update :: There is no url == "+www.url);
				}
				
				if (www.error != null && www.url.EndsWith(PlayerSettings.applicationIdentifier+".csv"))
				{	
					www=new UnityWebRequest(GetWhitelistConfigUrl());
					www.downloadHandler = new DownloadHandlerBuffer();
					www.SendWebRequest();
				}
				else
				{
					www=null;
					EditorApplication.update-=Update;
				}
			}
		}
	}
}
