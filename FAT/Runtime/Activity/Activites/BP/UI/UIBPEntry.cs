// ==================================================
// // File: UIBPEntry.cs
// // Author: liyueran
// // Date: 2025-06-23 17:06:37
// // Desc: bp活动棋盘入口
// // ==================================================

using System.Collections.Generic;
using EL;
using FAT.MSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIBPEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private RectTransform _mask;
        [SerializeField] private TextMeshProUGUI _count;
        [SerializeField] private Animator _animator;
        private BPActivity _activity;

        public void Start()
        {
            var button = transform.Find("Root/Bg").GetComponent<Button>();
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
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            MessageCenter.Get<MSG.GAME_BP_TASK_STATE_CHANGE>().AddListener(_OnTaskStateChange);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(WhenEnd);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
            MessageCenter.Get<MSG.GAME_BP_TASK_STATE_CHANGE>().RemoveListener(_OnTaskStateChange);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(WhenEnd);
        }

        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as BPActivity;
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
            //刷新红点
            _RefreshRP();
        }

        private void _OnTaskStateChange()
        {
            _RefreshRP();
        }

        private void _RefreshRP()
        {
            if (_activity == null)
                return;

            // 每日刷新任务的完成进度
            var (completeCount, totalCount) = _activity.CheckDailyTaskCount();
            if (completeCount > 0) // 是否有完成的任务
            {
                _mask.anchorMax = new Vector2(completeCount * 1f / totalCount, 1);
                _count.text = $"{completeCount}/{totalCount}";
            }
            else
            {
                _mask.anchorMax = new Vector2(0, 1);
                _count.text = $"{0}/{totalCount}";
            }

            //红点
            var showRed = _activity.CheckCanShowRP(out var num);
            _dot.SetActive(showRed);
            _num.SetText(num <= 99 ? num.ToString() : "99+");
        }

        private void FeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.BPExp)
            {
                return;
            }

            RefreshEntry(_activity);
            _animator.SetTrigger("Punch");
        }

        private void Visible(bool active)
        {
            _root.SetActive(active);
            transform.GetComponent<LayoutElement>().ignoreLayout = !active;
        }
        
        private void WhenEnd(ActivityLike act, bool expire)
        {
            Visible(false);
        }
    }
}