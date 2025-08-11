/*
 * @Author: qun.chao
 * @Date: 2025-01-10 12:28:06
 */
using UnityEngine;
using UnityEngine.UI;
using FAT.Merge;

namespace FAT
{
    public class UIMixSourceTips : UITipsBase
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Button btnInfo;
        [SerializeField] private float widthA = 366f;
        [SerializeField] private float widthB = 542f;
        private Item _mixSourceItem;

        protected override void OnCreate()
        {
            btnInfo.onClick.AddListener(OnClickInfo);
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var idx = i;
                var item = itemRoot.GetChild(idx);
                item.GetComponent<Button>().onClick.AddListener(() => OnClickItem(idx));
            }
        }

        protected override void OnParse(params object[] items)
        {
            _SetTipsPosInfo(items);
            _mixSourceItem = items[2] as Item;
        }

        protected override void OnPreOpen()
        {
            Refresh();
            _RefreshTipsPos(10f);
        }

        private void Refresh()
        {
            _mixSourceItem.TryGetItemComponent<ItemMixSourceComponent>(out var com);
            var max = com.totalMixRequire;
            if (max > 2)
            {
                panel.sizeDelta = new Vector2(widthB, panel.sizeDelta.y);
            }
            else
            {
                panel.sizeDelta = new Vector2(widthA, panel.sizeDelta.y);
            }
            var mixedItems = com.mixedItems;
            for (var i = 0; i < itemRoot.childCount; i++)
            {
                var id = i < mixedItems.Count ? mixedItems[i] : 0;
                ShowItem(i, id, i < max);
            }
        }

        private void ShowItem(int idx, int id, bool show)
        {
            var item = itemRoot.GetChild(idx);
            if (!show)
            {
                item.gameObject.SetActive(false);
                return;
            }
            item.gameObject.SetActive(true);
            var icon = item.Access<UIImageRes>("Icon");
            icon.gameObject.SetActive(id > 0);
            if (id > 0)
            {
                var cfg = Env.Instance.GetItemConfig(id);
                icon.SetImage(cfg.Icon);
            }
        }

        private void OnClickItem(int idx)
        {
            _mixSourceItem.TryGetItemComponent<ItemMixSourceComponent>(out var com);
            var mixedItems = com.mixedItems;
            if (idx < mixedItems.Count)
            {
                var board = Game.Manager.mergeBoardMan.activeWorld.activeBoard;
                var item = board.MixSourceExtract(_mixSourceItem, mixedItems[idx], out _);
                if (item != null)
                {
                    Refresh();
                }
            }
        }

        private void OnClickInfo()
        {
            UIConfig.UIMixSourceDetail.Open(_mixSourceItem);
        }
    }
}
