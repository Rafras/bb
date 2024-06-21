//
//  PMWKTags.h
//  Unity-iPhone
//
//  Created by Wasilewski on 11.05.2017.
//
//

#import <WebKit/WebKit.h>

@interface PMWKTags : NSObject<WKUIDelegate, WKNavigationDelegate, UIScrollViewDelegate>

@property (nonatomic, retain) NSMutableDictionary* tags;
@property (nonatomic, retain) NSMutableDictionary* placements;

+ (PMWKTags*)sharedManager;

-(void)LoadTagUrl:(NSString*) url WithType:(NSString*) type;
-(void)ShowTagWithType:(NSString*) type;
-(bool)IsTagReadyWithPlacement:(NSString*) placement;

@end
