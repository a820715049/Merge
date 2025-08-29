/*
 * @Author: yanfuxing
 * @Date: 2025-04-22 11:50:20
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static FAT.ActivityRanking;

namespace FAT
{
    public class UIActivityRankMilestone : UIBase
    {
        [SerializeField] private Button CloseBtn;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private GameObject cellItem;
        [SerializeField] private GameObject cellRoot;
        [SerializeField] private Transform curProCircleBg;
        [SerializeField] private Transform goalProCircleBg;
        [SerializeField] private Transform FinishProCircleBg;
        [SerializeField] private Transform goalLockCircleImg;
        [SerializeField] private UITextState bigRewardShowNum; //大奖里程碑Num
        [SerializeField] private Transform rightFinishBg; //当前已完成的里程碑Bg
        [SerializeField] private Transform rightCurProBg; //当前正在进行的里程碑Bg
        [SerializeField] private UICommonItem bigRewardItem; //大奖里程碑奖励
        [SerializeField] private Transform bigRewardCompleteBg;   //完成状态
        private List<NodeItem> nodes = new(); //里程节点List
        private ActivityRanking activity;
        private List<int> listPrime = new(); //阶段性奖励list
        private List<GameObject> cellList = new(); //cell列表

        protected override void OnCreate()
        {
            base.OnCreate();
            CloseBtn.onClick.AddListener(OnCloseBtnClick);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.Ranking_MILESTONE_CELL, cellItem); 
        }



        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0)
            {
                activity = (ActivityRanking)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            RefreshList();
            RefreshBigReward();
        }

        protected override void OnPostClose()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.Ranking_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }


        #region 事件

        private void OnCloseBtnClick()
        {
            Close();
        }

        #endregion

        #region 方法

        #region 刷新列表   
        private void RefreshList()
        {
            if (activity == null) return;
            activity.FillMilestoneData();
            for (int i = activity.MilestoneNodeList.Count - 2; i >= 0; i--)
            {
                var item = GameObjectPoolManager.Instance.CreateObject(PoolItemType.Ranking_MILESTONE_CELL, cellRoot.transform);
                item.transform.SetParent(cellRoot.transform);
                item.transform.localScale = Vector3.one;
                item.transform.localPosition = Vector3.zero;
                item.GetComponent<RankingMilestoneItem>().OnUpdateMilestoneItem(activity.MilestoneNodeList[i]);
                cellList.Add(item);
            }
            JumpToTargetposition(cellList.Count);
        }
        #endregion

        #region 大奖刷新
        private void RefreshBigReward()
        {
            var nodeLast = activity.MilestoneNodeList[activity.MilestoneNodeList.Count - 1];
            curProCircleBg.gameObject.SetActive(nodeLast.IsCurPro && !nodeLast.IsDonePro);
            goalProCircleBg.gameObject.SetActive(nodeLast.IsGoalPro);
            FinishProCircleBg.gameObject.SetActive(nodeLast.IsDonePro);
            goalLockCircleImg.gameObject.SetActive(nodeLast.IsGoalPro);
            rightFinishBg.gameObject.SetActive(!nodeLast.IsCurPro);
            rightCurProBg.gameObject.SetActive(nodeLast.IsCurPro);  
            bigRewardItem.Refresh(nodeLast.Reward, nodeLast.IsCurPro ? 17 : 9);
            bigRewardShowNum.text.text = nodeLast.showNum.ToString();
            bigRewardShowNum.Select(GetTextStateIndex(nodeLast));
            bigRewardCompleteBg.gameObject.SetActive(nodeLast.IsDonePro);
        }
        #endregion

        #region 列表cell位置跳转
        private void JumpToTargetposition(int rewardItemCount)
        {
            var currentIndex = activity.CurMilestoneIndex;
            var targetNodeIndex = activity.MilestoneNodeList.FindIndex(node => node.showNum - 1 == currentIndex);
            if (targetNodeIndex != -1)
            {
                var targetPosition = (float)targetNodeIndex / (rewardItemCount - 1);
                targetPosition = Mathf.Clamp01(targetPosition);
                scrollView.normalizedPosition = new Vector2(scrollView.normalizedPosition.x, targetPosition);
            }
        }
        #endregion

        #region 获取textState索引
        private int GetTextStateIndex(NodeItem itemData)
        {
            var isCurPro = itemData.IsCurPro;
            var isGoalPro = itemData.IsGoalPro;
            var isDonePro = itemData.IsDonePro;
            if (isCurPro)
            {
                return 1;
            }
            else if (isGoalPro)
            {
                return 2;
            }
            else
            {
                return 0;
            }
        }

        #endregion





        #endregion


    }
}
