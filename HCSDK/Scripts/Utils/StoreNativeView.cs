using System.Runtime.InteropServices;
using UnityEngine;

public class StoreNativeView : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern void _ShowAppInStore(string appid, bool landscape);
#else
	private static void _ShowAppInStore(string appid, bool landscape){}
#endif

	public static void ShowAppInStore(string appId)
	{
		_ShowAppInStore(appId, true);
	}
}
