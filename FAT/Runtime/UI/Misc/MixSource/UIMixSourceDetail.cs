/*
 * @Author: qun.chao
 * @Date: 2025-01-10 18:21:17
 */
using EL;
using FAT.Merge;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using fat.rawdata;
using System.Security.Cryptography.X509Certificates;

namespace FAT
{
    public class UIMixSourceDetail : UIBase
    {
        [SerializeField] private Button btnClose;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private GameObject itemPrefab;
        private Item _mixSourceItem;
        private PoolItemType poolItemType = PoolItemType.MIX_COST_ITEM;

        protected override void OnCreate()
        {
            btnClose.onClick.AddListener(Close);
            transform.AddButton("Mask", Close);
            GameObjectPoolManager.Instance.PreparePool(poolItemType, itemPrefab);
            GameObjectPoolManager.Instance.ReleaseObject(poolItemType, itemPrefab);
        }

        protected override void OnParse(params object[] items)
        {
            _mixSourceItem = items[0] as Item;
        }

        protected override void OnPreOpen()
        {
            UIConfig.UIMixSourceTips.Close();
            Refresh();
        }

        protected override void OnPostClose()
        {
            Clear();
        }

        private void Refresh()
        {
            _mixSourceItem.TryGetItemComponent<ItemMixSourceComponent>(out var com);

            // 已投入物品数量
            using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> mixedDict);
            foreach (var item in com.mixedItems)
            {
                if (mixedDict.ContainsKey(item) )
                {
                    ++mixedDict[item];
                }
                else
                {
                    mixedDict[item] = 1;
                }
            }

            using var __ = PoolMapping.PoolMappingAccess.Borrow(out List<int> outputList);
            // 遍历每一种合成组合
            foreach (var mixId in com.config.MixId)
            {
                var cfg = Env.Instance.GetMergeMixCostConfig(mixId);
                outputList.Clear();
                IDictionary<int, int> dict;
                if (cfg.Outputs.Count < 1)
                {
                    dict = com.config.DieInto;
                }
                else
                {
                    dict = cfg.Outputs;
                }
                foreach (var kv in dict)
                {
                    if (!outputList.Contains(kv.Key))
                    {
                        outputList.Add(kv.Key);
                    }
                }
                outputList.Sort();
                foreach (var output in outputList)
                {
                    ShowMixOutput(output, cfg, mixedDict);
                }
            }
        }

        private void ShowMixOutput(int output, MergeMixCost cost, Dictionary<int, int> mixedItems)
        {
            // 记录id出现的次数 数量足够时打勾
            using var _ = PoolMapping.PoolMappingAccess.Borrow(out Dictionary<int, int> cache);
            var mixIds = cost.MixInfo;
            var item = CreateMixItem();
            var beginIdx = FindSetDataBeginIdx(item, mixIds.Count);
            // 展示消耗项
            var itemIdx = 0;
            for (var i = beginIdx; i < item.childCount && itemIdx < mixIds.Count; i++, itemIdx++)
            {
                var id = mixIds[itemIdx];
                if (cache.ContainsKey(id)) ++cache[id];
                else cache[id] = 1;
                var child = item.GetChild(i);
                child.Access<UIImageRes>("Icon").SetImage(Env.Instance.GetItemConfig(id).Icon);
                mixedItems.TryGetValue(id, out var mixedCount);
                child.Access<Transform>("Right").gameObject.SetActive(cache[id] <= mixedCount);
            }
            // 展示产出项
            var outputItem = item.GetChild(item.childCount - 1);
            outputItem.Access<UIImageRes>("Icon").SetImage(Env.Instance.GetItemConfig(output).Icon);
        }

        // 判断设置数据的索引起点
        private int FindSetDataBeginIdx(Transform item, int itemCount)
        {
            // 每个道具 + 1个结果
            var needSlot = itemCount + 1;
            var skip = item.childCount - needSlot;
            for (var i = 0; i < item.childCount; i++)
            {
                var child = item.GetChild(i);
                child.gameObject.SetActive(i >= skip);
            }
            return Mathf.Max(0, skip);
        }

        private Transform CreateMixItem()
        {
            var go = GameObjectPoolManager.Instance.CreateObject(poolItemType, itemRoot);
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            return go.transform;
        }

        private void Clear()
        {
            var root = itemRoot;
            for (var i = root.childCount - 1; i >= 0; --i)
            {
                var item = root.GetChild(i);
                GameObjectPoolManager.Instance.ReleaseObject(poolItemType, item.gameObject);
            }
        }
    }
}