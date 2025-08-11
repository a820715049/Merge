/**
 * @Author: zhangpengjian
 * @Date: 2025/5/13 19:13:23
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/5/13 19:13:23
 * Description: 打怪棋盘棋盘入口
 */

using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

namespace FAT
{
    public class UIActivityFightEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _dot;
        [SerializeField] private TextMeshProUGUI _num;
        [SerializeField] private TextMeshProUGUI _cd;
        [SerializeField] private Animator _animator;
        [SerializeField] private SkeletonGraphic _monsterSpine;
        private FightBoardActivity _activity;

        public void Start()
        {
            var button = transform.Find("Root/Bg").GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            if (!_activity.Active)
                return;
            ActivityTransit.Enter(_activity, _activity.LoadingRes, _activity.BoardRes.res);
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
            _activity = activity as FightBoardActivity;
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
            _monsterSpine.AnimationState.SetAnimation(0, "idle", true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            if (_activity.CheckIsShowRedPoint(out var num))
            {
                if (num > 0)
                {
                    _dot.SetActive(true);
                    _num.text = num.ToString();
                }
                else
                {
                    _dot.SetActive(false);
                }
            }
        }

        public void FeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.FightBoardEntry)
                return;
            RefreshEntry(_activity);
            _monsterSpine.AnimationState.SetAnimation(0, "punch", false).Complete += delegate (TrackEntry entry)
            {
                _monsterSpine.AnimationState.SetAnimation(0, "idle", true);
            };
            _animator.SetTrigger("Punch");
        }

        private void Visible(bool v_)
        {
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}