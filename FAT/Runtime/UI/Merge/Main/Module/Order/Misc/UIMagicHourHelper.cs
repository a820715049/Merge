/*
 * @Author: qun.chao
 * @Date: 2025-01-24 11:00:53
 */
using UnityEngine;
using FAT.Merge;
using DG.Tweening;
using EL;

namespace FAT
{
    public class UIMagicHourHelper
    {
        private static bool IsBoardReady => BoardViewManager.Instance.IsReady;

        #region 星想事成发棋子流程演出

        public static void ShowMagicHourOutputEffect(MBBoardOrderCommitButton_Magic.Effect effGroup, Item targetItem, IOrderData targetOrder)
        {
            // 播放动画
            effGroup.PlayShow();

            // 延迟爆炸
            DOVirtual.DelayedCall(effGroup.delayBoom, () =>
            {
                if (effGroup.goBoom != null)
                {
                    effGroup.goBoom.SetActive(false);
                    effGroup.goBoom.SetActive(true);
                }
            });

            // 拖尾特效启动时挂接在local层级
            var itemType = BoardUtility.EffTypeToPoolType(ItemEffectType.MagicHourTrail);
            var trail = GameObjectPoolManager.Instance.CreateObject(itemType, effGroup.trailRoot);
            trail.SetActive(false);
            trail.transform.position = effGroup.trailStartRef.position;

            var seq = DOTween.Sequence();
            seq.AppendInterval(effGroup.trail_duration_wait_pop);
            seq.AppendCallback(() => trail.SetActive(true));
            seq.Append(trail.transform.DOMove(effGroup.trailEndRef.position, effGroup.trail_duration_pop).SetEase(Ease.InOutCirc));
            seq.AppendInterval(effGroup.trail_duration_wait);
            seq.OnComplete(() => MagicHourTrail_FlyToBoard(effGroup.trail_duration_drop, effGroup.trail_duration_wait_order, effGroup.trail_duration_order, trail, targetItem, targetOrder));
            seq.Play();
        }

        private static void MagicHourTrail_FlyToBoard(float fly_to_board_time, float fly_to_order_wait_time, float fly_to_order_time, GameObject trail, Item targetItem, IOrderData targetOrder)
        {
            if (!IsBoardReady)
            {
                TryReleaseTrail(trail);
                return;
            }
            // 第二阶段trail变更挂接到effect层
            var toPos = BoardUtility.GetWorldPosByCoord(targetItem.coord);
            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
            trail.transform.SetParent(effRoot, true);
            // 关开一次避免层级转换引起拖尾轨迹异常
            trail.SetActive(false);
            trail.SetActive(true);
            trail.transform.DOMove(toPos, fly_to_board_time).SetEase(Ease.InOutCirc).
                OnComplete(() =>
                {
                    MagicHourTrail_OnBoard(fly_to_order_wait_time, fly_to_order_time, trail, targetItem, targetOrder);
                });
        }

        private static void MagicHourTrail_OnBoard(float fly_to_order_wait_time, float fly_to_order_time, GameObject trail, Item targetItem, IOrderData targetOrder)
        {
            if (!IsBoardReady)
            {
                TryReleaseTrail(trail);
                return;
            }

            var v = BoardViewManager.Instance.GetItemView(targetItem.id);
            v.ResolveSpawnWait();

            // 播放落地效果
            BoardViewManager.Instance.boardView.boardEffect.ShowMagicHourHitEffect(targetItem.coord);
            if (targetOrder == null)
            {
                TryReleaseTrail(trail, true);
                return;
            }
            // 尝试飞到目标订单位置
            MessageCenter.Get<MSG.UI_ORDER_QUERY_TRANSFORM_BY_ORDER>().Dispatch(targetOrder, trans => MagicHourTrail_FlyToOrder(fly_to_order_wait_time, fly_to_order_time, trail, targetItem, trans));
        }

        private static void MagicHourTrail_FlyToOrder(float fly_to_order_wait_time, float fly_to_order_time, GameObject trail, Item targetItem, Transform orderTrans)
        {
            if (!CheckOrderTransformValid(orderTrans))
            {
                TryReleaseTrail(trail);
                return;
            }
            var state = UIUtility.CheckScreenViewState(orderTrans as RectTransform);
            var delay = UIUtility.GetScreenViewStateDelay(state);
            if (state != UIUtility.ScreenViewState.Inside)
            {
                // 目标订单在屏幕外 先scroll到画面内
                var contentRoot = orderTrans.parent.parent as RectTransform;
                var viewRoot = contentRoot.parent as RectTransform;
                if (UIUtility.CalcScrollPosForTargetInMiddleOfView(orderTrans as RectTransform, contentRoot, viewRoot, out var pos))
                {
                    MessageCenter.Get<MSG.UI_ORDER_ADJUST_SCROLL>().Dispatch(pos, delay);
                }
            }
            else
            {
                // 目标订单在屏幕内 也需要等待scroll基本稳定后再飞 避免粒子飞到目标后播放反馈时 订单继续滚动让特效对不上
                delay = fly_to_order_wait_time;
            }

            DOVirtual.DelayedCall(delay, () =>
            {
                // 不再依赖棋盘
                if (!CheckOrderTransformValid(orderTrans))
                {
                    TryReleaseTrail(trail);
                    return;
                }
                else
                {
                    var orderRect = orderTrans as RectTransform;
                    // 默认位置偏下一点
                    var toPosWorldDiff = orderTrans.TransformPoint(UIUtility.GetLocalCenterInRect(orderRect) - Vector2.up * 100f);
                    // 尝试找到目标item位置
                    var reqRoot = orderTrans.Access<Transform>("Info/Items");
                    // 目标链条
                    var targetCid = Game.Manager.mergeItemMan.GetItemCategoryId(targetItem.tid);
                    if (reqRoot != null)
                    {
                        for (var i = 0; i < reqRoot.childCount; i++)
                        {
                            var child = reqRoot.GetChild(i).Access<MBBoardOrderRequireItem>();
                            if (child != null && child.gameObject.activeSelf)
                            {
                                if (child.itemId == targetItem.tid)
                                {
                                    // 找到目标 停止
                                    var center = UIUtility.GetLocalCenterInRect(child.transform as RectTransform);
                                    var wp = child.transform.TransformPoint(center);
                                    toPosWorldDiff = wp - orderTrans.position;
                                    break;
                                }
                                else
                                {
                                    // 尝试找到链条
                                    if (Game.Manager.mergeItemMan.GetItemCategoryId(child.itemId) == targetCid)
                                    {
                                        // 找到链条 | 可以继续找
                                        var center = UIUtility.GetLocalCenterInRect(child.transform as RectTransform);
                                        var wp = child.transform.TransformPoint(center);
                                        toPosWorldDiff = wp - orderTrans.position;
                                    }
                                }
                            }
                        }
                    }

                    trail.SetActive(false);
                    trail.SetActive(true);
                    var tween = DOTween.To(() => trail.transform.position - orderTrans.position,
                        pos => trail.transform.position = orderTrans.position + pos,
                        toPosWorldDiff,
                        fly_to_order_time).SetEase(Ease.InOutCirc);
                    tween.OnComplete(() =>
                    {
                        // 显示反馈效果
                        if (CheckOrderTransformValid(orderTrans))
                        {
                            var itemType = BoardUtility.EffTypeToPoolType(ItemEffectType.MagicHourHit);
                            var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
                            var eff = GameObjectPoolManager.Instance.CreateObject(itemType, effRoot);
                            eff.transform.position = trail.transform.position;
                            eff.SetActive(true);
                            BoardUtility.AddAutoReleaseComponent(eff, 3f, itemType);
                        }
                        TryReleaseTrail(trail, true);
                    });
                }
            });
        }

        private static bool CheckOrderTransformValid(Transform orderTrans)
        {
            if (orderTrans != null && orderTrans.gameObject.activeSelf)
                return true;
            return false;
        }

        private static void TryReleaseTrail(GameObject trail, bool delay = false)
        {
            if (trail != null)
            {
                var itemType = BoardUtility.EffTypeToPoolType(ItemEffectType.MagicHourTrail);
                if (delay)
                {
                    BoardUtility.AddAutoReleaseComponent(trail, 0.5f, itemType);
                }
                else
                {
                    GameObjectPoolManager.Instance.ReleaseObject(itemType, trail);
                }
            }
        }

        #endregion
    }
}