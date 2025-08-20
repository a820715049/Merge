/*
 * @Author: yanfuxing
 * @Date: 2025-06-13 11:30:05
 */
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIWishBoardEntry : MonoBehaviour, IActivityBoardEntry
    {
        private GameObject _root;
        private TextMeshProUGUI _num;
        private TextMeshProUGUI _cd;
        private GameObject _redPoint;
        private Animator _animator;
        private WishBoardActivity _activity;
        void Awake()
        {
            _root = transform.Find("Root").gameObject;
            _num = transform.Find("Root/dotCount/Count").GetComponent<TextMeshProUGUI>();
            _cd = transform.Find("Root/cd").GetComponent<TextMeshProUGUI>();
            _redPoint = transform.Find("Root/dotCount").gameObject;
            _animator = transform.GetComponent<Animator>();
            transform.AddButton("Root/Bg", EntryClick);
        }

        void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null || activity.Type != fat.rawdata.EventType.WishBoard) return;
            _activity = activity as WishBoardActivity;
            if (_activity == null) return;
            if (_activity.Countdown <= 0)
            {
                _root.SetActive(false);
                return;
            }
            _root.SetActive(true);
            _num.SetRedPoint(_activity.GetTokenNum());
            _redPoint.SetActive(_activity.GetTokenNum() > 0);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
        }

        private void FeedBack(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType != FlyType.WishBoardToken) return;
            if (slice.CurIdx != 1) return;
            _num.SetRedPoint(_activity.GetTokenNum());
            _redPoint.SetActive(_activity.GetTokenNum() > 0);
            _animator.SetTrigger("Punch");
        }
        private void EntryClick()
        {
            _activity.EnterWishBoard();
        }
        void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
        }
    }
}