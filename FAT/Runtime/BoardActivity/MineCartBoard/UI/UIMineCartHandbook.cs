using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using Cysharp.Text;

namespace FAT
{
    public class UIMineCartHandbook : UIBase
    {
        [SerializeField] private UIImageRes bg;
        [SerializeField] private UIImageRes head;
        [SerializeField] private Image lineImage;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subTitle;
        private PoolItemType itemType = PoolItemType.MINE_CART_BOARD_GALLERY_ITEM;
        private MineCartActivity _activity;
        private Color previewColor = new Color(1, 1, 1, 0.5f);

        protected override void OnCreate()
        {
            //transform.AddButton("Mask", Close);
            transform.AddButton("Content/Panel/Bg/CloseBtn", Close);
            GameObjectPoolManager.Instance.PreparePool(itemType, itemPrefab);
            GameObjectPoolManager.Instance.ReleaseObject(itemType, itemPrefab);
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as MineCartActivity;
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            ShowItems();
            _activity.NeedShowHandbookRedDot = false;
        }

        protected override void OnPostClose()
        {
            Clear();
        }

        private void Clear()
        {
            UIUtility.ReleaseChildren(itemRoot, itemType);
        }

        private void RefreshTheme()
        {
            var visualRes = _activity.VisualHandbook;
            visualRes.visual.Refresh(bg, "bg");
            visualRes.visual.Refresh(head, "head");
            visualRes.visual.Refresh(title, "mainTitle");

            if (visualRes.visual.StyleMap.TryGetValue("lineColor", out var colStr))
            {
                if (!colStr.StartsWith('#'))
                    colStr = $"#{colStr}";
                lineImage.color = ColorUtility.TryParseHtmlString(colStr, out var col) ? col : Color.white;
            }
        }

        private void ShowItems()
        {
            var allUnlocked = true;
            var items = _activity.GetAllItemIdList();
            for (var i = 0; i < items.Count; i++)
            {
                var itemId = items[i];
                var item = GameObjectPoolManager.Instance.CreateObject(itemType, itemRoot);
                allUnlocked = RefreshItem(item.transform, itemId, i == items.Count - 1) && allUnlocked;
            }
            var visualRes = _activity.VisualHandbook;
            if (allUnlocked)
            {
                visualRes.visual.Refresh(subTitle, "desc2");
            }
            else
            {
                visualRes.visual.Refresh(subTitle, "desc1");
            }
        }

        // 最后一个item特殊处理 需要设置为已可预览
        private bool RefreshItem(Transform trans, int itemId, bool isLast)
        {
            var visualRes = _activity.VisualHandbook;
            var bg = trans.Access<UIImageRes>("Bg");
            var arrow = trans.Access<UIImageRes>("Arrow");
            var unknown = trans.Access<UIImageRes>("Unknown");
            var icon = trans.Access<UIImageRes>("Icon");

            visualRes.visual.Refresh(bg, "itemBgImage");
            visualRes.visual.Refresh(arrow, "itemArrowImage");
            visualRes.visual.Refresh(unknown, "itemMarkImage");

            var unlocked = _activity.IsItemUnlock(itemId);
            icon.gameObject.SetActive(unlocked || isLast);
            unknown.gameObject.SetActive(!unlocked && !isLast);
            arrow.gameObject.SetActive(!isLast);
            if (unlocked || isLast)
            {
                var cfg = Game.Manager.objectMan.GetBasicConfig(itemId);
                icon.SetImage(cfg.Icon);
                icon.image.color = isLast && !unlocked ? previewColor : Color.white;
            }
            return unlocked;
        }
    }
}
