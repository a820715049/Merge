/**
 * @Author: zhangpengjian
 * @Date: 2025/4/24 18:18:33
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/24 18:18:33
 * Description: 周任务入口
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIActivityWeeklyTaskEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private Animator _animator;
        private ActivityWeeklyTask _activity;

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
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenRefresh);
            MessageCenter.Get<MSG.ACTIVITY_WEEKLY_TASK_END>().AddListener(WhenEnd);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            RefreshEntry(_activity);
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenRefresh);
            MessageCenter.Get<MSG.ACTIVITY_WEEKLY_TASK_END>().RemoveListener(WhenEnd);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
        }

        public void PlayAnim()
        {
            _animator.SetTrigger("Punch");
        }

        private void FeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.WeeklyTaskEntry)
                return;
            RefreshEntry(_activity);
            _animator.SetTrigger("Punch");
            Game.Manager.audioMan.TriggerSound("WeeklyTaskAccept");
        }

        private void WhenRefresh()
        {
            RefreshEntry(_activity);
        }

        private void WhenEnd()
        {
            Visible(false);
        }

        public void RefreshEntry(ActivityLike activity = null)
        {
            _activity = activity as ActivityWeeklyTask;
            if (_activity == null)
            {
                Visible(false);
                return;
            }

            if (!_activity.Active || _activity.IsComplete())
            {
                Visible(false);
                return;
            }
            Visible(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            _dot.SetActive(_activity.CheckIsShowRedPoint());
        }

        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}