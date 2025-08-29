/**
 * @Author: zhangpengjian
 * @Date: 2025/3/26 11:25:17
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/3/26 11:25:17
 * Description: 钓鱼棋盘入口
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityFishEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private RectTransform _mask;
        [SerializeField] private TextMeshProUGUI _count;
        [SerializeField] private Animator _animator;
        private ActivityFishing _activity;

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

        public void PlayAnim()
        {
            _animator.SetTrigger("Punch");
        }

        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as ActivityFishing;
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
            _mask.anchorMax = new Vector2(((float)_activity.CurToken) /
                    _activity.MaxToken, 1);
            _count.text = $"{_activity.CurToken}/{_activity.MaxToken}";
            if (_activity.CheckIsShowRedPoint(out var num))
            {
                if (num > 0)
                {
                    _dot.SetActive(true);
                    _num.SetRedPoint(num);
                }
                else
                {
                    _dot.SetActive(false);
                }
            }
        }

        public void FeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.FishBoardEntry)
                return;
            RefreshEntry(_activity);
            _animator.SetTrigger("Punch");
        }

        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}