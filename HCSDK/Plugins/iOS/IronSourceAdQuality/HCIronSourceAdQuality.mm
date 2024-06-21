//
//  AppController_PlistAndAppVerReader.h
//  Unity-iPhone
//
//  Created by Malisz on 23.01.2013.
//  Modificated by Tomaso many, many times
//

#import <IronSourceAdQualitySDK/IronSourceAdQuality.h>

// Converts C style string to NSString
#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]


extern "C"
{
    void _HCIsAdQualityStart(char* appKey);
    void _HCIsAdQualitySetUserconsent(bool consent);
}

void _HCIsAdQualityStart(char* appKey)
{
    [[IronSourceAdQuality getInstance] initializeWithAppKey:GetStringParam(appKey)];
}

void _HCIsAdQualitySetUserconsent(bool consent)
{
    [[IronSourceAdQuality getInstance] setUserConsent:consent];
}

