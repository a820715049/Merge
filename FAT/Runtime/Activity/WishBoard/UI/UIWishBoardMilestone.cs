/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 11:20:05
 */
using System.Collections.Generic;
using Config;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FAT
{
    public class UIWishBoardMilestone : UIBase
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
        private WishBoardActivity activity;
        private List<GameObject> cellList = new();
        private List<int> listMilestoneWithoutLast = new();
        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.WISH_BOARD_MILESTONE_CELL, cell);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (WishBoardActivity)items[0];
        }

        protected override void OnPreOpen()
        {
            RefreshList();
            RefreshTheme();
            RefreshBigReward();
        }

        private void RefreshTheme()
        {
            var visual = activity.VisualUIMilestone.visual;
            if (visual != null)
            {
                visual.Refresh(title, "mainTitle");
                visual.Refresh(bg, "bg");
                visual.Refresh(tip, "tip1");
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
            var listMilestone = activity.GetCurGroupConfig().BarRewardId;
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

                var c = Game.Manager.configMan.GetCurWishBarRewardById(listMilestoneWithoutLast[i]);
                var reward = new RewardConfig[c.BarReward.Count];
                for (int j = 0; j < c.BarReward.Count; j++)
                {
                    reward[j] = c.BarReward[j].ConvertToRewardConfig();
                }
                cellSand.GetComponent<UIWishBoardMilestoneItem>().UpdateContent(
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
            var ListM = activity.GetCurGroupConfig().BarRewardId;
            var id = ListM[ListM.Count - 1];
            var c = Game.Manager.configMan.GetCurWishBarRewardById(id);
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
            for (int i = 0; i < c.BarReward.Count; i++)
            {
                item[i].Refresh(c.BarReward[i].ConvertToRewardConfig(), isCur ? 17 : 68);
                item[i].transform.Find("finish").gameObject.SetActive(isDone);
                item[i].transform.Find("count").gameObject.SetActive(!isDone);
                if (i == c.BarReward.Count - 1)
                {
                    RefreshlastItem(item[i]);
                }
            }
        }

        /// <summary>
        /// 刷新最后一个物品(跟策划约定最后一个物品为循环宝箱：做特殊Tips处理)
        /// </summary>
        /// <param name="item">最后一个奖励物品</param>
        private void RefreshlastItem(UICommonItem item)
        {
            var btn = item.transform.Find("icon").GetComponent<Button>();
            if (btn != null)
            {
                //移除旧的按钮监听
                btn.onClick.RemoveAllListeners();
                //开启射线检测
                btn.interactable = true;
                btn.GetComponent<Image>().enabled = true;
                btn.GetComponent<Image>().raycastTarget = true;
                btn.transform.Find("info").gameObject.SetActive(true);
                //判断是否已经添加过，没有添加就再进行添加
                if (btn.onClick.GetPersistentEventCount() == 0)
                {
                    btn.onClick.AddListener(() =>
                    {
                        var itemRect = item.transform as RectTransform;
                        var itemHeight = itemRect.rect.height * 0.5f;
                        UIManager.Instance.OpenWindow(UIConfig.UIWishBoardMilestoneTips, item.transform.position, itemHeight);
                    });
                }
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