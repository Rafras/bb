//
//  PMWKTags.h
//  Unity-iPhone
//
//  Created by Wasilewski on 11.05.2017.
//
//

#import <WebKit/WebKit.h>
#import <SafariServices/SafariServices.h>

@interface PMWebView : NSObject<SFSafariViewControllerDelegate>

+ (PMWebView*)sharedManager;

-(void)LoadWebViewWithUrl:(NSString*) url;

@end
