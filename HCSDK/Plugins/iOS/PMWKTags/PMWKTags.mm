//
//  PMWKTags.m
//  Unity-iPhone
//
//  Created by Wasilewski on 11.05.2017.
//
//

#import "PMWKTags.h"
#import <AdSupport/ASIdentifierManager.h>
#import <JavaScriptCore/JavaScriptCore.h>

@implementation PMWKTags

@synthesize tags, placements;

BOOL adReady = NO;
NSString* onScreenType;

UIView *UnityGetGLView();
WKWebView* videoWebView;
WKWebView* interstitialWebView;
void UnitySendMessage( const char * className, const char * methodName, const char * param );
+ (PMWKTags*)sharedManager
{
    static PMWKTags *sharedSingleton;
    
    if( !sharedSingleton )
    {
        sharedSingleton = [[PMWKTags alloc] init];
        
        sharedSingleton.tags=[NSMutableDictionary new];
        sharedSingleton.placements=[NSMutableDictionary new];
    }
    
    return sharedSingleton;
}

-(NSString*)GetHtmlStringFromTag:(NSString*) tag
{
    NSString *advertId;
    advertId=[[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
    
    NSLog(@"advertId %@", advertId);
    
    NSLog(@"%lu", (unsigned long)tag.length);
    NSString* replacement = [NSString stringWithFormat: @"idfa: \"%@\"", advertId];
    
    NSString* htmlString = [tag stringByReplacingOccurrencesOfString:@"idfa: \"\""
                                                       withString:replacement];
    return htmlString;
}

-(void)LoadTagUrl:(NSString*) url WithType:(NSString*) type
{
    NSURLRequest *request = [NSURLRequest requestWithURL:[NSURL URLWithString: url]];
    
    
    WKWebViewConfiguration* webViewConfiguration=[[WKWebViewConfiguration alloc] init];
    if([type  isEqual: @"video"])
    {
        videoWebView = [[WKWebView alloc] initWithFrame:UnityGetGLView().bounds configuration:webViewConfiguration];
        videoWebView.UIDelegate=self;
        videoWebView.navigationDelegate=self;
        videoWebView.scrollView.delegate=self;
        
        [videoWebView loadRequest: request];
    }
    else if([type  isEqual: @"interstitial"])
    {
        interstitialWebView = [[WKWebView alloc] initWithFrame:UnityGetGLView().bounds configuration:webViewConfiguration];
        interstitialWebView.UIDelegate=self;
        interstitialWebView.navigationDelegate=self;
        interstitialWebView.scrollView.delegate=self;
        
        [interstitialWebView loadRequest: request];
    }
}

-(void)ShowTagWithType:(NSString*) type
{
    if([type  isEqual: @"video"])
    {
        [UnityGetGLView() addSubview:videoWebView];
        onScreenType = type;
    }
    else if([type  isEqual: @"interstitial"])
    {
        [UnityGetGLView() addSubview:interstitialWebView];
        onScreenType = type;
    }
}

-(bool)IsTagReadyWithPlacement:(NSString*) placement
{
    if ([placements objectForKey:placement]!=nil)
    {
        return YES;
    }
    else return NO;
}


- (void)closeWebView:(WKWebView *)webView
{
    NSLog(@"PMWKTags :: webViewDidClose");
    
    UnitySendMessage("PMWKTagsManager", "TagDisplayDidSucceed", [onScreenType UTF8String]);
    
//    NSNumber* hash=[NSNumber numberWithUnsignedInteger:webView.hash];
//    
//    if ([tags objectForKey:hash]!=nil)
//    {
//        NSString* placement=[tags objectForKey:hash];
//        UnitySendMessage("PMWKTagsManager", "TagDisplayDidSucceed", [onScreenType UTF8String]);
//        
//        [placements removeObjectForKey:placement];
//        [tags removeObjectForKey:hash];
//    }
    
    [webView removeFromSuperview];
    webView.UIDelegate=nil;
    webView.navigationDelegate=nil;
    webView.scrollView.delegate=nil;
    webView=nil;
    
    if(NSClassFromString(@"WKWebsiteDataStore") != nil)
    {
        NSSet *websiteDataTypes
        = [NSSet setWithArray:@[
                                WKWebsiteDataTypeDiskCache,
                                //WKWebsiteDataTypeOfflineWebApplicationCache,
                                WKWebsiteDataTypeMemoryCache,
                                //WKWebsiteDataTypeLocalStorage,
                                //WKWebsiteDataTypeCookies,
                                //WKWebsiteDataTypeSessionStorage,
                                //WKWebsiteDataTypeIndexedDBDatabases,
                                //WKWebsiteDataTypeWebSQLDatabases
                                ]];
        //// All kinds of data
        //NSSet *websiteDataTypes = [WKWebsiteDataStore allWebsiteDataTypes];
        //// Date from
        NSDate *dateFrom = [NSDate dateWithTimeIntervalSince1970:0];
        //// Execute
        [[WKWebsiteDataStore defaultDataStore] removeDataOfTypes:websiteDataTypes modifiedSince:dateFrom completionHandler:^{
            // Done
        }];
    }
}


- (void)webViewDidClose:(WKWebView *)webView
{
    NSLog(@"PMWKTags :: webViewDidClose");
}

- (void)webView:(WKWebView *)webView didFailProvisionalNavigation:(null_unspecified WKNavigation *)navigation withError:(NSError *)error
{
    NSLog(@"PMWKTags :: webView: didFailProvisionalNavigation: withError");
    
    NSNumber* hash=[NSNumber numberWithUnsignedInteger:webView.hash];
    
    if ([tags objectForKey:hash]!=nil)
    {
        NSString* placement=[tags objectForKey:hash];
        UnitySendMessage("PMWKTagsManager", "LoadTagDidFail", [placement UTF8String]);
        
        [placements removeObjectForKey:placement];
        [tags removeObjectForKey:hash];
    }
    
    [webView removeFromSuperview];
    webView.UIDelegate=nil;
    webView.navigationDelegate=nil;
    webView.scrollView.delegate=nil;
    webView=nil;
    
}

- (void)webView:(WKWebView *)webView didFinishNavigation:(null_unspecified WKNavigation *)navigation
{
    NSLog(@"PMWKTags :: webView: didFinishNavigation");
    
    NSNumber* hash=[NSNumber numberWithUnsignedInteger:webView.hash];
    
    if ([tags objectForKey:hash]!=nil)
    {
        [UnityGetGLView() addSubview:webView];
    }
}
        
- (void)webView:(WKWebView *)webView decidePolicyForNavigationAction:(WKNavigationAction *)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler {
    
    NSLog(@"request: %@", navigationAction.request.URL);
    
    if ([navigationAction.request.URL.absoluteString isEqual:@"uniquescheme://window.close"])
    {
        [self closeWebView:webView];
        decisionHandler(WKNavigationActionPolicyCancel);
    }
    else if ([navigationAction.request.URL.absoluteString hasPrefix:@"itms-appss://"]
             || [navigationAction.request.URL.absoluteString hasPrefix:@"https://itunes"])
    {
        [self closeWebView:webView];
        decisionHandler(WKNavigationActionPolicyCancel);
        [[UIApplication sharedApplication] openURL:navigationAction.request.URL];
    }
    else
    {
        decisionHandler(WKNavigationActionPolicyAllow);
    }
}

- (nullable UIView *)viewForZoomingInScrollView:(UIScrollView *)scrollView
{
    return nil;
}
 
@end

