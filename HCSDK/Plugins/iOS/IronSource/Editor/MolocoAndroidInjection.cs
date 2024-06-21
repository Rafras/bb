#if UNITY_ANDROID
using System.Collections.Generic;

using Coredian.BuildTools.Android.Editor.Configuration;

public class MolocoAndroidInjection : IGradleInfoOptionalProvider
{
	/// <inheritdoc />
	public IReadOnlyList<GradleInfo> GetGradleInfos()
	{
#if UNITY_2022_1_OR_NEWER
		var list = new List<GradleInfo>();

		var molocoGradleInfo = new GradleInfo();
		molocoGradleInfo.dependencies = new List<Dependency>
		{
			new Dependency()
			{
				@group = "com.moloco.sdk.adapters",
				name = "ironsource",
				type = DependencyType.Default,
				version = "1.7.0.0"
			}
		};
		
		list.Add(molocoGradleInfo);
		return list;
#else
		var list = new List<GradleInfo>();

		var molocoGradleInfo = new GradleInfo();
		molocoGradleInfo.dependencies = new List<Dependency>
		{
			new Dependency()
			{
				@group = "com.moloco.sdk.adapters",
				name = "ironsource",
				type = DependencyType.Default,
				version = "1.5.0.0"
			}
		};
		
		list.Add(molocoGradleInfo);
		return list;
#endif
	}
}
#endif
