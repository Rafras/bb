//
//  PMStoreKit.m
//  Unity-iPhone
//
//  Created by mac on 27.01.2014.
//
//

#import "PMInjection.h"


UIViewController *UnityGetGLViewController();
void UnitySendMessage( const char * className, const char * methodName, const char * param );
#if UNITY_VERSION < 500
void UnityPause( bool pause );
#else
void UnityPause( int pause );
#endif

#define SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(v)  ([[[UIDevice currentDevice] systemVersion] compare:v options:NSNumericSearch] != NSOrderedAscending)

@implementation PMInjection

+ (PMInjection*)sharedManager
{
    static PMInjection *sharedSingleton;
    
    if( !sharedSingleton )
        sharedSingleton = [[PMInjection alloc] init];
    return sharedSingleton;
}

-(void)Injection:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions
{
}

-(void)Injection:application didRegisterForRemoteNotificationsWithDeviceToken:(NSData*)deviceToken
{
    [[AppsFlyerLib shared] registerUninstall:deviceToken];
}

-(void)Injection:application openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options
{
}

@end
