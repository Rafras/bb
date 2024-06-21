using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoomBit.HyperCasual;

#if CORE_FIREBASE_SDK
using Coredian.Firebase;
#endif

public class HCSDKExample : MonoBehaviour
{
    //If app should have different behaviour while review mode, this property will be set to TRUE
    //for example if ads should be disabled in review mode we should disabled them when ReviewMode is FALSE
    public bool ReviewMode { get; set; }

    void Start()
    {
        SetEvent();
        HCSDK.StartHCSDK();
        
        //First Banner should be showed after HCSDK have mediation ads provider ready, example below
        if (HCSDK.InitializationComplete) HCSDK.ShowBanner();
        else HCSDK.InitializationStartEvent += () => { StartCoroutine(WaitBeforeShowBannerOnStart()); };
        
        #if CORE_FIREBASE_REMOTE_CONFIG
        //Always when game fetch Remote Config update review mode
        if (Core.GetService<IFirebaseRemoteConfigService>() != null &&
            Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded)
            CheckReviewMode(new Dictionary<string, string>());

        if (Core.GetService<IFirebaseRemoteConfigService>() != null)
        {
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += CheckReviewMode;
        }
        #else
        CheckReviewMode(new Dictionary<string, string>());
        #endif
    }

    private void CheckReviewMode(IDictionary<string, string> obj)
    {
      //  ReviewMode = Application.version == HCSDK.GetRemoteParameter("review_mode");
    }
    
    private IEnumerator WaitBeforeShowBannerOnStart()
    {
        yield return new WaitForEndOfFrame();
        HCSDK.ShowBanner();
    }

    private void SetEvent()
    {
        HCSDK.VideoSuccessEvent += OnVideoSuccess;
        HCSDK.VideoFailEvent += OnVideoFail;
        HCSDK.VideoCancelEvent += OnVideoCancel;

        HCSDK.InterstitialCloseEvent += OnInterstitialClose;
        HCSDK.InterstitialFailEvent += OnInterstitialFail;
    }

    private void OnVideoSuccess()
    {
        //Rewarded was watched
        //Unmute Sound
        //Grant Reward to user
    }
    
    private void OnVideoFail()
    {
        //Rewarded fail, something goes wrong on ads mediation side
        //Unmute Sound
    }
    
    private void OnVideoCancel()
    {
        //Rewarded was close before end
        //Unmute Sound
    }
    
    private void OnInterstitialClose()
    {
        //Interstitial was close
        //Unmute Sound
    }
    
    private void OnInterstitialFail()
    {
        //Interstitial fail, something goes wrong on ads mediation side
        //Unmute Sound
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height*0.1f), "Review Mode: "+(ReviewMode?"ON":"OFF"));
        
        //Show Rewarded
        if (GUI.Button(
            new Rect(Screen.width * 0.1f, Screen.height * 0.1f, Screen.width * 0.3f, Screen.height * 0.2f),
            "Show Rewarded"))
        {
            //By using HCSDK.IsRewardedReady we can check if Rewarded is ready
            if (HCSDK.IsRewardedReady("Test_Placement"))
            {
                //Mute Sound
                HCSDK.ShowRewarded("Test_Placement");
            }
        }

        //Show Interstitial
        if (GUI.Button(
            new Rect(Screen.width * 0.6f, Screen.height * 0.1f, Screen.width * 0.3f, Screen.height * 0.2f),
            "Show Interstitial"))
        {
            //By using HCSDK.IsInterstitialReady we can check if Interstitial is ready
            if (HCSDK.IsInterstitialReady("Test_Placement"))
            {
                //Mute Sound
                HCSDK.ShowInterstitial("Test_Placement");
            }
        }

        //Show Banner
        if (GUI.Button(
            new Rect(Screen.width * 0.1f, Screen.height * 0.4f, Screen.width * 0.3f, Screen.height * 0.2f),
            "Show Banner"))
        {
            HCSDK.ShowBanner();
        }

        //Hide Banner
        if (GUI.Button(
            new Rect(Screen.width * 0.6f, Screen.height * 0.4f, Screen.width * 0.3f, Screen.height * 0.2f),
            "Hide Banner"))
        {
            HCSDK.HideBanner();
        }

    }

}
