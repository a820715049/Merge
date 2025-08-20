using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static fat.rawdata.FeatureEntry;

namespace FAT
{
    public class GroupCommon : ActivityGroup
    {
        public override (bool, string) CreateCheck(EventType type_, LiteInfo lite_)
        {
            var feature = Game.Manager.featureUnlockMan;
            var rf = nameof(feature);
            return type_ switch
            {
                EventType.MarketIapgift => (feature.IsFeatureEntryUnlocked(FeatureMarketIapgift), rf),
                EventType.FlashOrder => (feature.IsFeatureEntryUnlocked(FeatureFlashOrder), rf),
                EventType.Score => (feature.IsFeatureEntryUnlocked(FeatureScore), rf),
                EventType.Step => (feature.IsFeatureEntryUnlocked(FeatureStep), rf),
                EventType.OrderExtra => (feature.IsFeatureEntryUnlocked(FeatureOrderExtra), rf),
                EventType.LoginGift => (feature.IsFeatureEntryUnlocked(FeatureLoginGift), rf),
                EventType.Treasure => (feature.IsFeatureEntryUnlocked(FeatureTreasure), rf),
                EventType.Survey => (feature.IsFeatureEntryUnlocked(FeatureSurvey), rf),
                EventType.Race => (feature.IsFeatureEntryUnlocked(FeatureRace), rf),
                EventType.Digging => (feature.IsFeatureEntryUnlocked(FeatureDigging), rf),
                EventType.Invite => (feature.IsFeatureEntryUnlocked(FeatureInvite), rf),
                EventType.Rank => (feature.IsFeatureEntryUnlocked(FeatureRank), rf),
                EventType.ZeroQuest => (feature.IsFeatureEntryUnlocked(FeatureZeroQuest), rf),
                EventType.Stamp => (feature.IsFeatureEntryUnlocked(FeatureStamp), rf),
                EventType.Wishing => (feature.IsFeatureEntryUnlocked(FeatureWishing), rf),
                EventType.Guess => (feature.IsFeatureEntryUnlocked(FeatureGuess), rf),
                EventType.OrderDash => (feature.IsFeatureEntryUnlocked(FeatureOrderDash), rf),
                EventType.OrderStreak => (feature.IsFeatureEntryUnlocked(FeatureOrderStreak), rf),
                EventType.ItemBingo => (feature.IsFeatureEntryUnlocked(FeatureItemBingo), rf),
                EventType.OrderLike => (feature.IsFeatureEntryUnlocked(FeatureOrderLike), rf),
                EventType.ScoreDuel => (feature.IsFeatureEntryUnlocked(FeatureScoreDuel), rf),
                EventType.WeeklyTask => (feature.IsFeatureEntryUnlocked(FeatureWeeklyTask), rf),
                EventType.CastleMilestone => (feature.IsFeatureEntryUnlocked(FeatureCastleMilestone), rf),
                EventType.OrderRate => (feature.IsFeatureEntryUnlocked(FeatureOrderRate), rf),
                EventType.OrderBonus => (feature.IsFeatureEntryUnlocked(FeatureOrderBonus), rf),
                EventType.ClawOrder => (feature.IsFeatureEntryUnlocked(FeatureClawOrder), rf),
                EventType.WeeklyRaffle => (feature.IsFeatureEntryUnlocked(FeatureWeeklyRaffle), rf),
                EventType.ThreeSign => (feature.IsFeatureEntryUnlocked(FeatureThreeSign), rf),
                EventType.Community => (feature.IsFeatureEntryUnlocked(FeatureCommunity), rf),
                EventType.BingoTask => (feature.IsFeatureEntryUnlocked(FeatureBingoTask), rf),
                _ => (true, null),
            };
        }

        public override ActivityLike Create(EventType type_, ActivityLite lite_)
            => type_ switch
            {
                EventType.MarketIapgift => new ActivityMIG(lite_),
                EventType.FlashOrder => new ActivityFlashOrder(lite_),
                EventType.Score => new ActivityScore(lite_),
                EventType.Step => new ActivityStep(lite_),
                EventType.OrderExtra => new ActivityExtraRewardOrder(lite_),
                EventType.LoginGift => new LoginGiftActivity(lite_),
                EventType.Treasure => new ActivityTreasure(lite_),
                EventType.Survey => new ActivitySurvey(lite_),
                EventType.Race => new ActivityRace(lite_),
                EventType.Digging => new ActivityDigging(lite_),
                EventType.Invite => new ActivityInvite(lite_),
                EventType.Rank => new ActivityRanking(lite_),
                EventType.ZeroQuest => new ActivityOrderChallenge(lite_),
                EventType.Stamp => new ActivityStamp(lite_),
                EventType.Wishing => new ActivityMagicHour(lite_),
                EventType.Guess => new ActivityGuess(lite_),
                EventType.OrderDash => new ActivityOrderDash(lite_),
                EventType.OrderStreak => new ActivityOrderStreak(lite_),
                EventType.ItemBingo => new ActivityBingo(lite_),
                EventType.OrderLike => new ActivityOrderLike(lite_),
                EventType.ScoreDuel => new ActivityDuel(lite_),
                EventType.WeeklyTask => new ActivityWeeklyTask(lite_),
                EventType.CastleMilestone => new ActivityCastle(lite_),
                EventType.OrderRate => new ActivityOrderRate(lite_),
                EventType.OrderBonus => new ActivityOrderBonus(lite_),
                EventType.ClawOrder => new ActivityClawOrder(lite_),
                EventType.Redeem => new ActivityRedeemShopLike(lite_),
                EventType.WeeklyRaffle => new ActivityWeeklyRaffle(lite_),
                EventType.ThreeSign => new ActivityThreeSign(lite_),
                EventType.Community => new CommunityMailActivity(lite_),
                EventType.BingoTask => new ActivityBingoTask(lite_),
                _ => null,
            };
    }
}