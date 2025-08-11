/*
 * @Author: qun.chao
 * @Date: 2025-03-03 10:39:53
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using Config;

namespace FAT
{
    public class UIBingoHelp : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private UICommonItem rewardStraight;
        [SerializeField] private UICommonItem rewardSlash;
        [SerializeField] private UICommonItem rewardAll;
        [SerializeField] private Transform totalRewardRoot;
        private ActivityBingo actInst;

        protected override void OnCreate()
        {
            transform.Access<Button>("Mask").onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnConfirm.onClick.AddListener(Close);
        }

        protected override void OnParse(params object[] items)
        {
            actInst = items[0] as ActivityBingo;
        }

        protected override void OnPreOpen()
        {
            Refresh();
        }

        private void Refresh()
        {
            var (straight, slash, all) = BingoUtility.GetBoardRewardInfo(actInst.ConfBoardID);
            rewardStraight.Refresh(straight);
            rewardSlash.Refresh(slash);
            rewardAll.Refresh(all);

            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<(int, int)>>(out var mergedReward);
            var colNum = actInst.BoardColNum;
            var rowNum = actInst.BoardRowNum;
            UpdateItemNum(mergedReward, straight.Id, straight.Count * (colNum + rowNum));
            UpdateItemNum(mergedReward, slash.Id, slash.Count * 2);
            UpdateItemNum(mergedReward, all.Id, all.Count);

            for (var i = 0; i < totalRewardRoot.childCount; i++)
            {
                var child = totalRewardRoot.GetChild(i);
                if (i < mergedReward.Count)
                {
                    child.gameObject.SetActive(true);
                    child.GetComponent<UICommonItem>().Refresh(mergedReward[i].Item1, mergedReward[i].Item2);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateItemNum(List<(int, int)> list, int key, int value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Item1 == key)
                {
                    list[i] = (key, list[i].Item2 + value);
                    return;
                }
            }
            list.Add((key, value));
        }
    }
}