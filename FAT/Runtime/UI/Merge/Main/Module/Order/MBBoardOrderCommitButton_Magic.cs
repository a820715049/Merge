/*
 * @Author: qun.chao
 * @Description: 星想事成订单完成按钮
 * @Doc: https://centurygames.feishu.cn/wiki/NavRwNhCmiAwTskLP1mcDU0tnXd
 * @Date: 2025-01-17 15:39:45
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using Spine.Unity;
using DG.Tweening;
using FAT.Merge;

namespace FAT
{
    public class MBBoardOrderCommitButton_Magic : MonoBehaviour, MBBoardOrder.ICommitButton
    {
        [System.Serializable]
        public class Effect
        {
            public SkeletonGraphic[] spines;
            [Tooltip("boom效果延迟")]
            public float delayBoom;
            public GameObject goBoom;
            [Tooltip("浮起前等待时间")]
            public float trail_duration_wait_pop = 0.5f;
            [Tooltip("从帽子中浮起来的时间")]
            public float trail_duration_pop = 0.5f;
            [Tooltip("落到棋盘前的等待时间")]
            public float trail_duration_wait = 0.5f;
            [Tooltip("落到棋盘的时间")]
            public float trail_duration_drop = 0.5f;
            [Tooltip("飞回订单前的等待时间")]
            public float trail_duration_wait_order = 0.5f;
            [Tooltip("飞回订单的时间")]
            public float trail_duration_order = 0.5f;
            public Transform trailRoot;
            public Transform trailStartRef;
            public Transform trailEndRef;

            public void Reset()
            {
                PlayIdle();
                goBoom.SetActive(false);
            }

            public void PlayIdle()
            {
                foreach (var anim in spines)
                {
                    anim.AnimationState.SetAnimation(0, "idle", true);
                }
            }

            public void PlayShow()
            {
                foreach (var anim in spines)
                {
                    anim.AnimationState.SetAnimation(0, "show", false);
                    anim.AnimationState.AddAnimation(0, "idle", true, 0f);
                }
            }
        }

        [SerializeField] private Button btnFinish;
        [SerializeField] private Button btnFinishOld;
        [SerializeField] private Effect effGroup;

        private RectTransform progressFill;
        private TextMeshProUGUI txtBtnDesc;

        Button MBBoardOrder.ICommitButton.BtnCommit => UIUtility.ABTest_OrderItemChecker() ? btnFinish : btnFinishOld;
        private Button btnCommit => (this as MBBoardOrder.ICommitButton).BtnCommit;
        private IOrderData order;
        private int countdownSec;

        void MBBoardOrder.ICommitButton.OnDataChange(IOrderData data)
        {
            order = data;
            countdownSec = -1;
            progressFill = btnCommit.transform.Access<RectTransform>("Progress/Fill");
            txtBtnDesc = btnCommit.transform.Access<TextMeshProUGUI>("Text");
            effGroup.Reset();
        }

        void MBBoardOrder.ICommitButton.OnDataClear()
        {
            order = null;
            countdownSec = -1;
        }

        void MBBoardOrder.ICommitButton.Refresh()
        {
            var btn = btnCommit;
            btn.gameObject.SetActive(order.State != OrderState.Rewarded);
            btn.GetComponent<UIImageState>().Enabled(order.State == OrderState.Finished);
            txtBtnDesc.GetComponent<UITextState>().Enabled(order.State == OrderState.Finished);
        }

        void MBBoardOrder.ICommitButton.RefreshOffset(bool isExtraReward)
        { }

        public float ShowMagicOutputEffect(Item targetItem, IOrderData targetOrder)
        {
            UIMagicHourHelper.ShowMagicHourOutputEffect(effGroup, targetItem, targetOrder);
            // 等待必要的时间, 避免奖励还没发订单就开始播放消失动画
            return effGroup.trail_duration_wait_pop + effGroup.trail_duration_pop + effGroup.trail_duration_wait;
        }

        private void Update()
        {
            if (order == null || !order.Displayed)
                return;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            // TODO: 更合理的主界面判断方式
            if (!Game.Manager.guideMan.IsMatchUIState((int)GuideRequireChecker.UIState.BoardMain, 0))
                return;
            if (UIManager.Instance.GetLayerRootByType(UILayer.SubStatus).childCount > 0)
                return;

            var curMilli = order.MagicHourTimeLifeMilli + (int)(Time.deltaTime * 1000);
            var endMilli = order.MagicHourTimeDurationMilli;

            curMilli = Mathf.Min(endMilli, curMilli);
            order.MagicHourTimeLifeMilli = curMilli;

            // 进度条每秒走一轮
            var cd = Mathf.Max(0, endMilli - curMilli);
            progressFill.anchorMax = new Vector2(cd % 1000 / 1000f, 1f);

            var cdSec = cd / 1000;
            if (cdSec != countdownSec)
            {
                countdownSec = cdSec;
                txtBtnDesc.SetText(I18N.FormatText("#SysComDesc817", cdSec));
            }
            if (cd <= 0 && order.State != OrderState.Expired)
            {
                // 触发刷新移除订单
                Game.Manager.mergeBoardMan.activeTracer.Invalidate();
            }
        }
    }
}
