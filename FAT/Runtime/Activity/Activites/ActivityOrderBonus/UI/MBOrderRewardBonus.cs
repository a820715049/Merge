using Cysharp.Threading.Tasks.Triggers;
using EL;
using FAT.MSG;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class MBOrderRewardBonus : MonoBehaviour
    {
        public Animator animator;
        public UIImageState uIImageState;
        private int _detailID;
        private int _reward;

        void Awake()
        {
            transform.AddButton("IconRoot/ClickArea", Click);
        }

        private void Click()
        {
            UIManager.Instance.OpenWindow(UIConfig.UIOrderBonusRewardTips, transform.GetChild(0).GetChild(0).position - Vector3.down * 10f, 20, _detailID);
        }

        void OnEnable()
        {
            MessageCenter.Get<MSG.ROCKET_ANIM_COMPLETE>().AddListener(Show);
        }

        void OnDisable()
        {
            MessageCenter.Get<MSG.ROCKET_ANIM_COMPLETE>().RemoveListener(Show);
        }

        public void SetReward(int phase, int detail)
        {
            phase--;
            if (phase > 2) phase = 2;
            uIImageState.Select(phase);
            _reward = phase;
            _detailID = detail;
        }

        public void PlayAnim()
        {
            if (_reward == 0)
                animator.SetTrigger("Green");
            if (_reward == 1)
                animator.SetTrigger("Blue");
            if (_reward == 2)
                animator.SetTrigger("Purple");

        }

        public void PlayShowAnim()
        {
            GetComponent<Image>().color = Color.clear;
            uIImageState.transform.parent.localScale = Vector3.zero;
            MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().Dispatch(transform.parent.parent);
        }

        public void Show()
        {
            GetComponent<Image>().color = Color.white;
            uIImageState.transform.parent.localScale = Vector3.one;
        }
    }
}