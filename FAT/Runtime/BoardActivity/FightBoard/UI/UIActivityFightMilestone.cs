/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:11:31
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:11:31
 * Description: 打怪棋盘阶段进度
 */

using UnityEngine;
using EL;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Config;
using fat.rawdata;

namespace FAT
{
    public class UIActivityFightMilestone : UIBase
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
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem[] item;

        private FightBoardActivity activity;
        private List<GameObject> cellList = new();
        private List<EventFightLevel> listMilestoneWithoutLast = new();

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MINE_BOARD_MILESTONE_CELL, cell);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (FightBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshList();
            RefreshTheme();
            RefreshBigReward();
        }

        private void RefreshTheme()
        {
            activity.MilestoneRes.visual.Refresh(title, "mainTitle");
            activity.MilestoneRes.visual.Refresh(bg, "bg");
            activity.MilestoneRes.visual.Refresh(tip, "tip1");
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
            var listMilestone = activity.GetFightLevels();
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
                var c = listMilestoneWithoutLast[i].LevelReward;
                var reward = new RewardConfig[c.Count];
                for (int j = 0; j < c.Count; j++)
                {
                    reward[j] = c[j].ConvertToRewardConfig();
                }
                cellSand.GetComponent<UIActivityFightMilestoneCell>().UpdateContent(new Milestone() { reward = reward, showNum = listMilestoneWithoutLast.Count - i, isCur = listMilestoneWithoutLast.Count - i - 1 == activity.GetCurrentMilestoneIndex(), isDone = listMilestoneWithoutLast.Count - i - 1 < activity.GetCurrentMilestoneIndex(), isGoal = listMilestoneWithoutLast.Count - i - 1 > activity.GetCurrentMilestoneIndex() });
                cellList.Add(cellSand);
            }
        }

        private void RefreshBigReward()
        {
            var ListM = activity.GetFightLevels();
            var id = ListM[ListM.Count - 1];
            var c = id.LevelReward;
            var reward = new RewardConfig[c.Count];
            for (int j = 0; j < c.Count; j++)
            {
                reward[j] = c[j].ConvertToRewardConfig();
            }
            var nodeLast = new Milestone() { reward = reward, showNum = ListM.Count, isCur = ListM.Count - 1 == activity.GetCurrentMilestoneIndex(), isDone = ListM.Count - 1 < activity.GetCurrentMilestoneIndex() };
            num.Select(nodeLast.isCur ? 1 : 0);
            num.Text = nodeLast.showNum.ToString();
            cur.gameObject.SetActive(nodeLast.isCur);
            goal.gameObject.SetActive(!nodeLast.isCur);
            goalLock.gameObject.SetActive(!nodeLast.isCur);
            bg1.gameObject.SetActive(!nodeLast.isCur);
            bg2.gameObject.SetActive(nodeLast.isCur);
            for (int i = 0; i < nodeLast.reward.Length; i++)
            {
                if (nodeLast.reward[i] != null)
                {
                    item[i].Refresh(nodeLast.reward[i], nodeLast.isCur ? 17 : 68);
                    item[i].transform.Find("finish").gameObject.SetActive(nodeLast.isDone);
                    item[i].transform.Find("count").gameObject.SetActive(!nodeLast.isDone);
                }
                else
                {
                    item[i].gameObject.SetActive(false);
                }
            }
        }
    }
}