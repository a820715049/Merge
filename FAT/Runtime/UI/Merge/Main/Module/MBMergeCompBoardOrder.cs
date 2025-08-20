/*
 * @Author: qun.chao
 * @Date: 2023-10-25 12:13:36
 */
using System;
using System.Collections;
using System.Collections.Generic;
using EL;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace FAT
{
    class MBBoardOrderUtility
    {
        public static string GetDefaultTypeKey()
        {
            return "order_item_default";
        }

        public static string GetItemTypeKey(IOrderData order)
        {
            if (order.ShouldOverrideOrderRes())
            {
                if (order.TryGetOverrideRes(out var res))
                    return res;
            }
            return GetDefaultTypeKey();
        }
    }

    public class MBMergeCompBoardOrder : MonoBehaviour
    {
        enum SortGroup
        {
            None,
            StepOrder,
            FlashOrder,
            Bonus,
            Fulfilled,
            Partially,
            NoProgress,
            Locked,
        }

        [SerializeField] private ScrollRect scroll;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private GameObject goItem;
        private List<IOrderData> newlyAddedOrderList = new();
        private Dictionary<IOrderData, MBBoardOrder> orderInstTable = new();
        private bool isLayoutDirty;
        private bool isRequestingScrollReset;

        #region 玩法教学
        // 上次活跃时间
        private float lastActiveTime;
        #endregion

        // 正在退场的item当前的位置 记录下来准备避免其在数据改变后引起排序变动
        private List<(IOrderData data, int siblingIdx)> leavingOrderPreferedSiblingCache = new();
        private Dictionary<IOrderData, string> mReloadRequestDict = new();
        // api延迟
        private float spawn_delay_api = 0f;
        // 订单和订单之间的固有间隔
        private float spawn_delay_interval = 0f;
        // 订单显示固有的总延迟
        private float spawn_delay_total => spawn_delay_api + spawn_delay_interval;
        // 下次允许订单显示的时间
        private float nextOrderShowTime = -1f;
        private System.Threading.CancellationTokenSource ctsForSpawn;

        #region scroll anim
        private Tween scrollAnim;
        private RectTransform targetScrollTrans;
        #endregion

        public void Setup()
        {
            var defaultKey = MBBoardOrderUtility.GetDefaultTypeKey();
            GameObjectPoolManager.Instance.PreparePool(defaultKey, goItem);
            GameObjectPoolManager.Instance.ReleaseObject(defaultKey, goItem);
        }

        public void InitOnPreOpen()
        {
            ctsForSpawn?.Dispose();
            ctsForSpawn = new();
            lastActiveTime = Time.realtimeSinceStartup;

            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().AddListener(_OnMessageOrderChange);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_RELOAD>().AddListener(_OnMessageOrderItemNeedReload);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_TRY_RELEASE>().AddListener(_OnMessageBoardOrderTryRelease);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_ANIMATING>().AddListener(_OnMessageBoardOrderAnimating);
            MessageCenter.Get<MSG.UI_ORDER_ADJUST_SCROLL>().AddListener(_OnMessageAdjustOrderScrollToPos);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_COMMON_FINISHED_TRANSFORM>().AddListener(_OnMessageQueryFinishedCommonOrderTrasnform);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_RANDOMER_TRANSFORM>().AddListener(_OnMessageQueryRandomerOrderTrasnformById);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_TRANSFORM_BY_ORDER>().AddListener(_OnMessageQueryOrderTrasnformByOrder);
            MessageCenter.Get<MSG.UI_ORDER_BOX_TIPS_POSITION_REFRESH>().AddListener(_OnMessageOrderBoxTipPosRefresh);
            MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().AddListener(_OnMessageNewlyFinishedOrderShow);
            MessageCenter.Get<MSG.UI_ORDER_REQUEST_SCROLL>().AddListener(_OnMessageOrderRequestScroll);
            MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_RESET>().AddListener(_OnMessageOrderScrollResetWithAnim);
            spawn_delay_interval = Game.Manager.configMan.globalConfig.OrderEnterDelay / 1000f;
            spawn_delay_api = Game.Manager.configMan.globalConfig.OrderEnterApiDelay / 1000f;

            _FirstTimeShow();

            if (nextOrderShowTime < 0)
            {
                nextOrderShowTime = Time.realtimeSinceStartup + spawn_delay_total;
            }
        }

        public void CleanupOnPostClose()
        {
            ctsForSpawn?.Cancel();
            MessageCenter.Get<MSG.GAME_ORDER_CHANGE>().RemoveListener(_OnMessageOrderChange);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_RELOAD>().RemoveListener(_OnMessageOrderItemNeedReload);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_TRY_RELEASE>().RemoveListener(_OnMessageBoardOrderTryRelease);
            MessageCenter.Get<MSG.UI_BOARD_ORDER_ANIMATING>().RemoveListener(_OnMessageBoardOrderAnimating);
            MessageCenter.Get<MSG.UI_ORDER_ADJUST_SCROLL>().RemoveListener(_OnMessageAdjustOrderScrollToPos);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_COMMON_FINISHED_TRANSFORM>().RemoveListener(_OnMessageQueryFinishedCommonOrderTrasnform);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_RANDOMER_TRANSFORM>().RemoveListener(_OnMessageQueryRandomerOrderTrasnformById);
            MessageCenter.Get<MSG.UI_ORDER_QUERY_TRANSFORM_BY_ORDER>().RemoveListener(_OnMessageQueryOrderTrasnformByOrder);
            MessageCenter.Get<MSG.UI_ORDER_BOX_TIPS_POSITION_REFRESH>().RemoveListener(_OnMessageOrderBoxTipPosRefresh);
            MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().RemoveListener(_OnMessageNewlyFinishedOrderShow);
            MessageCenter.Get<MSG.UI_ORDER_REQUEST_SCROLL>().RemoveListener(_OnMessageOrderRequestScroll);
            MessageCenter.Get<MSG.BOARD_ORDER_SCROLL_RESET>().RemoveListener(_OnMessageOrderScrollResetWithAnim);
            _Cleanup();
        }

        #region item pool

        private float _CalcNextBornDelay()
        {
            var spawnTime = spawn_delay_api + Time.realtimeSinceStartup;
            if (spawnTime < nextOrderShowTime)
            {
                spawnTime = nextOrderShowTime;
            }
            nextOrderShowTime = spawnTime + spawn_delay_total;
            return spawnTime - Time.realtimeSinceStartup;
        }

        private async UniTaskVoid _DelayReload(IOrderData order, string res)
        {
            // 避免SetData调用期间刷新item
            await UniTask.NextFrame(ctsForSpawn.Token);

            // 订单已不在 无需处理
            if (!orderInstTable.ContainsKey(order))
                return;

            if (GameObjectPoolManager.Instance.HasPool(res))
            {
                _TryReloadOrderItem(order, res);
            }
            else
            {
                if (!mReloadRequestDict.ContainsKey(order))
                {
                    mReloadRequestDict.Add(order, res);

                    _PrepareItemPool(res, res.ConvertToAssetConfig(),
                        () =>
                        {
                            _TryReloadOrderItem(order, res);
                        }).Forget();
                }
            }
        }

        private async UniTaskVoid _PrepareItemPool(string key, Config.AssetConfig res, Action cb)
        {
            var task = EL.Resource.ResManager.LoadAsset<GameObject>(res.Group, res.Asset);
            await UniTask.WaitUntil(() => !task.keepWaiting, PlayerLoopTiming.Update, ctsForSpawn.Token);
            if (task.isSuccess)
            {
                var obj = Instantiate(task.asset as GameObject);
                GameObjectPoolManager.Instance.PreparePool(key, obj);
                GameObjectPoolManager.Instance.ReleaseObject(key, obj);
                cb?.Invoke();
            }
        }

        #endregion

        #region 玩法教学

        private void _TryShowGamePlayGuide()
        {
            if (!_IsMatchGamePlayGuideLevelRequire())
                return;
            if (Input.touchCount > 0 || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
            {
                lastActiveTime = Time.realtimeSinceStartup;
            }
            if (lastActiveTime > 0 && lastActiveTime + Game.Manager.configMan.globalConfig.MergeTutorialInterval / 1000f < Time.realtimeSinceStartup)
            {
                if (GuideUtility.IsBoardReady(UIConfig.UIMergeBoardMain) && !UIManager.Instance.IsOpen(UIConfig.UIGuide))
                {
                    lastActiveTime = Time.realtimeSinceStartup;
                    UIUtility.ShowGameplayHelp();
                }
            }
        }

        private bool _IsMatchGamePlayGuideLevelRequire()
        {
            return Game.Manager.mergeLevelMan.level < Game.Manager.configMan.globalConfig.MergeTutorialStop;
        }

        #endregion

        private void _FirstTimeShow()
        {
            _ResetScroll();
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var container))
            {
                BoardViewWrapper.FillBoardOrder(container);
                foreach (var order in container)
                {
                    if (order.IsExpired)
                        continue;
                    _TrySpawnOrder(order, false);
                }
                _ApplySort();
            }
        }

        private void _Cleanup()
        {
            scrollAnim?.Kill();
            scrollAnim = null;
            isRequestingScrollReset = false;
            isLayoutDirty = false;
            foreach (var kv in orderInstTable)
            {
                var inst = kv.Value;
                inst.Clear();
                GameObjectPoolManager.Instance.ReleaseObject(inst.poolKey, inst.gameObject);
            }
            if (itemRoot.childCount > 0)
            {
                DebugEx.Error($"Unexpected OrderItem");
                UIUtility.ReleaseClearableItem(itemRoot, MBBoardOrderUtility.GetDefaultTypeKey());
            }
            orderInstTable.Clear();
            newlyAddedOrderList.Clear();
            mReloadRequestDict.Clear();
        }

        private void Update()
        {
            _TryShowGamePlayGuide();
        }

        private void LateUpdate()
        {
            if (isRequestingScrollReset)
            {
                isRequestingScrollReset = false;
                if (GameProcedure.IsInGame)
                {
                    // 加载中不需要滚动 | 同时规避首次读档后刷新订单状态触发的'新入场'订单
                    DelayScrollToTarget().Forget();
                }
                else
                {
                    targetScrollTrans = null;
                }
            }
            if (isLayoutDirty)
            {
                isLayoutDirty = false;
                _MarkRebuild();
            }
        }

        private async UniTaskVoid DelayScrollToTarget()
        {
            if (targetScrollTrans != null)
            {
                await UniTask.Yield();
                _ScrollToTarget(targetScrollTrans);
                targetScrollTrans = null;
            }
        }

        #region spawn/release

        private bool _TrySpawnOrder(IOrderData order, bool newlyAdd)
        {
            var typeKey = MBBoardOrderUtility.GetItemTypeKey(order);
            if (!GameObjectPoolManager.Instance.HasPool(typeKey))
            {
                bool spawnAfterLoadRes = true;
                if (string.IsNullOrEmpty(typeKey))
                {
                    // 无效配置 用默认资源替代
                    DebugEx.Warning($"order res not found for order Id {order.Id}");
                    spawnAfterLoadRes = false;
                    typeKey = MBBoardOrderUtility.GetDefaultTypeKey();
                }

                if (spawnAfterLoadRes)
                {
                    _PrepareItemPool(typeKey, typeKey.ConvertToAssetConfig(),
                        () =>
                        {
                            if (!gameObject.activeSelf)
                                return;
                            var inst = _SpawnOrder(order, ref typeKey);
                            if (newlyAdd)
                            {
                                _OnNewlyAdd(inst);
                            }
                            else
                            {
                                OrderUtility.TryTrackOrderShow(order);
                            }
                            _ApplySort();
                        }).Forget();
                    return false;
                }
            }

            // pool有效 同步加载
            var inst = _SpawnOrder(order, ref typeKey);
            if (newlyAdd)
            {
                _OnNewlyAdd(inst);
            }
            else
            {
                OrderUtility.TryTrackOrderShow(order);
            }
            return true;
        }

        private void _OnNewlyAdd(MBBoardOrder inst)
        {
            inst.PlayAnim_Born(_CalcNextBornDelay());
        }

        private MBBoardOrder _SpawnOrder(IOrderData order, ref string poolKey)
        {
            if (!GameObjectPoolManager.Instance.HasPool(poolKey))
            {
                DebugEx.Error($"order res missing for order Id {order.Id} / event {order.GetValue(OrderParamType.EventId)}");
                poolKey = MBBoardOrderUtility.GetDefaultTypeKey();
            }
            var go = GameObjectPoolManager.Instance.CreateObject(poolKey);
            go.transform.SetParent(itemRoot);
            go.transform.localScale = Vector3.one;
            var mb = go.GetComponent<MBBoardOrder>();
            mb.poolKey = poolKey;
            mb.SetData(order);
            go.SetActive(true);
            orderInstTable.Add(order, mb);
            _SetLayoutDirty();
            _UpdateSortParam(mb, true);
            return mb;
        }

        private bool _TryReleaseOrderElement(MBBoardOrder element)
        {
            if (orderInstTable.TryGetValue(element.data, out var inst))
            {
                orderInstTable.Remove(element.data);
                inst.Clear();
                GameObjectPoolManager.Instance.ReleaseObject(inst.poolKey, inst.gameObject);
                _SetLayoutDirty();
                return true;
            }
            return false;
        }

        #endregion

        private int _Sort(MBBoardOrder a, MBBoardOrder b)
        {
            if (a.sortGroup == b.sortGroup)
            {
                if (a.sortWeight == b.sortWeight)
                {
                    if (a.data.Id == b.data.Id)
                    {
                        return a.data.ProviderType - b.data.ProviderType;
                    }
                    else
                    {
                        return a.data.Id - b.data.Id;
                    }
                }
                else
                {
                    return -(a.sortWeight - b.sortWeight);
                }
            }
            else
            {
                return a.sortGroup - b.sortGroup;
            }
        }

        private void _UpdateSortParam(MBBoardOrder item, bool newAdd = false)
        {
            var state = item.data.State;
            item.sortGroupPrev = item.sortGroup;
            if (item.data.IsStep)
            {
                item.sortGroup = (int)SortGroup.StepOrder;
            }
            else if (item.data.IsFlash)
            {
                item.sortGroup = (int)SortGroup.FlashOrder;
            }
            else if (item.data.BonusID > 0)
            {
                item.sortGroup = (int)SortGroup.Bonus;
            }
            else if (state == OrderState.Rewarded || state == OrderState.Finished)
            {
                item.sortGroup = (int)SortGroup.Fulfilled;
            }
            else if (state == OrderState.NotStart)
            {
                item.sortGroup = (int)SortGroup.NoProgress;
            }
            else if (state == OrderState.PreShow)
            {
                item.sortGroup = (int)SortGroup.Locked;
            }
            else
            {
                item.sortGroup = (int)SortGroup.NoProgress;
                foreach (var reqs in item.data.Requires)
                {
                    if (reqs.CurCount > 0)
                    {
                        item.sortGroup = (int)SortGroup.Partially;
                        break;
                    }
                }
            }
            if (newAdd)
            {
                item.sortWeight = -Time.frameCount;
            }
            else if (item.sortGroup != item.sortGroupPrev)
            {
                item.sortWeight = Time.frameCount;
            }
        }

        private void _ApplySort()
        {
            using (ObjectPool<List<MBBoardOrder>>.GlobalPool.AllocStub(out var list))
            {
                foreach (var kv in orderInstTable)
                {
                    list.Add(kv.Value);
                }
                list.Sort(_Sort);
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].transform.SetSiblingIndex(i);
                }
                _SetLayoutDirty();
            }
        }

        private void _UpdateActiveOrderElement(List<IOrderData> changedOrders)
        {
            var hasNewlyFinished = false;
            var instTable = orderInstTable;
            Transform transNeedScroll = null;
            foreach (var order in changedOrders)
            {
                if (instTable.TryGetValue(order, out var inst))
                {
                    // 更新排序参数
                    _UpdateSortParam(inst, false);
                    if (inst.data.State == OrderState.Rewarded || inst.data.State == OrderState.Expired)
                    {
                        // 退场
                        inst.PlayAnim_Die();
                    }
                    else if (inst.data.State == OrderState.Finished)
                    {
                        // 新进入列表的可完成订单 触发Scroll
                        hasNewlyFinished = true;
                        transNeedScroll = inst.transform;
                    }
                }
            }
            if (hasNewlyFinished)
                _RequestScrollToTarget(transNeedScroll);
        }

        private void _UpdatedLeavingOrderElement()
        {
            leavingOrderPreferedSiblingCache.Clear();
            foreach (var kv in orderInstTable)
            {
                if (kv.Key.State == OrderState.Rewarded || kv.Key.State == OrderState.Expired)
                {
                    leavingOrderPreferedSiblingCache.Add((kv.Key, kv.Value.transform.GetSiblingIndex()));
                }
            }
        }

        private void _TryKeepDyingItemSortingPosition()
        {
            if (leavingOrderPreferedSiblingCache.Count > 0)
            {
                leavingOrderPreferedSiblingCache.Sort((a, b) => a.siblingIdx - b.siblingIdx);
            }
            var instTable = orderInstTable;
            foreach (var item in leavingOrderPreferedSiblingCache)
            {
                if (instTable.TryGetValue(item.data, out var inst))
                {
                    inst.transform.SetSiblingIndex(item.siblingIdx);
                }
            }
            leavingOrderPreferedSiblingCache.Clear();
        }

        private void _TryReloadOrderItem(IOrderData order, string res)
        {
            if (orderInstTable.TryGetValue(order, out var oldInst))
            {
                var siblingIdx = oldInst.transform.GetSiblingIndex();
                _TryReleaseOrderElement(oldInst);
                // 同步加载
                var inst = _SpawnOrder(order, ref res);
                inst.transform.SetSiblingIndex(siblingIdx);
            }
            if (mReloadRequestDict.ContainsKey(order))
            {
                mReloadRequestDict.Remove(order);
            }
        }

        #region scroll / anim

        private void _ScrollToTarget(Transform target)
        {
            var state = UIUtility.CheckScreenViewState(target as RectTransform);
            var duration = UIUtility.GetScreenViewStateDelay(state);
            if (UIUtility.CalcScrollPosForTargetInMiddleOfView(target as RectTransform, scroll.content, scroll.viewport, out var pos))
            {
                _ScrollToPos(pos, duration);
            }
        }

        private void _ScrollToPos(float pos, float duration)
        {
            scrollAnim?.Kill();
            var trans = scroll.content;
            if (trans.rect.width < scroll.viewport.rect.width)
            {
                // 无需滚动
                GuideUtility.TriggerGuide();
                return;
            }

            var startPos = trans.anchoredPosition.x;
            var targetPos = pos;
            var distance = targetPos - startPos;

            // 使用DOTween来控制时间和缓动，在OnUpdate中设置位置
            scrollAnim = DOTween.To(
                () => 0f,
                progress =>
                {
                    // 直接用progress计算偏移量，让DOTween处理缓动
                    var currentPos = startPos + (distance * progress);
                    currentPos = Mathf.Clamp(currentPos, -(trans.rect.width - scroll.viewport.rect.width), 0);
                    trans.anchoredPosition = new Vector2(currentPos, trans.anchoredPosition.y);
                },
                1f,
                duration
            ).SetEase(Ease.InOutSine).OnComplete(GuideUtility.TriggerGuide);
        }

        private void _RequestScrollToTarget(Transform trans)
        {
            isRequestingScrollReset = true;
            targetScrollTrans = trans as RectTransform;
        }

        private void _ResetScroll()
        {
            scroll.content.anchoredPosition = Vector2.zero;
        }

        private void _SetLayoutDirty()
        {
            isLayoutDirty = true;
        }

        private void _MarkRebuild()
        {
            LayoutRebuilder.MarkLayoutForRebuild(itemRoot as RectTransform);
        }

        #endregion

        private void _OnMessageOrderChange(List<IOrderData> changedOrders, List<IOrderData> newlyAddedOrders)
        {
            foreach (var order in newlyAddedOrders)
            {
                _TrySpawnOrder(order, true);
            }

            // 仅处理已有的订单元素
            _UpdateActiveOrderElement(changedOrders);
            _UpdatedLeavingOrderElement();
            _ApplySort();
            // 将正在退场的item归位 避免不必要的抖动
            _TryKeepDyingItemSortingPosition();
        }

        private void _OnMessageBoardOrderAnimating()
        {
            _SetLayoutDirty();
        }

        private void _OnMessageBoardOrderTryRelease(MBBoardOrder element)
        {
            _TryReleaseOrderElement(element);
        }

        private void _OnMessageOrderItemNeedReload(IOrderData order, string res)
        {
            _DelayReload(order, res).Forget();
        }

        /// <summary>
        /// 外部查询订单transform
        /// </summary>
        /// <param name="orderId">订单Id</param>
        /// <param name="resolver">回调</param>
        private void _OnMessageQueryFinishedCommonOrderTrasnform(int orderId, Action<Transform> resolver)
        {
            foreach (var kv in orderInstTable)
            {
                var order = kv.Key;
                if (orderId < 0 || orderId == order.Id)
                {
                    if (order.ProviderType == (int)OrderProviderType.Common && order.State == OrderState.Finished)
                    {
                        resolver?.Invoke(kv.Value.transform);
                        return;
                    }
                }
            }
            resolver?.Invoke(null);
        }

        private void _OnMessageQueryRandomerOrderTrasnformById(int orderId, Action<Transform> resolver)
        {
            foreach (var kv in orderInstTable)
            {
                var order = kv.Key;
                if (orderId < 0 || orderId == order.Id)
                {
                    if (order.ProviderType == (int)OrderProviderType.Random)
                    {
                        resolver?.Invoke(kv.Value.transform);
                        return;
                    }
                }
            }
            resolver?.Invoke(null);
        }

        private void _OnMessageQueryOrderTrasnformByOrder(IOrderData order, Action<Transform> resolver)
        {
            orderInstTable.TryGetValue(order, out var inst);
            resolver?.Invoke(inst.transform);
        }

        private void _OnMessageOrderBoxTipPosRefresh(Vector3 wpInfo, Vector3 wpBox)
        {
            scroll.velocity = Vector2.zero;
            var contentRect = scroll.content;
            var viewSize = scroll.viewport.rect.width;
            var contentSize = contentRect.rect.width;

            var lpInfo = contentRect.parent.InverseTransformPoint(wpInfo);
            var lpBox = contentRect.parent.InverseTransformPoint(wpBox);

            if (viewSize < contentSize)
            {
                var delta = lpInfo.x - lpBox.x;
                var targetX = Mathf.Clamp(contentRect.localPosition.x - delta, -(contentSize - viewSize), 0);
                contentRect.DOLocalMoveX(targetX, 0.3f).SetEase(Ease.InOutSine);
            }
        }

        private void _OnMessageNewlyFinishedOrderShow(Transform trans)
        {
            _RequestScrollToTarget(trans);
        }

        private void _OnMessageOrderRequestScroll(Transform trans)
        {
            _RequestScrollToTarget(trans);
        }

        private void _OnMessageAdjustOrderScrollToPos(float pos, float duration)
        {
            _ScrollToPos(pos, duration);
        }

        private void _OnMessageOrderScrollResetWithAnim()
        {
            scrollAnim?.Kill();
            scrollAnim = scroll.content.DOLocalMoveX(0, 0.5f).OnComplete(_ResetScroll);
        }
    }
}