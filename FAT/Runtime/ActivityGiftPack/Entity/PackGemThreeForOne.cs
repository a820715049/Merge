/*
 * @Author: qun.chao
 * @Date: 2024-11-07 12:08:56
 */
using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using EL;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using static EL.PoolMapping;
using static FAT.ListActivity;

namespace FAT
{
    public class EntryPackGemThreeForOne : IEntrySetup
    {
        public Entry Entry => e;
        private readonly Entry e;
        private readonly PackGemThreeForOne pack;

        public EntryPackGemThreeForOne(Entry e_, PackGemThreeForOne pack_)
        {
            (e, pack) = (e_, pack_);
        }

        public override void Clear(Entry e_) { }

        public override string TextCD(long diff_)
        {
            diff_ = pack.PackEndTIme - Game.Instance.GetTimestampSeconds();
            if (diff_ < 0)
            {
                MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
            }
            return UIUtility.CountDownFormat(diff_);
        }
    }

    public class PackGemThreeForOne : GiftPack
    {
        public GemThreeForOne Conf;
        public override int PackId { get; set; } = 0;
        public override int StockTotal => Conf?.Paytimes ?? 0;
        public override int ThemeId => Conf.EventTheme;
        public override UIResAlt Res { get; } = new(UIConfig.UIPackGemThreeForOne);
        public override bool Valid => true;
        public int PackCount => _packList.Count;  // 奖励项目数量
        public override bool EntryVisible => Stock > 0 && base.EntryVisible &&
                                            PackEndTIme > 0 && PackEndTIme > Game.Instance.GetTimestampSeconds();
        public long PackEndTIme => _packEndTime;

        private readonly List<int> _packList = new();
        private readonly int _saveDataBeginId = 4;
        private int _packEndTime;

        public PackGemThreeForOne(ActivityLite lite_)
        {
            Lite = lite_;
            Conf = GetGemThreeForOne(lite_.Param);
            RefreshTheme(true);
        }

        public override void SetupFresh()
        {
            InitPack(_packList);
            base.RefreshPack();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            data_.AnyState.Add(ToRecord(3, _packEndTime));
            SavePack(data_, _saveDataBeginId, _packList);
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            _packEndTime = ReadInt(3, data_.AnyState);
            LoadPack(data_, _saveDataBeginId, _packList);
        }

        public override BonusReward MatchPack(int packId_)
        {
            if (GetIAPPack(packId_, out var iapPack))
            {
                var reward = new BonusReward();
                reward.Refresh(iapPack);
                return reward;
            }
            return null;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (Stock > 0 &&
                Game.Manager.shopMan.GetShopTabData(ShopTabType.Energy) is ShopTabEnergyData data &&
                data.EnergyDataList.Count > 0 &&
                data.EnergyDataList[0].BuyCount >= Conf.MarketBuyTimes)
            {
                base.TryPopup(popup_, state_);
            }
        }

        public CurrencyPack GetCurrencyPack(int idx)
        {
            if (idx >= _packList.Count)
                return null;
            if (Game.Manager.iap.FindCurrencyPack(_packList[idx], out var p))
                return p;
            return null;
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
            var cp = GetCurrencyPack(idx);
            if (cp == null)
            {
                DebugEx.Error($"[gem3for1] currency pack invalid {_packList[idx]}");
                return false;
            }
            if (cp.CoinType == CoinType.Iapcoin)
            {
                var valid = GetIAPPack(cp.Price, out var iapPack);
                if (!valid)
                {
                    DebugEx.Error($"[gem3for1] iap pack invalid {_packList[idx]} | {cp.Price}");
                    return false;
                }
                Game.Manager.activity.giftpack.Purchase(this, iapPack, (rewards) => OnPurchaseSuccess(rewards, whenSuccess, cp));
            }
            else
            {
                var reason = ReasonString.purchase;
                var coinMan = Game.Manager.coinMan;
                if (coinMan.CanUseCoin(cp.CoinType, cp.Price))
                {
                    coinMan.UseCoin(cp.CoinType, cp.Price, reason)
                    .OnSuccess(() =>
                    {
                        using var _ = PoolMappingAccess.Borrow<List<RewardCommitData>>(out var list);
                        foreach (var r in cp.Reward)
                        {
                            var reward = r.ConvertToRewardConfig();
                            list.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, reason));
                        }
                        OnPurchaseSuccess(list, whenSuccess, cp);
                        // 货币购买需要自行记录购买次数
                        ++BuyCount;
                    })
                    .FromActivity(Lite)
                    .Execute();
                }
                else
                {
                    if (cp.CoinType == CoinType.MergeCoin)
                    {
                        Game.Manager.commonTipsMan.ShowPopTips(Toast.GemThreeForOne);
                    }
                    DebugEx.Warning($"[gem3for1] currency not enough {cp.Price}@{cp.CoinType}");
                }
            }
            return true;
        }

        private void OnPurchaseSuccess(IList<RewardCommitData> rewards, Action<IList<RewardCommitData>> whenSuccess, CurrencyPack cp)
        {
            // 埋点
            DataTracker.gem_threeforone_reward.Track(this, cp.Id, cp.TgaName);
            _packEndTime = 0;
            whenSuccess?.Invoke(rewards);
            // 触发入口刷新
            MessageCenter.Get<MSG.ACTIVITY_UPDATE>().Dispatch();
            // 认为此处购买的一定是体力
            ResetPopup();
        }

        public bool TryRefreshPackEndTIme()
        {
            var now = Game.Instance.GetTimestampSeconds();
            if (_packEndTime > now)
                return false;
            var end = Game.Instance.GetTimestampSeconds() + Conf.Duration;
            if (end > endTS)
            {
                end = endTS;
            }
            _packEndTime = (int)end;
            return true;
        }

        private void InitPack(List<int> list)
        {
            var gradeMan = Game.Manager.userGradeMan;
            list.Clear();
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackOneGrpId));
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackTwoGrpId));
            list.Add(gradeMan.GetTargetConfigDataId(Conf.PackThreeGrpId));
        }

        private void SavePack(ActivityInstance data_, int seqId, List<int> list)
        {
            foreach (var item in list)
            {
                data_.AnyState.Add(ToRecord(seqId, item));
                ++seqId;
            }
        }

        private void LoadPack(ActivityInstance data_, int seqId, List<int> list)
        {
            list.Clear();
            for (var i = seqId; i <= data_.AnyState.Count; ++i)
            {
                list.Add(ReadInt(i, data_.AnyState));
            }
        }
    }
}
