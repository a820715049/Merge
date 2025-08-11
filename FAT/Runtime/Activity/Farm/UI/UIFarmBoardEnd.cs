// ================================================
// File: UIFarmBoardEnd.cs
// Author: yueran.li
// Date: 2025/04/29 14:46:21 星期二
// Desc: 农场活动结束界面
// ================================================


using System.Collections.Generic;
using System.Linq;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static EL.PoolMapping;

namespace FAT
{
    public class UIFarmBoardEnd : UIBase
    {
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TextMeshProUGUI desc;
        [SerializeField] private TextMeshProUGUI bgTitle;
        [SerializeField] private Button close;
        [SerializeField] private Button confirm;
        [SerializeField] private UICommonItem _item;
        [SerializeField] private GameObject descBg;

        // 进度条
        [SerializeField] private MBFarmBoardProgress progress;

        private readonly string completeKey = "#SysComDesc490"; // 完成全部活动目标时显示
        private readonly string unCompleteKey = "#SysComDesc273"; // 未完成时显示

        private FarmBoardActivity _activity;
        private Ref<List<RewardCommitData>> tokenRewardList; // 转换奖励
        private MBRewardLayout.CommitList _result;


        protected override void OnCreate()
        {
            close.onClick.AddListener(ClickBtn);
            confirm.onClick.AddListener(ClickBtn);
            progress.SetUpOnCreate();
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2) return;

            _activity = (items[0] as FarmBoardActivity);
            tokenRewardList = (Ref<List<RewardCommitData>>)items[1];
            _result = new() { list = tokenRewardList.obj };
            if (_result.list == null || _result.list.Count == 0)
            {
                descBg.SetActive(false);
                return;
            }

            descBg.SetActive(true);
            _item.Setup();
            var first = tokenRewardList.obj.FirstOrDefault();
            if (first != null)
            {
                _item.Refresh(first.rewardId, first.rewardCount);
            }
        }

        protected override void OnPreOpen()
        {
            if (_activity == null)
            {
                return;
            }

            progress.InitOnPreOpen(_activity);

            // 判断是否完成任务
            var complete = _activity.UnlockMaxLevel == _activity.GetAllItemIdList().Count;
            bgTitle.SetText(I18N.Text(complete ? completeKey : unCompleteKey));
            var tokenIcon = UIUtility.FormatTMPString(_activity.ConfD.TokenId);
            desc.SetTextFormat(I18N.Text("#SysComDesc1113"), tokenIcon);

            RefreshTheme();
        }

        protected override void OnPostOpen()
        {
            progress.RefreshProgress();
            progress.ScrollToItem(_activity.UnlockMaxLevel);
        }

        protected override void OnPreClose()
        {
            tokenRewardList.Free();
        }

        private void RefreshTheme()
        {
            _activity.EndPopup.visual.Refresh(title, "mainTitle");

            // var tokenIcon = UIUtility.FormatTMPString(_activity.ConfD.TokenId);
            // _activity.EndPopup.visual.RefreshText(_visualGroup, "desc", tokenIcon);
        }

        private void ClickBtn()
        {
            if (_result.list != null && _result.list.Count > 0)
            {
                UIFlyUtility.FlyReward(_result.list.FirstOrDefault(), _item.transform.position);
            }

            Close();
        }
    }
}