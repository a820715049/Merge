/**
 * @Author: zhangpengjian
 * @Date: 2024/8/19 10:30:02
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/19 10:30:02
 * Description: 挖沙活动关卡奖励界面
 */

using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using EL;
using Config;

namespace FAT
{
    public class UIDiggingLevelReward : UIBase
    {
        [SerializeField] private Transform diggingGroup;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private MapButton btnClaim;
        [SerializeField] private float durationIn;
        [SerializeField] private UIImageRes icon;

        private Vector3 diggingWorldPos;
        private List<RewardCommitData> rewards;
        private bool isEntering;
        private List<RewardConfig> showRewards = new();
        private fat.rawdata.EventDiggingLevel level;

        protected override void OnCreate()
        {
            btnClaim.WhenClick = _OnBtnClaim;

            UIUtility.CommonItemSetup(rewardRoot);
        }

        protected override void OnParse(params object[] items)
        {
            diggingWorldPos = (Vector3)items[0];
            rewards = items[1] as List<RewardCommitData>;
            level = items[2] as fat.rawdata.EventDiggingLevel;

        }

        protected override void OnPreOpen()
        {
            diggingGroup.position = diggingWorldPos;
            diggingGroup.localScale = Vector3.zero;

            isEntering = true;
            diggingGroup.DOLocalMove(Vector3.zero, durationIn);
            diggingGroup.DOScale(1f, durationIn).OnComplete(() => isEntering = false);
            UIDiggingUtility.PlaySound(UIDiggingUtility.SoundEffect.DiggingLevelReward);

            _ShowRewards();
            icon.SetImage(level.LevelRewardIcon1);
        }

        protected override void OnPostClose()
        {
            isEntering = false;
            if (!Game.Manager.specialRewardMan.CheckCanClaimSpecialReward())
            {
                MessageCenter.Get<MSG.DIGGING_LEVE_CLEAR>().Dispatch();
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
            if (!UIDiggingUtility.TryGetEventInst(out var act)) return;

            var root = rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    if (CheckShowGame(rewards[i].rewardId) || CheckShowGame(rewards[i].rewardId))
                        (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowGameNode();
                    if (CheckShowRes(rewards[i].rewardId) || CheckShowRes(rewards[i].rewardId))
                        (UIManager.Instance.TryGetUI(UIConfig.UICommonShowRes) as UICommonShowRes)?.ShowOtherRes();
                    var reward = rewards[i];
                    FlyType ft;
                    if (reward.rewardId == act.diggingConfig.TokenId)
                    {
                        ft = FlyType.DiggingShovel;
                    }
                    else
                    {
                        // 其他
                        ft = FlyType.None;
                    }
                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(rewards[i], from, () => { MessageCenter.Get<MSG.DIGGING_REWARD_FLY_FEEDBACK>().Dispatch(ft); }, 66f);
                }
            }

            Close();
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
    }
}