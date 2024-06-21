#if UNITY_EDITOR && UNITY_ANDROID
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BoomBit.HyperCasual.Editor
{
	public class HCAndroidPreprocess : IPreprocessBuildWithReport
	{
		public int callbackOrder
		{
			get { return 0; }
		}

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.Android) return;

			AndroidManifestTool.UpdateManifest();
			AssetDatabase.SaveAssets();
		}
	}
}
#endif
