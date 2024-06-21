#import <Foundation/Foundation.h>
#import <MolocoCustomAdapter/MolocoCustomAdapter-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif

    void moloco_setHasUserConsent(bool val);
    void moloco_setIsAgeRestrictedUser(bool val);
    void moloco_setIsDoNotSell(bool val);

#ifdef __cplusplus
}
#endif
