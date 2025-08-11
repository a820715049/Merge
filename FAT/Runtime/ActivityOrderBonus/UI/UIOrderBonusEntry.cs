using Cysharp.Text;
using DG.Tweening;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderBonusEntry : MonoBehaviour, IActivityBoardEntry
    {
        public Animator animator;
        public TextMeshProUGUI cd;
        public GameObject readPoint;
        public RectMask2D mask;
        public TextMeshProUGUI txtCD;
        public TextMeshProUGUI progressTxt;
        private ActivityOrderBonus _actInst;
        private int CurIdle;
        private void Start()
        {
            var btn = transform.Find("Root/Bg").GetComponent<Button>();
            btn.WithClickScale().onClick.AddListener(OnBtnClick);
        }

        private void OnBtnClick()
        {
            if (_actInst.needRedPoint)
            {
                _actInst.needRedPoint = false;
                readPoint.SetActive(false);
            }
            _actInst.Open();
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.ORDER_BONUS_PHASE_CHANGE>().AddListener(ChangePhase);
            if (animator.GetInteger("Idle") != CurIdle)
            {
                animator.SetInteger("Idle", CurIdle);
            }
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.ORDER_BONUS_PHASE_CHANGE>().RemoveListener(ChangePhase);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            _actInst = activity as ActivityOrderBonus;
            readPoint.SetActive(_actInst.needRedPoint);
            RefreshProgress();
            RefreshCD();
            ResolveIdleAnim();
        }

        private void RefreshProgress()
        {
            mask.padding = new Vector4(0, 0, _actInst.phase >= 3 ? 0 : 43 * (3 - _actInst.phase), 0);
            progressTxt.text = ZString.Format("{0}/{1}", _actInst.phase > 3 ? 3 : _actInst.phase, 3);
        }

        private void RefreshCD()
        {
            txtCD.text = UIUtility.CountDownFormat(_actInst.Countdown);
        }

        private void ResolveIdleAnim()
        {
            CurIdle = _actInst.phase > 3 ? 3 : _actInst.phase;
            animator.SetInteger("Idle", CurIdle);
        }

        private void ChangePhase()
        {
            if (_actInst.phase > CurIdle)
                PhaseUp();
            else
                PhaseDown();
        }

        private void PhaseUp()
        {
            MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().Dispatch(transform);
            if (CurIdle == 3) return;
            if (CurIdle == 0)
                animator.SetTrigger("Conversion01");
            else if (CurIdle == 1)
                animator.SetTrigger("Conversion02");
            else if (CurIdle == 2)
                animator.SetTrigger("Conversion03");
            DOTween.To(value =>
            {
                mask.padding = new Vector4(0, 0, value, 0);
            }, 43 * (4 - _actInst.phase), _actInst.phase >= 3 ? 0 : 43 * (3 - _actInst.phase), 1f).onComplete += () =>
            {
                animator.SetInteger("Idle", CurIdle);
            };
            progressTxt.text = ZString.Format("{0}/{1}", _actInst.phase > 3 ? 3 : _actInst.phase, 3);
            CurIdle++;
            Game.Manager.audioMan.TriggerSound("OrderBonusUp");
        }

        private void PhaseDown()
        {
            animator.SetInteger("Idle", 0);
            if (CurIdle == 1)
                animator.SetTrigger("Return01");
            else if (CurIdle == 2)
                animator.SetTrigger("Return02");
            else if (CurIdle == 3)
                animator.SetTrigger("Return03");
            mask.padding = new Vector4(0, 0, 128, 0);
            CurIdle = 0;
            progressTxt.text = "0/3";
            Game.Manager.audioMan.TriggerSound("OrderBonusDown");
            readPoint.SetActive(true);
            MessageCenter.Get<MSG.UI_NEWLY_FINISHED_ORDER_SHOW>().Dispatch(transform);
        }
    }
}