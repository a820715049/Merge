/*
 * @Author: qun.chao
 * @Date: 2024-04-23 12:01:10
 */
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using EL;

namespace FAT
{
    public class UITreasureHuntLevelReward : UIBase
    {
        [SerializeField] private Transform treasureGroup;
        [SerializeField] private Transform rewardRoot;
        [SerializeField] private MapButton btnClaim;
        [SerializeField] private float durationIn;
        [SerializeField] private float durationOut;

        private Vector3 treasureWorldPos;
        private List<Config.RewardConfig> rewards;
        private bool isEntering;
        private string poolKey = "treasure_level_reward_icon";

        protected override void OnCreate()
        {
            btnClaim.WhenClick = _OnBtnClaim;

            UIUtility.CommonItemSetup(rewardRoot);
            GameObjectPoolManager.Instance.PreparePool(poolKey, treasureGroup.gameObject);
        }

        protected override void OnParse(params object[] items)
        {
            treasureWorldPos = (Vector3)items[0];
            rewards = items[1] as List<Config.RewardConfig>;
        }

        protected override void OnPreOpen()
        {
            treasureGroup.position = treasureWorldPos;
            treasureGroup.localScale = Vector3.zero;

            isEntering = true;
            treasureGroup.DOLocalMove(Vector3.zero, durationIn);
            treasureGroup.DOScale(1f, durationIn).OnComplete(() => isEntering = false);

            _ShowRewards();
        }

        protected override void OnPostClose()
        {
            isEntering = false;
            MessageCenter.Get<MSG.UI_TREASURE_LEVEL_CLEAR>().Dispatch();
        }

        private void _ShowRewards()
        {
            UIUtility.CommonItemRefresh(rewardRoot, rewards);
        }

        private void _OnBtnClaim()
        {
            if (isEntering) return;
            if (!UITreasureHuntUtility.TryGetEventInst(out var act)) return;

            // 奖励飞入背包 / 钥匙飞入钥匙包
            var root = rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < rewards.Count)
                {
                    var reward = rewards[i];
                    var from = root.GetChild(i).position;
                    UITreasureHuntUtility.FLyTempReward(from, reward);
                }
            }

            // 图标飞到进度条
            var targetPos = UITreasureHuntUtility.levelRewardTarget;
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
            var go = GameObjectPoolManager.Instance.CreateObject(poolKey, effRoot);
            go.transform.position = treasureGroup.position;
            go.transform.localScale = treasureGroup.localScale;
            go.transform.DOScale(0.25f, durationOut);
            go.transform.DOMove(targetPos, durationOut).OnComplete(() =>
            {
                GameObjectPoolManager.Instance.ReleaseObject(poolKey, go);
                UITreasureHuntUtility.NotifyTreasureProgressFeedback();
            });

            base.Close();
        }
    }
}