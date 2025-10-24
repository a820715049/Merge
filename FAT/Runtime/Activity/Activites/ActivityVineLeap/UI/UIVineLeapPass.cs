// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using EL;
using TMPro;

namespace FAT
{
    public class UIVineLeapPass : UIBase
    {
        public TextMeshProUGUI tfRank;
        public TextMeshProUGUI tfCd;

        private ActivityVineLeap _activity;
        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnClose", OnClickClose);
            transform.AddButton("Content/Panel/BtnConfirm", OnClickConfirm);
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            _activity = (ActivityVineLeap)items[0];
            tfRank.text = _activity.GetResultRank().ToString();
            RefreshCD();
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
        }

        protected override void OnPreClose()
        {
            base.OnPreClose();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(tfCd, _activity.Countdown);
        }

        private void OnClickConfirm()
        {
            Close();
            _activity.StartCurStep();
        }

        private void OnClickClose()
        {
            Close();
            _activity.CurLevelState = LevelState.None;
        }
    }
}