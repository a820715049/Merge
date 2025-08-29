using EL;
using TMPro;
using UnityEngine;
namespace FAT
{
    public class UIMineCartEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private GameObject _redPoint;
        [SerializeField] private Animator _animator;

        private MineCartActivity _activity;

        private void Awake()
        {
            transform.AddButton("Root", EntryClick);
        }

        private void EntryClick()
        {
            _activity?.Open();
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
            if (activity == null || activity.Type != fat.rawdata.EventType.MineCart) return;
            _activity = activity as MineCartActivity;
            if (_activity == null) return;
            if (_activity.Countdown <= 0)
            {
                _root.SetActive(false);
                return;
            }
            _root.SetActive(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            RefreshRedPoint();
        }

        private void RefreshCD()
        {
            if (_activity == null) return;
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
        }
        /// <summary>
        /// 刷新红点显示
        /// </summary>
        private void RefreshRedPoint()
        {
            _redPoint.SetActive(_activity.World.rewardCount > 0);
            _num.SetRedPoint(_activity.World.rewardCount);
        }
        private void FeedBack(FlyableItemSlice slice)
        {
            if (_activity == null) return;
            if (slice.FlyType != FlyType.MineCartGetItem) return;
            if (slice.CurIdx != 1) return;
            _num.SetRedPoint(_activity.World.rewardCount);
            _redPoint.SetActive(_activity.World.rewardCount > 0);
            _animator.SetTrigger("Punch");
        }
    }
}
