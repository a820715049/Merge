/*
 * @Author: qun.chao
 * @Date: 2025-08-06 12:15:21
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using EL;

namespace FAT
{
    public class UIOrderDebug : UIBase
    {
        [SerializeField] private Button btnBg;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnReplace;
        [SerializeField] private List<TMP_InputField> requireList;
        [SerializeField] private TextMeshProUGUI txtDesc;

        private IOrderData order;

        protected override void OnCreate()
        {
            btnBg.onClick.AddListener(Close);
            btnClose.onClick.AddListener(Close);
            btnReplace.onClick.AddListener(OnBtnReplace);
        }

        protected override void OnParse(params object[] items)
        {
            order = items[0] as IOrderData;
        }

        protected override void OnPreOpen()
        {
            RefreshOrderInfo();
        }

        private void RefreshOrderInfo()
        {
            if (order == null)
            {
                txtDesc.text = "null";
                return;
            }
            var idx = 0;
            for (var i = 0; i < requireList.Count; i++)
            {
                requireList[i].text = string.Empty;
            }
            var rec = (order as OrderData).Record;
            var ids = rec.RequireIds;
            var nums = rec.RequireNums;
            for (var i = 0; i < ids.Count; i++)
            {
                for (var j = 0; j < nums[i]; j++)
                {
                    if (idx < requireList.Count)
                    {
                        requireList[idx].text = $"{ids[i]}";
                    }
                    idx++;
                }
            }
            txtDesc.text = $"Id:{order.Id}\nActDifficulty:{order.ActDifficulty}\nPayDifficulty:{order.PayDifficulty}";
        }

        private void OnBtnReplace()
        {
#if UNITY_EDITOR
            if (order == null)
            {
                DebugEx.Error("order is null");
                return;
            }

            using var _ = PoolMapping.PoolMappingAccess.Borrow(out List<int> list);
            foreach (var input in requireList)
            {
                if (int.TryParse(input.text, out var itemId))
                {
                    if (Game.Manager.objectMan.GetMergeItemConfig(itemId) != null)
                    {
                        list.Add(itemId);
                    }
                }
            }

            if (list.Count == 0)
            {
                DebugEx.Error("invalid item id");
                return;
            }

            // 更新订单需求
            var rec = (order as OrderData).Record;
            var reqs = order.Requires;
            reqs.Clear();
            for (var i = 0; i < list.Count; i++)
            {
                var idx = reqs.FindIndex(item => item.Id == list[i]);
                if (idx == -1)
                {
                    reqs.Add(new ItemCountInfo() { Id = list[i], CurCount = 0, TargetCount = 1 });
                }
                else
                {
                    reqs[idx].TargetCount++;
                }
            }
            reqs.Sort((a, b) => a.Id - b.Id);

            // 同步变更存档数据
            rec.RequireIds.Clear();
            rec.RequireNums.Clear();
            foreach (var item in reqs)
            {
                rec.RequireIds.Add(item.Id);
                rec.RequireNums.Add(item.TargetCount);
            }

            var tracer = Game.Manager.mergeBoardMan.activeTracer;
            // 更新难度
            if (order.ProviderType == (int)OrderProviderType.Random)
            {
                var totalRealDffy = 0;
                var totalPayDffy = 0;
                foreach (var item in order.Requires)
                {
                    var (realDffy, accDffy) = OrderUtility.CalcDifficultyForItem(item.Id, tracer);
                    var payDffy = realDffy - accDffy;
                    totalRealDffy += realDffy;
                    totalPayDffy += payDffy;
                }
                RecordStateHelper.UpdateRecord((int)OrderParamType.ActDifficulty, totalRealDffy, rec.Extra);
                RecordStateHelper.UpdateRecord((int)OrderParamType.PayDifficulty, totalPayDffy, rec.Extra);
            }

            // 刷新debug界面
            RefreshOrderInfo();

            OrderUtility.UpdateOrderStatus(order as OrderData, tracer, Game.Manager.mainOrderMan.curOrderHelper);
            MessageCenter.Get<MSG.GAME_ORDER_REFRESH>().Dispatch(order);
#endif
        }
    }
}