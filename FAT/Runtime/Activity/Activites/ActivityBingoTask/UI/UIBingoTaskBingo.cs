// ==================================================
// // File: UIBingoTaskMain.cs
// // Author: liyueran
// // Date: 2025-07-16 11:07:19
// // Desc: bingoTask bingo完成界面 
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UIBingoTaskBingo : UIBase
    {
        private NonDrawingGraphic _block;
        private SkeletonGraphic _spine;
        private Animator _animator;
        private TextProOnACircle _title;
        private TextMeshProUGUI _count;

        private ActivityBingoTask _activity;
        private BingoResult _result;

        #region UI Base
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
            transform.Access("block", out _block);
            transform.Access("", out _animator);
            transform.Access("Content/icon/title", out _title);
            transform.Access("Content/icon/count", out _count);
            transform.Access("Content/icon/pf_default_bingotask/pf_default_bingotask", out _spine);
        }

        private void AddButton()
        {
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (ActivityBingoTask)items[0];
            _result = (BingoResult)items[1];
        }

        protected override void OnPreOpen()
        {
            SetBlock(true);
            _title.SetText(I18N.Text("#SysComDesc1433"));

            var count = 0;
            var bingoResults = new[]
            {
                BingoResult.RowBingo,
                BingoResult.ColumnBingo,
                BingoResult.MainDiagonalBingo,
                BingoResult.AntiDiagonalBingo
            };

            foreach (var result in bingoResults)
            {
                if ((_result & result) == result)
                {
                    count++;
                }
            }

            _count.SetText($"x{count}");
        }

        protected override void OnPostOpen()
        {
            _animator.SetTrigger("Show");
            Game.Manager.audioMan.TriggerSound("BingoTaskBingo");
            _spine.AnimationState.SetAnimation(0, "show", true).Complete += _ => { Close(); };
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }


        protected override void OnPreClose()
        {
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<UI_BINGO_CLOSE>().Dispatch();
        }
        #endregion


        #region 事件
        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != _activity)
            {
                return;
            }

            // 活动结束 避免因为页面关闭 协程被打断 签到动画导致的block
            if (IsBlock)
            {
                SetBlock(false);
            }

            Close();
        }
        #endregion

        #region Block
        private void SetBlock(bool value)
        {
            _block.raycastTarget = value;
        }

        private bool IsBlock => _block.raycastTarget;
        #endregion
    }
}