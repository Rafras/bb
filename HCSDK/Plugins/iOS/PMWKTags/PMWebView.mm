//
//  PMWKTags.m
//  Unity-iPhone
//
//  Created by Wasilewski on 11.05.2017.
//
//

#import "PMWebView.h"
#import <AdSupport/ASIdentifierManager.h>
#import <JavaScriptCore/JavaScriptCore.h>

@implementation PMWebView

UIView *UnityGetGLView();
UIViewController *UnityGetGLViewController();
WKWebView* webView;

void UnitySendMessage( const char * className, const char * methodName, const char * param );
+ (PMWebView*)sharedManager
{
    static PMWebView *sharedSingleton;
    
    if( !sharedSingleton )
    {
        sharedSingleton = [[PMWebView alloc] init];
    }
    
    return sharedSingleton;
}

-(void)LoadWebViewWithUrl:(NSString*) url
{
    SFSafariViewController *safariVC = [[SFSafariViewController alloc]initWithURL:[NSURL URLWithString:url] entersReaderIfAvailable:NO];
    safariVC.delegate = self;
    [UnityGetGLViewController() presentViewController:safariVC animated:NO completion:nil];
}

#pragma mark - SFSafariViewController delegate methods

-(void)safariViewController:(SFSafariViewController *)controller didCompleteInitialLoad:(BOOL)didLoadSuccessfully
{
    
}

-(void)safariViewControllerDidFinish:(SFSafariViewController *)controller
{
    
}
 
@end

