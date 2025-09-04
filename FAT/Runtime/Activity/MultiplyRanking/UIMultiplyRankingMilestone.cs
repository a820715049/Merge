/*
 * @Author: yanfuxing
 * @Date: 2025-07-18 11:20:05
 */
using System.Collections.Generic;
using Config;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMultiplyRankingMilestone : UIBase
    {
        public struct Milestone
        {
            public int showNum;
            public bool isCur;
            public bool isDone;
            public bool isGoal;
            public RewardConfig[] reward;
        }

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text tip;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject cell;
        [SerializeField] private GameObject cellRoot;
        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform done;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem[] item;
        private ActivityMultiplierRanking activity;
        private List<GameObject> cellList = new();
        private List<int> listMilestoneWithoutLast = new();

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.WISH_BOARD_MILESTONE_CELL, cell);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                activity = (ActivityMultiplierRanking)items[0];
            }
        }

        protected override void OnPreOpen()
        {
            RefreshList();
            RefreshTheme();
            RefreshBigReward();
        }

        private void RefreshTheme()
        {
            var visual = activity.VisualUIRankingMilestone.visual;
            if (visual != null)
            {
                visual.Refresh(title, "mainTitle");
                visual.Refresh(bg, "bg");
                var id = activity.conf.Token;
                var str = UIUtility.FormatTMPString(id);
                tip.SetTextFormat(I18N.Text("#SysComDesc1457"), str);
            }
        }

        protected override void OnPostClose()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.WISH_BOARD_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }

        private void RefreshList()
        {
            listMilestoneWithoutLast.Clear();
            var listMilestone = activity.detail.MilestoneRewardGroup;
            //最后大奖单独显示 不参与滚动
            for (int i = listMilestone.Count - 2; i >= 0; i--)
            {
                listMilestoneWithoutLast.Add(listMilestone[i]);
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, (172 + 8) * listMilestoneWithoutLast.Count + 28);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            for (int i = 0; i < listMilestoneWithoutLast.Count; i++)
            {
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.WISH_BOARD_MILESTONE_CELL, cellRoot.transform);
                var c = fat.conf.MultiRankMilestoneVisitor.Get(listMilestoneWithoutLast[i]);
                var reward = new RewardConfig[c.MilestoneReward.Count];
                for (int j = 0; j < c.MilestoneReward.Count; j++)
                {
                    reward[j] = c.MilestoneReward[j].ConvertToRewardConfig();
                }
                cellSand.GetComponent<UIMultiplyRankingMilestoneItem>().UpdateContent(
                    new Milestone()
                    {
                        reward = reward,
                        showNum = listMilestoneWithoutLast.Count - i,
                        isCur = listMilestoneWithoutLast.Count - i - 1 == activity.GetCurProgressPhase(),
                        isDone = listMilestoneWithoutLast.Count - i - 1 < activity.GetCurProgressPhase(),
                        isGoal = listMilestoneWithoutLast.Count - i - 1 > activity.GetCurProgressPhase()
                    });
                cellList.Add(cellSand);
            }
            JumpToTargetposition(listMilestoneWithoutLast.Count);
        }

        private void RefreshBigReward()
        {
            var ListM = activity.detail.MilestoneRewardGroup;
            var id = ListM[ListM.Count - 1];
            var c = fat.conf.MultiRankMilestoneVisitor.Get(id);
            var isCur = ListM.Count - 1 == activity.GetCurProgressPhase();
            var isDone = ListM.Count - 1 < activity.GetCurProgressPhase();
            num.Select(isCur ? 1 : 0);
            num.Text = ListM.Count.ToString();
            cur.gameObject.SetActive(isCur);
            goal.gameObject.SetActive(!isCur);
            goalLock.gameObject.SetActive(!isCur && !isDone);
            done.gameObject.SetActive(isDone);
            bg1.gameObject.SetActive(!isCur);
            bg2.gameObject.SetActive(isCur);
            for (int i = 0; i < c.MilestoneReward.Count; i++)
            {
                item[i].Refresh(c.MilestoneReward[i].ConvertToRewardConfig());
                item[i].transform.Find("finish").gameObject.SetActive(isDone);
                item[i].transform.Find("count").gameObject.SetActive(!isDone);
            }
        }

        /// <summary>
        /// 跳转到目标位置(列表cell位置跳转)
        /// </summary>
        /// <param name="listCount">列表数量</param>
        private void JumpToTargetposition(int listCount)
        {
            var targetNodeIndex = activity.GetCurProgressPhase();
            if (targetNodeIndex != -1)
            {
                var targetPosition = (float)targetNodeIndex / (listCount - 1);
                targetPosition = Mathf.Clamp01(targetPosition);
                scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, targetPosition);
            }
        }
    }
}