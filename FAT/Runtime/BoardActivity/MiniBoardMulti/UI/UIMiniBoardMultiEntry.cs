/*
 *@Author:chaoran.zhang
 *@Desc:多轮迷你棋盘入口逻辑
 *@Created Time:2025.01.09 星期四 14:43:03
 */

using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIMiniBoardMultiEntry : MonoBehaviour, IActivityBoardEntry
    {
        public GameObject _root;
        private GameObject _redPoint;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _redPointNum;
        private TextMeshProUGUI _progressTxt;
        private Animator _animator;
        private RectTransform _mask;
        private MiniBoardMultiActivity _activity;

        public void Awake()
        {
            transform.Access("Root/cd", out _cd);
            transform.Access("Root/RedPoint/Num", out _redPointNum);
            transform.Access("Root/Progress/ProgressTxt", out _progressTxt);
            _animator = GetComponent<Animator>();
            _root = transform.GetChild(0).gameObject;
            _redPoint = transform.Find("Root/RedPoint").gameObject;
            _mask = transform.Find("Root/Progress/Mask") as RectTransform;
            var button = transform.Find("Root/Bg").GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(EntryClick);
        }

        private void EntryClick()
        {
            if (!Game.Manager.miniBoardMultiMan.IsValid)
                return;
            Game.Manager.miniBoardMultiMan.EnterMiniBoard();
        }

        private void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshEntry);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().AddListener(FeedBack);
            RefreshEntry();
        }

        private void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshEntry);
            MessageCenter.Get<MSG.FLY_ICON_FEED_BACK>().RemoveListener(FeedBack);
        }

        public void RefreshEntry(ActivityLike like)
        {
            _activity = like as MiniBoardMultiActivity;
            RefreshEntry();
        }

        private void RefreshEntry()
        {
            if (_activity == null || _activity.Countdown <= 0 ||
                _activity != Game.Manager.miniBoardMultiMan.CurActivity)
            {
                Visible(false);
                return;
            }

            Visible(true);
            UIUtility.CountDownFormat(_cd, _activity.Countdown);
            RefreshRedPoint();
            RefreshProgress();
        }

        /// <summary>
        /// 刷新红点显示
        /// </summary>
        private void RefreshRedPoint()
        {
            _redPoint.SetActive(false);
            if (!Game.Manager.miniBoardMultiMan.CheckIsShowRedPoint(out var num))
                return;
            _redPoint.SetActive(num > 0);
            _redPointNum.text = num.ToString();
        }

        private void RefreshProgress()
        {
            var max = Game.Manager.miniBoardMultiMan
                .GetTargetIndexInfoConfig(Game.Manager.miniBoardMultiMan.GetCurGroupConfig().InfoId.Count - 1).LevelItem
                .Count;
            var cur = Game.Manager.miniBoardMultiMan.GetCurUnlockItemMaxLevelEntry() + 1;
            _progressTxt.SetTextFormat("{0}/{1}", cur, max);
            _mask.anchorMax = new Vector2((float)cur / max, 1);
        }

        public void FeedBack(FlyableItemSlice item)
        {
            if (item.FlyType != FlyType.MiniBoardMulti)
                return;
            RefreshEntry();
            _animator.SetTrigger("Punch");
        }

        private void Visible(bool v_)
        {
            if (_root.activeSelf == v_) return;
            _root.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }
    }
}