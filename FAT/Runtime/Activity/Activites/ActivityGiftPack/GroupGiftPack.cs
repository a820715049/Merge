using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static fat.rawdata.FeatureEntry;

namespace FAT
{
    public class GroupGiftPack : ActivityGroup
    {
        public GroupGiftPack()
        {
            SetupListener();
        }

        public void SetupListener()
        {
            MessageCenter.Get<MSG.IAP_LATE_DELIVERY>().AddListenerUnique(LateDelivery);
            MessageCenter.Get<MSG.IAP_REWARD_CHECK>().AddListenerUnique(RefreshPack);
        }

        public override (bool, string) FilterOne((int, int) id_, EventType type_)
        {
            return type_ switch
            {
                EventType.Energy => (false, "filter rule"),
                EventType.GemEndlessThree => (false, "filter rule"),
                EventType.EnergyMultiPack => (false, "filter rule"),
                EventType.NewSession => (false, "filter deprecated"),//NOTE deprecated by trigger
                _ => (true, null),
            };
        }

        public override (bool, string) CreateCheck(EventType type_, LiteInfo lite_)
        {
            var feature = Game.Manager.featureUnlockMan;
            return type_ switch
            {
                EventType.OnePlusOne => (feature.IsFeatureEntryUnlocked(FeatureOnePlusOne), "feature"),
                EventType.MineOnePlusOne => (feature.IsFeatureEntryUnlocked(FeatureMineOnePlusOne), "feature"),
                EventType.OnePlusTwo => (feature.IsFeatureEntryUnlocked(FeatureOnePlusTwo), "feature"),
                EventType.EndlessPack => (feature.IsFeatureEntryUnlocked(FeatureEndlessPack), "feature"),
                EventType.EndlessThreePack => (feature.IsFeatureEntryUnlocked(FeatureEndlessThreePack), "feature"),
                EventType.GemEndlessThree => (feature.IsFeatureEntryUnlocked(FeatureGemEndlessThree), "feature"),
                EventType.FarmEndlessPack => (feature.IsFeatureEntryUnlocked(FeatureFarmEndlessPack), "feature"),
                EventType.ProgressPack => (feature.IsFeatureEntryUnlocked(FeatureProgressPack), "feature"),
                EventType.RetentionPack => (feature.IsFeatureEntryUnlocked(FeatureRetentionPack), "feature"),
                EventType.DailyPop => PackDaily.ReadyToCreate(lite_.param),
                EventType.NewSession => PackNewSession.ReadyToCreate(lite_.param),
                EventType.ShinnyGuarPack => PackShinnyGuar.ReadyToCreate(lite_.param),
                EventType.ThreeForOnePack => (feature.IsFeatureEntryUnlocked(FeatureThreeForOnePack), "feature"),
                EventType.MarketSlidePack => (feature.IsFeatureEntryUnlocked(FeatureMarketSlidePack), "feature"),
                EventType.GemThreeForOne => (feature.IsFeatureEntryUnlocked(FeatureGemThreeForOne), "feature"),
                EventType.EnergyMultiPack => (feature.IsFeatureEntryUnlocked(FeatureEnergyMultiPack), "feature"),
                EventType.DiscountPack => (feature.IsFeatureEntryUnlocked(FeatureDiscountPack), "feature"),
                EventType.ErgListPack => (feature.IsFeatureEntryUnlocked(FeatureErgListPack), "feature"),
                EventType.FightOnePlusOne => (feature.IsFeatureEntryUnlocked(FeatureFightOnePlusOne), "feature"),
                EventType.WishEndlessPack => (feature.IsFeatureEntryUnlocked(FeatureWishEndlessPack), "feature"),
                EventType.SpinPack => (feature.IsFeatureEntryUnlocked(FeatureSpinPack), "feature"),
                EventType.Bp => (feature.IsFeatureEntryUnlocked(FeatureBp), "feature"),
                EventType.CartOnePlusOne => (feature.IsFeatureEntryUnlocked(FeatureCartOnePlusOne), "feature"),
                _ => (true, null),
            };
        }

        public override ActivityLike Create(EventType type_, ActivityLite lite_)
            => type_ switch
            {
                EventType.Energy => new PackEnergy(lite_),
                EventType.DailyPop => new PackDaily(lite_),
                EventType.NewSession => new PackNewSession(lite_),
                EventType.OnePlusOne => new PackOnePlusOne(lite_),
                EventType.MineOnePlusOne => new PackOnePlusOneMine(lite_),
                EventType.OnePlusTwo => new PackOnePlusTwo(lite_),
                EventType.EndlessPack => new PackEndless(lite_),
                EventType.EndlessThreePack => new PackEndlessThree(lite_),
                EventType.GemEndlessThree => new PackGemEndlessThree(lite_),
                EventType.FarmEndlessPack => new PackEndlessFarm(lite_),
                EventType.ThreeForOnePack => new Pack1in3(lite_),
                EventType.ProgressPack => new PackProgress(lite_),
                EventType.RetentionPack => new PackRetention(lite_),
                EventType.MarketSlidePack => new PackMarketSlide(lite_),
                EventType.GemThreeForOne => new PackGemThreeForOne(lite_),
                EventType.EnergyMultiPack => new PackEnergyMultiPack(lite_),
                EventType.ShinnyGuarPack => new PackShinnyGuar(lite_),
                EventType.DiscountPack => new PackDiscount(lite_),
                EventType.LevelPack => new PackLevel(lite_),
                EventType.ErgListPack => new PackErgList(lite_),
                EventType.FightOnePlusOne => new PackOnePlusOneFight(lite_),
                EventType.WishEndlessPack => new PackEndlessWishBoard(lite_),
                EventType.SpinPack => new PackSpin(lite_),
                EventType.Bp => new BPActivity(lite_),
                EventType.CartOnePlusOne => new PackOnePlusOneMineCart(lite_),
                _ => null,
            };

        public void Purchase(IGiftPackLike pack_, Action<IList<RewardCommitData>> WhenComplete_ = null, Action<IAPPack> WhenFail_ = null)
            => Purchase(pack_, pack_.Content, WhenComplete_, WhenFail_);
        public void Purchase(IGiftPackLike pack_, IAPPack content_, Action<IList<RewardCommitData>> WhenComplete_ = null, Action<IAPPack> WhenFail_ = null)
        {
            var iap = Game.Manager.iap;
            iap.Purchase(content_, IAPFrom.GiftPack, (r_, p_) =>
            {
                if (!r_) { WhenFail_?.Invoke(p_); return; }
                PackPurchaseSuccess(pack_, content_.Id, WhenComplete_, late_: false);
            }, info_: ActivityLite.InfoCompact(pack_), order_: (ulong)ActivityLite.IdCompact(pack_));
        }

        private void PackPurchaseSuccess(IGiftPackLike pack_, int packId_, Action<IList<RewardCommitData>> WhenComplete_, bool late_)
        {
            var rewardMan = Game.Manager.rewardMan;
            using var _ = ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var list);
            var goods = pack_.MatchPack(packId_);
            if (goods == null) goto end;
            foreach (var r in goods.reward)
            {
                var data = rewardMan.BeginReward(r.Id, r.Count, ReasonString.purchase);
                list.Add(data);
                if (late_) rewardMan.CommitReward(data);
            }
            ++pack_.BuyCount;
            pack_.PurchaseSuccess(packId_, list, late_);
        end:
            WhenComplete_?.Invoke(list);
        }

        public void LateDelivery(IAPLateDelivery delivery_)
        {
            if (delivery_.from != IAPFrom.GiftPack) return;
            static string Reason((int, int) id_) => $"{nameof(GiftPack)} {id_}";
            var (valid, id, from, _, _) = ActivityLite.InfoUnwrap(delivery_.context.ProductName);
            var id2 = valid ? (id, from) : ActivityLite.IdUnwrap((int)delivery_.context.OrderId);
            var packId = delivery_.pack.Id;
            if (Activity.map.TryGetValue(id2, out var acti) && acti is GiftPack pack)
            {
                PackPurchaseSuccess(pack, packId, null, late_: true);
                delivery_.ClaimReason = Reason(id2);
                return;
            }

            DebugEx.Warning($"{nameof(Activity)} late delivery found no matching active pack of id:{id}, will try pack id {packId}");
            static void ClaimReward(IList<string> reward_)
            {
                var rewardMan = Game.Manager.rewardMan;
                foreach (var s in reward_)
                {
                    var r = s.ConvertToRewardConfig();
                    var data = rewardMan.BeginReward(r.Id, r.Count, ReasonString.purchase);
                    rewardMan.CommitReward(data);
                }
            }
            var iap = Game.Manager.iap;
            if (ActivityLite.Exist(id2) && iap.FindIAPPack(packId, out var p))
            {
                ClaimReward(p.Reward);
                delivery_.ClaimReason = Reason(id2);
                return;
            }
            DebugEx.Warning($"{nameof(Activity)} late delivery failed to find id:{id2} or packId:{packId}");
        }

        public void RefreshPack()
        {
            foreach (var (_, a) in Activity.map)
            {
                if (a is GiftPack p)
                    p.RefreshPack();
            }
        }
    }
}