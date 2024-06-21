//
//  PMStoreKit.h
//  Unity-iPhone
//
//  Created by mac on 27.01.2014.
//
//

#import <Foundation/Foundation.h>
#import "PMInjectionDefinitions.h"
#import <AppsFlyerLib/AppsFlyerLib.h>

@interface PMInjection : NSObject

+ (PMInjection*)sharedManager;

-(void)Injection:(UIApplication*)application didFinishLaunchingWithOptions:(NSDictionary*)launchOptions;

-(void)Injection:(UIApplication*)application didRegisterForRemoteNotificationsWithDeviceToken:(NSData*)deviceToken;

-(void)Injection:application openURL:(NSURL*)url options:(NSDictionary<NSString*, id>*)options;

@end

