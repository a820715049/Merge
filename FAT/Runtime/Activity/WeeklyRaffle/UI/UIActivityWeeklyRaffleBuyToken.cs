// ================================================
// File: UIActivityWeeklyRaffleBuyToken.cs
// Author: yueran.li
// Date: 2025/06/09 17:45:20 星期一
// Desc: 签到抽奖 补签界面
// ================================================

using EL;
using FAT.MSG;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIActivityWeeklyRaffleBuyToken : UIBase
    {
        private UIImageRes _token;
        private TextMeshProUGUI _buyTxt;
        private TextMeshProUGUI _tokenTxt;
        private ActivityWeeklyRaffle _activity;

        private int day;
        private bool buyAll;

        protected override void OnCreate()
        {
            transform.Access("Content/bg/tokenBg/token", out _token);
            transform.Access("Content/bg/buy/num", out _buyTxt);
            transform.Access("Content/bg/tokenBg/num", out _tokenTxt);

            transform.AddButton("Content/bg/buy", OnClickBuy).WithClickScale().FixPivot();
            transform.AddButton("Content/bg/close", Close).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = items[0] as ActivityWeeklyRaffle;
            day = (int)items[1];
            buyAll = (bool)items[2];
        }

        protected override void OnPreOpen()
        {
            var gemCount = _activity.CalBuyGem(1);
            _tokenTxt.SetText("1");
            if (buyAll)
            {
                var dayCount = _activity.GetConfigSignDayCount() - _activity.GetSignCount();
                gemCount = _activity.CalBuyGem(dayCount);
                _tokenTxt.SetText($"{dayCount}");
            }

            var tokenIcon = UIUtility.FormatTMPString((int)CoinType.Gem);
            _buyTxt.SetText($"{gemCount}{tokenIcon}");
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_REFILL>().AddListener(OnRefill);
        }


        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);

            MessageCenter.Get<WEEKLYRAFFLE_REFILL>().RemoveListener(OnRefill);
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
        }


        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            Close();
        }

        private void OnClickBuy()
        {
            ActivityWeeklyRaffle.CheckRet ret;
            if (buyAll)
            {
                _activity.TryBuyAll(out ret);
            }
            else
            {
                _activity.TryBuy(day, out ret);
            }

            // 钻石不足
            if (ret == ActivityWeeklyRaffle.CheckRet.GemNotEnough)
            {
                UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyRaffleDraw);
                UIManager.Instance.CloseWindow(UIConfig.UIActivityWeeklyRaffleMain);
                Close();
            }
        }

        // 补签成功
        private void OnRefill(int _day, bool _buyAll)
        {
            if (_buyAll)
            {
                var ui = UIManager.Instance.TryGetUI(UIConfig.UIActivityWeeklyRaffleDraw);
                if (ui != null && ui is UIActivityWeeklyRaffleDrawn drawn)
                {
                    drawn.PlayRefillAnim();
                }
            }
            else
            {
                var ui = UIManager.Instance.TryGetUI(UIConfig.UIActivityWeeklyRaffleMain);
                if (ui != null && ui is UIActivityWeeklyRaffleMain main)
                {
                    main.PlaySignAnim(_day, true);
                }
            }

            Close();
        }
    }
}