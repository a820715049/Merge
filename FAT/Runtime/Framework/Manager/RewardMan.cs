/**
 * @Author: handong.liu
 * @Date: 2020-09-08 10:48:54
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using EL;
using Config;
using fat.rawdata;
using EventType = fat.rawdata.EventType;
using System.Runtime.CompilerServices;
using System.Text;

/***
*
* 关于buff类Reward的count用作时间戳的说明：
* 为了保持接口不变，改动尽量少（不光客户端类，配置、服务器协议里也一样）， buff的结束时间戳用int32表示。从而可以实现RewardMan处理buff，增加游戏设计的自由度
* int32最大为2147483647，也就是到北京时间2038-01-19 11:14:07，在此之后，经过rewardman给的buff会不正常
*
****/
namespace FAT
{
    public class RewardMan : IGameModule
    {
        private Stack<RewardContext> mContextStack = new Stack<RewardContext>();
        private List<RoundCoin> mRoundCoinList = new();
        private List<RoundTool> mRoundToolList = new();
        private List<RoundLifeTime> mRoundLifeTimeList = new();
        //每次begin时都记录一下需要被commit的数据 用于在Reset时检查 避免有需要commit的奖励被遗漏
        private List<RewardCommitData> _needCommitDataList = new List<RewardCommitData>();

        private const int kRewardStringNoCountMask = /* (int)ObjConfigType.Deco | (int)ObjConfigType.RolePart | (int)ObjConfigType.Role | */(int)ObjConfigType.RewardVip;

        public void ReportCommit()
        {
            var b = new StringBuilder("reward commit data:").AppendLine();
            if (_needCommitDataList.Count == 0)
            {
                b.Append("none");
                DebugEx.Warning(b.ToString());
                return;
            }
            foreach (var d in _needCommitDataList)
            {
                b.Append(d.rewardId).Append(':').Append(d.rewardCount).Append(' ');
                b.Append("reason:").Append(d.reason).Append(' ');
                if (d.flags != RewardFlags.None) b.Append("flags:").Append(d.flags).Append(' ');
                var w = d.context.targetWorld;
                if (w != null) b.Append("world:").Append(w.activeBoard?.boardId ?? -1).Append(' ');
                if (d.WaitCommit) b.Append("(wait)");
                if (d.isFake) b.Append("(fake)");
                b.AppendLine();
                var s = "Scripts";
                var ss = d._f.IndexOf(s);
                var f = ss > 0 ? d._f[(ss + s.Length + 1)..] : d._f;
                b.Append('>').Append(d._m).Append('@').Append(f).Append('#').Append(d._l);
                b.AppendLine();
            }
            DebugEx.Warning(b.ToString());
        }

        public void TestCommit()
        {
            var w = Game.Manager.mergeBoardMan.activeWorld;
            var d = _GenerateReward(100, 10, ReasonString.daily_event);
            _needCommitDataList.Add(d);
            d = _GenerateReward(101, 10, ReasonString.free, flags: RewardFlags.IsEventPriority);
            _needCommitDataList.Add(d);
            d = _GenerateReward(102, 10, ReasonString.order, context_: new() { targetWorld = w });
            _needCommitDataList.Add(d);
            d = _GenerateReward(103, 10, ReasonString.free, flags: RewardFlags.IsUseIAP, context_: new() { targetWorld = w });
            d.WaitCommit = true;
            _needCommitDataList.Add(d);
            d = _GenerateReward(103, 10, ReasonString.free, flags: RewardFlags.IsUseIAP, context_: new() { targetWorld = w });
            d.isFake = true;
            _needCommitDataList.Add(d);
        }

        public void PushContext(RewardContext cxt)
        {
            DebugEx.FormatInfo("RewardMan::PushContext ----> {0},{1}", cxt.ToString(), mContextStack.Count);
            mContextStack.Push(cxt);
        }
        public void PopContext()
        {
            var poped = mContextStack.Pop();
            DebugEx.FormatInfo("RewardMan::PopContext ----> {0},{1}", poped.ToString(), mContextStack.Count);
        }
        public void CommitReward(RewardCommitData data)
        {
            var info = $"RewardMan.CommitReward ----> for reward: {data.ToString()}";
            DataTracker.TrackLogInfo(info);
            DebugEx.Info(info);
            //只有奖励数据被标记为WaitCommit时才会执行CommitReward逻辑，并在执行完后将其移出List
            if (data.WaitCommit)
            {
                _CommitReward(data);
                _needCommitDataList.Remove(data);
                data.WaitCommit = false;
            }
            //表现层会在commit执行后做逻辑 所以事件必然dispatch
            MessageCenter.Get<MSG.GAME_MERGE_POST_COMMIT_REWARD>().Dispatch(data);
        }

        //仅供UIFlyManager执行飞图标逻辑时使用
        public void CommitSplitReward(RewardCommitData data, int splitCount)
        {
            DebugEx.FormatInfo("RewardMan.CommitSplitReward ----> splitCount = {0}, for reward = {1}", splitCount, data.ToString());
            //reward飞行过程中会保持WaitCommit为true,在最后飞完时会走CommitReward,在其中将WaitCommit置为false
            if (data.WaitCommit && splitCount < data.rewardCount)
            {
                data.rewardCount -= splitCount;
                //每次commit被分割的部分
                _CommitReward(data, splitCount);
            }
        }

        //抽出更基本的方法用于commit
        private void _CommitReward(RewardCommitData data, int overrideCount = -1)
        {
            var isFake = data.isFake;
            if (isFake)
            {
                return;
            }
            var rewardId = data.rewardId;
            var rewardCount = overrideCount >= 0 ? overrideCount : data.rewardCount;
            switch (data.rewardType)
            {
                case ObjConfigType.Coin:
                    Game.Manager.coinMan.FinishFlyCoin(Game.Manager.coinMan.GetCoinTypeById(rewardId), rewardCount);
                    break;
                case ObjConfigType.RandomBox:
                case ObjConfigType.CardJoker:
                case ObjConfigType.CardPack:
                    Game.Manager.specialRewardMan.TryOpenSpecialReward(rewardId, rewardCount, data.reason);
                    break;
                case ObjConfigType.ActivityToken:
                    var tokenConf = Game.Manager.objectMan.GetTokenConfig(rewardId);
                    switch (tokenConf.Feature)
                    {
                        case FeatureEntry.FeatureOrderLike:
                            if (Game.Manager.activity.LookupAny(EventType.OrderLike, out var _orderlike))
                            {
                                (_orderlike as ActivityOrderLike).ResolveFlyingToken(rewardCount);
                                MessageCenter.Get<MSG.ORDERLIKE_TOKEN_CHANGE>().Dispatch();
                            }
                            break;
                        case FeatureEntry.FeatureDecorate:
                            if (rewardId == Game.Manager.decorateMan.Activity?.confD.RequireScoreId)
                            {
                                Game.Manager.decorateMan.FinishFlyCoin(rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureClawOrder:
                            if (Game.Manager.activity.LookupAny(EventType.ClawOrder, out var _clawOrder))
                            {
                                (_clawOrder as ActivityClawOrder).ResolveFlyingToken(rewardCount);
                            }
                            break;
                    }
                    break;
                default:
                    switch (rewardId)
                    {
                        case Constant.kMergeEnergyObjId:
                            Game.Manager.mergeEnergyMan.FinishFlyEnergy(rewardCount);
                            break;
                        case Constant.kMergeExpObjId:
                            Game.Manager.mergeLevelMan.FinishFlyExp(rewardCount);
                            break;
                        default:
                            {
                                if (Game.Manager.decorateMan.Activity == null)
                                    break;
                                if (rewardId == Game.Manager.decorateMan.Activity.confD.RequireScoreId)
                                {
                                    Game.Manager.decorateMan.FinishFlyCoin(rewardCount);
                                }
                                break;
                            }
                    }
                    break;
            }
        }

        private RewardCommitData _GenerateReward(int rewardId, int rewardCount, ReasonString reason, RewardFlags flags = RewardFlags.None, RewardContext context_ = default,
            [CallerLineNumber] int _l = 0, [CallerFilePath] string _f = null, [CallerMemberName] string _m = null)
        {
            if (string.IsNullOrEmpty(reason?.ToString()))
            {
                DebugEx.Error($"empty reason for reward {rewardId} {rewardCount}");
            }
            DebugEx.FormatInfo("RewardMan._GenerateReward ----> for id:{0}, count:{1}, reason:{2}", rewardId, rewardCount, reason);
            return new RewardCommitData(_l, _f, _m)
            {
                rewardId = rewardId,
                rewardType = Game.Manager.objectMan.DeduceTypeForId(rewardId),
                rewardCount = rewardCount,
                reason = reason,
                flags = flags,
                context = context_,
            };
        }

        private RewardCommitData _BeginReward(RewardCommitData data)
        {
            DebugEx.FormatInfo("RewardMan.BeginReward ----> for reward = {0}", data.ToString());
            if (mContextStack.Count > 0)
            {
                data.context = mContextStack.Peek();
            }
            if (data.isFake)
            {
                return data;
            }

            MessageCenter.Get<MSG.GAME_MERGE_PRE_BEGIN_REWARD>().Dispatch(data);

            var id = data.rewardId;
            var objectMan = Game.Manager.objectMan;
            static void TrackReward(RewardCommitData data) => DataTracker.get_item.Track(data.rewardId, data.rewardCount, data.reason);
            var networkMan = Game.Manager.networkMan;
            if (networkMan.isLogin) TrackReward(data);
            else networkMan.ExecuteAfterLogin(() => TrackReward(data));
            switch (data.rewardType)
            {
                case ObjConfigType.Coin:
                    data.WaitCommit = true;
                    Game.Manager.coinMan.AddFlyCoin(Game.Manager.coinMan.GetCoinTypeById(id), data.rewardCount, data.reason);
                    break;
                case ObjConfigType.RandomBox:
                case ObjConfigType.CardJoker:
                    data.WaitCommit = true;
                    Game.Manager.specialRewardMan.TryAddSpecialReward(id, data.rewardCount, data.reason);
                    break;
                case ObjConfigType.ActivityToken:
                    var tokenConf = Game.Manager.objectMan.GetTokenConfig(data.rewardId);
                    switch (tokenConf.Feature)
                    {
                        case FeatureEntry.FeatureDem:
                            Game.Manager.dailyEvent.UpdateMilestone(data.rewardId, data.rewardCount);
                            break;
                        case FeatureEntry.FeatureTreasure:
                            Game.Manager.activity.LookupAny(EventType.Treasure, out var act);
                            if (act != null)
                            {
                                var activityTreasure = (ActivityTreasure)act;
                                activityTreasure.UpdateScoreOrTreasureKey(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureDigging:
                            Game.Manager.activity.LookupAny(EventType.Digging, out var acti_);
                            if (acti_ != null)
                            {
                                var activityDigging = (ActivityDigging)acti_;
                                activityDigging.UpdateScoreOrKey(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureRedeem:
                            Game.Manager.activity.LookupAny(EventType.Redeem, out var activity);
                            if (activity != null)
                            {
                                var activityRedeemShop = (ActivityRedeemShopLike)activity;
                                activityRedeemShop.UpdateRedeemCoinNum(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureGuess:
                            if (Game.Manager.activity.LookupAny(EventType.Guess, out var actGuess))
                            {
                                var activityGuess = (ActivityGuess)actGuess;
                                activityGuess.UpdateScoreOrToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureCastleMilestone:
                            if (Game.Manager.activity.LookupAny(EventType.CastleMilestone, out var actCastle))
                            {
                                var activityCastle = (ActivityCastle)actCastle;
                                activityCastle.AddMilestoneScore(data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureDecorate:
                            data.WaitCommit = true;
                            Game.Manager.decorateMan.TryUpdateScore(data.rewardId, data.rewardCount, data.reason);
                            break;
                        case FeatureEntry.FeaturePachinko:
                            Game.Manager.pachinkoMan.TryAddToken(data.rewardId, data.rewardCount, data.reason);
                            break;
                        case FeatureEntry.FeatureEndlessPack:
                            Game.Manager.activity.LookupAny(EventType.EndlessPack, out var act1);
                            if (act1 != null)
                            {
                                var endlessAct = (PackEndless)act1;
                                endlessAct.TryAddProgressNum(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureDiscountPack:
                            Game.Manager.activity.LookupAny(EventType.DiscountPack, out var act3);
                            if (act3 != null)
                            {
                                var discountAct = (PackDiscount)act3;
                                discountAct.UpdateToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureEndlessThreePack:
                            Game.Manager.activity.LookupAny(EventType.EndlessThreePack, out var act2);
                            if (act2 != null)
                            {
                                var endlessThreeAct = (PackEndlessThree)act2;
                                endlessThreeAct.TryAddProgressNum(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureMine:
                            Game.Manager.mineBoardMan.TryAddToken(data.rewardId, data.rewardCount, data.reason);
                            break;
                        case FeatureEntry.FeatureFish:
                            Game.Manager.activity.LookupAny(EventType.Fish, out var actFish);
                            if (actFish != null)
                            {
                                var activityFish = (ActivityFishing)actFish;
                                activityFish.AddToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureFarmBoard:
                            Game.Manager.activity.LookupAny(EventType.FarmBoard, out var actFarm);
                            if (actFarm != null)
                            {
                                var activityFarm = (FarmBoardActivity)actFarm;
                                activityFarm.TryAddToken(data.rewardId, data.rewardCount, data.reason);
                            }
                            break;
                        case FeatureEntry.FeatureWeeklyTask:
                            break;
                        case FeatureEntry.FeatureOrderLike:
                            if (Game.Manager.activity.LookupAny(EventType.OrderLike, out var _orderlike))
                            {
                                data.WaitCommit = true;
                                var orderId = 0;
                                if (data.context.paramProvider is OrderLikeParamProvider orderLikeParamProvider)
                                {
                                    orderId = orderLikeParamProvider.orderId;
                                }
                                (_orderlike as ActivityOrderLike).AddToken(data.rewardId, data.rewardCount, orderId);
                            }
                            break;
                        case FeatureEntry.FeaturePuzzle:
                            if (Game.Manager.activity.LookupAny(EventType.Puzzle, out var _puzzle))
                            {
                                data.WaitCommit = true;
                                (_puzzle as ActivityPuzzle).AddToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureBp:
                            Game.Manager.activity.LookupAny(EventType.Bp, out var actBp);
                            if (actBp != null)
                            {
                                var activityBp = (BPActivity)actBp;
                                activityBp.TryAddMilestoneNum(data.rewardId, data.rewardCount, data.reason);
                            }
                            break;
                        case FeatureEntry.FeatureClawOrder:
                            if (Game.Manager.activity.LookupAny(EventType.ClawOrder, out var _clawOrder))
                            {
                                data.WaitCommit = true;
                                (_clawOrder as ActivityClawOrder).AddToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureLandmark:
                            Game.Manager.activity.LookupAny(EventType.Landmark, out var actLandMark);
                            if (actLandMark != null)
                            {
                                var activityLandMark = (LandMarkActivity)actLandMark;
                                activityLandMark.AddToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureSevenDayTask:
                            Game.Manager.activity.LookupAny(EventType.SevenDayTask, out var sevenDay);
                            if (sevenDay != null)
                            {
                                var sevenDayTask = sevenDay as ActivitySevenDayTask;
                                sevenDayTask.AddToken(data.rewardId, data.rewardCount);
                            }
                            break;
                        case FeatureEntry.FeatureMicMilestone:
                            Game.Manager.activity.LookupAny(EventType.MicMilestone, out var actScoreMic);
                            if (actScoreMic != null)
                            {
                                var activityMic = (ActivityScoreMic)actScoreMic;
                                activityMic.TryAddToken(data.rewardId, data.rewardCount, data.reason);
                            }
                            break;
                        default:
                            DebugEx.Warning($"RewardMan.BeginReward ----> unknown token {tokenConf.Feature}");
                            //Game.Instance.activityMan.CommitToken(data, tokenConf);
                            break;
                    }
                    break;
                case ObjConfigType.Wallpaper:
                    // FAT_TODO
                    // Game.Instance.wallpaperMan.Unlock(id, data.reason);
                    break;
                case ObjConfigType.CardPack:
                    {
                        var cfg = Game.Manager.objectMan.GetCardPackConfig(data.rewardId);
                        var target = Game.Manager.mainMergeMan.world;
                        var isAutoOpen = cfg.IsAutoOpen;
                        if (isAutoOpen)
                            data.WaitCommit = true;
                        for (int i = 0; i < data.rewardCount; i++)
                        {
                            //获得卡包时检查是否是特殊卡包 是的话 生成对应的唯一卡池
                            if (cfg.IsShinnyGuar)
                                Game.Manager.cardMan.OnGetSpecialCardPack(cfg.Id);
                            //根据卡包配置决定是否在获得时直接打开
                            //不直接开的话就进主棋盘奖励箱
                            if (!isAutoOpen)
                            {
                                data.context.targetWorld = target;
                                target.AddReward(id, cfg.IsTop || (data.flags & RewardFlags._IsPriority) > 0);
                            }
                            //直接打开
                            else
                            {
                                Game.Manager.specialRewardMan.TryAddSpecialReward(id, 1, data.reason);
                            }
                        }
                    }
                    break;
                case ObjConfigType.MergeItem:
                    {
                        Merge.MergeWorld target = null;
                        var config = Game.Manager.objectMan.GetMergeItemConfig(id);
                        if (config.BoardId == 0 && data.context.targetWorld != null)
                        {
                            target = data.context.targetWorld;
                        }
                        if (target == null)
                        {
                            target = Game.Manager.mergeBoardMan.GetMergeWorldForRewardByBoardId(config.BoardId);
                        }
                        if (target != null)
                        {

                            data.context.targetWorld = target;
                            // 沙漏道具
                            var cfg = Game.Manager.mergeItemMan.GetItemComConfig(id);
                            if (cfg.skillConfig != null && cfg.skillConfig.Type == SkillType.SandGlass)
                            {
                                var _item = target.AddReward(id, (data.flags & RewardFlags._IsPriority) > 0 || config.IsTop);
                                if (_item.TryGetItemComponent(out Merge.ItemSkillComponent skill))
                                {
                                    skill.MultiplyBy(data.rewardCount);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < data.rewardCount; i++)
                                {
                                    target.AddReward(id, (data.flags & RewardFlags._IsPriority) > 0 || config.IsTop);
                                }
                            }
                        }
                    }
                    break;
                case ObjConfigType.Role:
                    // FAT_TODO
                    // Game.Instance.studentMan.UnlockStudent(id, data.reason);
                    break;
                case ObjConfigType.RewardVip:
                    // FAT_TODO
                    // Game.Instance.rewardVipMan.AddRewardVipCard(id, data.reason);
                    break;
                case ObjConfigType.DecoReward:
                    // FAT_TODO
                    // var decoConf = Game.Instance.objectMan.GetDecoRewardConfig(id);
                    // Game.Instance.schoolMan.decoData.UnlockDeco(decoConf.SlotId, id);
                    break;
                case ObjConfigType.ProfileDeco:
                    // FAT_TODO
                    // Game.Instance.socialMan.UnlockProfileDeco(id, data.reason, false);
                    break;
                default:
                    switch (data.rewardId)
                    {
                        case Constant.kMergeEnergyObjId:
                            data.WaitCommit = true;
                            Game.Manager.mergeEnergyMan.AddFlyEnergy(data.rewardCount, data.reason);
                            break;
                        case Constant.kMergeExpObjId:
                            data.WaitCommit = true;
                            Game.Manager.mergeLevelMan.AddFlyExp(data.rewardCount, data.reason);
                            break;
                        case Constant.kEventExpObjId:
                            // FAT_TODO
                            // Game.Instance.timedEventsMan.AddFlyExp(data.rewardCount, data.reason);
                            break;
                        case Constant.kMergeInfinateEnergyObjId:
                            // Game.Manager.mergeEnergyMan.AddInfinateEnergy(data.rewardCount);
                            break;
                        default:
                            DebugEx.FormatWarning("RewardMan.BeginReward ----> not implemented reward type {0}", data.rewardType);
                            break;
                    }
                    break;
            }
            if (data.WaitCommit)
            {
                _needCommitDataList.Add(data);
            }
            return data;
        }

        public RewardCommitData BeginReward(int rewardId, int rewardCount, ReasonString reason, RewardFlags flags = RewardFlags.None, RewardContext context_ = default,
            [CallerLineNumber] int _l = 0, [CallerFilePath] string _f = null, [CallerMemberName] string _m = null)
        {
            return _BeginReward(_GenerateReward(rewardId, rewardCount, reason, flags, context_: context_, _l, _f, _m));
        }

        public string GetRewardName(int id)
        {
            var basicInfo = Game.Manager.objectMan.GetBasicConfig(id);
            return I18N.Text(basicInfo?.Name ?? "null");
        }

        public RewardFlySound GetRewardFlySound(int id, int count)
        {
            string snd = null;
            if (id == Constant.kMergeEnergyObjId)
            {
                snd = "AddEnergy";
            }
            else if (id == Constant.kMergeExpObjId)
            {
                snd = "Xp";
            }
            else
            {
                var coinType = Game.Manager.coinMan.GetCoinTypeById(id);
                switch (coinType)
                {
                    case CoinType.Gem:
                        if (count <= 32) snd = "AddGemEx";
                        else snd = "AddGem";
                        break;
                    default:
                        snd = "AddCoin";
                        break;
                }
            }
            return new RewardFlySound() { startSndEvent = snd };
        }

        public string GetRewardCountString(int id, int count)
        {
            if (IsRewardTimed(id))
            {
                return string.Format("{0}", TimeUtility.FormatCountDownWithLimit(count));
            }
            else if (IsRewardCountable(id))
            {
                // 沙漏道具
                var cfg = Game.Manager.mergeItemMan.GetItemComConfig(id);
                if (cfg.skillConfig != null && cfg.skillConfig.Type == SkillType.SandGlass)
                {
                    var sec = cfg.skillConfig.Params[0] * count;
                    return string.Format("{0}", TimeUtility.FormatCountDownOmitZeroTail(sec));
                }
                return string.Format("x{0}", count);
            }
            else
            {
                return null;
            }
        }

        public string GetRewardString(int id, int count)
        {
            var name = GetRewardName(id);
            var countStr = GetRewardCountString(id, count);
            if (string.IsNullOrEmpty(countStr))
            {
                return name;
            }
            else
            {
                return string.Format("{0} {1}", name, countStr);
            }
        }

        public string GetRewardDetail(int id, int count)
        {
            return Game.Manager.objectMan.GetItemRewardDesc(id, count);
        }

        /*
        商店reward 货币图标使用image
        如 PushShop
        */
        public AssetConfig GetShopRewardIcon(int id, int count)
        {
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
            {
                return GetRewardImage(id, count);
            }
            return GetRewardIcon(id, count);
        }

        public AssetConfig GetRewardIcon(int id, int count)
        {
            var basicInfo = Game.Manager.objectMan.GetBasicConfig(id);
            if (basicInfo == null)
            {
                return Constant.kDefaultSocialAvatar;
            }
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
            {
                return basicInfo.Icon.ConvertToAssetConfig();
            }
            else if (id == Constant.kEventExpObjId)
            {
                var tid = Game.Manager.mergeItemMan.GetBonusItemByCount(Constant.kEventExpObjId, count);
                var obj = Game.Manager.objectMan.GetBasicConfig(tid);
                if (obj != null)
                {
                    return obj.Icon.ConvertToAssetConfig();
                }
                else
                {
                    return basicInfo.Icon.ConvertToAssetConfig();
                }
            }
            else
            {
                return basicInfo.Icon.ConvertToAssetConfig();
            }
        }

        public AssetConfig GetRewardImage(int id, int count)
        {
            var basicInfo = Game.Manager.objectMan.GetBasicConfig(id);
            if (basicInfo == null)
            {
                return Constant.kDefaultSocialAvatar;
            }
            // if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
            // {
            //     return Game.Manager.coinMan.GetImageByCount(id, count);
            // }
            // else
            // {
            if (!string.IsNullOrEmpty(basicInfo.Image))
            {
                return basicInfo.Image.ConvertToAssetConfig();
            }
            else
            {
                return GetRewardIcon(id, count);
            }
            // }
        }

        public bool IsRewardPossible(int rewardId, int rewardCount)
        {
            return rewardId > 0 && !IsRewardAlreadyHave(rewardId, rewardCount);
        }

        //是否是可数的
        public bool IsRewardCountable(int rewardId)
        {
            return !Game.Manager.objectMan.IsOneOfType(rewardId, kRewardStringNoCountMask);
        }

        public bool IsRewardTimed(int rewardId)
        {
            return rewardId == Constant.kMergeInfinateEnergyObjId || Game.Manager.objectMan.IsType(rewardId, ObjConfigType.Buff);
        }

        public bool IsRewardAlreadyHave(int rewardId, int rewardCount)
        {
            var type = Game.Manager.objectMan.DeduceTypeForId(rewardId);
            switch (type)
            {
                // FAT_TODO
                // case ObjConfigType.Wallpaper:
                //     return Game.Instance.wallpaperMan.IsUnlocked(rewardId);
                // case ObjConfigType.Role:
                //     return Game.Instance.studentMan.IsStudentUnlocked(rewardId);
                // case ObjConfigType.RewardVip:
                //     return Game.Instance.rewardVipMan.IsVipActive(rewardId);
                default:
                    switch (rewardId)
                    {
                        // case Constant.kIAPAdsDog:
                        //     return Game.Instance.adsMan.isVIP;
                        default:
                            break;
                    }
                    return false;
            }
        }

        void IGameModule.Reset()
        {
            _CheckNeedCommitDataList();
            mContextStack.Clear();
            mRoundCoinList.Clear();
            mRoundToolList.Clear();
            mRoundLifeTimeList.Clear();
        }

        void IGameModule.LoadConfig()
        {
            mRoundCoinList.AddRange(Game.Manager.configMan.GetRoundCoinConfig());
            mRoundToolList.AddRange(Game.Manager.configMan.GetRoundToolConfig());
            mRoundLifeTimeList.AddRange(Game.Manager.configMan.GetRoundLifeTimeConfig());
        }

        void IGameModule.Startup()
        {
        }

        //Reset时直接清理目前还没有commit的数据 不再帮忙commit 一切以服务器存档为准
        private void _CheckNeedCommitDataList()
        {
            if (_needCommitDataList == null || _needCommitDataList.Count < 1)
                return;
            foreach (var data in _needCommitDataList)
            {
                data.WaitCommit = false;
            }
            _needCommitDataList.Clear();
        }

        public int CalcDailyEventTaskRequireCount(string param)
        {
            var r = param?.ConvertToRewardConfig();
            return r != null ? CalcDailyEventTaskRequireCount(r.Id, r.Count) : 0;
        }

        public int CalcDailyEventTaskRequireCount(int baseCount, int method, int levelRate = 0)
        {
            levelRate = levelRate == 0 ? Game.Manager.mergeLevelMan.GetCurrentLevelRate() : levelRate;
            switch (method)
            {
                case 0:
                case 3:
                    var reward = CalcDynamicReward(0, baseCount, levelRate, 0, method);
                    return reward.Count;
                default:
                    // 配置有误
                    DebugEx.Error($"RewardMan::CalcDynamicRewardCount bad config : {baseCount} {method}");
                    return 1;
            }
        }

        public int CalcDynamicOrderLifeTime(int method, int baseTime, int realDifficulty)
        {
            switch (method)
            {
                case 0:
                    return baseTime;
                case 7:
                    return _RoundLifeTime(baseTime * realDifficulty / 100f);
            }
            return 0;
        }

        public (int Id, int Count) CalcDynamicReward(int id, int baseCount, int levelRate, int realDifficulty, int method)
        {
            switch (method)
            {
                case 0:
                    {
                        return (id, baseCount);
                    }
                case 1:
                    {
                        var raw = baseCount * (levelRate / 100f) * (realDifficulty / 100f);
                        return (id, _RoundCoin(raw));
                    }
                case 2:
                    {
                        var raw = baseCount * (levelRate / 100f) * (realDifficulty / 100f);
                        return _RoundTool(raw);
                    }
                case 3:
                    {
                        var raw = baseCount * (levelRate / 100f);
                        return (id, _RoundCoin(raw));
                    }
                case 4:
                    {
                        var raw = baseCount * (levelRate / 100f);
                        return _RoundTool(raw);
                    }
                case 5:
                    {
                        var raw = baseCount * (realDifficulty / 100f);
                        return (id, _RoundCoin(raw));
                    }
                case 6:
                    {
                        var raw = baseCount * (realDifficulty / 100f);
                        return _RoundTool(raw);
                    }
            }
            return (id, baseCount);
        }

        private int _RoundLifeTime(float raw)
        {
            var idx = _FindBoundIndex(raw, mRoundLifeTimeList.Count, _RoundLifeTimeVisitor);
            if (idx < 0)
            {
                DebugEx.Error($"RewardMan::_RoundLifeTime no valid data found by {raw}");
                idx = 0;
            }
            ;
            return mRoundLifeTimeList[idx].LifeTime;
        }

        private int _RoundCoin(float raw)
        {
            var idx = _FindBoundIndex(raw, mRoundCoinList.Count, _RoundCoinVisitor);
            if (idx < 0)
            {
                DebugEx.Error($"RewardMan::_RoundCoin fail to round by {raw}");
                return 1;
            }
            var roundBy = mRoundCoinList[idx].RoundBy;
            var ret = Mathf.RoundToInt(raw / roundBy) * roundBy;
            if (ret < 1)
                ret = 1;
            return ret;
        }

        private (int, int) _RoundTool(float raw)
        {
            var idx = _FindBoundIndex(raw, mRoundToolList.Count, _RoundToolVisitor);
            if (idx < 0)
            {
                DebugEx.Error($"RewardMan::_RoundTool no valid reward found by {raw}");
                idx = 0;
            }
            ;
            var reward = mRoundToolList[idx].Reward.ConvertToRewardConfig();
            return (reward.Id, reward.Count);
        }

        private int _RoundLifeTimeVisitor(int idx) { return mRoundLifeTimeList[idx].From; }
        private int _RoundCoinVisitor(int idx) { return mRoundCoinList[idx].From; }
        private int _RoundToolVisitor(int idx) { return mRoundToolList[idx].From; }

        // 区间有序 / 二分查找
        private int _FindBoundIndex(float target, int count, Func<int, int> visitor)
        {
            int left = 0;
            int right = count - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (visitor(mid) < target)
                {
                    left = mid + 1;
                }
                else if (visitor(mid) > target)
                {
                    right = mid - 1;
                }
                else if (visitor(mid) == target)
                {
                    right = mid;
                    break;
                }
            }
            return right;
        }
    }
}
