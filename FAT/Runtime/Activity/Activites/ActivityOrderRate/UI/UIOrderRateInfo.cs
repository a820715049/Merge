
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using FAT.Merge;
using System.Linq;

namespace FAT
{
    public class UIOrderRateInfo : UIBase
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private UIImageRes resIcon;
        [SerializeField] private RectTransform itemRoot;
        [SerializeField] private GameObject goItem;
        [SerializeField] private Button btnClose;
        private int order;


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
            if (items.Length > 1)
                order = (int)items[1];
            ShowBoxInfo(itemId);
            ShowOutput(itemId, order);
        }

        protected override void OnPostClose()
        {
            Clear();
        }

        private void ShowBoxInfo(int itemId)
        {
            var cfg = Game.Manager.configMan.GetEventOrderRateBoxConfig(itemId);
            resIcon.SetImage(cfg.OrderInfo);
            txtTitle.text = I18N.Text(cfg.OrderInfoKey);
        }

        private void ShowOutput(int itemId, int order)
        {
            var conf = Game.Manager.configMan.GetEventOrderRateRandomConfig();
            var confDetail = conf.FirstOrDefault(x => x.RandomerId == order && x.BoxInfo == itemId);
            if (confDetail == null)
            {
                return;
            }
            var diffyMin = confDetail.DiffLeft;
            var diffyMax = confDetail.DiffRight;
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                Game.Manager.mergeItemDifficultyMan.CalcSpecialBoxOutput(diffyMin, diffyMax, list);
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