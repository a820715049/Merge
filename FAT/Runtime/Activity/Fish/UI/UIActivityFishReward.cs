// ================================================
// File: UIActivityFishReward.cs
// Author: yueran.li
// Date: 2025/04/14 16:50:43 星期一
// Desc: 钓鱼棋盘里程碑奖励界面
// ================================================


using System.Collections.Generic;
using Config;
using EL;
using FAT.MSG;
using UnityEngine;
using static fat.conf.Data;

namespace FAT
{
    public class UIActivityFishReward : UIBase, INavBack
    {
        [SerializeField] private Transform fishGroup;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private MapButton btnClaim;
        [SerializeField] private float durationIn;
        [SerializeField] private UIImageRes icon;

        private ActivityFishing activityFish;

        private List<RewardCommitData> rewards = new();

        #region UI
        protected override void OnCreate()
        {
            btnClaim.WhenClick = _OnBtnClaim;

            UIUtility.CommonItemSetup(rewardRoot);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 1) return;
            activityFish = (ActivityFishing)items[0];

            rewards.Clear();
            activityFish.FillMilestoneRewards(rewards);
        }

        protected override void OnPreOpen()
        {
            // 宝箱icon
            int idx = activityFish.MilestoneIdx;
            var conf = GetEventFishMilestone(idx);
            icon.SetImage(conf.RewardIcon);

            // 奖励Icon
            ShowRewards();
            Game.Manager.audioMan.TriggerSound("FishBoardMilestone");
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<ACTIVITY_END>().AddListener(WhenEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        protected override void OnPostClose()
        {
        }
        #endregion


        #region Listener
        private void _OnBtnClaim()
        {
            MessageCenter.Get<FISHING_MILESTONE_REWARD_CLOSE>().Dispatch();
            // 遍历奖励 播放奖励动画
            var root = rewardRoot;
            for (var i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    // 判断是否显示进入主棋盘按钮
                    if (CheckShowGame(rewards[i].rewardId))
                        (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowGameNode();

                    // 判断是否显示资源栏
                    if (CheckShowRes(rewards[i].rewardId))
                        (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();

                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(rewards[i], from);
                }
            }

            Close();
        }

        private void WhenEnd(ActivityLike act, bool expire)
        {
            if (act != activityFish || !expire) return;
            _OnBtnClaim();
        }
        #endregion

        private void ShowRewards()
        {
            var showRewards = new List<RewardConfig>(rewards.Count);
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

        private bool CheckShowGame(int id)
        {
            if (id == Constant.kMergeEnergyObjId)
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.ActivityToken))
                return false;
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.RandomBox))
                return false;
            return true;
        }

        private bool CheckShowRes(int id)
        {
            if (Game.Manager.objectMan.IsType(id, ObjConfigType.Coin))
                return true;
            if (id == Constant.kMergeEnergyObjId)
                return true;
            return false;
        }

        public void OnNavBack()
        {
            _OnBtnClaim();
        }
    }
}