/*
 *@Author:chaoran.zhang
 *@Desc:弹珠活动入口表现逻辑
 *@Created Time:2024.12.17 星期二 16:46:10
 */

using System;
using System.Collections;
using Cysharp.Text;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

namespace FAT
{
    public class UIPachinkoEntry : MonoBehaviour, IActivityBoardEntry
    {
        private GameObject _group;
        private GameObject _redPoint;
        private TextMeshProUGUI _cd;
        private TextMeshProUGUI _count;
        private TextMeshProUGUI _addNum;
        private TextMeshProUGUI _addNumShow;
        private MBRewardProgress _progress;
        private MBRewardIcon _reward;
        private Animator _animator;
        private Animator _progressAnim;
        private Animator _scoreAnim;
        private Animator _addNumAnim;
        private Animator _redPointAnim;

        private int _curAddNum; //当前动画表现中，增加的积分数量
        private bool _isShowNum; //当前是否在展示积分数量
        private float _showTime; //当前展示积分的剩余时间
        private (int, int) _curProgress;

        public void Awake()
        {
            InitComp();
            var button = _group.GetComponent<Button>().WithClickScale().FixPivot();
            button.onClick.AddListener(OnClick);
        }

        /// <summary>
        /// 获取各种需要的组件
        /// </summary>
        private void InitComp()
        {
            _group = transform.Find("group").gameObject;
            _redPoint = transform.Find("group/redTarget/dotCountGlow").gameObject;
            transform.Access("group/cdBg/cd", out _cd);
            transform.Access("group/redTarget/dotCountGlow/redNum", out _count);
            transform.Access("group/progress/addNum", out _addNum);
            transform.Access("group/progress/addNumShow", out _addNumShow);
            transform.Access("group/progress", out _progress);
            transform.Access("group/rewardbg", out _reward);
            _animator = transform.GetComponent<Animator>();
            transform.Access("group/progress/mask", out _progressAnim);
            transform.Access("group/icon", out _scoreAnim);
            transform.Access("group/progress/addNum", out _addNumAnim);
            transform.Access("group/redTarget/dotCountGlow", out _redPointAnim);
        }

        private void OnClick()
        {
            if (!Game.Manager.pachinkoMan.Valid) return;
            Game.Manager.pachinkoMan.EnterMainScene();
        }

        public void OnEnable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(RefreshCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().AddListener(PlayGetRewardAnim);
            MessageCenter.Get<MSG.PACHINKO_SCORE_UPDATE>().AddListener(UpdateScore);
        }

        public void OnDisable()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(RefreshCD);
            MessageCenter.Get<MSG.UI_REWARD_FEEDBACK>().RemoveListener(PlayGetRewardAnim);
            MessageCenter.Get<MSG.PACHINKO_SCORE_UPDATE>().RemoveListener(UpdateScore);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            if (!Game.Manager.pachinkoMan.Valid)
            {
                Visible(false);
                return;
            }

            if (!_group.activeSelf) Visible(true);
            RefreshCD();
            RefreshRedPoint();
            RefreshReward();
            _curProgress = Game.Manager.pachinkoMan.GetScoreProgress();
            _progress.Refresh(_curProgress.Item1, _curProgress.Item2);
        }

        /// <summary>
        /// 刷新倒计时文本
        /// </summary>
        private void RefreshCD()
        {
            var time = Game.Manager.pachinkoMan.GetActivity()?.Countdown ?? 0;
            UIUtility.CountDownFormat(_cd, time);
            if (time <= 0) Visible(false);
        }

        /// <summary>
        /// 刷新红点显示
        /// </summary>
        private void RefreshRedPoint()
        {
            if (!Game.Manager.pachinkoMan.Valid) return;
            var count = Game.Manager.pachinkoMan.GetCoinCount();
            _redPoint.SetActive(count != 0);
            _count.SetRedPoint(count);
        }

        /// <summary>
        /// 显隐接口
        /// </summary>
        /// <param name="v_"></param>
        private void Visible(bool v_)
        {
            _group.SetActive(v_);
            transform.GetComponent<LayoutElement>().ignoreLayout = !v_;
        }

        /// <summary>
        /// 获得奖励时的动画表现
        /// </summary>
        /// <param name="ft"></param>
        private void PlayGetRewardAnim(FlyType ft)
        {
            if (ft != FlyType.Pachinko) return;
            _progress.Refresh(0, _curProgress.Item2);
            _redPointAnim.SetTrigger("Punch");
            _animator.SetTrigger("Punch");
            _progress.Refresh(_curProgress.Item1, _curProgress.Item2, 0.75f);
            RefreshRedPoint();
            RefreshReward();
        }

        /// <summary>
        /// 更新获得数量显示
        /// </summary>
        /// <param name="num"></param>
        private void UpdateScore(int num)
        {
            _curAddNum += num;
            _showTime = 0.5f;
            _addNumShow.SetTextFormat("+{0}", _curAddNum);
            if (_isShowNum) return;
            _isShowNum = true;
            _addNumShow.gameObject.SetActive(true);
        }

        /// <summary>
        /// 更新进度条状态
        /// </summary>
        private void UpdateProgress()
        {
            _curProgress.Item1 += _curAddNum;
            _progress.Refresh(_curProgress.Item1, _curProgress.Item2, 0.75f);
            var commit = Game.Manager.pachinkoMan.GetScoreRewardCommit();
            _scoreAnim.SetTrigger("Punch");
            _progressAnim.SetTrigger("Punch");
            if (commit == null) return;
            MessageCenter.Get<MSG.SCORE_FLY_REWARD_CENTER>().Dispatch((_reward.icon.transform.position, commit,
                Game.Manager.pachinkoMan.GetActivity()));
            _curProgress = Game.Manager.pachinkoMan.GetScoreProgress();
            Game.Manager.pachinkoMan.SetScoreCommit(null);
        }

        /// <summary>
        /// 更新里程碑奖励
        /// </summary>
        private void RefreshReward()
        {
            var reward = Game.Manager.pachinkoMan.GetScoreReward();
            if (reward != null) _reward.Refresh(reward);
        }

        private void Update()
        {
            if (!_isShowNum) return;
            _showTime -= Time.deltaTime;
            if (_showTime > 0) return;
            _isShowNum = false;
            _addNum.SetTextFormat("+{0}", _curAddNum);
            _addNumAnim.SetTrigger("Punch");
            _addNumShow.gameObject.SetActive(false);
            UpdateProgress();
            _curAddNum = 0;
        }
    }
}