/**
 * @Author: zhangpengjian
 * @Date: 2024/9/19 15:23:11
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/9/19 15:23:11
 * Description: 积分活动里程碑样式帮助界面
 */

using UnityEngine;
using EL;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace FAT
{
    public class UIScoreMilestone : UIBase
    {
        [SerializeField] private Button playBtn;
        [SerializeField] private Transform hint;
        [SerializeField] private Transform helpRoot;
        [SerializeField] private Button helpBtn;
        [SerializeField] private Button block;
        [SerializeField] private TextProOnACircle title;
        [SerializeField] private TMP_Text cd;
        [SerializeField] private TMP_Text tip;
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes cdBg;
        [SerializeField] private UIImageRes bgMask;
        [SerializeField] private UIImageRes titleBg;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private GameObject cell;
        [SerializeField] private GameObject cellRoot;
        [SerializeField] private Animator primeRewardNum;
        [SerializeField] private Animator primeRewardItem;
        [SerializeField] private Animation primeRewardAnim;

        [SerializeField] private Transform cur;
        [SerializeField] private Transform goal;
        [SerializeField] private Transform goalLock;
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;
        [SerializeField] private UITextState num;
        [SerializeField] private UICommonItem item;

        private int currentPrimeRewardNum = 0;
        private int itemHeight = 172;
        private List<ActivityScore.Node> nodes = new();
        private ActivityScore activity;
        private bool isPopup;
        private Action WhenCD;
        private List<GameObject> cellList = new();
        private Coroutine coroutine;
        private List<int> listPrime = new();
        private int spacing = 8;
        private int topAndBottome = 28;

        protected override void OnCreate()
        {
            transform.AddButton("Content/close", Close);
            helpBtn.onClick.AddListener(OnClickHelp);
            playBtn.onClick.AddListener(OnClickPlay);
            block.onClick.AddListener(OnClickBlock);
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.SCORE_MILESTONE_CELL, cell);
        }

        private void OnClickPlay()
        {
            Close();
            GameProcedure.SceneToMerge();
        }

        private void OnClickBlock()
        {
            helpRoot.gameObject.SetActive(false);
            block.gameObject.SetActive(false);
        }

        private void OnClickHelp()
        {
            helpRoot.gameObject.SetActive(true);
            block.gameObject.SetActive(true);
        }

        protected override void OnParse(params object[] items)
        {
            activity = (ActivityScore)items[0];
            isPopup = (bool)items[1];
        }

        protected override void OnPreOpen()
        {
            RefreshList();
            if (isPopup)
            {
                OnClickHelp();
                StartCoroutine(CoDelayHideHelp());
            }
            else
            {
                block.gameObject.SetActive(false);
                helpRoot.gameObject.SetActive(false);
            }
            RefreshTheme();
            RefreshBigReward();
            var isShowBtn = Game.Manager.mapSceneMan.scene.Active;
            playBtn.gameObject.SetActive(isShowBtn);
            hint.gameObject.SetActive(!isShowBtn);
            RefreshCD();
        }

        private IEnumerator CoDelayHideHelp()
        {
            yield return new WaitForSeconds(3f);
            OnClickBlock();
        }

        private void RefreshTheme()
        {
            activity.Visual.Refresh(cdBg, "cdBg");
            activity.Visual.Refresh(title, "mainTitle");
            activity.Visual.Refresh(bg, "bg");
            activity.Visual.Refresh(titleBg, "titleBg");
            activity.Visual.Refresh(bgMask, "bg");
            activity.Visual.Refresh(tip, "tip1");
            activity.Visual.Refresh(cd, "cdColor");
        }

        protected override void OnAddListener()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        private void RefreshCD()
        {
            var v = activity.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if (v <= 0)
                Close();
        }

        protected override void OnPostClose()
        {
            foreach (var item in cellList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.SCORE_MILESTONE_CELL, item);
            }
            cellList.Clear();
        }

        private void Update()
        {
            var list = listPrime;
            float contentY = scroll.content.anchoredPosition.y - spacing;
            int idx = Mathf.FloorToInt(contentY / (itemHeight + spacing));
            var curTopShowNum = Math.Abs(idx - (activity.ListM.Count - 1)) + 2;
            int newPrimeRewardNum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (curTopShowNum >= list[i])
                {
                    if (i + 1 < list.Count)
                    {
                        newPrimeRewardNum = list[i + 1];
                    }
                    else
                    {
                        newPrimeRewardNum = list[i];
                    }
                }
            }
            if (newPrimeRewardNum == 0)
            {
                newPrimeRewardNum = list[0];
            }
            if (newPrimeRewardNum != currentPrimeRewardNum && newPrimeRewardNum != 0 && currentPrimeRewardNum != 0)
            {
                currentPrimeRewardNum = newPrimeRewardNum;
                RefreshBigReward(newPrimeRewardNum - 1);
            }
        }

        private void RefreshList()
        {
            activity.FillMilestoneData();
            nodes.Clear();
            for (int i = activity.ListM.Count - 2; i >= 0; i--)
            {
                if (activity.ListM[i].showNum - 1 < activity.GetMilestoneIndex())
                    continue;
                nodes.Add(activity.ListM[i]);
            }
            listPrime.Clear();
            foreach (var item in activity.ConfDetail.RewardStepNum)
            {
                if (item > activity.GetMilestoneIndex() + 4 || (item > activity.GetMilestoneIndex() && nodes.Count < 4))
                {
                    listPrime.Add(item);
                }
            }
            if (nodes.Count < 4)
            {
                var needNum = 4 - nodes.Count;
                for (int i = 0; i < needNum; i++)
                {
                    nodes.Add(activity.ListM[activity.GetMilestoneIndex() - 1 - i]);
                }
            }
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, (itemHeight + spacing) * nodes.Count + topAndBottome);
            scroll.normalizedPosition = new Vector2(scroll.normalizedPosition.x, 0f);
            for (int i = 0; i < nodes.Count; i++)
            {
                var cellSand = GameObjectPoolManager.Instance.CreateObject(PoolItemType.SCORE_MILESTONE_CELL, cellRoot.transform);
                cellSand.GetComponent<UIMilestoneItem>().UpdateContent(nodes[i]);
                cellList.Add(cellSand);
            }
        }

        private void RefreshBigReward(int previewIndex = 0)
        {
            var curIndex = activity.GetMilestoneIndex();
            if (previewIndex > 0)
            {
                if (coroutine == null)
                {
                    primeRewardNum.SetTrigger("Punch");
                    primeRewardItem.SetTrigger("Punch");
                    primeRewardAnim.gameObject.SetActive(false);
                    primeRewardAnim.gameObject.SetActive(true);
                }
                curIndex = previewIndex;
            }
            var showPrimeRewardNum = 0;
            for (int i = 0; i < listPrime.Count; i++)
            {
                if (listPrime[i] >= curIndex + 1)
                {
                    showPrimeRewardNum = listPrime[i];
                    break;
                }
            }
            currentPrimeRewardNum = showPrimeRewardNum;
            if (showPrimeRewardNum > 0)
            {
                var node = activity.ListM[showPrimeRewardNum - 1];
                coroutine = StartCoroutine(CoRefreshItem(node));
            }

            var nodeLast = activity.ListM[activity.ListM.Count - 1];
            num.Select(nodeLast.isCur ? 1 : 0);
            cur.gameObject.SetActive(nodeLast.isCur);
            goal.gameObject.SetActive(!nodeLast.isCur);
            goalLock.gameObject.SetActive(!nodeLast.isCur);
            bg1.gameObject.SetActive(!nodeLast.isCur);
            bg2.gameObject.SetActive(nodeLast.isCur);
        }

        private IEnumerator CoRefreshItem(ActivityScore.Node node)
        {
            yield return new WaitForSeconds(0.1f);
            num.text.text = node.showNum.ToString();
            yield return new WaitForSeconds(0.13f);
            item.Refresh(node.reward, node.isCur ? 17 : 41);
            coroutine = null;
        }
    }
}