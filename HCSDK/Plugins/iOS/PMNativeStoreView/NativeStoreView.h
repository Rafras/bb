//
//  PMStoreKit.h
//  Unity-iPhone
//
//  Created by mac on 27.01.2014.
//
//

#import <Foundation/Foundation.h>
#import <StoreKit/StoreKit.h>
#import "ProductViewController.h"

@interface NativeStoreView : NSObject<SKStoreProductViewControllerDelegate, SKRequestDelegate>

@property (nonatomic, retain) UIImageView *banner;
@property (nonatomic, retain) NSString *actualAppid;

+ (NativeStoreView*)sharedManager;
- (void)showStoreView:(NSString*) appid AndLandscape:(bool) isLandscape;

@end

