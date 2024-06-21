#import "MolocoPrivacy.h"

void moloco_setHasUserConsent(bool val)
{
[MolocoPrivacySettings setHasUserConsent:val];
}

void moloco_setIsAgeRestrictedUser(bool val)
{
[MolocoPrivacySettings setIsAgeRestrictedUser:val];
}

void moloco_setIsDoNotSell(bool val)
{
[MolocoPrivacySettings setIsDoNotSell:val];
}
