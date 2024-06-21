using AppsFlyerSDK;

namespace BoomBit.HyperCasual
{
    using System.Globalization;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using MiniJSON;
    using System.Text;

    using Coredian;
    using Coredian.Facebook;

    using UnityEngine.Networking;
    using Coredian.Firebase;

    public class AdImpressionTracker : MonoBehaviour
    {
        private const string ServerVersion = "v1";
        private const string MoPubServerApi = "https://mopub.boombit.cloud/send-data/";
        private const string IronSourceServerApi = "https://mopub.boombit.cloud/send-data/ironsource/";
        private const int MaxFbEventFloor = 11;
        private float failCount = 0f;
        private bool impressionSending = false;

        private List<object> impressionList;
        private List<double> revenueFloors = new List<double> {0.0d, 0.1d, 0.2d, 0.3d, 0.4d, 0.5d, 1.0d, 2.0d};
        private int actualFloor = 0;
        
        private List<double> revenueAppsFlyerFloors = new List<double> {0.0d, 0.3d, 0.4d, 0.5d, 0.7d, 1.0d, 2.0d};
        private int actualAppsFlyerFloor = 0;

        private HashSet<string> appsFlyerWhitelist = new HashSet<string>();
        private HashSet<string> appsFlyerBlacklist = new HashSet<string>();
        
        private HashSet<string> facebookWhitelist = new HashSet<string>();

        private HashSet<string> facebookBlacklist = new HashSet<string>();

        private List<double> fbImpressionFloors = new List<double>
        {
            0.0d, 0.1d, 0.2d, 0.3d, 0.4d, 0.5d, 0.6d, 0.7d, 0.8d, 0.9d, 1d,
            1.5d, 2.0d, 2.5d, 3.0d, 3.5d, 4.0d, 4.5d, 5d, 10d, 20d, 50d
        };
        private int actualFbFloor=0;

        private double lifetimeRevenue = 0f;
        
        public void Initialize()
        {
            if (Core.IsInitialized) BindFirebaseRemoteConfigEvents();
            Core.InitializationSucceeded += OnCoreInitializationSucceeded;
            Core.WillDeinitialize += OnCoreWillDeinitialize;
            
            GetLifetimeRevenue();
            CalculateActualFbFloor();
            CalculateActualFloors();
            
            impressionList=DeserializeImpressions();
            if (!impressionSending) StartCoroutine(SendingImpressionData());
        }

        public void TrackImpression(Dictionary<string, object> impressionDict, string adMediation, double? revenue=null)
        {
            if (impressionDict==null) return;
            
            SentAdImpressionEventToFacebook((float?)revenue);
            
            IncreaseLifetimeRevenue(revenue);

            TrackImpressionInFacebook();
            
            AddFieldsToImpressionData(impressionDict, adMediation);
            impressionList.Add(impressionDict);
            SerializeImpressions(impressionList, true);

            if (!impressionSending) StartCoroutine(SendingImpressionData());
        }
        
        private void SentAdImpressionEventToFacebook(float? revenue)
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() != null &&
                Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded)
            {
                if (Core.GetService<IFirebaseRemoteConfigService>()
                        .RemoteConfig.TryGetValue("FB_TRACKING_AD_IMPRESSION", out var value) &&
                    value.ToUpper() == "TRUE" && Core.GetService<IFacebookService>().IsSdkInitialized)
                {
                    Log.InfoFormat("AdImpressionTracker :: SentAdImpressionEventToFacebook :: AdImpression with revenue = "+revenue);
                    Core.GetService<IFacebookService>().LogEvent("AdImpression", revenue, new Dictionary<string, object>(){ {"fb_currency", "USD"} });
                }
            }
        }

        private void BindFirebaseRemoteConfigEvents()
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() == null) return;

            if (Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded) GetConfiguration(Core.GetService<IFirebaseRemoteConfigService>().RemoteConfig);
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded += GetConfiguration;
        }

        private void UnbindCoreIapEvents()
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() == null) return;
            Core.GetService<IFirebaseRemoteConfigService>().RemoteConfigLoadingSucceeded -= GetConfiguration;
        }
        
        private void OnCoreInitializationSucceeded()
        {
            BindFirebaseRemoteConfigEvents();
        }

        private void OnCoreWillDeinitialize()
        {
            UnbindCoreIapEvents();
        }
        
        private void GetConfiguration(IDictionary<string, string> config)
        {
            ReadWhitelist(facebookWhitelist, "FB_REVENUE_WHITELIST", config);
            ReadWhitelist(facebookBlacklist, "FB_REVENUE_BLACKLIST", config);
            ReadWhitelist(appsFlyerWhitelist, "AF_REVENUE_WHITELIST", config);
            ReadWhitelist(appsFlyerBlacklist, "AF_REVENUE_BLACKLIST", config);
        }

        private void ReadWhitelist(HashSet<string> whitelist, string whitelistKey, IDictionary<string, string> config)
        {
            whitelist?.Clear();

            if (config.ContainsKey(whitelistKey))
            {
                string[] sources = config[whitelistKey].Split(',', StringSplitOptions.RemoveEmptyEntries);

                for (var i = 0; i != sources.Length; i++)
                {
                    whitelist?.Add(sources[i].Trim());
                }
            }
        }

        private void TrackImpressionInFacebook()
        {
            if (Core.GetService<IFirebaseRemoteConfigService>() != null &&
                Core.GetService<IFirebaseRemoteConfigService>().IsRemoteConfigLoaded)
            {
                for (int i = actualFbFloor+1; i < fbImpressionFloors.Count; i++)
                {
                    if (lifetimeRevenue >= fbImpressionFloors[i])
                    {
                        actualFbFloor = i;
                        SentImpressionToFacebook();
                    }
                    else break;
                }
            }
        }

        private bool IsFbWhitelisted()
        {
            var source = PlayerPrefs.GetString("HCSDK_ad_source", "unknown");

            var isWhitelisted = IsWhitelisted(source, facebookWhitelist, facebookBlacklist);
            Log.InfoFormat("AdImpressionTracker :: IsFbWhitelisted :: source=>{0} :: isWhitelisted=>{1}", source, isWhitelisted);

            return isWhitelisted;
        }
        
        private bool IsAfWhitelisted()
        {
            var source = PlayerPrefs.GetString("HCSDK_ad_source", "unknown");

            var isWhitelisted = IsWhitelisted(source, appsFlyerWhitelist, appsFlyerBlacklist);
            Log.InfoFormat("AdImpressionTracker :: IsAfWhitelisted :: source=>{0} :: isWhitelisted=>{1}", source, isWhitelisted);

            return isWhitelisted;
        }
        
        private bool IsWhitelisted(string source, HashSet<string> whitelist, HashSet<string> blacklist)
        {
            if (blacklist != null && blacklist.Contains(source)) return false;
            if (whitelist == null || whitelist.Count == 0) return true;

            return whitelist.Contains(source);
        }

        private void SentImpressionToFacebook()
        {
            var impressionValue = fbImpressionFloors[actualFbFloor].ToString("0.00", CultureInfo.InvariantCulture).Split('.');

            if (IsFbWhitelisted())
            {
                if(Core.GetService<IFacebookService>() != null)
                    Core.GetService<IFacebookService>()
                        .LogEvent("ad_revenue_" + impressionValue[0] + "_" + impressionValue[1] + "_usd");
            }

            if (MaxFbEventFloor > actualFbFloor)
            {
                if (Core.GetService<IFirebaseRemoteConfigService>() != null &&
                    Core.GetService<IFirebaseRemoteConfigService>()
                        .RemoteConfig.TryGetValue("FB_TRACKING_IMPRESSION", out var value) &&
                    value.ToUpper() == "TRUE" &&
                    Core.GetService<IFacebookService>().IsSdkInitialized)
                {
                    Core.GetService<IFacebookService>().LogPurchase(0.1f, "USD");
                    Log.InfoFormat("AdImpressionTracker :: SentImpressionToFacebook :: Log :: Purchase 0.1$");
                }
            }
        }

        private void AddFieldsToImpressionData(Dictionary<string, object> impressinoData, string adMediation)
        {
            if (impressinoData.ContainsKey("ad_mediation")) impressinoData["ad_mediation"] = adMediation;
            else impressinoData.Add("ad_mediation", adMediation);
            
            string appId=HCSDK.GetLocalParameter("appId");
#if UNITY_IOS
            appId = "id" + appId;
#endif
            if (impressinoData.ContainsKey("app_id")) impressinoData["app_id"] = appId;
            else impressinoData.Add("app_id", appId);

            if (impressinoData.ContainsKey("appsflyer_id"))
                impressinoData["appsflyer_id"] = AppsFlyerSDK.AppsFlyer.getAppsFlyerId();
            else impressinoData.Add("appsflyer_id", AppsFlyerSDK.AppsFlyer.getAppsFlyerId());
            
            if (impressinoData.ContainsKey("server_version")) impressinoData["server_version"] = ServerVersion;
            else impressinoData.Add("server_version", ServerVersion);

            if (impressinoData.ContainsKey("event_timestamp"))
                impressinoData["event_timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            else impressinoData.Add("event_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
        }

        private void AddRequestTimestampToImpressionData(Dictionary<string, object> impressinoData)
        {
            if (impressinoData.ContainsKey("request_timestamp"))
                impressinoData["request_timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            else impressinoData.Add("request_timestamp", DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
        }

        private void SerializeImpressions(List<object> impressionList, bool forceSave)
        {
            SerializeImpressions(impressionList);
            if (forceSave) PlayerPrefs.Save();
        }
        
        private void SerializeImpressions(List<object> impressionList)
        {
            if (impressionList.Count > 0)
            {
                PlayerPrefs.SetString("MP_IMP_LIST", Json.Serialize(impressionList));
            }
            else if (PlayerPrefs.HasKey("MP_IMP_LIST")) PlayerPrefs.DeleteKey("MP_IMP_LIST");
        }

        private void GetLifetimeRevenue()
        {
            lifetimeRevenue = 0d;

            if (PlayerPrefs.HasKey("HC_LIFETIME_REVENUE"))
            {
                double revenue;
                if (double.TryParse(PlayerPrefs.GetString("HC_LIFETIME_REVENUE"), NumberStyles.Any, CultureInfo.InvariantCulture, out revenue))
                {
                    this.lifetimeRevenue = revenue;
                }
            }
        }

        private void CalculateActualFbFloor()
        {
            for (int i = 0; i != fbImpressionFloors.Count; i++)
            {
                if (fbImpressionFloors[i] <= lifetimeRevenue) actualFbFloor = i;
                else break;
            }
        }
        
        private void CalculateActualFloors()
        {
            for (int i = 0; i != revenueFloors.Count; i++)
            {
                if (revenueFloors[i] <= lifetimeRevenue) actualFloor = i;
                else break;
            }
            
            for (int i = 0; i != revenueAppsFlyerFloors.Count; i++)
            {
                if (revenueAppsFlyerFloors[i] <= lifetimeRevenue) actualAppsFlyerFloor = i;
                else break;
            }
        }

        private void IncreaseLifetimeRevenue(double? revenue)
        {
            if (revenue.HasValue)
            {
                lifetimeRevenue += revenue.Value;
            }

            for (int i = actualFloor + 1; i < revenueFloors.Count; i++)
            {
                if (lifetimeRevenue > revenueFloors[i])
                {
                    actualFloor = i;

                    if (Core.GetService<IFirebaseService>() != null)
                    {
                        Core.GetService<IFirebaseService>()
                            .LogEventOnce(
                                "ad_revenue_over_" +
                                revenueFloors[i].ToString(CultureInfo.InvariantCulture).Replace('.', '_'));
                    }
                }
                else break;
            }
            
            for (int i = actualAppsFlyerFloor + 1; i < revenueAppsFlyerFloors.Count; i++)
            {
                if (lifetimeRevenue > revenueAppsFlyerFloors[i])
                {
                    actualAppsFlyerFloor = i;
                    if (Core.GetService<IFirebaseRemoteConfigService>() != null &&
                        Core.GetService<IFirebaseRemoteConfigService>()
                            .RemoteConfig.TryGetValue("AF_TRACKING_IMPRESSION", out var value) &&
                        value.ToUpper() == "TRUE" && IsAfWhitelisted())
                    {
                        AppsFlyerSDK.AppsFlyer.sendEvent("ad_revenue_over_" +
                                                         revenueAppsFlyerFloors[i].ToString(CultureInfo.InvariantCulture).Replace('.', '_'), null);
                    }
                }
                else break;
            }

            SaveLifetimeRevenue();
        }
        
        private void SaveLifetimeRevenue()
        {
            PlayerPrefs.SetString("HC_LIFETIME_REVENUE", lifetimeRevenue.ToString(CultureInfo.InvariantCulture));
        }
        
        private List<object> DeserializeImpressions()
        {
            if (PlayerPrefs.HasKey("MP_IMP_LIST"))
            {
                try
                {
                    string impessionJson=PlayerPrefs.GetString("MP_IMP_LIST");

                    var impressions=Json.Deserialize(impessionJson);
                    if (impressions is List<object>)
                    {
                        return impressions as List<object>;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("AdImpressionTracker :: DeserializeImpressions :: Can't Deserialize impression list :: "+e.Message);
                    
                }
            }
            
            return new List<object>();
        }

        private IEnumerator SendingImpressionData()
        {
            impressionSending = true;
            
            while (impressionList.Count > 0)
            {
                AddRequestTimestampToImpressionData(impressionList[0] as Dictionary<string, object>);
                byte[] body = Encoding.ASCII.GetBytes(Json.Serialize(impressionList[0]));
                string jsonData = Json.Serialize(impressionList[0]);
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);

                string serverApi;
                if (((Dictionary<string, object>)impressionList[0]).ContainsKey("ad_mediation") && (((Dictionary<string, object>)impressionList[0])["ad_mediation"].ToString()=="MoPub"))
                {
                    serverApi = MoPubServerApi;
                }
                else serverApi = IronSourceServerApi;

                using (UnityWebRequest webRequest = new UnityWebRequest(serverApi + ServerVersion, "POST"))
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                    webRequest.downloadHandler = new DownloadHandlerBuffer();
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    yield return webRequest.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
                    if (webRequest.responseCode!=200 || (webRequest.result == UnityWebRequest.Result.ConnectionError) || (webRequest.result == UnityWebRequest.Result.ProtocolError) )
#else
                    if (webRequest.responseCode!=200 || webRequest.isNetworkError || webRequest.isHttpError)
#endif
                    {
                        yield return new WaitForSeconds(TimeForNextRetry());
                    }
                    else
                    {
                        failCount = 0;
                        try
                        {
                            Dictionary<string, object> response=(Dictionary<string, object>) Json.Deserialize(webRequest.downloadHandler.text);
                            if (response["status"].ToString() != "ok")
                            {
                                Debug.LogError("AdImpressionTracker :: SendingImpressionData :: Server Error :: "+response["message"]);    
                            }
                            else
                            {
                                Debug.Log("AdImpressionTracker :: SendingImpressionData :: OK");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("AdImpressionTracker :: SendingImpressionData :: Can't Deserialize server response :: "+e.Message);
                        }

                        impressionList.RemoveAt(0);
                        SerializeImpressions(impressionList);
                        webRequest.Dispose();
                    }
                }
            }

            impressionSending = false;
        }

        private float TimeForNextRetry()
        {
            failCount++;
            return failCount * failCount * failCount;
        }

    }

}
