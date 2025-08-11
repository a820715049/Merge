/*
 * @Author: qun.chao
 * @Date: 2024-07-06 13:58:15
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.Merge;

namespace FAT
{
    public class UISpecialBoxInfo : UIBase
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtLevel;
        [SerializeField] private TextMeshProUGUI txtTip;
        [SerializeField] private UIImageRes resIcon;
        [SerializeField] private GameObject levelMaxIcon;
        [SerializeField] private RectTransform itemRoot;
        [SerializeField] private GameObject goItem;
        [SerializeField] private Button btnClose;

        private PoolItemType itemType = PoolItemType.SPECIAL_BOX_ITEM;

        protected override void OnCreate()
        {
            transform.AddButton("Mask", base.Close);
            GameObjectPoolManager.Instance.PreparePool(itemType, goItem);
            btnClose.onClick.AddListener(base.Close);
        }

        protected override void OnParse(params object[] items)
        {
            var itemId = (int)items[0];
            ShowBoxInfo(itemId);
            ShowOutput(itemId);
        }

        protected override void OnPostClose()
        {
            Clear();
        }

        private void ShowBoxInfo(int itemId)
        {
            var cfg = Game.Manager.objectMan.GetBasicConfig(itemId);
            var cfgSpecialBox = Env.Instance.GetItemComConfig(itemId).specialBoxConfig;
            var level = ItemUtility.GetItemLevel(itemId);
            resIcon.SetImage(cfg.Icon);
            txtTitle.text = I18N.Text(cfg.Name);
            txtLevel.text = I18N.FormatText("#SysComDesc18", level);
            levelMaxIcon.SetActive(ItemUtility.IsItemMaxLevel(itemId));
            txtTip.text = I18N.FormatText("#SysComDesc426", cfgSpecialBox.LimitCount);
        }

        private void ShowOutput(int itemId)
        {
            var config = Env.Instance.GetItemComConfig(itemId).specialBoxConfig;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutput(config.ActDiffRange[0], config.ActDiffRange[1], list);
                itemRoot.anchoredPosition = Vector2.zero;
                UIUtility.CreateGenericPooItem(itemRoot, itemType, list);
            }
        }

        private void Clear()
        {
            UIUtility.ReleaseClearableItem(itemRoot, itemType);
        }
    }
}