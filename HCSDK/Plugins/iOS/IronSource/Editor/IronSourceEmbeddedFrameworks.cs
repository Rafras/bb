#if UNITY_IOS
using System.Collections.Generic;
using Coredian.BuildTools.Apple.Editor.XCodeInjection;

public class IronSourceEmbeddedFrameworks : IEmbeddedFrameworks
{
    /// <inheritdoc />
    public IReadOnlyList<string> GetEmbeddedFrameworks() => new List<string> { "HyprMX", "LiftoffAds", "OMSDK_Smaato", "OMSDK_Ogury", "OMSDK_Appodeal", "IASDKCore", "OMSDK_Bigosg", "MolocoCustomAdapter", "MolocoSDK", "DTBiOSSDK" };
}
#endif