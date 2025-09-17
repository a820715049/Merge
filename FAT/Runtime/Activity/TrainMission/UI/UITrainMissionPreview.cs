// ================================================
// File: UITrainOrderMain.cs
// Author: yueran.li
// Date: 2025/07/28 17:57:11 星期一
// Desc: 火车任务预览界面
// ================================================

using System.Collections.Generic;
using EL;
using fat.conf;
using FAT.Merge;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace FAT
{
    public class UITrainMissionPreview : UIBase
    {
        public List<UICommonItem> rewards;
        public List<MBTrainMissionPreviewSpawner> spawners = new();

        // 活动实例 
        private TrainMissionActivity _activity;

        #region UI基础
        protected override void OnCreate()
        {
            RegisterComp();
            AddButton();
        }

        private void RegisterComp()
        {
        }

        private void AddButton()
        {
            transform.AddButton("Content/Panel/BtnClose", Close).WithClickScale().FixPivot();
            transform.AddButton("Content/Panel/BtnConfirm", OnClickConfirm).WithClickScale().FixPivot();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1)
            {
                return;
            }

            _activity = (TrainMissionActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            var challengeIDList = TrainGroupDetailVisitor.Get(_activity.groupDetailID)?.IncludeChallenge;
            if (challengeIDList == null)
            {
                return;
            }

            // 刷新生成器
            for (var index = 0; index < spawners.Count; index++)
            {
                var spawner = spawners[index];

                var challengeID = challengeIDList[index];
                var challenge = TrainChallengeVisitor.Get(challengeID);
                if (challenge == null)
                {
                    continue;
                }

                spawner.Init(_activity, index, challenge.ConnectSpawner.ToList());
            }

            // 刷新奖励
            var previewRewards = _activity.GetTotalRewardPreview();

            for (var i = 0; i < previewRewards.Count; i++)
            {
                var previewReward = previewRewards[i];
                var reward = rewards[i];
                if (i < previewRewards.Count)
                {
                    reward.gameObject.SetActive(true);
                    reward.Refresh(previewReward);
                }
                else
                {
                    reward.gameObject.SetActive(false);
                }
            }
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostOpen()
        {
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
            if (act is TrainMissionActivity)
            {
                Close();
            }
        }

        private void OnClickConfirm()
        {
            Close();
        }
        #endregion
    }
}