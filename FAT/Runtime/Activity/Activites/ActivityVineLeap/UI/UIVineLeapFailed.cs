// ===================================================
// Author: mengqc
// Date: 2025/09/02
// ===================================================

using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIVineLeapFailed : UIBase
    {
        public TextMeshProUGUI tfSubDesc;
        public TextMeshProUGUI tfCd;
        public RectTransform rewardGroup;
        public TextMeshProUGUI tfStepLeft;
        public UIImageRes imgChest;


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
            imgChest.SetImage(_activity.GetChestIcon());
            UIUtility.CommonItemSetup(rewardGroup);
            UIUtility.CommonItemRefresh(rewardGroup, _activity.GetMilestoneRewards().ToList());
            var lvCfg = _activity.GetLevelConf(_activity.CurLevel);
            tfSubDesc.text = I18N.FormatText("#SysComDesc1859", lvCfg.TotalNum);
            var leftStep = _activity.CurGroup.LevelId.Count - _activity.CurLevel;
            tfStepLeft.text = I18N.FormatText("#SysComDesc1741", leftStep);
        }

        protected override void OnAddListener()
        {
            base.OnAddListener();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(UpdateTime);
        }

        protected override void OnRemoveListener()
        {
            base.OnRemoveListener();
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(UpdateTime);
        }

        private void UpdateTime()
        {
            UIUtility.CountDownFormat(tfCd, _activity.Countdown);
        }

        private void OnClickConfirm()
        {
            Close();
            _activity.StartCurStep();
            if (!UIManager.Instance.IsShow(UIConfig.UIVineLeapMain))
            {
                _activity.OpenMain();
            }
        }

        private void OnClickClose()
        {
            Close();
            _activity.CurLevelState = LevelState.None;
        }
    }
}