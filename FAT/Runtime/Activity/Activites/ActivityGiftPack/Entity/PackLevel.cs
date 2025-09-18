// ==================================================
// // File: PackLevel.cs
// // Author: liyueran
// // Date: 2025-08-20 10:08:44
// // Desc: $等级礼包
// // ==================================================


using System;
using System.Collections.Generic;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;
using static FAT.ListActivity;

namespace FAT
{
    public class PackLevel : GiftPack
    {
        public override int PackId { get; set; }
        public override int ThemeId => ConfD.EventTheme;
        public override int StockTotal => ConfDetail.Count;

        #region 存档字段
        private int _detailID;
        private int _triggerLv;
        #endregion

        public LevelPack ConfD;
        public LevelPackDetail ConfDetail;
        public override UIResAlt Res { get; } = new(UIConfig.UILevelPackPanel);
        public VisualPopup MainPop { get; } = new(UIConfig.UILevelPackPanel); //活动开启theme

        public override ActivityVisual Visual => MainPop.visual;

        #region Activity
        public PackLevel(ActivityLite lite)
        {
            Lite = lite;
            ConfD = GetLevelPack(lite.Param);
            RefreshTheme();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            _detailID = ReadInt(3, data_.AnyState);
            _triggerLv = ReadInt(4, data_.AnyState);
            ConfDetail = GetLevelPackDetail(_detailID);
            if (ConfDetail == null)
            {
                return;
            }

            PackId = ConfDetail.PackDetail[0];
            RefreshContent();
            InitTheme();
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            any.Add(ToRecord(3, _detailID));
            any.Add(ToRecord(4, _triggerLv));
        }

        private void InitTheme()
        {
            MainPop.Setup(ConfD.EventTheme, this);
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(Popup, state_, false);
        }

        public override void SetupFresh()
        {
            _detailID = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.GradeId);
            ConfDetail = GetLevelPackDetail(_detailID);
            if (ConfDetail == null)
            {
                return;
            }

            PackId = ConfDetail.PackDetail[0];
            var mgr = Game.Manager.mergeLevelMan;
            var curLv = mgr.level;
            _triggerLv = curLv;

            RefreshContent();
            InitTheme();

            MainPop.Popup();
        }

        public override void WhenEnd()
        {
            base.WhenEnd();
            _triggerLv = 0;
        }
        #endregion

        #region Pack
        public override BonusReward MatchPack(int packId_)
        {
            return base.MatchPack(packId_);
        }

        public override void PurchaseSuccess(int packId_, IList<RewardCommitData> rewards_, bool late_)
        {
            base.PurchaseSuccess(packId_, rewards_, late_);
        }
        #endregion

        #region 对外接口
        public void TryPurchase(Action<IList<RewardCommitData>> onPurchase)
        {
            void OnPurchase(IList<RewardCommitData> list)
            {
                DataTracker.level_pack_reward.Track(this, PackId, GetCurConfLv());
                onPurchase.Invoke(list);
            }

            Game.Manager.activity.giftpack.Purchase(this, Content, OnPurchase);
        }

        public (string, string) GetPrice()
        {
            var before = string.Empty;
            var cur = string.Empty;

            var iapPack = GetIAPPack(PackId);
            if (iapPack == null)
            {
                return (before, cur);
            }

            // 计算折扣后的价格
            cur = Game.Manager.iap.PriceInfo(iapPack.IapId);

            // 计算折扣前的价格
            before = Game.Manager.iap.PriceInfo(ConfDetail.PackDetail[1]);

            return (before, cur);
        }


        public int GetCurConfLv()
        {
            if (_triggerLv != 0)
            {
                return _triggerLv;
            }

            if (ConfD == null)
            {
                return -1;
            }

            if (ConfD.Level.Count > 1)
            {
                var mgr = Game.Manager.mergeLevelMan;
                var curLv = mgr.level;

                for (var i = 0; i < ConfD.Level.Count; i++)
                {
                    var level = ConfD.Level[i];

                    if (level == curLv)
                    {
                        return level;
                    }

                    if (level > curLv && i > 0)
                    {
                        return ConfD.Level[i - 1];
                    }
                }

                if (ConfD.Level[^1] <= curLv)
                {
                    return ConfD.Level[^1];
                }
            }
            else
            {
                return ConfD.Level[0];
            }


            return -1;
        }

        public int GetNextLevelDiff()
        {
            var mgr = Game.Manager.mergeLevelMan;
            var curLv = mgr.level;

            // 下一个配置
            var next = GetLevelPack(ConfD.Id + 1);
            if (next != null)
            {
                if (next.Level.Count > 1)
                {
                    // 下一个配置有多个预设等级
                    // 获得配置的下一个等级
                    var nextLv = -1;
                    foreach (var lv in next.Level)
                    {
                        if (lv > curLv)
                        {
                            nextLv = lv;
                            break;
                        }
                    }

                    if (nextLv == -1)
                    {
                        DebugEx.Info($"packLevel not find nextLv {ConfD.Id} {curLv}");
                        return -1;
                    }

                    return nextLv - curLv;
                }

                // 只有一个预设等级
                return next.Level[0] - curLv;
            }
            else
            {
                // 当前是最后一个配置 
                // 获得配置的下一个等级
                var nextLv = -1;
                foreach (var lv in ConfD.Level)
                {
                    if (lv > curLv)
                    {
                        nextLv = lv;
                        break;
                    }
                }

                if (nextLv == -1)
                {
                    DebugEx.Info($"packLevel not find nextLv {ConfD.Id} {curLv}");
                    return -1;
                }

                return nextLv - curLv;
            }
        }
        #endregion
    }

    public class PackLevelEntry : IEntrySetup
    {
        public Entry Entry => e;
        private readonly Entry e;
        private readonly PackLevel p;

        public PackLevelEntry(Entry e_, PackLevel p_)
        {
            (e, p) = (e_, p_);
            e.innerTxt.gameObject.SetActive(true);
            e.innerTxt.SetText(p.GetCurConfLv().ToString());
        }

        public override void Clear(Entry e_)
        {
        }

        public override string TextCD(long diff_)
        {
            e.innerTxt.SetText(p.GetCurConfLv().ToString());
            return UIUtility.CountDownFormat(diff_);
        }
    }
}