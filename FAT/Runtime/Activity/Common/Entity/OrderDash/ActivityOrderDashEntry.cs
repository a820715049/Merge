
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class ActivityOrderDashEntry : MonoBehaviour
    {
        [SerializeField] private Button btn;
        [SerializeField] private MBBoardOrder orderInst;
        [SerializeField] private Animation anim;
        [SerializeField] private GameObject effect;
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
            var act = (ActivityOrderDash)Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderDash);
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
            var act = (ActivityOrderDash)Game.Manager.activity.LookupAny(fat.rawdata.EventType.OrderDash);
            act?.Open();
        }

        public void ActivityRefresh(ActivityLike acti_) {
            if (acti_ is not ActivityOrderDash act) return;
            bar.Refresh(act.orderIndex, act.orderTotal);
        }
    }
}