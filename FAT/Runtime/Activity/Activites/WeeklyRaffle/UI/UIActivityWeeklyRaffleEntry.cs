// ==================================================
// File: UIActivityWeeklyRaffleEntry.cs
// Author: liyueran
// Date: 2025-06-03 19:06:15
// Desc: $签到抽奖 棋盘入口
// ==================================================

using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityWeeklyRaffleEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private Animator _animator;

        private ActivityWeeklyRaffle _activity;
        private readonly int Punch = Animator.StringToHash("Punch");

        public void Start()
        {
            var button = transform.Find("Root/Bg").GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            if (!_activity.Active)
                return;
            _activity.Open();
        }

        private void OnEnable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
        }

        public void PlayPunch()
        {
            _animator.SetTrigger(Punch);
        }

        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as ActivityWeeklyRaffle;
            if (_activity == null)
            {
                Visible(false);
                return;
            }

            if (!_activity.Active)
            {
                Visible(false);
                return;
            }

            if (!_activity.CheckIsRaffleDay())
            {
                Visible(false);
                return;
            }

            Visible(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);

            if (_activity.TokenNum > 0)
            {
                _dot.SetActive(true);
                _num.SetRedPoint(_activity.TokenNum);
            }
            else
            {
                _dot.SetActive(false);
            }
        }


        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}