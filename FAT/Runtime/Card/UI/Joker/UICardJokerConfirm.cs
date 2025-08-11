/*
 * @Author: tang.yan
 * @Description: 万能卡确认选择界面 
 * @Date: 2024-03-27 20:03:58
 */
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UICardJokerConfirm : UIBase
    {
        [SerializeField] private UICardJokerItemCell cardCell;
        [SerializeField] private Button confirmBtn;
        [SerializeField] private Button cancelBtn;
        
        private int _curCardId;
        
        protected override void OnCreate()
        {
            confirmBtn.FixPivot().WithClickScale().onClick.AddListener(_OnConfirmBtnClick);
            cancelBtn.FixPivot().WithClickScale().onClick.AddListener(_OnCancelBtnClick);
        }

        protected override void OnParse(params object[] items)
        {
        }

        protected override void OnPreOpen()
        {
            var roundData = Game.Manager.cardMan.GetCardRoundData();
            if (roundData == null) return;
            var curJokerCardData = roundData.GetCurIndexJokerData();
            if (curJokerCardData == null) return;
            _curCardId = curJokerCardData.GetCurSelectCardId();
            cardCell.ForceRefresh(_curCardId);
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnPostClose()
        {
            cardCell.ForceClear();
        }
        
        private void _OnConfirmBtnClick()
        {
            Game.Manager.cardMan.TryUseCardJoker(_curCardId);
            Close();
        }

        private void _OnCancelBtnClick()
        {
            Close();
        }
    }
}