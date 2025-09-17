// ==================================================
// // File: UITrainMissionEntry.cs
// // Author: liyueran
// // Date: 2025-08-05 11:08:50
// // Desc: $火车任务 棋盘入口
// // ==================================================

using System.Collections.Generic;
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UITrainMissionEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject startGroup;
        [SerializeField] private GameObject unstartGroup;

        [SerializeField] private GameObject dot;
        [SerializeField] private RectTransform mask;
        [SerializeField] private TextMeshProUGUI startCd;
        [SerializeField] private TextMeshProUGUI progressValue;
        [SerializeField] private UICommonItem reward;

        [SerializeField] private TextMeshProUGUI unstartCd;


        private TrainMissionActivity _activity;

        public void Start()
        {
            transform.AddButton("Root/EnterBtn", EntryClick);
        }

        private void EntryClick()
        {
            if (!_activity.Active)
                return;

            _activity.Open();
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
        }


        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as TrainMissionActivity;
            if (_activity == null)
            {
                Visible(false);
                return;
            }

            if (!_activity.Active)
            {
                Visible(false);
                return;
            }

            Visible(true);

            // 判断是否打开过活动
            var chooseGroup = _activity.NeedChooseGroup();

            unstartGroup.SetActive(chooseGroup);
            startGroup.SetActive(!chooseGroup);

            if (chooseGroup)
            {
                UIUtility.CountDownFormat(unstartCd, _activity.Countdown);
            }
            else
            {
                // 进度条
                SetProgress(CalProgressValue());

                // 绿色角标
                dot.SetActive(_activity.CheckCanShowRP());

                // 最后一档奖励
                var milestones = new List<TrainMilestone>();
                _activity.GetTrainMilestones(milestones);
                var (id, count, _) = milestones[^1].Reward.ConvertToInt3();
                reward.Refresh(id, count);

                UIUtility.CountDownFormat(startCd, _activity.Countdown);
            }
        }


        private void Visible(bool value)
        {
            root.SetActive(value);
            transform.GetComponent<LayoutElement>().ignoreLayout = !value;
        }

        private float CalProgressValue()
        {
            // 获取mask相对于canvas的缩放
            var maskRect = mask.parent.GetComponent<RectTransform>();
            var max = maskRect.rect.width;

            var maxLv = _activity.GetCurMilestoneTotal();
            var curLv = _activity.GetCurMilestoneProgress();
            var progress = (float)curLv / maxLv;

            progressValue.SetText($"{curLv}/{maxLv}");
            return max * progress;
        }

        private void SetProgress(float to)
        {
            mask.sizeDelta = new Vector2(to, mask.sizeDelta.y);
        }
    }
}