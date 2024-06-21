using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coredian.CrossPromo;
using System;

namespace BoomBit.HyperCasual
{
	using Coredian.Firebase;

	public class InterstitialMechanism : MonoBehaviour
	{
		public event Action InterstitialCloseEvent;
		public event Action<string> InterstitialDisplayEvent;
		public event Action InterstitialClickEvent;
		public event Action InterstitialFailEvent;

		private void OnInterstitialClose()
		{
			if (showBannerAgain)
			{
				HCSDK.ShowBanner();
				showBannerAgain = false;
			}
			if (InterstitialCloseEvent != null) InterstitialCloseEvent();
		}
		private void OnInterstitialDisplay(string placement)
		{
			if (InterstitialDisplayEvent != null) InterstitialDisplayEvent(placement);
		}
		private void OnInterstitialDisplay()
		{
			if (BannerMechanism.BannerOnScreen)
			{
				HCSDK.HideBanner();
				showBannerAgain = true;
			}
			if (InterstitialDisplayEvent != null) InterstitialDisplayEvent(lastPlacement);
		}
		
		private void OnInterstitialClick()
		{
			if (InterstitialClickEvent != null) InterstitialClickEvent();
		}
		private void OnInterstitialFail()
		{
			if (InterstitialFailEvent != null) InterstitialFailEvent();
		}
		
		private void OnInterstitialCpClose()
		{
			StartCoroutine(WaitBeforeInterstitialCpClose());
		}

		private IEnumerator WaitBeforeInterstitialCpClose()
		{
			yield return new WaitForEndOfFrame();
			
			DestroyCpInterstitial();
			
			if (showBannerAgain)
			{
				HCSDK.ShowBanner();
				showBannerAgain = false;
			}
			
			if (InterstitialCloseEvent != null) InterstitialCloseEvent();
		}
		
		private void OnInterstitialCpFail()
		{
			StartCoroutine(WaitBeforeInterstitialCpFail());
		}
		
		private IEnumerator WaitBeforeInterstitialCpFail()
		{
			yield return new WaitForEndOfFrame();
			
			DestroyCpInterstitial();
			if (InterstitialFailEvent != null) InterstitialFailEvent();
		}

		private int normalAdsCounter = 0;
		private int cpInterstitialInterval = 0;
		private IAd cpInterstitial=null;
		private int cpInterstitialCounter = 0;
		private int cpInterstitialMaxNumber = -1;

		private string lastPlacement = "";

		private bool showBannerAgain = false;
		private bool firstNonCPFired = false;
		
		private IAdsProvider adsProvider;
		public IAdsProvider AdsProvider
		{
			set
			{
				if (adsProvider == null)
				{
					adsProvider = value;
					adsProvider.EndOfAdEvent += OnInterstitialClose;
					adsProvider.InterstitialClickedEvent += OnInterstitialClick;
					adsProvider.InterstitialDisplayEvent += OnInterstitialDisplay;
					adsProvider.AdFailEvent += OnInterstitialFail;
				}
			}
		}
		
		public void Initialize()
		{
			if(Core.GetService<IFirebaseRemoteConfigService>() != null && Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded)
				GetConfiguration(new Dictionary<string, string>());
    
			if(Core.GetService<IFirebaseRemoteConfigService>() != null)
				Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += GetConfiguration;

			Core.WillDeinitialize += DeinitializeCrossPromo;
		}

		private void GetConfiguration(IDictionary<string, string> obj)
		{
			if (HCSDK.GetParameter("CpInterstitialPossibleFirst") != "" && HCSDK.GetParameter("CpInterstitialPossibleFirst") == "TRUE")
			{
				firstNonCPFired = true;
			}
			if (HCSDK.GetParameter("XpigInterstitialInterval") != "")
			{
				if (!int.TryParse(HCSDK.GetParameter("XpigInterstitialInterval"), out cpInterstitialInterval))
				{
					cpInterstitialInterval = 0;
				}
				else if (cpInterstitialInterval > 0 && cpInterstitial == null && firstNonCPFired)
				{
					LoadCpInterstitial();
				}
			}
			
			if (HCSDK.GetParameter("XpigInterstitialMaxNumber") != "")
			{
				if (!int.TryParse(HCSDK.GetParameter("XpigInterstitialMaxNumber"), out cpInterstitialMaxNumber))
				{
					cpInterstitialMaxNumber = -1;
				}
			}
		}

		public void StartCachingInterstitial()
		{
			if (adsProvider!=null) adsProvider.StartCachingInterstitial();
		}

		public bool IsInterstitialReady(string placement)
		{
			if (adsProvider != null && adsProvider.IsInterstitialReady(placement)) return true;
			
			if (firstNonCPFired)
			{
				if (cpInterstitial == null) LoadCpInterstitial();
				if (cpInterstitial != null) return true;
			}

			
			return false;
		}

		private void LoadCpInterstitial()
		{
			if (cpInterstitialInterval < 0) return;
				
			cpInterstitial=Core.GetService<CrossPromoService>().GetAd(AdUnit.Interstitial);
			if (cpInterstitial != null)
			{
				cpInterstitial.Clicked += OnInterstitialClick;
				cpInterstitial.Shown += OnInterstitialDisplay;
				cpInterstitial.Hidden += OnInterstitialCpClose;
				cpInterstitial.ShowFailed += OnInterstitialCpFail;
			}
		}

		private void DestroyCpInterstitial()
		{
			if (cpInterstitial != null)
			{
				cpInterstitial.Clicked -= OnInterstitialClick;
				cpInterstitial.Shown -= OnInterstitialDisplay;
				cpInterstitial.Hidden -= OnInterstitialCpClose;
				cpInterstitial.ShowFailed -= OnInterstitialCpFail;

				cpInterstitial.Destroy();
				cpInterstitial = null;
			}
		}

		public void ShowInterstitial(string placement)
		{
			lastPlacement = placement;

			if ((cpInterstitialInterval > 0) && ((normalAdsCounter+1) >= cpInterstitialInterval) && IsCpInterstitialReady() && IsCpInterstitialAllowed())
			{
				normalAdsCounter = 0;
				firstNonCPFired = true;
				cpInterstitialCounter++;
				cpInterstitial.Show();
			}
			else if (adsProvider != null && adsProvider.IsInterstitialReady(placement))
			{
				normalAdsCounter++;
				adsProvider.ShowAdWithLocation(placement);
			}
			else if ((cpInterstitialInterval >= 0) && IsCpInterstitialReady() && IsCpInterstitialAllowed())
			{
				normalAdsCounter = 0;
				cpInterstitialCounter++;
				cpInterstitial.Show();
			}
			else StartCoroutine(WaitBeforeFail());
		}

		private bool IsCpInterstitialReady()
		{
			if (cpInterstitial==null) LoadCpInterstitial();
			if (cpInterstitial != null)
			{
				return true;
			}
			
			return false;
		}
		
		private bool IsCpInterstitialAllowed()
		{
			if (cpInterstitialMaxNumber >= 0 && cpInterstitialCounter >= cpInterstitialMaxNumber) return false;
			return true;
		}

		private IEnumerator WaitBeforeFail()
		{
			yield return new WaitForEndOfFrame();
			OnInterstitialFail();
		}

		private void DeinitializeCrossPromo()
		{
			DestroyCpInterstitial();
		}
	}

}
