/**
 * @Author: zhangpengjian
 * @Date: 2025/2/18 17:37:02
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/2/18 17:37:02
 * Description: 砍价礼包    
 */

using EL;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class PackDiscount : GiftPack
    {
        public DiscountPack ConfD;
        public DiscountProgress ConfProgress;
        public override int PackId { get; set; }
        public override int ThemeId => ConfD.EventTheme;
        public override int StockTotal => ConfD.Paytimes;
        public override UIResAlt Res { get; } = new(UIConfig.UIPackDiscount);
        public UIResAlt HelpRes { get; } = new(UIConfig.UIPackDiscountHelp);
        private bool needOpen = false;
        private int progressId;
        private int token;
        private int tokenPhase;
        public int TokenPrev { get; private set; }

        public PackDiscount() { }


        public PackDiscount(ActivityLite lite_)
        {
            Lite = lite_;
            ConfD = GetDiscountPack(lite_.Param);
            RefreshTheme();
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(OnFlyComplete);
        }

        public override void SetupFresh()
        {
            progressId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.Detail);
            ConfProgress = GetDiscountProgress(progressId);
            PackId = ConfProgress.PackDetail[0];
            RefreshContent();
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            base.LoadSetup(data_);
            var any = data_.AnyState;
            progressId = ReadInt(3, any);
            tokenPhase = ReadInt(4, any);
            token = ReadInt(5, any);
            TokenPrev = token;
            if (progressId > 0)
            {
                ConfProgress = GetDiscountProgress(progressId);
            }
            else
            {
                progressId = Game.Manager.userGradeMan.GetTargetConfigDataId(ConfD.Detail);
                ConfProgress = GetDiscountProgress(progressId);
            }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            base.SaveSetup(data_);
            var any = data_.AnyState;
            any.Add(ToRecord(3, progressId));
            any.Add(ToRecord(4, tokenPhase));
            any.Add(ToRecord(5, token));
        }

        public override void WhenEnd()
        {
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(OnFlyComplete);
        }

        public override void WhenReset()
        {
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(OnFlyComplete);
        }


        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(Popup, state_, false);
        }

        public void SetPrevToken(int token_)
        {
            TokenPrev = token_;
        }

        public void UpdateToken(int rewardId, int addNum)
        {
            if (rewardId == ConfD.TokenId)
            {
                if (tokenPhase < ConfProgress.ProgressDetail.Count)
                {
                    TokenPrev = token;
                    token += addNum;
                    CheckToken();
                }
            }
        }

        public (string, string, string) GetPrice()
        {
            if (tokenPhase == 0)
            {
                return (Price, null, null);
            }
            var pricePrev = ConfProgress.PackDetail[tokenPhase - 1];
            var c2 = GetIAPPack(pricePrev);
            var p2 = Game.Manager.iap.PriceInfo(c2.IapId);
            if (tokenPhase > 1)
            {
                var pricePrev2 = ConfProgress.PackDetail[tokenPhase - 2];
                var c3 = GetIAPPack(pricePrev2);
                var p3 = Game.Manager.iap.PriceInfo(c3.IapId);
                return (Price, p2, p3);
            }
            return (Price, p2, null);
        }

        private void CheckToken()
        {
            var max = ConfProgress.ProgressDetail[tokenPhase];
            var need = max.ConvertToRewardConfig();
            if (token < 2 && tokenPhase == 0)
            {
                needOpen = true;
            }
            if (token < need.Count) return;

            tokenPhase++;

            PackId = ConfProgress.PackDetail[tokenPhase];
            RefreshContent();
            DataTracker.event_discountpack_stage.Track(this, tokenPhase, tokenPhase == ConfProgress.ProgressDetail.Count);
            if (tokenPhase < ConfProgress.ProgressDetail.Count)
            {
                token -= need.Count;
            }
            needOpen = true;
        }

        public (int, int, int) GetTokenProgress()
        {
            var t = tokenPhase >= ConfProgress.ProgressDetail.Count ? ConfProgress.ProgressDetail.Count - 1 : tokenPhase;
            var max = ConfProgress.ProgressDetail[t];
            return (token, max.ConvertToRewardConfig().Count, max.ConvertToRewardConfig().Id);
        }

        private void OnFlyComplete(FlyType ft)
        {
            if (ft != FlyType.PackDiscountToken) return;
            if (needOpen)
            {
                UIManager.Instance.RegisterIdleAction("ui_idle_pack_discount", 601, () => { Res.ActiveR.Open(this, tokenPhase > 0 ? true : false); });
                needOpen = false;
            }
        }

        public bool IsMax()
        {
            return tokenPhase >= ConfProgress.ProgressDetail.Count;
        }

        public int GetTokenPhase()
        {
            return tokenPhase;
        }
    }

    public class PackDiscountEntry : ListActivity.IEntrySetup
    {
        private readonly ListActivity.Entry e;
        private readonly PackDiscount p;
        public PackDiscountEntry(ListActivity.Entry entry, PackDiscount packDiscount)
        {
            (e, p) = (entry, packDiscount);
            var phase = packDiscount.GetTokenPhase();
            e.discount.gameObject.SetActive(phase > 0);
            e.obj.GetComponent<MBFlyTarget>().flyType = FlyType.PackDiscountToken;
            if (phase > 0)
            {
                var show = packDiscount.ConfProgress.ProgressDetail[phase - 1].ConvertToRewardConfig().Id;
                e.discount.text = I18N.FormatText("#SysComDesc959", show);
            }
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(Refresh);
        }

        public override void Clear(ListActivity.Entry e_)
        {
            e.obj.GetComponent<MBFlyTarget>().flyType = FlyType.None;
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(Refresh);
        }

        public void Refresh(FlyType ft)
        {
            if (e == null || e.discount == null || !e.discount.gameObject)
            {
                return;
            }
            if (ft != FlyType.PackDiscountToken) return;
            var phase = p.GetTokenPhase();

            e.discount.gameObject.SetActive(phase > 0);
            if (phase > 0)
            {
                var show = p.ConfProgress.ProgressDetail[phase - 1].ConvertToRewardConfig().Id;
                e.discount.text = I18N.FormatText("#SysComDesc959", show);
            }
        }
    }
}