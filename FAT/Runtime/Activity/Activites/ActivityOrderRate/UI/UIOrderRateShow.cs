
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;
using Coffee.UIExtensions;

namespace FAT
{
    public class UIOrderRateShow : UIBase
    {
        [SerializeField] private UIImageRes imgRes;
        [SerializeField] private Transform trailRoot;
        [SerializeField] private GameObject goHitEff;
        [Tooltip("挂接拖尾等待时间")]
        [SerializeField] private float wait_add_trail_time;
        [Tooltip("UI关闭前等待时间")]
        [SerializeField] private float wait_close_time;
        [Tooltip("拖尾飞到目标时间")]
        [SerializeField] private float trail_duration;

        private ActivityOrderRate _actInst;
        private string hit_res_key => $"{_actInst.Id}_eff_hit_feedback";

        // protected override void OnCreate()
        // {
        //     transform.Access<Button>("Mask").onClick.AddListener(Close);
        // }

        protected override void OnParse(params object[] items)
        {
            _actInst = items[0] as ActivityOrderRate;
            var reward = Game.Manager.configMan.GetEventOrderRateBoxConfig(_actInst.GetCurReward());
            if (reward != null)
                imgRes.SetImage(reward.OrderInfo);
        }

        protected override void OnPreOpen()
        {
            Game.Manager.audioMan.TriggerSound("OrderRatePop");
            Prepare();
            Show();
        }

        private void Prepare()
        {
            if (GameObjectPoolManager.Instance.HasPool(hit_res_key))
                return;
            GameObjectPoolManager.Instance.PreparePool(hit_res_key, goHitEff);
        }

        private void Show()
        {
            StartCoroutine(CoShowNextRound());
        }

        private IEnumerator CoShowNextRound()
        {
            // 等待添加拖尾
            yield return new WaitForSeconds(wait_add_trail_time);
            using var _ = PoolMapping.PoolMappingAccess.Borrow<List<IOrderData>>(out var orderList);
            Game.Manager.mainOrderMan.FillActiveOrders(orderList, (int)OrderProviderTypeMask.All);
            foreach (var order in orderList)
            {
                if (order.RateId > 0)
                {
                    // 订单有好评奖励 则可以添加拖尾
                    MessageCenter.Get<MSG.UI_ORDER_QUERY_TRANSFORM_BY_ORDER>().Dispatch(order, trans => TrailToOrder(trans));
                }
            }
            // 等待关闭
            Game.Manager.audioMan.TriggerSound("OrderRateShoot");
            yield return new WaitForSeconds(wait_close_time);
            Close();
        }

        private void TrailToOrder(Transform orderTrans)
        {
            var targetTrans = orderTrans.Find("ExtraReward") as RectTransform;
            if (targetTrans.childCount > 0)
            {
                targetTrans = targetTrans.GetChild(0) as RectTransform;
            }
            var targetPos = targetTrans.TransformPoint(UIUtility.GetLocalCenterInRect(targetTrans));
            var trail_res_key = _actInst.order_trail_key;
            // 挂接拖尾
            GameObjectPoolManager.Instance.CreateObject(trail_res_key, trailRoot, trail =>
            {
                trail.gameObject.SetActive(false);
                trail.transform.localPosition = Vector3.zero;
                trail.transform.position = trailRoot.position;
                var par = trail.transform.Access<UIParticle>("particle");
                trail.gameObject.SetActive(true);
                trail.GetOrAddComponent<MBAutoRelease>().Setup(trail_res_key, trail_duration + .5f);
                // 显示head粒子
                trail.transform.DOMove(targetPos, trail_duration).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    // 飞到目标后添加反馈特效
                    var effRoot = UIManager.Instance.GetLayerRootByType(UILayer.Effect);
                    var hitEff = GameObjectPoolManager.Instance.CreateObject(hit_res_key, effRoot);
                    hitEff.SetActive(false);
                    hitEff.transform.position = targetPos;
                    hitEff.SetActive(true);
                    var script = hitEff.GetOrAddComponent<MBAutoRelease>();
                    script.Setup(hit_res_key, 1f);
                });
            });
        }
    }
}