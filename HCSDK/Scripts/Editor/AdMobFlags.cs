#if UNITY_EDITOR && UNITY_ANDROID
namespace BoomBit.HyperCasual.Editor
{
	using System.Collections.Generic;

	using Coredian.BuildTools.Android.Editor.Configuration;
	using Coredian.BuildTools.Android.Editor.Manifest;

	// ReSharper disable once UnusedType.Global
	public class AdMobMetadata : IApplicationMetadata
	{
		/// <inheritdoc />
		public IEnumerable<Metadata> GetApplicationMetadata() =>
			new List<Metadata>
			{
				new Metadata()
				{
					Name = "com.google.android.gms.ads.flag.OPTIMIZE_INITIALIZATION",
					Value = "true",
					Resource = null
				},
				new Metadata()
				{
					Name = "com.google.android.gms.ads.flag.OPTIMIZE_AD_LOADING",
					Value = "true",
					Resource = null
				}
			};
	}
}
#endif
