// ================================================
// File: UIFarmBoardEntry.cs
// Author: yueran.li
// Date: 2025/05/12 16:17:39 星期一
// Desc: 农场棋盘入口
// ================================================


using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIFarmBoardEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private Animator _animator;

        private FarmBoardActivity _activity;
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
            ActivityTransit.Enter(_activity, _activity.VisualLoading, _activity.VisualBoard.res);
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
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
            _activity = activity as FarmBoardActivity;
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
            Visible(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);

            if (_activity.TokenNum > 0)
            {
                _dot.SetActive(true);
                _num.text = _activity.TokenNum.ToString();
            }
            else
            {
                _dot.SetActive(false);
            }
        }

        public void FeedBack(FlyableItemSlice slice)
        {
            if (_activity == null)
            {
                return;
            }

            if (slice.FlyType != FlyType.FarmToken)
            {
                return;
            }

            RefreshEntry(_activity);
            PlayPunch();
        }

        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}