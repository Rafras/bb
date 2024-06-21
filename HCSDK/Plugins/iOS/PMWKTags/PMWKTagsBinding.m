//
//  PMWKTagsBinding.m
//  Unity-iPhone
//
//  Created by Wasilewski on 11.05.2017.
//
//

#import "PMWKTags.h"
#import "PMWebView.h"

// Converts C style string to NSString
#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]
// Converts C style string to NSString as long as it isnt empty
#define GetStringParamOrNil( _x_ ) ( _x_ != NULL && strlen( _x_ ) ) ? [NSString stringWithUTF8String:_x_] : nil
// Converts NSString to C style string by way of copy (Mono will free it)
#define MakeStringCopy( _x_ ) ( _x_ != NULL && [_x_ isKindOfClass:[NSString class]] ) ? strdup( [_x_ UTF8String] ) : NULL

void _PMLoadWebView(char* url)
{
    [[PMWebView sharedManager] LoadWebViewWithUrl:GetStringParam(url)];
}

void _PMWKTagsLoad(char* url, char* type)
{
    [[PMWKTags sharedManager] LoadTagUrl:GetStringParam(url) WithType:GetStringParam(type)];
}

void _PMWKTagsShow(char* type)
{
    [[PMWKTags sharedManager] ShowTagWithType:GetStringParam(type)];
}

bool _PMWKTagsIsReady(char* placement)
{
    NSLog(@"PMWKTagsBinding :: _PMWKTagsIsReady :: %d",[[PMWKTags sharedManager] IsTagReadyWithPlacement:GetStringParam(placement)]);
    return [[PMWKTags sharedManager] IsTagReadyWithPlacement:GetStringParam(placement)];
}
