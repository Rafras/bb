#if UNITY_EDITOR && UNITY_ANDROID
namespace BoomBit.HyperCasual.Editor
{
	using System.Collections.Generic;

	using Coredian.BuildTools.Android.Editor.Configuration;
	using Coredian.BuildTools.Android.Editor.Manifest;
	using Coredian.StoreVariants;

	public class ChannelSamsung : IApplicationMetadata
	{
		/// <inheritdoc />
		public IEnumerable<Metadata> GetApplicationMetadata()
		{
			var metadata = new List<Metadata>();
			
			var variantSelection = VariantSelections.FindOrCreateInstance();
			var currentVariant = variantSelection.GetStoreForPlatform(VariantSelections.currentPlatform).ToLower();

			if (currentVariant == "samsung")
			{
				metadata.Add(new Metadata()
				{
					Name = "CHANNEL",
					Value = "Samsung",
					Resource = null
				});
			}
			else if (currentVariant == "amazon")
			{
				metadata.Add(new Metadata()
				{
					Name = "CHANNEL",
					Value = "Amazon",
					Resource = null
				});
			} 
			
			return metadata;
		}
	}
}
#endif
