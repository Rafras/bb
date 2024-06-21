using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class IOSReviewController
{
#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern void _RequestReview();
#else
    private static void _RequestReview(){}
#endif

    public static void RequestReview()
    {
        _RequestReview();
    }
}
