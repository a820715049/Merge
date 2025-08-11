/*
 * @Author: qun.chao
 * @Date: 2023-10-16 17:07:46
 */

[System.Flags]
public enum ObjConfigType
{
    None = 0,
    Basic = 1,
    Deco = 1 << 1,
    HomeItem = 1 << 2,
    RolePart = 1 << 3,
    Wallpaper = 1 << 4,
    Floor = 1 << 5,
    Island = 1 << 6,
    Pose = 1 << 7,
    Role = 1 << 8,
    Coin = 1 << 9,
    Item = 1 << 10,
    Career = 1 << 11,
    RandomBox = 1 << 12,
    MergeItem = 1 << 13,
    CardPack = 1 << 14, //卡包 ObjCardPack
    Card = 1 << 15,     //卡包中的卡片 ObjCard
    Buff = 1 << 16,
    AvatarFrame = 1 << 17,
    DecoReward = 1 << 18,
    RewardVip = 1 << 19,
    ProfileDeco = 1 << 20,
    ActivityToken = 1 << 21,
    CardJoker = 1 << 22,    //集卡系统中的万能卡
    SeasonItem = 1 << 23,   //赛季物品 没有视图层相关属性 自身会根据配置转换成某个具体物品
}