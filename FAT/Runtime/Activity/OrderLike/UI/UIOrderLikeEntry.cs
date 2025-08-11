/*
 * @Author: qun.chao
 * @Date: 2025-03-25 18:42:26
 */
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;
using Cysharp.Threading.Tasks;
using Coffee.UIExtensions;

namespace FAT
{
    public class UIOrderLikeEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private Animator animator;
        [SerializeField] private UIParticle feedbackEff;

        private ActivityOrderLike _actInst;

        private void Start()
        {
            var btn = GetComponent<Button>();
            btn.WithClickScale().onClick.AddListener(OnBtnClick);
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.ORDERLIKE_TOKEN_CHANGE>().AddListener(OnMessageOrderLikeTokenChange);
            MessageCenter.Get<MSG.ORDERLIKE_ROUND_CHANGE>().AddListener(OnMessageOrderLikeRoundChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(OnMessageOnePass);
            if (_actInst != null)
            {
                RefreshProgress();
                RefreshCD();
                ResolveIdleAnim();
            }
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.ORDERLIKE_TOKEN_CHANGE>().RemoveListener(OnMessageOrderLikeTokenChange);
            MessageCenter.Get<MSG.ORDERLIKE_ROUND_CHANGE>().RemoveListener(OnMessageOrderLikeRoundChange);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(OnMessageOnePass);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            _actInst = activity as ActivityOrderLike;
            RefreshProgress();
            RefreshCD();
            ResolveIdleAnim();
        }

        private void RefreshProgress()
        {
            progressBar.ForceSetup(0, _actInst.MaxToken, _actInst.DisplayToken);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, _actInst.Countdown);
        }

        private void OnBtnClick()
        {
            _actInst.Open();
        }

        private void ResolveIdleAnim()
        {
            var ready = _actInst.DisplayToken >= _actInst.MaxToken;
            animator.SetBool("Ready", ready);
        }

        private void PlayAnim_Collect()
        {
            UIUtility.ManuallyEmitParticle(feedbackEff);
            animator.SetTrigger("Collect");
        }

        private void PlayAnim_ChangeToReady()
        {
            animator.SetTrigger("ChangeToReady");
        }

        private void PlayAnim_Spawn()
        {
            animator.SetTrigger("Spawn");
        }

        private void OnMessageOrderLikeTokenChange()
        {
            progressBar.SetProgress(_actInst.DisplayToken);
            var cur = _actInst.DisplayToken;
            var max = _actInst.MaxToken;
            ResolveIdleAnim();
            if (cur >= max)
            {
                PlayAnim_ChangeToReady();
            }
            else
            {
                PlayAnim_Collect();
            }
            Game.Manager.audioMan.TriggerSound("OrderLikeToken");
        }

        private void OnMessageOrderLikeRoundChange()
        {
            RefreshProgress();
            ResolveIdleAnim();
            PlayAnim_Spawn();
        }

        private void OnMessageOnePass()
        {
            RefreshCD();
        }
    }
}