/*
 * @Author: tang.yan
 * @Description: 1+2礼包数据类  
 * @Date: 2024-07-03 16:07:47
 */
using System.Collections.Generic;
using Config;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT {
    public class PackOnePlusTwo : GiftPack {
        public OnePlusTwo confD;
        public readonly List<RewardConfig> freeGoods1 = new();  //第一列赠品奖励
        public readonly List<RewardConfig> freeGoods2 = new();  //第二列赠品奖励
        public override UIResAlt Res { get; } = new(UIConfig.UIOnePlusTwoPack);
        public override int PackId { get; set; }
        public override int ThemeId => confD.EventTheme;
        //限购次数默认设成4次 BuyCount =0说明没有付费购买 =1说明付费购买但没领免费奖励
        //=2说明只领了第一个免费奖励 =3说明只领了第二个免费奖励 =4说明免费奖励都领了 活动结束
        public override int StockTotal => 4;
        
        private int _freeGrpId1 = 0;     //礼包中的第一列赠品id
        private int _freeGrpId2 = 0;     //礼包中的第二列赠品id
        private bool _hasBuy = false;    //记录是否已经购买过礼包 用于区别是否为重复购买

        public PackOnePlusTwo() { }


        public PackOnePlusTwo(ActivityLite lite_) {
            Lite = lite_;
            confD = GetOnePlusTwo(lite_.Param);
            RefreshTheme(popupCheck_:true);
        }

        public override void SetupFresh() {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            _freeGrpId1 = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.FreeOneGrpId);
            _freeGrpId2 = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.FreeTwoGrpId);
            RefreshContent();
        }
        
        public override void SaveSetup(ActivityInstance data_) {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            any.Add(ToRecord(3, _freeGrpId1));
            any.Add(ToRecord(4, _freeGrpId2));
            any.Add(ToRecord(5, _hasBuy));
        }

        public override void LoadSetup(ActivityInstance data_) {
            base.LoadSetup(data_);
            var isRefresh = false;
            var any = data_.AnyState;
            var freeGrpId1 = ReadInt(3, any);
            if (freeGrpId1 > 0) {
                _freeGrpId1 = freeGrpId1;
                isRefresh = true;
            }
            var freeGrpId2 = ReadInt(4, any);
            if (freeGrpId2 > 0) {
                _freeGrpId2 = freeGrpId2;
                isRefresh = true;
            }
            _hasBuy = ReadBool(5, any);
            if (isRefresh)
                RefreshContent();
        }

        public override void RefreshPack() {
            freeGoods1.Clear();
            var freeConf1 = GetIAPFree(_freeGrpId1);
            if (freeConf1 != null)
            {
                var list = Goods.AlbumActive ? freeConf1.AlbumReward : freeConf1.FreeReward;
                foreach(var r in list) {
                    freeGoods1.Add(r.ConvertToRewardConfig());
                }
            }
            freeGoods2.Clear();
            var freeConf2 = GetIAPFree(_freeGrpId2);
            if (freeConf2 != null)
            {
                var list = Goods.AlbumActive ? freeConf2.AlbumReward : freeConf2.FreeReward;
                foreach(var r in list) {
                    freeGoods2.Add(r.ConvertToRewardConfig());
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

        //BuyCount =0说明没有付费购买 =1说明付费购买但没领免费奖励
        //=2说明只领了第一个免费奖励 =3说明只领了第二个免费奖励 =4说明免费奖励都领了 活动结束
        public bool CollectFreeReward(int index, List<RewardCommitData> rewards)
        {
            if (rewards == null || BuyCount <= 0 || BuyCount >= 4) 
                return false;
            //领取第一列免费奖励
            if (index == 1 && BuyCount != 2)
            {
                if (freeGoods1.Count <= 0)
                    return false;
                foreach (var reward in freeGoods1)
                {
                    rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                }
                //两个赠品都没领时 置为2 领了另外一个时置为4
                BuyCount = BuyCount == 1 ? BuyCount = 2 : BuyCount = 4;
                //打点
                DataTracker.oneplustwo_reward.Track(this, 2, true);
            }
            //领取第二列免费奖励
            if (index == 2 && BuyCount != 3)
            {
                if (freeGoods2.Count <= 0)
                    return false;
                foreach (var reward in freeGoods2)
                {
                    rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                }
                //两个赠品都没领时 置为3 领了另外一个时置为4
                BuyCount = BuyCount == 1 ? BuyCount = 3 : BuyCount = 4;
                //打点
                DataTracker.oneplustwo_reward.Track(this, 3, true);
            }
            //领取完免费奖励后 关闭活动
            if (WillEnd)
            {
                Game.Manager.activity.EndImmediate(this,false);
            }
            return true;
        }

        //当活动结束时 如果有未领取的免费奖励 则自动领取
        public override void WhenEnd()
        {
            //BuyCount<=0说明没有付费购买 >=4说明所有奖励都领了
            if (BuyCount <= 0 || BuyCount >= 4) 
                return;
            var rewardMan = Game.Manager.rewardMan;
            var isCollect1 = false;
            var isCollect2 = false;
            //=1说明俩免费奖励都没领  两个都帮领取
            if (BuyCount == 1) { isCollect1 = true; isCollect2 = true; }
            //=2说明只领了第一个免费奖励  自动帮领第二个
            if (BuyCount == 2) { isCollect2 = true; }
            //=3说明只领了第二个免费奖励  自动帮领第一个
            else if (BuyCount == 3) { isCollect1 = true; }
            if (isCollect1)
            {
                foreach (var reward in freeGoods1)
                {
                    rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                } 
                //打点
                DataTracker.oneplustwo_reward.Track(this, 2, true);
            }
            if (isCollect2)
            {
                foreach (var reward in freeGoods2)
                {
                    rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                } 
                //打点
                DataTracker.oneplustwo_reward.Track(this, 3, true);
            }
            BuyCount = 4;
        }

        private void _CollectAllFreeReward()
        {
            var rewardMan = Game.Manager.rewardMan;
            //领取免费奖励1
            foreach (var reward in freeGoods1)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
            } 
            //打点
            DataTracker.oneplustwo_reward.Track(this, 2, true);
            //领取免费奖励2
            foreach (var reward in freeGoods2)
            {
                rewardMan.CommitReward(rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
            } 
            //打点
            DataTracker.oneplustwo_reward.Track(this, 3, true);
        }
    }
}