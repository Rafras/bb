using System;
using System.Collections;

using BoomBit.HyperCasual;

using UnityEngine;

using Coredian.CrossPromo;

public class BannerMechanism : MonoBehaviour
{
	[Obsolete("This event is obsolete. Use 'BannerShown' instead")]
	public event Action BannerShowedEvent
	{
		add => BannerShown += value;
		remove => BannerShown -= value;
	}

	/// <summary>
	/// Event executed every time a banner is shown
	/// </summary>
	public event Action BannerShown;

	public static bool BannerOnScreen => instance.bannerOnScreen;

	private bool bannerOnScreen = false;
	
	private bool bannerShouldBeShowed=false;

	private int count;

	private int interval;

	private MonoBehaviour mono;

	private float timer;

	private float refreshTimer;

	private bool timerRunning;

	private float xpigBannerTime = 0f;

	private bool xpigOnScreen;

	private bool interstitialOnScreen;

	private bool moreGamesOnScreen;

	private bool bannerQueued;

	private IAdsProvider adsProvider;

	private static BannerMechanism instance;

	public IAdsProvider AdsProvider
	{
		get => adsProvider;
		internal set
		{
			if (adsProvider != null) adsProvider.BannerIsNotShownEvent -= CatchBanner;
			adsProvider = value;
			if (adsProvider != null) adsProvider.BannerShowEvent += CatchBanner;
		}
	}

	/// <summary>
	/// Reference to the currently displayed cross promo banner.
	/// This reference should be null after hiding the banner
	/// </summary>
	private IAd currentlyDisplayedCrossPromoBanner;

	public static BannerMechanism Instance => instance;

	private void Awake()
	{
		instance = this;
		SetAdEvents();
	}
	
	private void SetAdEvents()
	{
		HCSDK.InterstitialCloseEvent += AfterFullscreenAd;
		HCSDK.InterstitialFailEvent += AfterFullscreenAd;
		HCSDK.VideoCancelEvent += AfterFullscreenAd;
		HCSDK.VideoFailEvent += AfterFullscreenAd;
		HCSDK.VideoSuccessEvent += AfterFullscreenAd;
	}

	private void AfterFullscreenAd()
	{
		interstitialOnScreen = false;
		if (bannerShouldBeShowed && !bannerOnScreen)
		{
			ShowBanner();
		}
	}
	public void AfterMoreGames()
	{
		moreGamesOnScreen = false;
		if (bannerShouldBeShowed && !bannerOnScreen)
		{
			ShowBanner();
		}
	}

	private void Update()
	{
		if (timerRunning && Time.realtimeSinceStartup - timer > refreshTimer) ReloadBanners();
	}

	private void ReloadBanners()
	{
		if (interval != 0)
		{
			HideBanner(false);
			ShowBanner();
		}
	}

	public void ShowBanner()
	{
		if (!HCSDK.InitializationComplete)
		{
			return;
		}
		if (bannerOnScreen) return;
		if (interstitialOnScreen) return;
		
		HideBanner();
		bannerShouldBeShowed = true;

		
		if (string.IsNullOrEmpty(HCSDK.GetParameter("BannerRefreshTime")))
		{
			refreshTimer = 20;
		}
		else
		{
			try
			{
				refreshTimer = float.Parse(HCSDK.GetParameter("BannerRefreshTime"));
			}
			catch (Exception e)
			{
				Debug.LogWarning("xpig banner time refresh parse error: " + e.Message);
			}
		}

		timerRunning = true;
		if (refreshTimer == 0) refreshTimer = 20;

		timer = Time.realtimeSinceStartup;

		if (string.IsNullOrEmpty(HCSDK.GetParameter("XpigBannerInterval")))
		{
			interval = 0;
		}
		else
		{
			try
			{
				interval = int.Parse(HCSDK.GetParameter("XpigBannerInterval"));
			}
			catch (Exception e)
			{
				Debug.LogWarning("jes banner config parse error: " + e.Message);
				interval = 0;
			}
		}

		if (interval > 0 && count % interval == 0)
		{
			xpigOnScreen = true;
			
			if (currentlyDisplayedCrossPromoBanner == null)
			{
				currentlyDisplayedCrossPromoBanner = Core.GetService<CrossPromoService>().GetAd(AdUnit.Banner);
			}
			
			if (currentlyDisplayedCrossPromoBanner != null)
			{
				currentlyDisplayedCrossPromoBanner.Shown += BannerShown;
				currentlyDisplayedCrossPromoBanner?.Show();
			}

			if (adsProvider != null) adsProvider.HideBanner();
		}
		else
		{
			if (adsProvider == null)
			{
				if (bannerQueued)
				{
					return;
				}
				else
				{
					bannerQueued = true;
					StartCoroutine(BannerQueue());
				}
				
				return;
			}
			
			adsProvider.ShowBanner();
		}
		
		bannerOnScreen = true;
		count++;
	}

	IEnumerator BannerQueue()
	{
		while (adsProvider == null)
		{
			yield return new WaitForSecondsRealtime(1);
		}
		
		adsProvider.ShowBanner();
		bannerQueued = false;
	}
	
	public void FullscreenAdShowed()
	{
		interstitialOnScreen = true;
		if (bannerOnScreen)
		{
			HideBanner(false);
		}
	}

	public void MoreGamesShowed()
	{
		moreGamesOnScreen = true;
		if (bannerOnScreen)
		{
			HideBanner(false);
		}
	}

	public void HideBanner(bool userAction=true)
	{
		if (xpigOnScreen)
		{
			xpigBannerTime+=Time.realtimeSinceStartup - timer;
		}

		if (userAction)
		{
			bannerShouldBeShowed = false;
		}
		bannerOnScreen = false;
		xpigOnScreen = false;
		timerRunning = false;

		if (currentlyDisplayedCrossPromoBanner != null) currentlyDisplayedCrossPromoBanner.Shown -= BannerShown;
		if (xpigBannerTime > refreshTimer)
		{
			currentlyDisplayedCrossPromoBanner?.Destroy();
			currentlyDisplayedCrossPromoBanner = null;
			xpigBannerTime = 0f;
		}
		else
		{
			currentlyDisplayedCrossPromoBanner?.Hide();
		}
		
#if !UNITY_EDITOR
		if (adsProvider == null) return;

		adsProvider.HideBanner();
#endif
	}

	private void CatchBanner()
	{
		if (!xpigOnScreen) return;
		if (adsProvider == null) return;

		adsProvider.HideBanner();
	}
}
