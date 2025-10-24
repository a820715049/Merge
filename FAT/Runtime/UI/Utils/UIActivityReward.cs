/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:09:54
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:09:54
 * Description: 活动通用领奖界面
 */

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Config;
using TMPro;
using EL;

namespace FAT
{
    public class UIActivityReward : UIBase
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
        private string descStr;
        private string closeTipStr;

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
            descStr = items[3] as string;
            closeTipStr = items.Length > 4 ? items[4] as string : I18N.Text("#SysComBtn7");
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
            desc.text = descStr;
            btnClaim.text.text.text = closeTipStr;
        }

        protected override void OnPostClose()
        {
            isEntering = false;
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLOSE>().Dispatch();
            }
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
            MessageCenter.Get<MSG.GAME_ACTIVITY_REWARD_CLICK_CLAIM>().Dispatch();
            var root = rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    var reward = rewards[i];
                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(rewards[i], from, () => { }, 66f);
                }
            }

            Close();
        }
    }
}