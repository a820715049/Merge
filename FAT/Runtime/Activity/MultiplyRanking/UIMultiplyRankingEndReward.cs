/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public class UIMultiplyRankingEndReward : UIBase
    {
        [SerializeField] private Transform _rewardRoot;
        [SerializeField] private MapButton _btnClaim;
        [SerializeField] private GameObject _cellItem;
        private List<RewardCommitData> _rewards = new();
        private List<GameObject> _cellList = new();
        protected override void OnCreate()
        {
            _btnClaim.WhenClick = OnBtnClaim;
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.Ranking_REWARD_ITEM, _cellItem);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _rewards = items[1] as List<RewardCommitData>;
            }
        }

        protected override void OnPreOpen()
        {
            RefreshRewards();
        }

        private void OnBtnClaim()
        {
            var root = _rewardRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                if (i < _rewards.Count)
                {
                    var reward = _rewards[i];
                    var from = root.GetChild(i).position;
                    UIFlyUtility.FlyReward(_rewards[i], from);
                }
            }
            Close();
        }

        private void RefreshRewards()
        {
            for (int i = 0; i < _rewards.Count; i++)
            {
                var rewardItem = _rewards[i];
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Ranking_REWARD_ITEM, _rewardRoot.transform);
                cell.transform.localPosition = Vector3.zero;
                cell.transform.localScale = Vector3.one;
                cell.SetActive(true);
                var item = cell.GetComponent<UICommonItem>();
                item.Refresh(rewardItem.rewardId, rewardItem.rewardCount);
                _cellList.Add(cell);
            }
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            foreach (var item in _cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Ranking_REWARD_ITEM, item);
            }
            _cellList.Clear();
        }
    }
}