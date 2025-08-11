/*
 * @Author: qun.chao
 * @Date: 2024-12-02 11:55:56
 */
using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class PackEnergyMultiPack : GiftPack
    {
        public EnergyMultiPack Conf;
        public override int PackId { get; set; } = 0;
        public override int StockTotal => 1;
        public override int ThemeId => Conf.EventTheme;
        public override UIResAlt Res { get; } = new(UIConfig.UIPackEnergyMultiPack);
        public override bool Valid => true;
        public override int LabelId => Conf.Label;
        public int PackCount => _packList.Count;  // 奖励项目数量
        public int LastPackId => _packList.Count > 0 ? _packList[_packList.Count - 1] : 0;

        private readonly List<int> _packList = new();

        public PackEnergyMultiPack(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = GetEnergyMultiPack(lite_.Param);
            RefreshTheme(true);
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var saveId = 1;
            any.Add(ToRecord(saveId++, BuyCount));
            foreach (var item in _packList)
            {
                any.Add(ToRecord(saveId++, item));
            }
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            var saveId = 1;
            BuyCount = ReadInt(saveId++, any);
            _packList.Clear();
            for (var i = saveId; i <= any.Count; ++i)
            {
                _packList.Add(ReadInt(i, any));
            }
            RefreshContent();
        }

        public override void SetupFresh()
        {
            var gradeMan = Game.Manager.userGradeMan;
            var list = _packList;
            list.Clear();
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackOneGrpId));
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackTwoGrpId));
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackThreeGrpId));
            RefreshContent();
        }

        public override void RefreshContent()
        {
            RefreshPack();
        }

        public override void RefreshPack()
        {
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().Dispatch(this);
        }

        public override BonusReward MatchPack(int packId_)
        {
            GetIAPPack(packId_, out var iapPack);
            if (iapPack != null)
            {
                var reward = new BonusReward();
                reward.Refresh(iapPack);
                return reward;
            }
            return null;
        }

        public bool GetIAPPackByIdx(int idx, out IAPPack iapPack)
        {
            var id = _packList[idx];
            return GetIAPPack(id, out iapPack);
        }

        public bool GetIAPPack(int id, out IAPPack iapPack)
        {
            var iap = Game.Manager.iap;
            iap.FindIAPPack(id, out iapPack);
            var valid = iap.Initialized && iapPack != null;
            return valid;
        }

        public bool TryPurchase(int idx, Action<IList<RewardCommitData>> whenSuccess)
        {
            if (idx >= _packList.Count)
                return false;
            var packId = _packList[idx];
            var valid = GetIAPPack(packId, out var iapPack);
            if (!valid)
            {
                DebugEx.Error($"[gem3for1] iap pack invalid {packId}");
                return false;
            }
            Game.Manager.activity.giftpack.Purchase(this, iapPack, (rewards) => OnPurchaseSuccess(rewards, whenSuccess));
            return true;
        }

        private void OnPurchaseSuccess(IList<RewardCommitData> rewards, Action<IList<RewardCommitData>> whenSuccess)
        {
            // 埋点
            DataTracker.energymultipack_reward.Track(this);
            whenSuccess?.Invoke(rewards);
            // // 触发入口刷新
            // MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
            // 认为此处购买的一定是体力
            ResetPopup();
        }
    }
}