/**
 * @Author: zhangpengjian
 * @Date: 2025/6/30 14:12:13
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/6/30 14:12:13
 * Description: 连续订单活动入口
 */

using EL;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBActivityOrderStreakEntry : MonoBehaviour
    {
        [SerializeField] private Button btn;
        [SerializeField] private MBBoardOrder orderInst;
        [SerializeField] private Animation anim;
        public MBRewardProgress bar;
        public Animator animator;
        private bool isEffectShowing;

        private void Awake()
        {
            btn.onClick.AddListener(_OnBtnGoToActivity);
        }

        private void Update()
        {
            if (IsOrderCanCommit(orderInst.data))
            {
                if (!isEffectShowing)
                {
                    RefreshEffect();
                }
            }
            else
            {
                if (isEffectShowing)
                {
                    RefreshEffect();
                }
            }
        }

        private void RefreshEffect()
        {
            isEffectShowing = IsOrderCanCommit(orderInst.data);
            if (isEffectShowing)
                anim.Play();
            else
                anim.Stop();
        }

        private bool IsOrderCanCommit(IOrderData order)
        {
            if (orderInst.data == null)
                return false;
            return order.State == OrderState.Rewarded || order.State == OrderState.Finished;
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().AddListener(ActivityRefresh);
            var act = (ActivityOrderStreak)Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderStreak);
            ActivityRefresh(act);
            // animator.Play("UIOrderItemOrderDash_Punch");
            RefreshEffect();
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.ACTIVITY_REFRESH>().RemoveListener(ActivityRefresh);
        }

        private void _OnBtnGoToActivity()
        {
            var act = (ActivityOrderStreak)Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderStreak);
            act?.Open();
        }

        public void ActivityRefresh(ActivityLike acti_)
        {
            if (acti_ is not ActivityOrderStreak act) return;
            bar.Refresh(act.OrderIdx, act.OrderList.Count);
        }
    }
}