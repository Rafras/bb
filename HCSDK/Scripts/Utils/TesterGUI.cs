using System;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using BoomBit.HyperCasual;

using Coredian;
using Coredian.CrossPromo;
using Coredian.Firebase;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TesterGUI : MonoBehaviour
{
	public static TesterGUI Instance;

	/// <summary>
	/// Public for the Editor. 
	/// </summary>
	public bool EnableGUI = true;

	/// <summary>
	/// Sequence to activate the GUI, should be like: "TL;BL;BR;TR;"
	/// - this means Top-Left; Bottom-Left; Bottom-Right; Top-Right; - must use semicolon after each one.
	/// The above means you have to click corners - Top Left, Bottom Left, Bottom Right and Top Right consecutively
	/// </summary>
	public string Sequence = "BL;BR;TR;TL;BR";

	public float CornerSize = 0.2f; // size of invisible corner, size is screen portion (0-1)

	string helpDocUrl = "https://docs.google.com/document/d/13v9CNGmXlvE-Ygo0TfyyBj6v0ctjUtwoS4Dhcwl7BzU/";

	const string TopLeft = "TL";
	const string TopRight = "TR";
	const string BottomLeft = "BL";
	const string BottomRight = "BR";
	const float buttonHeightRatio = 0.075f;
	
	private enum ScreenCorner
	{
		TopLeft,
		BottomLeft,
		TopRight,
		BottomRight,
		None,
	};

	Vector2 vTopLeft;
	Vector2 vBottomLeft;
	Vector2 vTopRight;
	Vector2 vBottomRight;
	ScreenCorner lastCorner = ScreenCorner.None;

	GUIStyle buttonStyle;
	GUIStyle backgroundBoxStyle;
	private Color backgroundColor = new Color(0, 0.5f, 0.5f, 0.8f);
	private float backgroundOpacity = 0.9f;
	private Rect topButtonsBackgroundRect;

	public bool Hidden = true;
	private bool keepEnabled;
	

	private ScreenOrientation orientation;

	private List<string> sequenceArray = new List<string>();
	/// <summary>
	/// The sequence read from input.
	/// </summary>
	private List<string> inputSequence = new List<string>();
	private List<string> last10Ads = new List<string>();

	bool initialized = false;
	bool stylesPrepared;

	private CrossPromoConfigNode config;

	/// <summary>
	/// Recalculate TesterGUI when screen size changes
	/// </summary>
	public static void RecalculateScreenSize()
	{
		if (Instance != null)
			Instance.RecalculateScreen();
	}

	/// <summary>
	/// Show the Tester GUI - this method is called on correct input sequence
	/// </summary>
	public void ShowTesterGUI()
	{
		Hidden = false;
		useGUILayout = true;
		Initialize();
	}
	
	void Awake()
	{
		// if another TesterGUI object is found - terminate self
		TesterGUI[] found = FindObjectsOfType<TesterGUI>();
		if (found != null && found.Length > 1)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		orientation = Screen.orientation;
		DontDestroyOnLoad(this);
	}

	void OnDisable()
	{
		if (!Hidden)
			HideTesterGUI();
	}

	void Start()
	{
		string pattern = @"(TL;|TR;|BL;|BR;)+";

		Sequence = CheckSemicolonAtSequenceEnd(Sequence);
		Sequence = Sequence.ToUpperInvariant();
		Sequence = ConvertSequence(Sequence);

		Regex rgx = new Regex(pattern);
		MatchCollection matches = rgx.Matches(Sequence);

		if (matches.Count == 0)
		{
			Debug.LogError("Sequence is empty! You must enter a sequence!");
		}
		// matches[0] contains the whole sequence if it is valid
		else if (matches[0].ToString().CompareTo(Sequence) != 0)
		{
			Debug.LogError("Sequence is invalid. Not possible to unlock screen with this sequence.");
		}
		else
		{
			if (Sequence.EndsWith(";"))
				Sequence = Sequence.TrimEnd(';');
			sequenceArray = new List<string>(Sequence.Split(';'));
		}

		CalculateScreenCorners();
		topButtonsBackgroundRect = new Rect(0, 0, Screen.width, 2 * buttonHeightRatio * Screen.height);
		useGUILayout = !Hidden;

		GUI.depth = 0;
		config = Core.Config.GetConfigNode<CrossPromoConfigNode>("CrossPromo");
	}

	void Update()
	{
		if (EnableGUI)
		{
			if (orientation != Screen.orientation)
			{
				orientation = Screen.orientation;
				RecalculateScreen();
			}

			// in editor track mouse
			if (Application.isEditor || !Application.isMobilePlatform)
			{
				if (Input.GetMouseButtonDown(0))
					ResetTracking();
				else if (Input.GetMouseButton(0))
					TrackTouch(Input.mousePosition);
			}
			// on device track touch
			else if (Input.touchCount > 0)
			{
				switch (Input.GetTouch(0).phase)
				{
					case TouchPhase.Began:
						ResetTracking();
						break;
					case TouchPhase.Moved:
						TrackTouch(Input.GetTouch(0).position);
						break;
				}
			}
		}
	}

	private void OnGUI()
	{
		if (!EnableGUI)
			return;
		
		// show
		if (!Hidden)
		{
			if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * 0f, Screen.width * .2f, Screen.height * .1f),
			               "Show Video"))
			{
				HCSDK.ShowRewarded("DefaultRewardedVideo");
			}
			if (GUI.Button(new Rect(Screen.width * .2f, Screen.height * 0f, Screen.width * .2f, Screen.height * .1f),
			               "Show Interstitial"))
			{
				HCSDK.ShowInterstitial("DefaultRewardedVideo");
			}
			if (GUI.Button(new Rect(Screen.width * .4f, Screen.height * 0f, Screen.width * .2f, Screen.height * .1f),
			               "Show Banner"))
			{
				HCSDK.ShowBanner();
			}
			if (GUI.Button(new Rect(Screen.width * .6f, Screen.height * 0f, Screen.width * .2f, Screen.height * .1f),
			               "Hide Banner"))
			{
				HCSDK.HideBanner();
			}
			if (GUI.Button(new Rect(Screen.width * .8f, Screen.height * 0f, Screen.width * .2f, Screen.height * .1f),
			               "Exit Tester Mode"))
			{
				HideTesterGUI();
			}
			
			if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * .1f, Screen.width * .2f, Screen.height * .1f),
						   "Firebase Log Event"))
			{
				if (Core.GetService<IFirebaseService>() != null)
					Core.GetService<IFirebaseService>().LogEvent("testEvent", new Dictionary<string, object>(){{"keyA", 1.0f}, {"keyB", "val2"}, {"keyC", 15}});
			}
			
			if (GUI.Button(new Rect(Screen.width * .2f, Screen.height * .1f, Screen.width * .2f, Screen.height * .1f),
						   "Firebase Fetch RemoteConfig"))
			{
				if (Core.GetService<IFirebaseRemoteConfigService>() != null)
					Core.GetService<IFirebaseRemoteConfigService>().FetchRemoteConfig();
			}
		
			if (GUI.Button(new Rect(Screen.width * .8f, Screen.height * .1f, Screen.width * .2f, Screen.height * .1f),
			               "CRASH TEST"))
			{
				CrashTest();
			}
			
			if (GUI.Button(new Rect(Screen.width * .8f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f),
						   "Show FPS"))
			{
				HCSDK.ShowFPSCounter(0.8f, 0.75f);
			}
			
			if (GUI.Button(new Rect(Screen.width * .8f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f),
						   "Hide FPS"))
			{
				HCSDK.HideFPSCounter();
			}
			
			if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * .2f, Screen.width * .2f, Screen.height * .1f),
			               "IronSource integration test"))
			{
				IronSource.Agent.validateIntegration();
			}
			if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f),
						   "IronSource Testu Suite"))
			{
				IronSource.Agent.launchTestSuite();
			}
			
			if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * .5f, Screen.width * 1f, Screen.height * .1f),
				    config.waterfallsSource)) {}
			
#if GAME_SERVICES
	        if (GUI.Button(new Rect(Screen.width * 0f, Screen.height * 0.4f, Screen.width * 0.2f, Screen.height * 0.1f),
	                       "Game Center LogIn"))
	        {
	            HCSDK.GameCenterLogIn();
	        }
#endif
			
#if HCSDK_DIAGNOSTICS

         // draws a button on the scene to fetch the average time
         Log.InfoFormat("Draws HCSDK DIAGNOSTICS buttons");

         if (GUI.Button(
            new Rect(Screen.width * .0f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f),
            "Get AverageTime"))
         {
            var result = PlayerPrefs.GetFloat("timeAverage");

            Log.Info("Average time: Get  " + result);
         }

         // draws a button on the stage to reset the average time
         if (GUI.Button(
            new Rect(Screen.width * .2f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f),
            "Reset AverageTime"))
         {
            TimeAverage.ResetAverageTime();
            var resultReset = PlayerPrefs.GetFloat("averageTime");
            Log.Info("Average time: reset of measurements. Current average after reset: " + resultReset);
         }

         if (GUI.Button(
            new Rect(Screen.width * .4f, Screen.height * .3f, Screen.width * .2f, Screen.height * .1f),
            "Number of measurements"))
         {
            var n = PlayerPrefs.GetFloat("n");
            Log.Info("Average time: number of measurements = " + n);
         }

         GUI.Label(
            new Rect(Screen.width * .0f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f),
            PlayerPrefs.GetFloat("timeAverage").ToString());

         GUI.Label(
            new Rect(Screen.width * .4f, Screen.height * .4f, Screen.width * .2f, Screen.height * .1f),
            PlayerPrefs.GetFloat("n").ToString());

#endif
		}
	}
	
	void Initialize()
	{
		if (!initialized)
		{
			initialized = true;

			HCSDK.VideoSuccessEvent += () => { Debug.Log("TestHCSDK :: VideoSuccessEvent"); };
			HCSDK.VideoFailEvent += () => { Debug.Log("TestHCSDK :: VideoFailEvent"); };
			HCSDK.VideoCancelEvent += () => { Debug.Log("TestHCSDK :: VideoCancelEvent"); };

			HCSDK.VideoDisplayEvent += () => { Debug.Log("TestHCSDK :: VideoDisplayEvent"); };
			HCSDK.VideoClickEvent += () => { Debug.Log("TestHCSDK :: VideoClickEvent"); };

			HCSDK.InterstitialCloseEvent += () => { Debug.Log("TestHCSDK :: InterstitialCloseEvent"); };
			HCSDK.InterstitialDisplayEvent += () => { Debug.Log("TestHCSDK :: InterstitialDisplayEvent"); };
			HCSDK.InterstitialClickEvent += () => { Debug.Log("TestHCSDK :: InterstitialClickEvent"); };
			HCSDK.InterstitialFailEvent += () => { Debug.Log("TestHCSDK :: InterstitialFailEvent"); };

			HCSDK.BannerDisplayEvent += () => { Debug.Log("TestHCSDK :: BannerDisplayEvent"); };

			HCSDK.LoginInGCSuccessfullEvent += () => { Debug.Log("TestHCSDK :: LoginInGCSuccessfullEvent event"); };
			HCSDK.LoginInGCFailEvent += () => { Debug.Log("TestHCSDK :: LoginInGCFailEvent event"); };
		}
	}
	
	private void RecalculateScreen()
	{
		StartCoroutine(RecalculateScreenCoroutine());
	}

	private IEnumerator RecalculateScreenCoroutine()
	{
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();

		orientation = Screen.orientation;
		stylesPrepared = false;
		CalculateScreenCorners();
		topButtonsBackgroundRect = new Rect(0, 0, Screen.width, 2 * buttonHeightRatio * Screen.height);

	}

	private void CalculateScreenCorners()
	{
		vTopLeft = new Vector2(Screen.width * CornerSize, Screen.height * (1 - CornerSize));
		vBottomLeft = new Vector2(Screen.width * CornerSize, Screen.height * CornerSize);
		vTopRight = new Vector2(Screen.width * (1 - CornerSize), Screen.height * (1 - CornerSize));
		vBottomRight = new Vector2(Screen.width * (1 - CornerSize), Screen.height * CornerSize);
		stylesPrepared = false;
	}

	void TrackTouch(Vector2 touchPosition)
	{
		ScreenCorner currentCorner = GetCorner(touchPosition);

		if (currentCorner != lastCorner)
		{
			AddToInputSequence(currentCorner);
			lastCorner = currentCorner;
			CheckSequence();
		}
	}
	
	ScreenCorner GetCorner(Vector2 touchPosition)
	{
		if (touchPosition.x <= vTopLeft.x && touchPosition.y >= vTopLeft.y)
			return ScreenCorner.TopLeft;
		if (touchPosition.x <= vBottomLeft.x && touchPosition.y <= vBottomLeft.y)
			return ScreenCorner.BottomLeft;
		if (touchPosition.x >= vTopRight.x && touchPosition.y >= vTopRight.y)
			return ScreenCorner.TopRight;
		if (touchPosition.x >= vBottomRight.x && touchPosition.y <= vBottomRight.y)
			return ScreenCorner.BottomRight;

		return ScreenCorner.None;
	}

	void AddToInputSequence(ScreenCorner corner)
	{
		if (corner != ScreenCorner.None)
		{
		}

		switch (corner)
		{
			case ScreenCorner.TopLeft:
				inputSequence.Add(TopLeft);
				break;
			case ScreenCorner.BottomLeft:
				inputSequence.Add(BottomLeft);
				break;
			case ScreenCorner.TopRight:
				inputSequence.Add(TopRight);
				break;
			case ScreenCorner.BottomRight:
				inputSequence.Add(BottomRight);
				break;
		}
	}

	void ResetTracking()
	{
		inputSequence.Clear();
		lastCorner = ScreenCorner.None;
	}

	void CheckSequence()
	{
		if (sequenceArray.Count <= 0)
			return;

		// sequence correct
		if (CheckInputSequence())
		{
			if (Hidden)
				ShowTesterGUI();
			else
				HideTesterGUI();
		}
	}

	private void HideTesterGUI()
	{
		Hidden = true;
		useGUILayout = false;
	}
	
	IEnumerator DelayedQuit()
	{
		yield return new WaitForEndOfFrame();
		Application.Quit();
	}

	private bool CheckInputSequence()
	{
		if (inputSequence.Count < sequenceArray.Count)
			return false;

		while (inputSequence.Count > sequenceArray.Count)
			inputSequence.RemoveAt(0);

		for (int i = 0; i < sequenceArray.Count; i++)
		{
			if (inputSequence[i].CompareTo(sequenceArray[i]) != 0)
				return false;
		}

		ResetTracking();
		return true;
	}

	/// <summary>
	/// Check for semicolon at end of sequence. Add it if not found.
	/// </summary>
	/// <returns>The semicolon at sequence end.</returns>
	/// <param name="seq">Seq.</param>
	private string CheckSemicolonAtSequenceEnd(string seq)
	{
		if (seq != null && seq.EndsWith(";"))
		{
			return seq;
		}
		else
		{
			return seq + ";";
		}
	}

	/// <summary>
	/// Convert old type sequence UL;DL;DR;UR;
	/// to new type sequence TL;BL;BR;TR;
	/// </summary>
	/// <returns>The sequence.</returns>
	/// <param name="sequence">Sequence.</param>
	private string ConvertSequence(string sequence)
	{
		bool converted = false;
		if (sequence.Contains("UL"))
		{
			sequence = sequence.Replace("UL", TopLeft);
			converted = true;
		}

		if (sequence.Contains("DL"))
		{
			sequence = sequence.Replace("DL", BottomLeft);
			converted = true;
		}

		if (sequence.Contains("DR"))
		{
			sequence = sequence.Replace("DR", BottomRight);
			converted = true;
		}

		if (sequence.Contains("UR"))
		{
			sequence = sequence.Replace("UR", TopRight);
			converted = true;
		}
		
		return sequence;
	}
	internal static object GetInstanceField(Type type, object instance, string fieldName)
	{
		BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
		                         | BindingFlags.Static;
		FieldInfo field = type.GetField(fieldName, bindFlags);
		return field?.GetValue(instance);
	}
	void CrashTest()
	{
#if UNITY_ANDROID
    // https://stackoverflow.com/questions/17511070/android-force-crash-with-uncaught-exception-in-thread
    var message = new AndroidJavaObject("java.lang.String", "This is a test crash, ignore.");
    var exception = new AndroidJavaObject("java.lang.Exception", message);
   
    var looperClass = new AndroidJavaClass("android.os.Looper");
    var mainLooper = looperClass.CallStatic<AndroidJavaObject>("getMainLooper");
    var mainThread = mainLooper.Call<AndroidJavaObject>("getThread");
    var exceptionHandler = mainThread.Call<AndroidJavaObject>("getUncaughtExceptionHandler");
    exceptionHandler.Call("uncaughtException", mainThread, exception);
#else
		List<int> tab = new List<int> {2, 3, 4, 5, 6};

		//try
		//{
		foreach (int i in tab)
		{
			tab.Remove(i);
		}
#endif
	}
}

internal class CrossPromoEmbeddedSquareService
{
}
