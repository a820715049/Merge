/*
 * @Author: qun.chao
 * @Date: 2025-03-05 10:04:28
 */
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;
using fat.rawdata;
using EL;

namespace FAT
{
    public class UIBingoItem : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnCommit;
        [SerializeField] private Button btnNotReady;
        [SerializeField] private UICommonItem targetItem;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private GridLayoutGroup rewardRoot;
        [SerializeField] private float heightOneLine;
        [SerializeField] private float heightTwoLine;

        private ActivityBingo actInst;
        private BingoItem bingoItem;
        private Action onCommit;

        protected override void OnCreate()
        {
            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnCommit.onClick.AddListener(OnBtnCommit);
            btnNotReady.onClick.AddListener(OnBtnNotReady);
        }

        protected override void OnParse(params object[] items)
        {
            actInst = items[0] as ActivityBingo;
            bingoItem = items[1] as BingoItem;
            onCommit = items[2] as Action;
        }

        protected override void OnPreOpen()
        {
            Refresh();
        }

        protected override void OnPostClose()
        {
            actInst = null;
            bingoItem = null;
            onCommit = null;
        }

        private void Refresh()
        {
            targetItem.Refresh(bingoItem.ItemId, 1);
            targetItem.ExtendTipsForMergeItem(bingoItem.ItemId);
            var canCommit = CanCommit();
            targetItem.transform.Find("Check").gameObject.SetActive(canCommit);
            btnCommit.gameObject.SetActive(canCommit);
            btnNotReady.gameObject.SetActive(!canCommit);

            // 显示奖励
            actInst.PreviewCompleteBingo(bingoItem, out var rewards);
            rewards.Reverse();

            for (var i = 0; i < rewardRoot.transform.childCount; i++)
            {
                var child = rewardRoot.transform.GetChild(i);
                if (i < rewards.Count)
                {
                    child.gameObject.SetActive(true);
                    child.GetComponent<UICommonItem>().Refresh(rewards[i].Item1, rewards[i].Item2);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }

            // 动态调整UI尺寸
            var rectTrans = panelRoot;
            if (rewards.Count > 3)
            {
                // 显示2行
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, heightTwoLine);
            }
            else
            {
                // 显示1行
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, heightOneLine);
            }
        }

        private bool CanCommit()
        {
            return BingoUtility.HasActiveItemInMainBoardAndInventory(bingoItem.ItemId);
        }

        private void OnBtnCommit()
        {
            Item itemInInventory = null;
            void WalkItem(Item item)
            {
                if (item.tid == bingoItem.ItemId) itemInInventory = item;
            }

            if (CanCommit() && !BingoUtility.HasActiveItemInMainBoard(bingoItem.ItemId))
            {
                // 在背包里 需要弹窗确认是否扣除
                var world = Game.Manager.mainMergeMan.world;
                world.inventory.WalkAllItem(WalkItem);
                if (itemInInventory == null) return;

                var temp = PoolMapping.PoolMappingAccess.Take<List<Item>>(out var confirmList);
                confirmList.Add(itemInInventory);

                // 使用正式的弹窗 | UI关闭时释放temp
                UIManager.Instance.OpenWindow(UIConfig.UICompleteOrderBag, confirmList, new Action(() =>
                {
                    temp.Free();
                    ResolveCommit();
                }));
            }
            else
            {
                ResolveCommit();
            }
        }

        private void ResolveCommit()
        {
            onCommit?.Invoke();
            Close();
        }

        private void OnBtnNotReady()
        {
            Game.Manager.commonTipsMan.ShowPopTips(Toast.ItemBingoNoItem);
        }
    }
}