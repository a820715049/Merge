/*
 *@Author:chaoran.zhang
 *@Desc:
 *@Created Time:2024.03.07 星期四 19:24:54
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FAT
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/UIFlyConfig", order = 1)]
    public class UIFlyConfig : ScriptableObject
    {
        public static UIFlyConfig Instance;
        public GameObject icon;
        public float durationAdd;
        public float durationFly;
        public float durationScatter;
        public float durationStop;
        public float scatterWidth;
        public float scatterHeight;
        public float scaleScatterEnd;
        public float durationElasticStart;
        public float durationRewardElasticStart;
        public float scaleElasticStartTo;
        public float scaleElasticEnd;
        public float scaleRewardElasticStart;
        public float scaleRewardElasticStartTo;
        public float scaleRewardElasticEnd;

        public float durationShowRewardStart;
        public float durationShowReward;
        public float durationShowRewardEnd;
        public float scaleShowRewardStart;
        public float scaleShowRewardEnd;

        public float durationShowItemOrderRate;
        public float rotateSpeed;
        public float attackScatter;
        public float attackInterval;

        public AnimationCurve curveElasticStart;
        public AnimationCurve curveElasticEnd;
        public AnimationCurve curveRewardElasticStart;
        public AnimationCurve curveRewardElasticEnd;
        public AnimationCurve curveHorizontal;
        public AnimationCurve curveVertical;
        public AnimationCurve curveRankingUp;
        public AnimationCurve curveRankingDown;


        public static List<int> ClipNumList = new() { 1, 2, 4, 8, 10, 12 };
        public static List<int> ExpRank = new() { 1, 3, 8, 20, 50 };
        public static List<int> EnergyRank = new() { 2, 6, 16, 40, 100 };
        public static List<int> CoinRank = new() { 1, 3, 8, 20, 50, 120 };
        public static List<int> GemRank = new() { 1, 3, 8, 20 };
        public static List<int> DefaultRank = new() { 1, 3, 8, 20, 50, 120 };
        public static List<int> SingleRank = new() { 1 };
    }

    public enum FlyStyle
    {
        Common,
        Cost,
        Reward,
        Show,
        Score
    }

    public enum FlyReason
    {
        None,
        OrderItemDie,
        CoinChange,
        ExpChange
    }

    public enum FlyType
    {
        None,
        Coin,
        Gem,
        Energy,
        Star,
        Exp,
        MergeItemFlyTarget,
        Inventory,
        EventExp,
        EventCoin,
        EventScore,
        BoardCenter,
        Wallpaper,
        ProfileDeco, // 头像 & 头像框
        CardJoker, // 集卡系统万能卡
        TreasureBag, // 寻宝临时背包
        TreasureKey, // 寻宝钥匙
        DecorateToken, //装饰活动代币
        RaceToken,
        MiniBoard, //mini棋盘奖励
        TapBonus, //双击领取棋子
        DiggingShovel, //挖沙铲子
        Handbook, //图鉴
        MilestoneToken,
        Pachinko,
        EnergyBoost,
        MiniBoardMulti, //多轮mini棋盘奖励
        EndlessToken,   //6格无限礼包进度条版token
        EndlessThreeToken, //3格无限礼包进度条版token
        GuessToken,
        GuessMilestone,
        PackDiscountToken, //砍价礼包token
        MineToken, //挖矿棋盘代币
        MineScore, //挖矿棋盘进度条进度值
        FlyToMainBoard, //在活动棋盘中获得了必须发往主棋盘的奖励时
        OrderLikeToken,    //好评订单token
        DuelToken,
        DuelMilestone,
        FishBoardEntry,
        FishSpawnPoint,
        FishMilestoneStar,
        OrderBonus,
        WeeklyTaskEntry,
        RedeemCoindEntry,
        FarmBoardEntry, //在主棋盘上获得农场棋盘代币并飞往活动入口
        FarmToken, // 农场棋盘代币
        TreasureBonusToken,
        FightBoardEntry,
        FightBoardMonster,
        FightBoardTreasure,
        WishBoardToken,
        WishBoardMilestone,
        WishBoardScore,
        WeeklyRaffleToken, // 签到抽奖
        BPExp, //BP经验
        CastleToken,
        ClawOrderToken,     // 抓宝活动代币
        MineCartGetItem,    //矿车棋盘从订单或耗体行为产出活动棋子 并飞到活动入口
        MineCartUseItem,    //矿车棋盘使用活动棋子，棋子飞往上方矿车处
        MineCartGetItemReward, //矿车棋盘使用活动棋子后，棋子飞行的起点
        MineCartRewardBubble, //矿车棋盘发奖气泡
        MultiRankingToken,
        PuzzleToken,
        Default,
        SevenDayToken,
    }
}
