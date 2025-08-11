/**
 * @Author: handong.liu
 * @Date: 2020-07-14 10:54:23
 */
public static class Constant
{
    public const int MainBoardId = 1;   //默认主棋盘id为1
    //public const int kArchiveVersion = 6;
    // public const int kArchiveVersion = 7;       //版本7变化：EnergyInfo从PlayerGameData放到PlayerBaseData里
    // public const int kArchiveVersion = 8;       //版本8变化：PushShop的Provider数据保存到一个字典里
    // public const int kArchiveVersion = 9;       //版本9变化：AutoSource,Bubble,ClickSource,Dying四个组件的Start时间取消，都归到Counter里
    // public const int kArchiveVersion = 10;       //版本10变化：Merge.InvItems, Merge.InvCapacity 独立成结构，放到Merge.Inventory里
    public const int kArchiveVersion = 11;       //版本11变化：ABTest结束, 根据建造信息统一调整用户等级
    public const int kArchiveAutoSaveInterval = 5;  //玩家存档自动向服务器保存的时间间隔
    public const string kUrl = "RP.PY^2iqnsCI$N$L'_'_j4>8owB{wQU";     //其实是协议签名用的salt。不要被变量名迷惑
    public readonly static float kMapTileSize = 0.5f;
    public const string kSignHeader = "x-sign";
    public const byte kUserLength1 = (byte)'$' ^ (byte)0x35;         //加密魔法数1、5 异或 0x35
    public const byte kUserLength2 = (byte)'C' ^ (byte)0x75;         //加密魔法数2 异或 0x75
    public const byte kUserLength3 = (byte)'G' ^ (byte)0x91;         //加密魔法数3 异或 0x91
    public const byte kUserLength4 = (byte)'F' ^ (byte)0x45;         //加密魔法数4 异或 0x45
    public static readonly string kUserMagic = "$CGF$";
    public static readonly byte[] kUserLengthLength1 = new byte[] {0x3e, 0x8e, 0x85, 0x59, 0xe6, 0x5b, 0x19, 0x33, 0x5e, 0xae, 0x96, 0x2f, 0xd3, 0x35, 0x85, 0x9f};       //加密key(每个数字异或斐波那契数列对应位置)
    public readonly static Config.AssetConfig kCommonRes = new Config.AssetConfig(){Group = "common_firstload", Asset = "commonres.asset"};
    public readonly static Config.AssetConfig KFlyConfig = new Config.AssetConfig() { Group = "fat_fly", Asset = "UIFlyConfig.asset" };
    public readonly static Config.AssetConfig kFontMatRes = new Config.AssetConfig(){Group = "fat_font_style", Asset = "Font_MaterialRes.asset"};
    public readonly static Config.AssetConfig kNiNiHomeData = new Config.AssetConfig(){Group = "data", Asset = "dective_home.json"};
    public readonly static Config.AssetConfig kDefaultSocialAvatar = new Config.AssetConfig(){Group = "ui_common", Asset = "tx_empty_role.png"};
    public readonly static Config.AssetConfig kMainBoardIcon = new Config.AssetConfig() {Group = "ui_merge", Asset = "common_icon_merge.png"};
    public readonly static Config.AssetConfig kFBShareImage = new Config.AssetConfig(){Group = "ui_friend", Asset = "tx_img_fb_share.png"};
    public readonly static string kExternalResList = "external_file_list.x";
    public readonly static string kEmptyAvatarUrl = "__empty";
    public readonly static string kPredefineI18NMyName = "#__MYNAME";
    public const int kMergeItemIdBase = 12000000;
    public const int kCardIdBase = 22000000;
    public const int kStudentIdBase = 71001;
    public const int kWallpaperIdBase = 30001;
    public const int kObjIdCapacity = 1000000;
    public const int kChangeBoardOrderId = 99999999;
    public readonly static int kPartyNPCId = 6000003;
    public readonly static int kUserNPCId = 6000000;
    public readonly static int kPushShopNPCId = 6000033;
    public readonly static int kNiNiNPCId = 6000001;
    public readonly static int kLuckyBoxNPCId = 6000032;
    public readonly static int kSubconsciousNPCId = 6000020;
    public readonly static int kDailySignNPCId = 6000019;
    public readonly static int kIdleActionPoseId = 5000034;
    public readonly static int kPlayerHomeSceneId = 2001;
    public readonly static string kArchiveKey1 = "TaskDataTime";
    public readonly static string kArchiveKey2 = "SurveyData";
    public const long kSubscribeValidDelay = 600;
    public const int kNetworkBatchCount = 50;
    public readonly static int kSocialRobotGuideId = 7;
    public const int kMergeBubbleGuideId = 12;
    public const int kWheelExpId = 43;
    public readonly static int kActivityOpenLevel = 8;
    public const int kPetSystemOpenIslandCount = 2;
    public const int kPetSelectWaitTime = 4 * 3600;         //宠物选择等待时间
    public const float kPetFollowSpace = 1.0f;
    public const float kPetFollowMinSpace = 0.5f;
    public const int kDayRefreshSeconds = 0;
    public const long kFriendListTimeout = 300;         //5分钟取一次好友列表
    public const int kSecondsPerDay = 86400;
    public const int kGameVirtualTimeScale = 600;           //游戏中日月变换时间加速factor
    public const int kGameVirtualDayMilli = 86400000;       //游戏中一天多少毫秒
    public const int kUploadDataTimeout = 10;     //in seconds
    public const float kMaxCameraDist = 30f;
    public const float kMinCameraDist = 15f;
    public const int kIntimacyObjId = 10;
    public const int kIAPMentorObjId = 20;
    public const float kDitherMaxFadeDistance = 10;
    public const float kDitherMinFadeDistance = 9;
    public const int kIAPCloneRobotObjId = 21;
    public const int kIAPAdsDog = 22;
    public const int kStrangerCardObjId = 24;
    public const int kSpeedupGuideObjId = 12000768;
    public const int kSpeedupGuideId = 30;
    public const int kPartyGuideId1 = 13;
    public const int kPartyGuideId2 = 14;
    public const int kAdsVIPObjId = 22;
    public const int kNiNiAdsGuideId = 7;
    public const int kHostPlayerRoleId = 1;
    public const int kHostPlayerPetId = 2;
    public const int kHostPlayerShipId = 3;
    public const int kHeadObjId = 7000001;
    public readonly static string kPrefKeyDebugFPID = "UserKey";
    public readonly static string kPrefKeyDebugSession = "UserSession";
    public readonly static string kPrefKeyUserData = "UserData";
    public readonly static string kPrefKeyRoleData = "RoleData";
    public readonly static string kPrefKeyTaskData = "TaskData";
    public readonly static string kPrefKeySurveyData = "SurveyData";
    public const int kInvalidObjId = 0;
    public const string kHotfixPath = "hotfix";
    public const string kConfPath = "lua/gen";
    public const string kConfExt = ".bytes";
    public static readonly string kHaveRated = "HaveRated";
    // public const int kMergeBubbleItemLife = 120000;              //气泡的持续时间（毫秒）
    public const int kMergeBubbleDeadItem = kMergeCoinItemObjId + 2;              //气泡的持续时间（毫秒）
    public const int kMergeExpItemObjId = 12000001;              //第一个exp合成物品
    public const int kMergeCoinItemObjId = 12000791;             //第一个coin合成物品
    public const int kMergeEnergyCostNum = 1;                   //棋盘点击产出棋子时默认要消耗的能量点数
    public const int kMergeExpObjId = 30;
    public const int kEventExpObjId = 43;
    public const int kMergeEnergyObjId = 31;
    public const int kMergeEnergyForEventObjId = 32;            //活动专用体力
    public const int kMergeInfinateEnergyObjId = 40;            //无限体力
    public const int kRandomPostCardObjId = 41;
    public const int kBingoGeneratorId = 12001614;
    public const int kChannelFunplus = 8;
    public readonly static Config.AssetConfig[] kPresetAvatars = new Config.AssetConfig[] {
        new Config.AssetConfig(){Group = "ui_common", Asset = "tx_test_role_1.png"},
    };
    public readonly static Config.AssetConfig kDefaultRoleController = new Config.AssetConfig() {Group = "char_ske", Asset = "CharacterAnimator.controller"};
    public readonly static Config.AssetConfig kPetCageScene = new Config.AssetConfig() {Group = "pet_scene", Asset = "cage.prefab"};
    public readonly static Config.AssetConfig kPetRewardBox = new Config.AssetConfig() {Group = "pet_scene", Asset = "pet_prop_gift.prefab"};
    public readonly static Config.AssetConfig kPetNormalScene = new Config.AssetConfig() {Group = "pet_scene", Asset = "normal.prefab"};
    public readonly static Config.AssetConfig kDefaultBallon = new Config.AssetConfig(){Group = "scene_balloon", Asset = "balloon.prefab"};

    #region dreammerge field
    public const int kMaxTaskId = 1000000;          //not included
    public const int kMainRoleId = 71001;               //主角的角色id
    #endregion
}