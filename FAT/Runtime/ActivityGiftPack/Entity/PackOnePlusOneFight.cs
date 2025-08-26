/**
 * @Author: zhangpengjian
 * @Date: 2025/5/16 17:43:58
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/16 17:43:58
 * Description: 战斗棋盘 1+1礼包数据类  
 */

using System.Collections.Generic;
using Config;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class PackOnePlusOneFight : GiftPack
    {
        public EventFightOnePlusOne confD;
        public readonly List<RewardConfig> freeGoods = new();
        public override UIResAlt Res { get; } = new(UIConfig.UIOnePlusOnePack);
        public override int PackId { get; set; }
        public override int ThemeId => confD.EventTheme;
        public override int StockTotal => 2;    //限购次数默认设成2次  一次为真正付费购买，一次为免费领取 当购买次数=2时代表活动结束

        private int _freeGrpId = 0;     //礼包中的赠品id
        private bool _hasBuy = false;    //记录是否已经购买过礼包 用于区别是否为重复购买

        public PackOnePlusOneFight() { }


        public PackOnePlusOneFight(ActivityLite lite_)
        {
            Lite = lite_;
            confD = GetEventFightOnePlusOne(lite_.Param);
            RefreshTheme(popupCheck_: true);
        }

        public override void SetupFresh()
        {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            _freeGrpId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.FreeGrpId);
            RefreshContent();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            any.Add(ToRecord(3, _freeGrpId));
            any.Add(ToRecord(4, _hasBuy));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            var freeGrpId = ReadInt(3, any);
            if (freeGrpId > 0)
            {
                _freeGrpId = freeGrpId;
                RefreshContent();
            }
            _hasBuy = ReadBool(4, any);
        }

        public override void RefreshPack()
        {
            freeGoods.Clear();
            var freeConf = GetIAPFree(_freeGrpId);
            if (freeConf != null)
            {
                var list = Goods.AlbumActive ? freeConf.AlbumReward : freeConf.FreeReward;
                foreach (var r in list)
                {
                    freeGoods.Add(r.ConvertToRewardConfig());
                }
            }
            base.RefreshPack();
        }

        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            //发生补单
            if (late_)
            {
                //发生重复购买
                if (_hasBuy)
                {
                    //BuyCount不增加
                    BuyCount--;
                    //直接帮忙领取另外两项免费奖励
                    _CollectAllFreeReward();
                }
                //只购买了一次
                else
                {
                    //这里不做处理 底层会直接发放购买项对应的奖励 剩余的免费奖励让玩家手动领取
                }
            }
            _hasBuy = true;
        }
        
        public bool CollectFreeReward(List<RewardCommitData> rewards)
        {
            if (freeGoods.Count <= 0 || rewards == null)
                return false;
            foreach (var reward in freeGoods)
            {
                rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
            }
            BuyCount++;
            //打点
            //DataTracker.oneplusone_fight_reward.Track(this, true);
            //领取完免费奖励后 关闭活动
            if (WillEnd)
            {
                Game.Manager.activity.EndImmediate(this, false);
            }
            return true;
        }

        //当活动结束时 如果有未领取的免费奖励 则自动领取
        public override void WhenEnd()
        {
            if (BuyCount == 1)   //购买次数为1说明已购买但是没有领免费奖励
            {
                var rewardMan = Game.Manager.rewardMan;
                foreach (var reward in freeGoods)
                {
                    rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                }
                BuyCount++;
                //打点
                //DataTracker.oneplusone_fight_reward.Track(this, true);
            }
        }
                
        private void _CollectAllFreeReward()
        {
            var rewardMan = Game.Manager.rewardMan;
            foreach (var reward in freeGoods)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
            }
            //打点
            //DataTracker.oneplusone_fight_reward.Track(this, true);
        }
    }
}