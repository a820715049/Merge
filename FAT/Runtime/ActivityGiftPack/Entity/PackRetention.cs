/**
 * @Author: zhangpengjian
 * @Date: 2024/8/7 18:09:09
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/12 19:12:00
 * Description: https://centurygames.yuque.com/ywqzgn/ne0fhm/bku7aiilti3sp3lg 付费留存礼包
 */

using System.Collections.Generic;
using Config;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using static FAT.ListActivity;
using static EL.MessageCenter;
using FAT.MSG;

namespace FAT
{
    public class PackRetention : GiftPack
    {
        public RetentionPack confD;
        public readonly List<RewardConfig> freeGoods1 = new();  //第一列赠品奖励
        public readonly List<RewardConfig> freeGoods2 = new();  //第二列赠品奖励
        public override UIResAlt Res { get; } = new(UIConfig.UIPackRetention);
        public override int PackId { get; set; }
        public override int ThemeId => confD.EventTheme;
        //限购次数默认设成3次 BuyCount =0说明没有付费购买 =1说明付费购买但没领免费奖励
        //=2说明领了第一个免费奖励 =3说明领了第二个免费奖励 则免费奖励都领了 活动结束
        public override int StockTotal => 3;
        private int _freeGrpId1 = 0;     //礼包中的第一列赠品id
        private int _freeGrpId2 = 0;     //礼包中的第二列赠品id
        private int _nextCollectTs = 0;     //下一个可领取的免费奖励时间戳
        private Entry _entry;
        private bool _willEnd = false;
        public PackRetention() { }

        public PackRetention(ActivityLite lite_)
        {
            Lite = lite_;
            confD = GetRetentionPack(lite_.Param);
            RefreshTheme();
            Get<GAME_ONE_SECOND_DRIVER>().AddListenerUnique(OnTick);
        }
        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(Popup, state_, false);
        }

        private void OnTick()
        {
            if (_entry != null)
            {
                SetupEntry(_entry);
            }
        }

        public override void SetupFresh()
        {
            PackId = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.PackGrpId);
            _freeGrpId1 = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.FreeGrpId[0]);
            _freeGrpId2 = Game.Manager.userGradeMan.GetTargetConfigDataId(confD.FreeGrpId[1]);
            RefreshContent();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            //从3开始是因为 父类 GiftPack用 1 2 分别记录了PackId和BuyCount
            any.Add(ToRecord(3, _freeGrpId1));
            any.Add(ToRecord(4, _freeGrpId2));
            any.Add(ToRecord(5, _nextCollectTs));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            _freeGrpId1 = ReadInt(3, any);
            _freeGrpId2 = ReadInt(4, any);
            _nextCollectTs = ReadInt(5, any);
            RefreshContent();
        }

        public override void RefreshPack()
        {
            freeGoods1.Clear();
            var freeConf1 = GetIAPFree(_freeGrpId1);
            if (freeConf1 != null)
            {
                var list = Goods.AlbumActive ? freeConf1.AlbumReward : freeConf1.FreeReward;
                foreach (var r in list)
                {
                    freeGoods1.Add(r.ConvertToRewardConfig());
                }
            }
            freeGoods2.Clear();
            var freeConf2 = GetIAPFree(_freeGrpId2);
            if (freeConf2 != null)
            {
                var list = Goods.AlbumActive ? freeConf2.AlbumReward : freeConf2.FreeReward;
                foreach (var r in list)
                {
                    freeGoods2.Add(r.ConvertToRewardConfig());
                }
            }
            base.RefreshPack();
        }

        public IEntrySetup SetupEntry(Entry e_)
        {
            if (e_.activity != this)
            {
                return null;
            }
            _entry = e_;
            var buyCount = BuyCount;
            e_.cd.gameObject.SetActive(buyCount <= 0);
            e_.dot.SetActive(buyCount > 0 && Game.TimestampNow() >= GetNextCollectTs());
            e_.progress.gameObject.SetActive(buyCount > 0);
            string formatter(long cur, long tar)
            {
                return $"{cur}/{tar}";
            }
            e_.progress.SetFormatter(formatter);
            e_.progress.ForceSetup(0, 3, buyCount);
            return null;
        }

        public bool CollectFreeReward(List<RewardCommitData> rewards)
        {
            if (rewards == null || BuyCount <= 0 || BuyCount >= 3)
            {
                return false;
            }
            var global = Game.Manager.configMan.globalConfig;
            //领取第一个免费奖励
            if (BuyCount == 1)
            {
                if (freeGoods1.Count <= 0)
                {
                    return false;
                }
                foreach (var reward in freeGoods1)
                {
                    rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                }
                BuyCount = 2;
                if (!_willEnd)
                {
                    endTS = _nextCollectTs + ((confD.AutoClaimDay - 1) * 86400);
                    Get<ACTIVITY_TS_SYNC>().Dispatch(this);
                }
                //打点
                DataTracker.retentionpack_reward.Track(this, 2, true);
            }
            //领取第二个免费奖励
            else if (BuyCount == 2)
            {
                if (freeGoods2.Count <= 0)
                {
                    return false;
                }
                foreach (var reward in freeGoods2)
                {
                    rewards.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.purchase));
                }
                //两个赠品都没领时 置为3 领了另外一个时置为4
                BuyCount = 3;
                //打点
                DataTracker.retentionpack_reward.Track(this, 3, true);
            }
            _nextCollectTs = (int)Game.Timestamp(Game.NextTimeOfDay(global.RetentionPackRefreshUtc));
            //领取完免费奖励后 关闭活动
            if (WillEnd)
            {
                endTS = Game.TimestampNow();
                Get<ACTIVITY_TS_SYNC>().Dispatch(this);
                Game.Manager.activity.EndImmediate(this, false);
            }
            return true;
        }

        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
            BuyCount = 1;
            var global = Game.Manager.configMan.globalConfig;
            _nextCollectTs = (int)Game.Timestamp(Game.NextTimeOfDay(global.RetentionPackRefreshUtc));
            endTS = _nextCollectTs + ((confD.AutoClaimDay - 1) * 86400);
            Get<ACTIVITY_TS_SYNC>().Dispatch(this);
            //购买留存礼包属积极行为 上报 影响活动触发
            Get<ACTIVITY_SUCCESS>().Dispatch(this);
            DataTracker.retentionpack_reward.Track(this, 1, false);
        }

        //当活动结束时 如果有未领取的免费奖励 则按规则自动领取
        public override void WhenEnd()
        {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(OnTick);
            //BuyCount<=0说明没有付费购买 >=3说明所有奖励都领了
            if (BuyCount <= 0 || BuyCount >= 3)
            {
                return;
            }
            _willEnd = true;
            UIManager.Instance.OpenWindow(Res.ActiveR, this, true);
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(Res.ActiveR, this, false);
            DataTracker.event_popup.Track(this);
        }

        public override void WhenReset()
        {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(OnTick);
        }

        public int GetNextCollectTs()
        {
            return _nextCollectTs;
        }
    }
}