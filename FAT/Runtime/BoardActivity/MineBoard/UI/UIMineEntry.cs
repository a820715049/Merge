using EL;
using TMPro;
using UnityEngine;
namespace FAT
{
    public class UIMineEntry : MonoBehaviour, IActivityBoardEntry
    {
        private GameObject _root;
        private TextMeshProUGUI _num;
        private TextMeshProUGUI _cd;
        private GameObject _redPoint;
        private Animator _animator;
        private MineBoardActivity _activity;

        private void Awake()
        {
            _root = transform.Find("Root").gameObject;
            _num = transform.Find("Root/RedPoint/Num").GetComponent<TextMeshProUGUI>();
            _cd = transform.Find("Root/cd").GetComponent<TextMeshProUGUI>();
            _redPoint = transform.Find("Root/RedPoint").gameObject;
            _animator = transform.GetComponent<Animator>();
            transform.AddButton("Root/Bg", EntryClick);
        }

        private void EntryClick()
        {
            Game.Manager.mineBoardMan.EnterMineBoard();
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null || activity.Type != fat.rawdata.EventType.Mine) return;
            _activity = activity as MineBoardActivity;
            if (_activity == null) return;
            if (_activity.Countdown <= 0)
            {
                _root.SetActive(false);
                return;
            }
            _root.SetActive(true);
            _num.text = _activity.GetTokenNum().ToString();
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
            if (slice.FlyType != FlyType.MineToken) return;
            if (slice.CurIdx != 1) return;
            _num.text = _activity.GetTokenNum().ToString();
            _redPoint.SetActive(_activity.GetTokenNum() > 0);
            _animator.SetTrigger("Punch");
        }
    }
}