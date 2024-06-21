//
//  PMStoreKit.m
//  Unity-iPhone
//
//  Created by mac on 27.01.2014.
//
//

#import "NativeStoreView.h"

UIViewController *UnityGetGLViewController();
void UnitySendMessage( const char * className, const char * methodName, const char * param );
void UnityPause( int pause );

#define SYSTEM_VERSION_GREATER_THAN_OR_EQUAL_TO(v)  ([[[UIDevice currentDevice] systemVersion] compare:v options:NSNumericSearch] != NSOrderedAscending)

#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]

extern "C"
{
    void _ShowAppInStore(char* appid, bool landscape);
}

void _ShowAppInStore(char* appid, bool landscape)
{
    [[NativeStoreView sharedManager] showStoreView:GetStringParam(appid) AndLandscape:landscape];
}

@implementation NativeStoreView

@synthesize banner, actualAppid;

+ (NativeStoreView*)sharedManager
{
	static NativeStoreView *sharedSingleton;
	
	if( !sharedSingleton )
    {
		sharedSingleton = [[NativeStoreView alloc] init];
    }
    
	return sharedSingleton;
}

- (void)showStoreView:(NSString*)appid AndLandscape: (bool) isLandscape {
    
    //UnitySendMessage( "IOSPauseCollector", "CollectPause", "" );
    //UnityPause( true );
    
    SKStoreProductViewController* storeViewController = [[ProductViewController alloc] init];
    
    storeViewController.delegate = [NativeStoreView sharedManager];
    
     NSDictionary *parameters =
     @{SKStoreProductParameterITunesItemIdentifier:
     [NSNumber numberWithInteger:[appid intValue]]};
    
    NSLog(@"PMStoreKit showStoreView with appid %@",appid);
    
    [storeViewController loadProductWithParameters:parameters completionBlock:^(BOOL result, NSError *error) {
        if (result)
        {
            NSLog(@"PMStoreKit showStoreView with positive result");
        }
        else
        {
            NSLog(@"PMStoreKit showStoreView with negative result = %@",error.description);
        }
        
    }];
    
    [UnityGetGLViewController() presentViewController:storeViewController animated:YES completion:nil];
}

#pragma mark SKStoreProductViewControllerDelegate

-(void)productViewControllerDidFinish:(SKStoreProductViewController *)viewController
{
    NSLog(@"finisz");
    
    UnityPause( false );
    
    UnitySendMessage( "StoreKit", "StoreWasClosed", "" );
    
    NSLog(@"PMStoreKit productViewControllerDidFinish");
    [viewController dismissViewControllerAnimated:YES completion:nil];
}

@end
