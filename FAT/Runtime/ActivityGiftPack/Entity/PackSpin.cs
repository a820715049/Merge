using System;
using System.Collections.Generic;
using System.Linq;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using FAT.MSG;
using static FAT.ListActivity;

namespace FAT
{
    public class PackSpin : GiftPack
    {
        public readonly int rewardStartIndex = 100;
        public readonly int showStartIndex = 200;
        #region 运行时字段
        public EntrySpin entry;
        public override bool WillEnd => _nextRewardIndex >= Detail.PayRewardGroup.Count + Detail.FreeRewardGroup.Count;
        public SpinPack ConfD;
        public SpinPackDetail Detail;
        public override int ThemeId => ConfD.EventTheme;
        public override int StockTotal { get; }
        public override int PackId { get; set; }
        public List<int> ShowIndexList => _showIDList;
        public List<int> RewardIDList => _rewardIDList;
        public override bool Valid => ConfD != null;
        public override UIResAlt Res { get; } = new(UIConfig.UISpinPackPanel);
        public string Price => Game.Manager.iap.PriceInfo(Content.IapId);
        public float totalWeight;
        public bool HasFreeCount => _freeChanceCount > 0;
        #endregion

        #region 存档字段
        private int _groupId;
        private int _freeChanceCount;
        private int _nextRewardIndex;
        private List<int> _rewardIDList = new();
        private List<int> _showIDList = new();
        #endregion

        #region Activity
        public ListActivity.IEntrySetup RefreshEntry(Entry e)
        {
            e.dot.SetActive(_freeChanceCount > 0);
            return null;
        }
        public PackSpin() { }

        public PackSpin(ActivityLite lite)
        {
            Lite = lite;
            ConfD = fat.conf.Data.GetSpinPack(1);
            RefreshTheme();
        }

        public override void Open()
        {
            UIManager.Instance.OpenWindow(Res.ActiveR, this);
        }

        public override void SetupFresh()
        {
            if (ConfD != null) { _groupId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.EventGroup); }
            if (_groupId != 0) { Detail = fat.conf.Data.GetSpinPackDetail(_groupId); }
            if (Detail != null) { _InitRewardIndexList(); }
            _freeChanceCount = Detail.FreeRewardGroup.Count;
            RefreshContent();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            var startIndex = 3;
            _groupId = RecordStateHelper.ReadInt(startIndex++, data_.AnyState);
            Detail = fat.conf.Data.GetSpinPackDetail(_groupId);
            _freeChanceCount = RecordStateHelper.ReadInt(startIndex++, data_.AnyState);
            _nextRewardIndex = RecordStateHelper.ReadInt(startIndex++, data_.AnyState);
            for (var i = 0; i < Detail.PayRewardGroup.Count + Detail.FreeRewardGroup.Count; i++) { _rewardIDList.Add(RecordStateHelper.ReadInt(rewardStartIndex + i, data_.AnyState)); }
            for (var i = 0; i < Detail.PayRewardGroup.Count + Detail.FreeRewardGroup.Count; i++) { _showIDList.Add(RecordStateHelper.ReadInt(showStartIndex + i, data_.AnyState)); }
            RefreshContent();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var startIndex = 3;
            data_.AnyState.Add(RecordStateHelper.ToRecord(startIndex++, _groupId));
            data_.AnyState.Add(RecordStateHelper.ToRecord(startIndex++, _freeChanceCount));
            data_.AnyState.Add(RecordStateHelper.ToRecord(startIndex++, _nextRewardIndex));
            startIndex = rewardStartIndex;
            foreach (var index in _rewardIDList) { data_.AnyState.Add(RecordStateHelper.ToRecord(startIndex++, index)); }
            startIndex = showStartIndex;
            foreach (var index in _showIDList) { data_.AnyState.Add(RecordStateHelper.ToRecord(startIndex++, index)); }
        }
        #endregion

        #region 内部逻辑
        /// <summary>
        /// 初始化抽奖顺序List
        /// </summary>
        private void _InitRewardIndexList()
        {
            var tempList = new List<int>();
            foreach (var free in Detail.FreeRewardGroup)
            {
                if (!_CheckWhetherStageIsSame(tempList, free)) { _FillRewardIndexList(tempList); }
                tempList.Add(free);
            }
            _FillRewardIndexList(tempList);
            foreach (var pay in Detail.PayRewardGroup)
            {
                if (!_CheckWhetherStageIsSame(tempList, pay)) { _FillRewardIndexList(tempList); }
                tempList.Add(pay);
            }
            _FillRewardIndexList(tempList);
            _FillShowIndexList();
        }
        /// <summary>
        /// 检查List中的奖励 stage时候和传入的stage相同
        /// </summary>
        /// <param name="list">当前list</param>
        /// <param name="stage">当前奖励stagj</param>
        /// <returns>true为相同，false为不同组</returns>
        private bool _CheckWhetherStageIsSame(List<int> list, int rewardId)
        {
            if (list.Count == 0) { return true; }
            return fat.conf.Data.GetSpinPackRewardPool(list[^1]).Stage == fat.conf.Data.GetSpinPackRewardPool(rewardId).Stage;
        }

        /// <summary>
        /// 将同一组的奖励打乱塞入_rewardIndexList
        /// </summary>
        /// <param name="list">当前档位的所有奖励</param>
        private void _FillRewardIndexList(List<int> list)
        {
            while (list.Count > 0)
            {
                var id = list.RandomChooseByWeight(e => fat.conf.Data.GetSpinPackRewardPool(e).Weight);
                _rewardIDList.Add(id);
                list.Remove(id);
            }
        }

        private void _FillShowIndexList()
        {
            _showIDList.AddRange(_rewardIDList);
            _showIDList.Shuffle();
        }

        #endregion

        #region 礼包逻辑
        public List<RewardCommitData> rewardWaitCommit = new();
        public List<RewardCommitData> extraWaitCommit = new();
        public int GetAnimIndex()
        {
            _rewardIDList.TryGetByIndex(_nextRewardIndex - 1, out var ret);
            return _showIDList.IndexOf(ret);
        }

        public bool CheckHasBought(int id)
        {
            return _rewardIDList.IndexOf(id) < _nextRewardIndex;
        }

        public void RefreshTotalWeight()
        {
            totalWeight = 0f;
            foreach (var reward in _rewardIDList)
            {
                if (CheckHasBought(reward)) continue;
                totalWeight += fat.conf.Data.GetSpinPackRewardPool(reward).Weight;
            }
        }
        public float GetRewardRate(int id)
        {
            if (CheckHasBought(id)) { return 0; }
            return fat.conf.Data.GetSpinPackRewardPool(id).Weight / totalWeight;
        }
        /// <summary>
        /// 购买礼包，内部分为免费购买和付费购买
        /// </summary>
        public void TryBuyPack(Action success)
        {
            if (_freeChanceCount > 0) { _buyFree(success); }
            else { _buyPay(success); }
        }

        private void _buyFree(Action success)
        {
            _BeginPackRewardFree(false);
            _freeChanceCount--;
            _nextRewardIndex++;
            success?.Invoke();
            entry?.Refresh();
            RefreshContent();
        }

        private void _buyPay(Action success)
        {
            Game.Manager.activity.giftpack.Purchase(this, (reward) => success.Invoke());
        }

        private void _BeginPackRewardFree(bool late)
        {
            rewardWaitCommit.Clear();
            _rewardIDList.TryGetByIndex(_nextRewardIndex, out var rewardId);
            var conf = fat.conf.Data.GetSpinPackRewardPool(rewardId);
            foreach (var reward in conf.RewardList)
            {
                var config = reward.ConvertToRewardConfig();
                rewardWaitCommit.Add(Game.Manager.rewardMan.BeginReward(config.Id, config.Count, ReasonString.spin_pack));
            }
            if (!late) { DataTracker.event_spinpack_claim.Track(this, _nextRewardIndex + 1, _rewardIDList.Count, Detail.Diff, _rewardIDList[_nextRewardIndex], _freeChanceCount > 0, _nextRewardIndex == _rewardIDList.Count - 1); }
        }

        private void _BeginPackReward(IList<RewardCommitData> rewards, bool late)
        {
            rewardWaitCommit.Clear();
            _rewardIDList.TryGetByIndex(_nextRewardIndex, out var rewardId);
            var count = fat.conf.SpinPackRewardPoolVisitor.Get(rewardId).RewardList.Count;
            for (var i = 0; i < count; i++) { rewardWaitCommit.Add(rewards[i]); }
            if (!late) { DataTracker.event_spinpack_claim.Track(this, _nextRewardIndex + 1, _rewardIDList.Count, Detail.Diff, _rewardIDList[_nextRewardIndex], _freeChanceCount > 0, _nextRewardIndex == _rewardIDList.Count - 1); }
        }

        public override void RefreshContent()
        {
            if (_freeChanceCount > 0) { return; }
            if (!Detail.PriceGroup.TryGetByIndex(_nextRewardIndex - Detail.FreeRewardGroup.Count, out var iap)) { return; }
            Content = fat.conf.Data.GetIAPPack(iap);
            Goods.Refresh(Content);
        }

        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            _BeginPackReward(rewards_, late_);
            var index = Detail.PriceGroup.IndexOf(packId_);
            var curIndex = _nextRewardIndex - Detail.FreeRewardGroup.Count;
            if (index >= curIndex) { _nextRewardIndex++; }
            extraWaitCommit.Clear();
            if (rewardWaitCommit.Count < rewards_.Count) { extraWaitCommit.Add(rewards_[^1]); }
            RefreshContent();
            if (WillEnd) Game.Manager.activity.EndImmediate(this, expire_: false);
        }

        public override BonusReward MatchPack(int packId_)
        {
            var index = Detail.PriceGroup.IndexOf(packId_);
            var curIndex = _nextRewardIndex - Detail.FreeRewardGroup.Count;
            var iapPack = fat.conf.Data.GetIAPPack(packId_);
            var reward = new BonusReward();
            if (iapPack != null)
            {
                reward.Refresh(iapPack);
                var conf = fat.conf.Data.GetSpinPackRewardPool(_rewardIDList[Detail.FreeRewardGroup.Count + index]);
                foreach (var str in conf.RewardList)
                {
                    reward.reward.Add(str.ConvertToRewardConfig());
                }
            }
            return reward;
        }
        #endregion

    }

    public class EntrySpin : IEntrySetup
    {
        public Entry Entry => e;
        private readonly Entry e;
        private readonly PackSpin p;

        public EntrySpin(Entry e_, PackSpin p_)
        {
            (e, p) = (e_, p_);
            e.dot.SetActive(p.HasFreeCount);
            p.entry = this;
        }

        public override void Clear(Entry e_)
        {
            p.entry = null;
        }

        public void Refresh()
        {
            e.dot.SetActive(p.HasFreeCount);
        }
    }
}
