// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务 本轮达成界面
// ================================================

using System.Collections.Generic;
using DG.Tweening;
using EL;
using FAT.Merge;
using FAT.MSG;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionComplete : UIBase
    {
        [SerializeField] private GameObject btnGo;
        private NonDrawingGraphic _block;
        private SkeletonGraphic _spine;
        private Animator _animator;
        private TextMeshProUGUI _descComplete;
        private TextMeshProUGUI _descReady;
        private TextProOnACircle _roundBefore;
        private TextProOnACircle _roundAfter;

        // 活动实例 
        private TrainMissionActivity _activity;
        private UITrainMissionMain _main;

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
            transform.Access("Content/desc_complete", out _descComplete);
            transform.Access("Content/desc_ready", out _descReady);
            transform.Access("Content/spine/round_before", out _roundBefore);
            transform.Access("Content/spine/round_after", out _roundAfter);
        }

        private void AddButton()
        {
            transform.AddButton("Content/BtnGo", OnClickGo).WithClickScale().FixPivot();
            transform.AddButton("Mask", OnClickGo);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
            _main = (UITrainMissionMain)items[1];
        }

        protected override void OnPreOpen()
        {
            SetBlock(true);
            DOVirtual.DelayedCall(2.6f, () => { SetBlock(false); });

            _roundBefore.SetText(I18N.FormatText("#SysComDesc1544", _activity.challengeIndex + 1));
            _roundAfter.SetText(I18N.FormatText("#SysComDesc1544", _activity.challengeIndex + 2));

            // 判断是最后一档里程碑
            _descComplete.SetText(I18N.Text("#SysComDesc1549"));
            _descReady.SetText(I18N.Text("#SysComDesc1550"));
        }

        protected override void OnPostOpen()
        {
            _animator.SetTrigger("Show");
            
            Game.Manager.audioMan.TriggerSound("TrainNewChallenge"); // 火车-新一轮挑战
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

        private void OnClickGo()
        {
            // 判断是否还有下一轮
            if (_activity.waitEnterNextChallenge)
            {
                // 进入下一轮
                TrainMissionUtility.EnterNextChallenge();
            }


            _animator.SetTrigger("Hide");
            DOVirtual.DelayedCall(0.15f, () => Close());
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