using System.Runtime.InteropServices;
using UnityEngine;

public class DeviceRegionMechanism : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
	[DllImport("__Internal")]
	private static extern string _GetAppstoreLang();
	
	[DllImport("__Internal")]
	private static extern string _GetDeviceLang();
#endif

	public string GetDeviceLocalization()
	{
		string lang = "";
#if UNITY_IOS && !UNITY_EDITOR
		lang = _GetAppstoreLang();
#elif UNITY_ANDROID && !UNITY_EDITOR
		var pluginClass = new AndroidJavaClass( "com.tes.aidemandroidnative.Utils" );
		var _plugin = pluginClass.CallStatic<AndroidJavaObject>( "instance" );
		lang = ConvertISO3ToISO2(_plugin.CallStatic<string> ("getISOCountry"));
#endif
		return lang;
	}
	private string ConvertISO3ToISO2(string iso3)
	{
		iso3 = iso3.ToUpperInvariant();

		if (iso3 == "AFG") return "AF";    // Afghanistan
		else if (iso3 == "ALB") return "AL";    // Albania
		else if (iso3 == "ARE") return "AE";    // U.A.E.
		else if (iso3 == "ARG") return "AR";    // Argentina
		else if (iso3 == "ARM") return "AM";    // Armenia
		else if (iso3 == "AUS") return "AU";    // Australia
		else if (iso3 == "AUT") return "AT";    // Austria
		else if (iso3 == "AZE") return "AZ";    // Azerbaijan
		else if (iso3 == "BEL") return "BE";    // Belgium
		else if (iso3 == "BGD") return "BD";    // Bangladesh
		else if (iso3 == "BGR") return "BG";    // Bulgaria
		else if (iso3 == "BHR") return "BH";    // Bahrain
		else if (iso3 == "BIH") return "BA";    // Bosnia and Herzegovina
		else if (iso3 == "BLR") return "BY";    // Belarus
		else if (iso3 == "BLZ") return "BZ";    // Belize
		else if (iso3 == "BOL") return "BO";    // Bolivia
		else if (iso3 == "BRA") return "BR";    // Brazil
		else if (iso3 == "BRN") return "BN";    // Brunei Darussalam
		else if (iso3 == "CAN") return "CA";    // Canada
		else if (iso3 == "CHE") return "CH";    // Switzerland
		else if (iso3 == "CHL") return "CL";    // Chile
		else if (iso3 == "CHN") return "CN";    // People's Republic of China
		else if (iso3 == "COL") return "CO";    // Colombia
		else if (iso3 == "CRI") return "CR";    // Costa Rica
		else if (iso3 == "CZE") return "CZ";    // Czech Republic
		else if (iso3 == "DEU") return "DE";    // Germany
		else if (iso3 == "DNK") return "DK";    // Denmark
		else if (iso3 == "DOM") return "DO";    // Dominican Republic
		else if (iso3 == "DZA") return "DZ";    // Algeria
		else if (iso3 == "ECU") return "EC";    // Ecuador
		else if (iso3 == "EGY") return "EG";    // Egypt
		else if (iso3 == "ESP") return "ES";    // Spain
		else if (iso3 == "EST") return "EE";    // Estonia
		else if (iso3 == "ETH") return "ET";    // Ethiopia
		else if (iso3 == "FIN") return "FI";    // Finland
		else if (iso3 == "FRA") return "FR";    // France
		else if (iso3 == "FRO") return "FO";    // Faroe Islands
		else if (iso3 == "GBR") return "GB";    // United Kingdom
		else if (iso3 == "GEO") return "GE";    // Georgia
		else if (iso3 == "GRC") return "GR";    // Greece
		else if (iso3 == "GRL") return "GL";    // Greenland
		else if (iso3 == "GTM") return "GT";    // Guatemala
		else if (iso3 == "HKG") return "HK";    // Hong Kong S.A.R.
		else if (iso3 == "HND") return "HN";    // Honduras
		else if (iso3 == "HRV") return "HR";    // Croatia
		else if (iso3 == "HUN") return "HU";    // Hungary
		else if (iso3 == "IDN") return "ID";    // Indonesia
		else if (iso3 == "IND") return "IN";    // India
		else if (iso3 == "IRL") return "IE";    // Ireland
		else if (iso3 == "IRN") return "IR";    // Iran
		else if (iso3 == "IRQ") return "IQ";    // Iraq
		else if (iso3 == "ISL") return "IS";    // Iceland
		else if (iso3 == "ISR") return "IL";    // Israel
		else if (iso3 == "ITA") return "IT";    // Italy
		else if (iso3 == "JAM") return "JM";    // Jamaica
		else if (iso3 == "JOR") return "JO";    // Jordan
		else if (iso3 == "JPN") return "JP";    // Japan
		else if (iso3 == "KAZ") return "KZ";    // Kazakhstan
		else if (iso3 == "KEN") return "KE";    // Kenya
		else if (iso3 == "KGZ") return "KG";    // Kyrgyzstan
		else if (iso3 == "KHM") return "KH";    // Cambodia
		else if (iso3 == "KOR") return "KR";    // Korea
		else if (iso3 == "KWT") return "KW";    // Kuwait
		else if (iso3 == "LAO") return "LA";    // Lao P.D.R.
		else if (iso3 == "LBN") return "LB";    // Lebanon
		else if (iso3 == "LBY") return "LY";    // Libya
		else if (iso3 == "LIE") return "LI";    // Liechtenstein
		else if (iso3 == "LKA") return "LK";    // Sri Lanka
		else if (iso3 == "LTU") return "LT";    // Lithuania
		else if (iso3 == "LUX") return "LU";    // Luxembourg
		else if (iso3 == "LVA") return "LV";    // Latvia
		else if (iso3 == "MAC") return "MO";    // Macao S.A.R.
		else if (iso3 == "MAR") return "MA";    // Morocco
		else if (iso3 == "MCO") return "MC";    // Principality of Monaco
		else if (iso3 == "MDV") return "MV";    // Maldives
		else if (iso3 == "MEX") return "MX";    // Mexico
		else if (iso3 == "MKD") return "MK";    // Macedonia (FYROM)
		else if (iso3 == "MLT") return "MT";    // Malta
		else if (iso3 == "MNE") return "ME";    // Montenegro
		else if (iso3 == "MNG") return "MN";    // Mongolia
		else if (iso3 == "MYS") return "MY";    // Malaysia
		else if (iso3 == "NGA") return "NG";    // Nigeria
		else if (iso3 == "NIC") return "NI";    // Nicaragua
		else if (iso3 == "NLD") return "NL";    // Netherlands
		else if (iso3 == "NOR") return "NO";    // Norway
		else if (iso3 == "NPL") return "NP";    // Nepal
		else if (iso3 == "NZL") return "NZ";    // New Zealand
		else if (iso3 == "OMN") return "OM";    // Oman
		else if (iso3 == "PAK") return "PK";    // Islamic Republic of Pakistan
		else if (iso3 == "PAN") return "PA";    // Panama
		else if (iso3 == "PER") return "PE";    // Peru
		else if (iso3 == "PHL") return "PH";    // Republic of the Philippines
		else if (iso3 == "POL") return "PL";    // Poland
		else if (iso3 == "PRI") return "PR";    // Puerto Rico
		else if (iso3 == "PRT") return "PT";    // Portugal
		else if (iso3 == "PRY") return "PY";    // Paraguay
		else if (iso3 == "QAT") return "QA";    // Qatar
		else if (iso3 == "ROU") return "RO";    // Romania
		else if (iso3 == "RUS") return "RU";    // Russia
		else if (iso3 == "RWA") return "RW";    // Rwanda
		else if (iso3 == "SAU") return "SA";    // Saudi Arabia
		else if (iso3 == "SCG") return "CS";    // Serbia and Montenegro (Former)
		else if (iso3 == "SEN") return "SN";    // Senegal
		else if (iso3 == "SGP") return "SG";    // Singapore
		else if (iso3 == "SLV") return "SV";    // El Salvador
		else if (iso3 == "SRB") return "RS";    // Serbia
		else if (iso3 == "SVK") return "SK";    // Slovakia
		else if (iso3 == "SVN") return "SI";    // Slovenia
		else if (iso3 == "SWE") return "SE";    // Sweden
		else if (iso3 == "SYR") return "SY";    // Syria
		else if (iso3 == "TAJ") return "TJ";    // Tajikistan
		else if (iso3 == "THA") return "TH";    // Thailand
		else if (iso3 == "TKM") return "TM";    // Turkmenistan
		else if (iso3 == "TTO") return "TT";    // Trinidad and Tobago
		else if (iso3 == "TUN") return "TN";    // Tunisia
		else if (iso3 == "TUR") return "TR";    // Turkey
		else if (iso3 == "TWN") return "TW";    // Taiwan
		else if (iso3 == "UKR") return "UA";    // Ukraine
		else if (iso3 == "URY") return "UY";    // Uruguay
		else if (iso3 == "USA") return "US";    // United States
		else if (iso3 == "UZB") return "UZ";    // Uzbekistan
		else if (iso3 == "VEN") return "VE";    // Bolivarian Republic of Venezuela
		else if (iso3 == "VNM") return "VN";    // Vietnam
		else if (iso3 == "YEM") return "YE";    // Yemen
		else if (iso3 == "ZAF") return "ZA";    // South Africa
		else if (iso3 == "ZWE") return "ZW";    // Zimbabwe
		else return "US";
	}
}