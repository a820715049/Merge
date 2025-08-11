/*
 * @Author: yanfuxing
 * @Date: 2025-04-22 13:31:00
 */
using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    // 里程碑奖励
    public class UIActivityRankingMilestoneReward : UIBase
    {
        [SerializeField] private TextMeshProUGUI rankNumText; //奖励数量
        [SerializeField] private TextMeshProUGUI collectRewardNum; //奖励数量
        [SerializeField] private GameObject cellRoot;  //cell的根节点
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private GameObject cellItem;
        [SerializeField] private TextProOnACircle topTitleOne;
        [SerializeField] private TextProOnACircle topTitleTwo;
        private List<GameObject> cellList = new(); //cell列表
        private List<RewardCommitData> commitRewardList = new(); //里程碑奖励数据列表
        private List<RewardItemCell> _currentRewardList = new List<RewardItemCell>();
        private ActivityRanking activity;

        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Mask", OnCloseBtnClick);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.Ranking_REWARD_CELL, cellItem);
        }
        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0)
            {
                activity = (ActivityRanking)items[0];
                commitRewardList = (List<RewardCommitData>)items[1];
            }

        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            InitPanel();
        }

        protected override void OnPostClose()
        {
            base.OnPostClose();
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Ranking_REWARD_CELL, item);
            }
            cellList.Clear();
        }

        private void InitPanel()
        {
            rankNumText.text = activity.Rank.ToString();
            collectRewardNum.text = activity.RankingScore.ToString();
            RefreshList();
        }

        private void RefreshList()
        {
            // 获取里程节点
            var rewardDataList = commitRewardList;
            if (rewardDataList == null || rewardDataList.Count == 0)
            {
                DebugEx.Info("_currentRewardData is null or empty");
                return;
            }
            _currentRewardList.Clear();
            for (int i = 0; i < rewardDataList.Count; i++)
            {
                var rewardItem = rewardDataList[i];
                var cell = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Ranking_REWARD_CELL, cellRoot.transform);
                cell.transform.localPosition = Vector3.zero;
                cell.transform.localScale = Vector3.one;
                cell.SetActive(true);
                var item = cell.GetComponent<UICommonItem>();
                item.Refresh(rewardItem.rewardId, rewardItem.rewardCount);
                cellList.Add(cell);
                var rewardItemCell = new RewardItemCell();
                rewardItemCell.Init(cell);
                rewardItemCell.SetData(rewardItem);
                _currentRewardList.Add(rewardItemCell);
            }
        }


        private void OnCloseBtnClick()
        {
            Close();
            foreach (var item in _currentRewardList)
            {
                UIFlyUtility.FlyReward(item.data, item.cell.transform.position);
            }
        }
    }
    
    public class RewardItemCell
    {
        public GameObject cell;
        public UICommonItem item;
        public RewardCommitData data;

        public void Init(GameObject cell_)
        {
            cell = cell_;
            item = cell.GetComponent<UICommonItem>();
        }

        public void SetData(RewardCommitData data_)
        {
            data = data_;
        }

    }

}




