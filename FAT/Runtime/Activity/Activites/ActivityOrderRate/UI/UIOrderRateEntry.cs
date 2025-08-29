using System.Collections;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderRateEntry : MonoBehaviour, IActivityBoardEntry
    {

        [SerializeField] private UICommonProgressBar progressBar;
        [SerializeField] private TextMeshProUGUI txtCD;
        [SerializeField] private Animator animator;
        [SerializeField] private TextMeshProUGUI txtToken;
        [SerializeField] private Animator mask;
        [SerializeField] private Animator punch;

        private ActivityOrderRate _actInst;
        private bool _show;

        private void Start()
        {
            var btn = GetComponent<Button>();
            btn.WithClickScale().onClick.AddListener(OnBtnClick);
        }

        private void OnBtnClick()
        {
            _actInst.Open();
        }

        private void OnEnable()
        {
            _show = true;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            if (_actInst != null)
            {
                RefreshProgress();
                RefreshCD();
                ResolveIdleAnim();
            }
        }

        private void OnDisable()
        {
            _show = false;
            txtToken.text = "";
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            _actInst = activity as ActivityOrderRate;
            RefreshProgress();
            RefreshCD();
            ResolveIdleAnim();
        }

        private void RefreshProgress()
        {
            var phase = _actInst.GetCurReward() / 3f * _actInst.Reward3.Item2 + _actInst.Reward3.Item2 / 3f * (_actInst.phase - _actInst.GetLastMile()) / (_actInst.GetCurRewardInfo().Item2 - _actInst.GetLastMile());
            if (_actInst.GetCurReward() == 3) phase = _actInst.Reward3.Item2;
            progressBar.ForceSetup(0, _actInst.Reward3.Item2, (long)phase);
        }

        private void RefreshCD()
        {
            UIUtility.CountDownFormat(txtCD, _actInst.Countdown);
        }

        private void ResolveIdleAnim()
        {
            txtToken.color = new Color(1, 1, 1, 0);
        }

        private void FeedBack(FlyableItemSlice feedBack)
        {
            if (feedBack.FlyType != FlyType.Coin || feedBack.CurIdx != 1 || feedBack.Reward.reason == ReasonString.sell_item)
            {
                return;
            }
            if (!_show) return;
            var phase = _actInst.GetCurReward() / 3f * _actInst.Reward3.Item2 + _actInst.Reward3.Item2 / 3f * (_actInst.phase - _actInst.GetLastMile()) / (_actInst.GetCurRewardInfo().Item2 - _actInst.GetLastMile());
            if (_actInst.GetCurReward() == 3) phase = _actInst.Reward3.Item2;
            progressBar.SetProgress((long)phase);
            animator.SetTrigger("Punch");
            txtToken.text = ZString.Concat("+", feedBack.Amount);
            punch.SetTrigger("Punch");
            mask.SetTrigger("Punch");
        }
    }
}