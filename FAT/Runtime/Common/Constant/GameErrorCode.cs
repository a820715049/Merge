/**
 * @Author: handong.liu
 * @Date: 2020-09-28 13:58:48
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public enum GameErrorCode
{
    Ok = 0,
    Unknown = 1,
    EmptyResponse = 2,
    NotInitialize = 3,
    NoConnection = 4,
    DecodeError = 5,
    PlatformInitError = 6,
    PlatformLoginError = 7,
    UserCanceled = 8,
    WrongPassword = 9,

    Duplicated = 10,
    HumanReadableError = 11,
    NoNetwork = 12,
    ConfigLoadFail = 14,
    ArchiveError = 15,  
    ServerSignError = 16,
    ResManifestLoadError = 17,
    CoinError = 18,
    WrongState = 19,


    SocialAlreadyBound = 101,
    SocialNoPermission = 102,
    SocialTokenError = 103,
    SocialBoundToOther = 104,
    SocialNotBound = 105,


    NotEnoughCoin = 301,
    NoChanceLeft = 311,
    NoItem       = 312,
    CoolDown     = 313,
    Ads          = 314,

    Delayed = 401,

    AccountChanged = 501,

    GiftCodeWrong = 601,

    AlreadyClaimed = 611,
}

public enum ErrorCodeType : int
{
    GameError = 0,
    HttpError = 1,
    ServerError = 2,
    SDKError = 3,
    UpdateError = 4
}

public static class ErrorCodeUtility
{
    public const long kCodeRange = 10000000;
    private const long kSDKCodeRange = 100000;

    public static long ConvertToCommonCode(GameErrorCode code)
    {
        return (long)code;
    }

    public static long ConvertToCommonCode(long code,  ErrorCodeType type)
    {
        if(type == ErrorCodeType.SDKError)
        {
            code = _ConvertSDKError(code);
        }
        return kCodeRange * (int)type + code;
    }

    public static void ExtractComonCode(long commonCode, out ErrorCodeType type, out long code)
    {
        type = (ErrorCodeType)(commonCode / kCodeRange);
        code = commonCode % kCodeRange;
    }

    /// <summary>
    /// 游戏发生错误时 根据错误码算出type来获取对应key
    /// </summary>
    /// <param name="t"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public static string GetNoticeContentByErrorCodeType(long code)
    {
        var t = (ErrorCodeType)(code / kCodeRange);
        var content = "";
        switch (t)
        {
            case ErrorCodeType.UpdateError:
                content = I18N.FormatText("#SysComDesc714", code);
                break;
            case ErrorCodeType.SDKError:
                content = I18N.FormatText("#SysComDesc713", code);
                break;
            case ErrorCodeType.HttpError:
                content = I18N.FormatText("#SysComDesc711", code);
                break;
            case ErrorCodeType.GameError:
                content = I18N.FormatText("#SysComDesc710", code);
                break;
            case ErrorCodeType.ServerError:
                content = I18N.FormatText("#SysComDesc712", code);
                break;
            default: break;
        }
        return content;
    }

    public static GameErrorCode ConvertToGameError(long commonCode)
    {
        GameErrorCode error = GameErrorCode.Unknown;
        ExtractComonCode(commonCode ,out var type, out var code);
        switch(type)
        {
            case ErrorCodeType.SDKError:
            {
                code = code % kSDKCodeRange;
                switch(code)
                {
        #if UNITY_ANDROID
                    case 2108:          //FacebookNeedUserFriendsPermission
                        error = GameErrorCode.SocialNoPermission;
                        break;
                    case 2100:          //FacebookNotLoggedIn
                        error = GameErrorCode.SocialTokenError;
                        break;
                    case 2106:
                    case 2403:
                    case 2501:
                    case 2601:
                        error = GameErrorCode.SocialBoundToOther;
                        break;
                    case 2104:
                    case 3104:
                    case 3311:      //survey monkey
                        error = GameErrorCode.UserCanceled;
                        break;
                    case 1104:
                    case 1105:
                        error = GameErrorCode.WrongPassword;
                        break;
        #elif UNITY_IPHONE
                    case 2108:          //FacebookNeedUserFriendsPermission
                        error = GameErrorCode.SocialNoPermission;
                        break;
                    case 2100:          //FacebookNotLoggedIn
                        error = GameErrorCode.SocialTokenError;
                        break;
                    case 2106:
                    case 2005:
                    case 2410:
                    case 2605:
                    case 2800:
                    case 2903:
                        error = GameErrorCode.SocialBoundToOther;
                        break;
                    case 2104:
                    case 3207:
                        error = GameErrorCode.UserCanceled;
                        break;
                    case 1104:
                    case 1105:
                        error = GameErrorCode.WrongPassword;
                        break;
        #endif
                }
                break;
            }
            case ErrorCodeType.GameError:
            error = (GameErrorCode)code;
            break;
        }
        return error;
    }
    
    private static long _ConvertSDKError(long code)
    {
        long add = 
#if UNITY_ANDROID
            1 * kSDKCodeRange;
#elif UNITY_IPHONE
            2 * kSDKCodeRange;
#else
            0 * kSDKCodeRange;
#endif
        return code + add;
    }
}