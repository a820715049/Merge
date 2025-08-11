/*
 * @Author: qun.chao
 * @Date: 2024-06-12 14:43:40
 */
using System;
using System.Collections.Generic;
using EL;
using Newtonsoft.Json;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FAT.RemoteAnalysis
{
    public class RemoteOrderWrapper
    {
        private static readonly string apiUrl = "https://dsc-ai.centurygame.com/api/v1/fat/order-predict";

        // item信息缓存
        private static Dictionary<int, RequireItemInfo> mCacheRequireItemInfo = new();

        private Dictionary<IOrderData, (Dictionary<int, int> requires, int diff_pay, int diff_act)> mOrderApiResultMap = new();

        public void Reset()
        {
            mCacheRequireItemInfo.Clear();
            foreach (var kv in mOrderApiResultMap)
            {
                FreeDict(kv.Value.requires);
            }
            mOrderApiResultMap.Clear();
        }

        public bool TryApplyOrder(IOrderData order, IOrderHelper helper, IList<string> reward, int minDiffRate)
        {
            if (!mOrderApiResultMap.TryGetValue(order, out var parsedOrder))
            {
                DebugEx.Info("[ORDERDEBUG] api result not ready");
                return false;
            }

            // 更新订单需求
            var reqs = order.Requires;
            reqs.Clear();
            foreach (var kv in parsedOrder.requires)
            {
                reqs.Add(new ItemCountInfo() { Id = kv.Key, CurCount = 0, TargetCount = kv.Value });
            }
            reqs.Sort((a, b) => a.Id - b.Id);

            var rec = (order as OrderData).Record;

            // // 删除订单自身携带的奖励 / 保留活动追加的奖励
            // _RemoveNoneActivityRewards(order);

            // 清除所有活动奖励
            OrderUtility.ClearActivityRewarsForApiOrder(order as OrderData);
            order.Rewards.Clear();
            rec.RewardIds.Clear();
            rec.RewardNums.Clear();

            var realDffRound = OrderUtility.CalcRealDffyRound(parsedOrder.diff_pay, parsedOrder.diff_act, minDiffRate);
            // 更新常规订单奖励
            OrderUtility.MakeOrder_Reward(order as OrderData, helper, realDffRound, reward);

            // 同步变更存档数据
            rec.RequireIds.Clear();
            rec.RequireNums.Clear();
            foreach (var item in reqs)
            {
                rec.RequireIds.Add(item.Id);
                rec.RequireNums.Add(item.TargetCount);
            }
            rec.RewardIds.Clear();
            rec.RewardNums.Clear();
            foreach (var item in order.Rewards)
            {
                rec.RewardIds.Add(item.Id);
                rec.RewardNums.Add(item.Count);
            }

            // 更新难度
            RecordStateHelper.UpdateRecord((int)OrderParamType.PayDifficulty, parsedOrder.diff_pay, rec.Extra);
            RecordStateHelper.UpdateRecord((int)OrderParamType.ActDifficulty, parsedOrder.diff_act, rec.Extra);

            FreeDict(parsedOrder.requires);
            mOrderApiResultMap.Remove(order);
            return true;
        }

        private void _RemoveNoneActivityRewards(IOrderData order)
        {
            bool IsExtraBonus(IOrderData od, int id, int count)
            {
                if (!od.HasExtraReward)
                    return false;
                if (od.GetValue(OrderParamType.ExtraBonusRewardId) == id && od.GetValue(OrderParamType.ExtraBonusRewardNum) == count)
                    return true;
                return false;
            }

            bool IsExtraBonusMini(IOrderData od, int id, int count)
            {
                if (!od.HasExtraRewardMini)
                    return false;
                if (od.GetValue(OrderParamType.ExtraBonusRewardId_Mini) == id && od.GetValue(OrderParamType.ExtraBonusRewardNum_Mini) == count)
                    return true;
                return false;
            }

            var rec = (order as OrderData).Record;
            var rewards = order.Rewards;
            for (int i = rewards.Count - 1; i >= 0; --i)
            {
                if (IsExtraBonus(order, rewards[i].Id, rewards[i].Count) || IsExtraBonusMini(order, rewards[i].Id, rewards[i].Count))
                {
                    continue;
                }
                rewards.RemoveAt(i);
                rec.RewardIds.RemoveAt(i);
                rec.RewardNums.RemoveAt(i);
            }
        }


        /// <summary>
        /// 发送请求获取一个订单
        /// </summary>
        /// <param name="nextOrder">客户端决策的订单</param>
        /// <param name="lastOrder">玩家上次完成的订单</param>
        /// <param name="candList">决策时的其他候选订单</param>
        /// <param name="curList">当前活跃的其他订单</param>
        internal void SendRequest(string modelVersion, IOrderData nextOrder, IOrderData lastOrder, List<OrderProviderRandom.CandidateGroupInfo> candList, List<IOrderData> curList)
        {
            Game.Manager.remoteApiMan.OnPreSendOrderRequest();
            var req = new OrderRequest();
            FillUserInfo(req);
            req.user_info.order_id = nextOrder.Id;
            req.model_info = new ModelInfo()
            {
                model_version = modelVersion,
            };
            req.purchase_info = ActivityRedirect.CreatePurchase(null);
            FillLastInfo(req, lastOrder);
            FillRecentDiff(req);
            FillNextInfo(req, nextOrder, candList, curList);
            _InnerSend(nextOrder, req).Forget();
        }

        private async UniTaskVoid _InnerSend(IOrderData order, OrderRequest req)
        {
            var json = JsonConvert.SerializeObject(req);

            if (Application.isEditor || GameSwitchManager.Instance.isDebugMode)
            {
                DebugEx.Info($"[ORDERDEBUG] api upload => {json}");
            }

            var begin = Time.realtimeSinceStartup;
            using (var webPost = UnityWebRequest.Post(apiUrl, json, "application/json"))
            {
                await webPost.SendWebRequest();
                var end = Time.realtimeSinceStartup;

                if (webPost.result != UnityWebRequest.Result.Success)
                {
                    DebugEx.Warning($"[ORDERDEBUG] api failed {req.user_info.account_id}@{req.user_info.event_time} | {webPost.error} | {webPost.result}");
                    return;
                }
                try
                {
                    if (Application.isEditor || GameSwitchManager.Instance.isDebugMode)
                    {
                        DebugEx.Info($"[ORDERDEBUG] api response => cost:{(end - begin).ToString("F4")} | {webPost.downloadHandler.text}");
                    }
                    if (order.ShouldNotChange)
                    {
                        // api太慢 订单已固定 不能再使用api的预测值
                        return;
                    }
                    var resp = JsonConvert.DeserializeObject<OrderResponse>(webPost.downloadHandler.text);
                    var apiOrder = ParseApiOrder(resp.data.rec_order);
                    if (!CheckApiResultMatchCandidate(req, apiOrder, out var diff_pay, out var diff_act))
                    {
                        // 预测结果不在备选池中
                        DebugEx.Error($"[ORDERDEBUG] api cache missing {req.user_info.account_id}@{req.user_info.event_time} {webPost.downloadHandler.text}");
                        return;
                    }
                    // 记录返回结果
                    mOrderApiResultMap.TryAdd(order, (apiOrder, diff_pay, diff_act));
                }
                catch
                {
                    DebugEx.Error($"[ORDERDEBUG] api bad data {req.user_info.account_id}@{req.user_info.event_time} {webPost.downloadHandler.text}");
                    return;
                }
            }
        }

        //  "rec_order": "12000314:1,12000458:1,12000450:1"
        private Dictionary<int, int> ParseApiOrder(string data)
        {
            var dict = AllocDict();
            var span = data.AsSpan();
            do
            {
                var idx_split = span.IndexOf(',');
                var idx_colon = span.IndexOf(':');
                int.TryParse(span[..idx_colon], out var id);
                int count;
                if (idx_split < 0)
                    int.TryParse(span[(idx_colon + 1)..], out count);
                else
                    int.TryParse(span[(idx_colon + 1)..idx_split], out count);
                if (id > 0 && count > 0)
                {
                    if (dict.ContainsKey(id))
                    {
                        dict[id] += count;
                    }
                    else
                    {
                        dict.Add(id, count);
                    }
                }
                if (idx_split < 0)
                    break;
                span = span.Slice(idx_split + 1);
            }
            while (true);
            return dict;
        }

        // 检查逻辑仅适用item不重复
        private bool CheckApiResultMatchCandidate(OrderRequest req, Dictionary<int, int> dict, out int diff_pay, out int diff_act)
        {
            diff_pay = 0;
            diff_act = 0;
            var totalReqCount = 0;
            foreach (var kv in dict)
            {
                totalReqCount += kv.Value;
            }
            var found = false;
            var cands = req.next_info.order_rec_set;
            foreach (var info in cands)
            {
                // 检查总数量需求是否一致
                if (totalReqCount != info.require_info.Count)
                    continue;
                // 检查item是否都存在
                found = true;
                foreach (var item in info.require_info)
                {
                    if (!dict.ContainsKey(item.item_id))
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    diff_pay = info.total_pay_diff;
                    diff_act = info.total_act_diff;
                    return true;
                }
            }
            return false;
        }

        private static RequireItemInfo GetRequireItemInfo(int itemId)
        {
            RequireItemInfo info;
            if (mCacheRequireItemInfo.ContainsKey(itemId))
            {
                info = mCacheRequireItemInfo[itemId];
            }
            else
            {
                var catId = Game.Manager.mergeItemMan.GetItemCategoryId(itemId);
                var orderCat = Game.Manager.mergeItemMan.GetOrderCategoryConfig(catId);
                info = new RequireItemInfo()
                {
                    item_id = itemId,
                    category_id = catId,
                    is_auto = orderCat.IsAutoGraph,
                };
                mCacheRequireItemInfo.Add(itemId, info);
            }
            return info;
        }

        private static void FillUserInfo(OrderRequest req)
        {
            req.user_info = UserInfo.Current;
        }

        private static void FillLastInfo(OrderRequest req, IOrderData lastOrder)
        {
            req.last_info = GetLastInfo(lastOrder);
        }

        internal static LastInfo GetLastInfo(IOrderData lastOrder)
        {
            if (lastOrder == null)
                return null;
            return new() {
                last_order_type = lastOrder.ProviderType,
                last_order_id = lastOrder.Id,
                last_is_api_order = lastOrder.IsApiOrder,
                last_order_info = OrderData_To_Api_Info(lastOrder),
            };
        }

        private static void FillRecentDiff(OrderRequest req)
        {
            var diff = new RecentOrderDiff()
            {
                order_pay_diff_list = new(),
                order_act_diff_list = new(),
            };
            Game.Manager.mainOrderMan.curOrderHelper.proxy.FillRecentApiOrderDiff(diff.order_act_diff_list, diff.order_pay_diff_list);
            req.last_5_order_diff = diff;
        }

        private static void FillNextInfo(OrderRequest req, IOrderData nextOrder, List<OrderProviderRandom.CandidateGroupInfo> candList, List<IOrderData> curList)
        {
            req.next_info ??= new NextInfo();
            var next = req.next_info;
            next.order_rec_next = OrderData_To_Api_Info(nextOrder);

            var candSet = new List<OrderInfo>();
            next.order_rec_set = candSet;
            foreach (var cand in candList)
            {
                candSet.Add(CandidateGroupInfo_To_Api_Info(cand));
            }

            var curSet = new List<OrderInfo>();
            next.order_cur_set = curSet;
            foreach (var order in curList)
            {
                curSet.Add(OrderData_To_Api_Info(order));
            }
        }

        private static OrderInfo OrderData_To_Api_Info(IOrderData order)
        {
            var info = new OrderInfo();
            var reqs = new List<RequireItemInfo>();
            for (var i = 0; i < order.Requires.Count; i++)
            {
                var r = order.Requires[i];
                for (var idx = 0; idx < r.TargetCount; idx++)
                {
                    reqs.Add(GetRequireItemInfo(r.Id));
                }
            }
            info.require_info = reqs;
            info.total_pay_diff = order.PayDifficulty;
            info.total_act_diff = order.ActDifficulty;
            return info;
        }

        private static OrderInfo CandidateGroupInfo_To_Api_Info(OrderProviderRandom.CandidateGroupInfo candInfo)
        {
            var info = new OrderInfo();
            var reqs = new List<RequireItemInfo>();
            var total_pay = 0;
            var total_act = 0;
            for (var i = 0; i < candInfo.ItemCount; i++)
            {
                var item = candInfo.Items[i];
                total_pay += item.PayDffy;
                total_act += item.RealDffy;
                reqs.Add(GetRequireItemInfo(item.Id));
            }
            info.require_info = reqs;
            info.total_pay_diff = total_pay;
            info.total_act_diff = total_act;
            return info;
        }

        private Dictionary<int, int> AllocDict()
        {
            return ObjectPool<Dictionary<int, int>>.GlobalPool.Alloc();
        }

        private void FreeDict(Dictionary<int, int> dict)
        {
            ObjectPool<Dictionary<int, int>>.GlobalPool.Free(dict);
        }
    }
}