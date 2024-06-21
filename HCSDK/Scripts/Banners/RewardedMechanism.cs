namespace BoomBit.HyperCasual
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;

    using Coredian.CrossPromo;

    using Coredian.Firebase;

    public class RewardedMechanism : MonoBehaviour
    {
        public event Action VideoCachedSuccessfulEvent;
        public event Action<string> VideoDisplayEvent;
        public event Action VideoSuccessEvent;
        public event Action<string> VideoFailEvent;
        public event Action VideoCanceledEvent;
        public event Action VideoClickEvent;

        protected virtual void OnVideoCachedSuccessfulEvent()
        {
#if HCSDK_DIAGNOSTICS
         // stops measuring the display time between the start of mediation and the ad being displayed on the device

         Log.Info("Stops counting time.");
         Coredian.Diagnostics.Performance.EndExecution("counting_time");
         TimeAverage.CalculateAverage();

#endif
            VideoCachedSuccessfulEvent?.Invoke();
        }

        protected virtual void OnVideoDisplayEvent(string placement)
        {
            VideoDisplayEvent?.Invoke(placement);
        }

        protected virtual void OnVideoSuccessEvent()
        {
            VideoSuccessEvent?.Invoke();
        }

        protected virtual void OnVideoFailEvent(string placement)
        {
            VideoFailEvent?.Invoke(placement);
        }

        protected virtual void OnVideoCanceledEvent()
        {
            VideoCanceledEvent?.Invoke();
        }

        protected virtual void OnVideoClickEvent()
        {
            VideoClickEvent?.Invoke();
        }
        
        private int normalAdsCounter = 0;
        private int cpRewardedInterval = 0;
        private Rewarded cpRewarded=null;
        private int cpRewardedCounter = 0;
        private int cpRewardedMaxNumber = -1;

        private string lastPlacement = "";
        private bool firstNonCPFired = false;

        
        private IAdsProvider adsProvider;
        public IAdsProvider AdsProvider
        {
            set
            {
                if (adsProvider == null)
                {
                    adsProvider = value;
                    adsProvider.VideoCachedSuccessfulEvent += OnVideoCachedSuccessfulEvent;
                    adsProvider.VideoDisplayEvent += OnVideoDisplayEvent;
                    adsProvider.VideoSuccessEvent += OnVideoSuccessEvent;
                    adsProvider.VideoFailEvent += OnVideoFailEvent;
                    adsProvider.VideoCanceledEvent += OnVideoCanceledEvent;
                    adsProvider.VideoClickEvent += OnVideoClickEvent;
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
            if (HCSDK.GetParameter("CpRewardedPossibleFirst") != "" && HCSDK.GetParameter("CpRewardedPossibleFirst") == "TRUE")
            {
                firstNonCPFired = true;
            }
            if (HCSDK.GetParameter("XpigRewardedInterval") != "")
            {
                if (!int.TryParse(HCSDK.GetParameter("XpigRewardedInterval"), out cpRewardedInterval))
                {
                    cpRewardedInterval = 0;
                }
                else if (cpRewardedInterval > 0 && cpRewarded == null && firstNonCPFired)
                {
                    LoadCpRewarded();
                }
            }

            if (HCSDK.GetParameter("XpigRewardedMaxNumber") != "")
            {
                if (!int.TryParse(HCSDK.GetParameter("XpigRewardedMaxNumber"), out cpRewardedMaxNumber))
                {
                    cpRewardedMaxNumber = -1;
                }
            }

        }

        public void ShowRewarded(string placement)
        {
            lastPlacement = placement;

            if ((cpRewardedInterval > 0) && ((normalAdsCounter+1) >= cpRewardedInterval) && IsCpRewardedReady() && IsCpRewardedAllowed())
            {
                normalAdsCounter = 0;
                cpRewardedCounter++;
                cpRewarded.Show();
            }
            else if (adsProvider != null && adsProvider.IsVideoAvailable(placement))
            {
                normalAdsCounter++;
                firstNonCPFired = true;
                adsProvider?.ShowVideo(placement);
            }
            else if ((cpRewardedInterval >= 0) && IsCpRewardedReady() && IsCpRewardedAllowed())
            {
                normalAdsCounter = 0;
                cpRewardedCounter++;
                cpRewarded.Show();
            }
            else StartCoroutine(WaitBeforeFail());
        }

        public bool IsRewardedReady(string placement, bool withCP)
        {
            if (adsProvider != null && adsProvider.IsVideoAvailable(placement)) return true;
            if (firstNonCPFired)
            {
                if (withCP)
                {
                    if (cpRewarded == null) LoadCpRewarded();
                }

                if (cpRewarded != null) return true;
            }

            return false;
        }
        
        public bool IsRewardedShowed()
        {
            if (adsProvider != null) return adsProvider.IsVideoShowed();
            else return false;
        }
        
        private void LoadCpRewarded()
        {
            if (cpRewardedInterval < 0) return; 
                
            cpRewarded=Core.GetService<CrossPromoService>().GetAd(AdUnit.Rewarded) as Rewarded;
            if (cpRewarded != null)
            {
                cpRewarded.Clicked += OnVideoClickEvent;
                cpRewarded.Shown += OnCpRewardedShown;
                cpRewarded.Hidden += OnCpRewardedHidden;
                cpRewarded.ShowFailed += OnCpRewardedFail;
            }
        }

        private void DestroyCpRewarded()
        {
            if (cpRewarded != null)
            {
                cpRewarded.Clicked -= OnVideoClickEvent;
                cpRewarded.Shown -= OnCpRewardedShown;
                cpRewarded.Hidden -= OnCpRewardedHidden;
                cpRewarded.ShowFailed -= OnCpRewardedFail;

                cpRewarded.Destroy();
                cpRewarded = null;
            }
        }
        
        private bool IsCpRewardedReady()
        {
            if (cpRewarded==null) LoadCpRewarded();
            if (cpRewarded != null)
            {
                return true;
            }
			
            return false;
        }

        private bool IsCpRewardedAllowed()
        {
            if (cpRewardedMaxNumber >= 0 && cpRewardedCounter >= cpRewardedMaxNumber) return false;
            return true;
        }

        private void OnCpRewardedShown()
        {
            OnVideoDisplayEvent(lastPlacement);
        }
        
        private void OnCpRewardedHidden()
        {
            StartCoroutine(WaitBeforeCpRewardedHidden());
        }

        private IEnumerator WaitBeforeCpRewardedHidden()
        {
            yield return new WaitForEndOfFrame();
            
            if (cpRewarded.GrantReward) OnVideoSuccessEvent();
            else OnVideoCanceledEvent();

            DestroyCpRewarded();
            LoadCpRewarded();
        }

        private void OnCpRewardedFail()
        {
            StartCoroutine(WaitBeforeCpRewardedFail());
        }

        private IEnumerator WaitBeforeCpRewardedFail()
        {
            yield return new WaitForEndOfFrame();
            
            OnVideoFailEvent(lastPlacement);
            DestroyCpRewarded();
            LoadCpRewarded();
        }
        
        private IEnumerator WaitBeforeFail()
        {
            yield return new WaitForEndOfFrame();
            OnVideoFailEvent(lastPlacement);
        }

        private void DeinitializeCrossPromo()
        {
            DestroyCpRewarded();
        }

    }
}
