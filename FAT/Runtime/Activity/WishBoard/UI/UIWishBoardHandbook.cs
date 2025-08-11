/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 11:40:05
 */
using EL;
using FAT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWishBoardHandbook : UIBase
{
    [SerializeField] private UIVisualGroup visualGroup;
    [SerializeField] private Button btnClose;
    [SerializeField] private Image lineImage;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private GameObject itemPrefab;
    private PoolItemType itemType = PoolItemType.WISH_BOARD_GALLERY_ITEM;
    private WishBoardActivity actInst;
    private Color previewColor = new Color(1, 1, 1, 0.5f);

#if UNITY_EDITOR
    private void OnValidate()
    {
        var root = transform.Find("Content/Panel");
        visualGroup.Prepare(root.Access<UIImageRes>("Bg"), "bgImage");
        visualGroup.Prepare(root.Access<UIImageRes>("Bg/Head"), "headImage");
        visualGroup.Prepare(root.Access<TextMeshProUGUI>("Bg/Title"), "mainTitle");
        visualGroup.Prepare(root.Access<TextMeshProUGUI>("Desc"), "desc");
        visualGroup.CollectTrim();
    }
#endif

    protected override void OnCreate()
    {
        transform.Access<Button>("Mask").onClick.AddListener(Close);
        btnClose.onClick.AddListener(Close);

        GameObjectPoolManager.Instance.PreparePool(itemType, itemPrefab);
        GameObjectPoolManager.Instance.ReleaseObject(itemType, itemPrefab);
    }

    protected override void OnParse(params object[] items)
    {
        actInst = items[0] as WishBoardActivity;
    }

    protected override void OnPreOpen()
    {
        RefreshTheme();
        ShowItems();
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
        var visual = actInst.VisualUIHandbook.visual;
        visual.Refresh(visualGroup);
        if (visual.StyleMap.TryGetValue("lineColor", out var colStr))
        {
            if (!colStr.StartsWith('#'))
                colStr = $"#{colStr}";
            lineImage.color = ColorUtility.TryParseHtmlString(colStr, out var col) ? col : Color.white;
        }
    }

    private void ShowItems()
    {
        var allUnlocked = true;
        var items = actInst.GetAllItemIdList();
        for (var i = 0; i < items.Count; i++)
        {
            var itemId = items[i];
            var item = GameObjectPoolManager.Instance.CreateObject(itemType, itemRoot);
            allUnlocked = RefreshItem(item.transform, itemId, i == items.Count - 1) && allUnlocked;
        }
    }

    // 最后一个item特殊处理 需要设置为已可预览
    private bool RefreshItem(Transform trans, int itemId, bool isLast)
    {
        var visual = actInst.VisualUIHandbook.visual;
        var bg = trans.Access<UIImageRes>("Bg");
        var arrow = trans.Access<UIImageRes>("Arrow");
        var unknown = trans.Access<UIImageRes>("Unknown");
        var icon = trans.Access<UIImageRes>("Icon");

        visual.Refresh(bg, "itemBgImage");
        visual.Refresh(arrow, "itemArrowImage");
        visual.Refresh(unknown, "itemMarkImage");

        var unlocked = actInst.IsItemUnlock(itemId);
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