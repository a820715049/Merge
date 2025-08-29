// ==================================================
// // File: UIBingoTaskMain.cs
// // Author: liyueran
// // Date: 2025-07-16 11:07:19
// // Desc: bingoTask主界面    
// // ==================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIBingoTaskMain : UIBase, INavBack
    {
        public float bingoSpreadTime = 0.12f;
        public float brokeWaitTime = 0.2f;

        public MBBingoTaskProgress progress;
        public MBBingoTaskBoard board;

        private TextMeshProUGUI _cd;
        private TextProOnACircle _title;
        private NonDrawingGraphic _block;

        private ActivityBingoTask _activity;

        #region UI Base
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("Content/block", out _block);
            transform.Access("Content/Bg/cd", out _cd);
            transform.Access("Content/Bg/Title", out _title);

            board = AddModule(new MBBingoTaskBoard(transform.Find("Content/Bg/board")));
            progress = AddModule(new MBBingoTaskProgress(transform.Find("Content/Bg/progress")));
        }

        private void AddButton()
        {
            transform.AddButton("Content/Bg/help", OnClickHelp).WithClickScale().FixPivot();
            transform.AddButton("Content/Bg/close", Close).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (ActivityBingoTask)items[0];
        }

        protected override void OnPreOpen()
        {
            _title.SetText(I18N.Text("#SysComDesc1430"));
            Game.Manager.screenPopup.Block(delay_: true);
            board.Show(_activity, this);
            progress.Show(_activity, this);
        }

        protected override void OnPostOpen()
        {
            board.OnPostOpen();
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(_RefreshCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(_RefreshCD);
        }


        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            progress.Hide();
            board.Hide();
            Game.Manager.screenPopup.Block(false, false);
        }
        #endregion


        #region 事件
        private void _RefreshCD()
        {
            UIUtility.CountDownFormat(_cd, _activity?.Countdown ?? 0);
        }


        private void OnClickHelp()
        {
            UIManager.Instance.OpenWindow(_activity.VisualHelp.res.ActiveR);
        }
        #endregion

        #region Block
        public void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        private bool IsBlock => _block.raycastTarget;
        #endregion

        public void OnNavBack()
        {
            if (IsBlock)
            {
                return;
            }

            Close();
        }
    }
}