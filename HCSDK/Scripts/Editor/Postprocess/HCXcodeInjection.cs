#if UNITY_IOS

using System;
using System.Collections.Generic;
using System.IO;

using Coredian.BuildTools.Apple.Editor;
using Coredian.BuildTools.Apple.Editor.XCodeInjection;
using Coredian.Editor;

using HCExtension;

using PlistCS;

using UnityEditor.iOS.Xcode;

using UnityEngine;

/// <summary>
/// Xcode injection.
/// </summary>
public class HCXcodeInjection : IBuildProperties, IUrlSchemes, IQueriedUrlSchemes
{
	private readonly string whitelistPath = Application.dataPath+@"/HCSDK/Configuration/Resources/Whitelist/default.csv";
	private readonly string appWhitelistPath = Application.dataPath+@"/HCSDK/Configuration/Resources/Whitelist/"+Application.identifier+".csv";

	/// <inheritdoc />
	public IReadOnlyList<PbxBuildProperty> GetBuildProperties()
	{
		var propertiesToUpdate = new List<PbxBuildProperty>
		{
			new PbxBuildProperty{
				key = "GCC_PREPROCESSOR_DEFINITIONS",
				target = UnityTarget.UnityMainTarget,
				PropertiesToAdd = { "AFSDK_SHOULD_SWIZZLE=1" },
				PropertiesToRemove = { null }
			}, 
			new PbxBuildProperty{
				key = "GCC_PREPROCESSOR_DEFINITIONS",
				target = UnityTarget.UnityFrameworkTarget,
				PropertiesToAdd = { "AFSDK_SHOULD_SWIZZLE=1" },
				PropertiesToRemove = { null }
			}
		};
		
		return propertiesToUpdate; 
	}

	/// <inheritdoc />
	public IReadOnlyList<UrlScheme> GetUrlSchemes()
	{
		string localConfig = Application.dataPath+"/HCSDK/Configuration/Resources/iOS/iOSConfiguration.xml";
		Dictionary<string, object> configuration = (Dictionary<string, object>)Plist.readPlist (localConfig);

		var schemes = new List<UrlScheme>
		{
			new UrlScheme
			{
				identifier = Application.identifier,
				schemes = new List<string> { "aso" + configuration ["appId"] },
				urlSchemeRole = UrlSchemeRole.Editor
			},
			new UrlScheme
			{
				identifier = Application.identifier,
				schemes = new List<string> { "NewPluginManager" + configuration ["appId"] },
				urlSchemeRole = UrlSchemeRole.Editor
			}
		};
		
		if (configuration.ContainsKey("unique_app_name"))
		{
			schemes.Add(new UrlScheme
			{
				identifier = Application.identifier,
				schemes = new List<string> { configuration["unique_app_name"].ToString() },
				urlSchemeRole = UrlSchemeRole.Editor
			});
		}
		
		var sr=new StreamReader((File.Exists(appWhitelistPath)?appWhitelistPath:whitelistPath));
		while (!sr.EndOfStream)
		{
			string[] whitelist=sr.ReadLine().SplitWithoutEmpty(',');
			
			if (whitelist.Length==3)
			{
				if (whitelist[2]==Application.identifier && !whitelist[0].StartsWith ("NewPluginManager"))
				{
					schemes.Add(new UrlScheme
					{
						identifier = Application.identifier,
						schemes = new List<string> { whitelist[0] },
						urlSchemeRole = UrlSchemeRole.Editor
					});
					break;
				}
			}
		}
		
		return schemes;
	}

	/// <inheritdoc />
	public IReadOnlyList<string> GetQueriedUrlSchemes()
	{
		var schemes = new List<string>();

		var sr=new StreamReader((File.Exists(appWhitelistPath)?appWhitelistPath:whitelistPath));
		while (!sr.EndOfStream)
		{
			string[] whitelist = sr.ReadLine().SplitWithoutEmpty(',');

			if (whitelist.Length > 0)
			{
				schemes.Add(whitelist[0]);
			}
		}

		return schemes.AsReadOnly();
	}
}
#endif
