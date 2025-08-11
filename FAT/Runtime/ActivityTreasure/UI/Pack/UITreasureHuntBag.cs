/*
 * @Author: qun.chao
 * @Date: 2024-04-19 15:40:24
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UITreasureHuntBag : UIBase
    {
        [SerializeField] private GameObject goItem;
        [SerializeField] private GameObject goScrollView;
        [SerializeField] private RectTransform defaultRoot;
        [SerializeField] private RectTransform scrollRoot;

        private PoolItemType itemType = PoolItemType.EVENT_TREASURE_PACK_ITEM;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);

            GameObjectPoolManager.Instance.PreparePool(itemType, goItem);
        }

        protected override void OnPreOpen()
        {
            _Show();
        }

        protected override void OnPostClose()
        {
            _Clear();
        }

        private void _Show()
        {
            if (!UITreasureHuntUtility.TryGetEventInst(out var act)) return;
            var items = act.GetTempBagReward();
            Transform itemRoot;
            if (items.Count > 6 * 4)
            {
                defaultRoot.gameObject.SetActive(false);
                goScrollView.SetActive(true);
                scrollRoot.anchoredPosition = Vector2.zero;
                itemRoot = scrollRoot;
            }
            else
            {
                defaultRoot.gameObject.SetActive(true);
                goScrollView.SetActive(false);
                itemRoot = defaultRoot;
                var layout = itemRoot.GetComponent<GridLayoutGroup>();
                // 一行最多4个
                if (items.Count <= 4)
                {
                    layout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    layout.constraintCount = 1;
                }
                else
                {
                    layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    layout.constraintCount = 4;
                }
            }
            UIUtility.CreateGenericPooItem(itemRoot, itemType, items);
        }

        private void _Clear()
        {
            UIUtility.ReleaseClearableItem(defaultRoot, itemType);
            UIUtility.ReleaseClearableItem(scrollRoot, itemType);
        }
    }
}