namespace BoomBit.HyperCasual
{
	using System;

	public interface IAdsProvider
	{
		event Action AdFailEvent;
		event Action EndOfAdEvent;
		
		event Action InterstitialCachedSuccessfulEvent;
		event Action InterstitialCachedFailEvent;
		event Action<string> InterstitialDisplayEvent;
		event Action InterstitialClickedEvent;
		event Action GetInterstitialRequestEvent;
		
		event Action VideoCachedSuccessfulEvent;
		event Action VideoCachedFailEvent;
		event Action<string> VideoDisplayEvent;
		event Action VideoSuccessEvent;
		event Action<string> VideoFailEvent;
		event Action VideoCanceledEvent;
		event Action VideoClickEvent;
		event Action GetVideoRequestEvent;
		
		event Action BannerShowEvent;
		event Action BannerHideEvent;
		event Action BannerIsNotShownEvent;
		event Action<string> ClickOnBannerEvent;

		void Initialize(bool autoInterstitialCaching);
		void SetConsent(bool hasConsent);

		void StartCachingInterstitial();
		bool IsInterstitialReady(string placement);
		void ShowAdWithLocation(string placement);

		bool IsVideoAvailable(string placement);
		bool IsVideoShowed();
		void ShowVideo(string placement);

		void ShowBanner();
		void HideBanner();
	}
}
