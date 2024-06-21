//
//  AppController_PlistAndAppVerReader.h
//  Unity-iPhone
//
//  Created by Malisz on 23.01.2013.
//  Modificated by Tomaso many, many times
//

#import <UIKit/UIKit.h>
#import <AdSupport/AdSupport.h>
#import <StoreKit/StoreKit.h>
#import <FBAudienceNetwork/FBAudienceNetwork.h>
#import <BigoADS/BigoADS.h>

#if defined(__IPHONE_10_0)
#import <UserNotifications/UserNotifications.h>
#endif

// Converts C style string to NSString
#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

int UnityGetTargetFPS();

UIView* UnityGetGLView();
UIViewController *UnityGetGLViewController();


#define SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(v)  ([[[UIDevice currentDevice] systemVersion] compare:v options:NSNumericSearch] != NSOrderedAscending)

extern "C"
{
    bool _HasAppByScheme(char* appId);
    char* _GetAppstoreLang();
    char* _GetDeviceLang();
    bool _IsTablet();
    bool _IsLimitedAdTracking();
    void _RequestReview();
    void _SetLDUForFAN();
    void _SetFANAdvertiserTrackingEnabled(bool isTrackingEnabled);
    void _SetBigoAdsConsent(bool hasConsent);
}

void _RequestReview()
{
    [SKStoreReviewController requestReview];
}

bool _IsTablet()
{
    if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad)
    {
        return YES;
    }
    else return NO;
}

bool _IsLimitedAdTracking()
{
    if ([[ASIdentifierManager sharedManager] isAdvertisingTrackingEnabled]) return NO;
    return YES;
}

bool _HasAppByScheme(char* schemeName)
{
    NSLog(@"_HasAppByScheme");
    
    if (schemeName==nil) return NO;
    
    UIApplication *ourApplication = [UIApplication sharedApplication];
    
    NSString *sName = [[NSString alloc] initWithUTF8String:schemeName];
    
    NSLog(@"_HasAppByScheme %@",sName);
    
    NSString *ourPath = [NSString stringWithFormat:@"%@://",sName];
    NSURL *ourURL = [NSURL URLWithString:ourPath];
    if ([ourApplication canOpenURL:ourURL]) {
        NSLog(@"_HasApp END");
        return YES;
    }
    
    return NO;
}
void* geCopytHeap(const void* p, size_t size)
{
    void* heapCopy = malloc(size);
    memcpy(heapCopy, p, size);
    return heapCopy;
}
char* _GetAppstoreLang(){
    NSString* preferredLang=[[NSLocale currentLocale] objectForKey:NSLocaleCountryCode];
    if (preferredLang==nil) preferredLang=@"";
    
    char* lang = (char*)geCopytHeap([preferredLang UTF8String], [preferredLang length]+1);
    return lang;
}

char* _GetDeviceLang()
{
    NSString * language = [[NSLocale preferredLanguages] objectAtIndex:0];
    char* lang = (char*)geCopytHeap([language UTF8String], [language length]+1);
    return lang;
}

void _SetLDUForFAN()
{
    [FBAdSettings setDataProcessingOptions:@[]];
}

void _SetFANAdvertiserTrackingEnabled(bool isTrackingEnabled)
{
    [FBAdSettings setAdvertiserTrackingEnabled:isTrackingEnabled];
}

void _SetBigoAdsConsent(bool hasConsent)
{
    [BigoAdSdk setUserConsentWithOption:BigoConsentOptionsGDPR consent:hasConsent];
    [BigoAdSdk setUserConsentWithOption:BigoConsentOptionsCCPA consent:hasConsent];
}
