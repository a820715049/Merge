// ==================================================
// // File: UIBPScrollTip.cs
// // Author: liyueran
// // Date: 2025-06-19 15:06:19
// // Desc: BP 通用的滚动Tip
// // ==================================================

using System.Collections.Generic;
using Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPRewardTip : UITipsBase
    {
        public UICommonItem commonItem;

        private Transform content;
        private GridLayoutGroup gridLayoutGroup;
        private VerticalLayoutGroup verticalLayoutGroup;

        List<RewardConfig> rewardList = new(); // <id>
        private string rewardItemKey = "bp_reward_item_tip";
        List<UICommonItem> commonItemList = new();
        private bool _showNum = true;

        private BPActivity _activity;

        protected override void OnCreate()
        {
            transform.Access("Panel/Content", out content);
            transform.Access("Panel", out verticalLayoutGroup);
            transform.Access("Panel/Content", out gridLayoutGroup);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2)
            {
                return;
            }

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);


            _activity = (BPActivity)items[2];
            rewardList = (List<RewardConfig>)items[3];
            _showNum = (bool)items[4];
        }

        protected override void OnPreOpen()
        {
            PreparePool();

            gridLayoutGroup.constraintCount = rewardList.Count < 6 ? rewardList.Count : 6;

            foreach (var reward in rewardList)
            {
                var id = reward.Id;
                var count = reward.Count;

                GameObjectPoolManager.Instance.CreateObject(rewardItemKey, content, obj =>
                {
                    obj.SetActive(true);
                    var item = obj.GetComponent<UICommonItem>();
                    item.Setup();
                    item.Refresh(id, count);

                    // 是否显示数量
                    item.transform.Access<TextMeshProUGUI>("Count", out var num);
                    num.gameObject.SetActive(_showNum);

                    commonItemList.Add(item);
                });
            }


            var row = rewardList.Count / gridLayoutGroup.constraintCount;

            var width = verticalLayoutGroup.padding.left + verticalLayoutGroup.padding.right +
                        gridLayoutGroup.cellSize.x * gridLayoutGroup.constraintCount +
                        gridLayoutGroup.spacing.x * (gridLayoutGroup.constraintCount - 1);

            _SetCurTipsWidth(width);


            var height = verticalLayoutGroup.padding.top + verticalLayoutGroup.padding.bottom +
                         gridLayoutGroup.cellSize.y * row + gridLayoutGroup.spacing.y * (row - 1);

            _SetCurTipsHeight(height);
            // 刷新tips位置
            _RefreshTipsPos(18);
        }

        private void PreparePool()
        {
            if (GameObjectPoolManager.Instance.HasPool(rewardItemKey))
            {
                return;
            }

            GameObjectPoolManager.Instance.PreparePool(rewardItemKey, commonItem.gameObject);
        }

        protected override void OnPostClose()
        {
            foreach (var item in commonItemList)
            {
                GameObjectPoolManager.Instance.ReleaseObject(rewardItemKey, item.gameObject);
            }

            commonItemList.Clear();
        }
    }
}