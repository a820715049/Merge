/**
 * @Author: zhangpengjian
 * @Date: 2025/4/30 10:54:51
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/30 10:54:51
 * Description: 周任务奖励
 */

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Config;
using TMPro;
using EL;

namespace FAT
{
    public class UIActivityWeeklyTaskReward : UIBase
    {
        [SerializeField] private Transform group;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private MapButton btnClaim;
        [SerializeField] private float durationIn;
        [SerializeField] private UIImageRes icon;
        [SerializeField] private TextMeshProUGUI desc;

        private Vector3 pos;
        private List<RewardCommitData> rewards;
        private bool isEntering;
        private List<RewardConfig> showRewards = new();
        private string iconStr;
        private ActivityWeeklyTask task;
        private bool isFinal;

        protected override void OnCreate()
        {
            btnClaim.WhenClick = _OnBtnClaim;

            UIUtility.CommonItemSetup(rewardRoot);
        }

        protected override void OnParse(params object[] items)
        {
            pos = (Vector3)items[0];
            rewards = items[1] as List<RewardCommitData>;
            iconStr = items[2] as string;
            task = items[3] as ActivityWeeklyTask;
            isFinal = (bool)items[4];
        }

        protected override void OnPreOpen()
        {
            group.position = pos;
            group.localScale = Vector3.zero;

            isEntering = true;
            group.DOLocalMove(Vector3.zero, durationIn);
            group.DOScale(1f, durationIn).OnComplete(() => isEntering = false);
            Game.Manager.audioMan.TriggerSound("DiggingLevelReward");
            _ShowRewards();
            icon.SetImage(iconStr);
            desc.text = isFinal ? I18N.Text("#SysComDesc1081") : I18N.Text("#SysComDesc1080");
        }

        protected override void OnPostClose()
        {
            isEntering = false;
        }

        private void _ShowRewards()
        {
            showRewards.Clear();
            foreach (var item in rewards)
            {
                var r = new RewardConfig
                {
                    Id = item.rewardId,
                    Count = item.rewardCount
                };
                showRewards.Add(r);
            }
            UIUtility.CommonItemRefresh(rewardRoot, showRewards);
        }

        private void _OnBtnClaim()
        {
            if (isEntering) return;
            if (task == null) return;

            var root = rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    var reward = rewards[i];
                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(rewards[i], from, () => {}, 66f);
                }
            }

            Close();
        }
    }
}