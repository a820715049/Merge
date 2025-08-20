/**
 * @Author: zhangpengjian
 * @Date: 2025/3/14 17:57:15
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/14 17:57:15
 * Description: 挖矿阶段进度
 */

using UnityEngine;
using EL;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Config;

namespace FAT
{
    public class UIMineBoardMilestone : UIBase
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
        [SerializeField] private Transform done;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem[] item;

        private MineBoardActivity activity;
        private List<GameObject> cellList = new();
        private List<int> listMilestoneWithoutLast = new();

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MINE_BOARD_MILESTONE_CELL, cell);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (MineBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshList();
            RefreshTheme();
            RefreshBigReward();
        }

        private void RefreshTheme()
        {
            activity.MilestoneTheme.Refresh(title, "mainTitle");
            activity.MilestoneTheme.Refresh(bg, "bg");
            activity.MilestoneTheme.Refresh(tip, "tip1");
        }

        protected override void OnAddListener()
        {
        }

        protected override void OnRemoveListener()
        {
        }

        protected override void OnPostClose()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MINE_BOARD_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }

        private void RefreshList()
        {
            listMilestoneWithoutLast.Clear();
            var listMilestone = activity.GetCurGroupConfig().MilestoneRewardId;
            //最后大奖单独显示 不参与滚动
            for (int i = listMilestone.Count - 2; i >= 0; i--)
            {
                listMilestoneWithoutLast.Add(listMilestone[i]);
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, (172 + 8) * listMilestoneWithoutLast.Count + 28);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            for (int i = 0; i < listMilestoneWithoutLast.Count; i++)
            {
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MINE_BOARD_MILESTONE_CELL, cellRoot.transform);
                var c = fat.conf.Data.GetEventMineReward(listMilestoneWithoutLast[i]);
                var reward = new RewardConfig[c.MilestoneReward.Count];
                for (int j = 0; j < c.MilestoneReward.Count; j++)
                {
                    reward[j] = c.MilestoneReward[j].ConvertToRewardConfig();
                }
                cellSand.GetComponent<UIMineBoardMilestoneItem>().UpdateContent(new Milestone() { reward = reward, showNum = listMilestoneWithoutLast.Count - i, isCur = listMilestoneWithoutLast.Count - i - 1 == activity.GetCurProgressPhase(), isDone = listMilestoneWithoutLast.Count - i - 1 < activity.GetCurProgressPhase(), isGoal = listMilestoneWithoutLast.Count - i - 1 > activity.GetCurProgressPhase() });
                cellList.Add(cellSand);
            }
        }

        private void RefreshBigReward()
        {
            var ListM = activity.GetCurGroupConfig().MilestoneRewardId;
            var id = ListM[ListM.Count - 1];
            var c = fat.conf.Data.GetEventMineReward(id);
            var reward = new RewardConfig[c.MilestoneReward.Count];
            for (int j = 0; j < c.MilestoneReward.Count; j++)
            {
                reward[j] = c.MilestoneReward[j].ConvertToRewardConfig();
            }
            var nodeLast = new Milestone() { reward = reward, showNum = ListM.Count, isCur = ListM.Count - 1 == activity.GetCurProgressPhase(), isDone = ListM.Count - 1 < activity.GetCurProgressPhase() };
            num.Select(nodeLast.isCur ? 1 : 0);
            num.Text = nodeLast.showNum.ToString();
            cur.gameObject.SetActive(nodeLast.isCur);
            goal.gameObject.SetActive(!nodeLast.isCur);
            done.gameObject.SetActive(nodeLast.isDone);
            goalLock.gameObject.SetActive(!nodeLast.isCur && !nodeLast.isDone);
            bg1.gameObject.SetActive(!nodeLast.isCur);
            bg2.gameObject.SetActive(nodeLast.isCur);
            for (int i = 0; i < nodeLast.reward.Length; i++)
            {
                item[i].Refresh(nodeLast.reward[i], nodeLast.isCur ? 17 : 68);
                item[i].transform.Find("finish").gameObject.SetActive(nodeLast.isDone);
                item[i].transform.Find("count").gameObject.SetActive(!nodeLast.isDone);
            }
        }
    }
}