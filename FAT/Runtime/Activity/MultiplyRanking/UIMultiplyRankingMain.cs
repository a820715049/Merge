/*
 * @Author: yanfuxing
 * @Date: 2025-07-22 15:40:09
 */
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using FAT.MSG;
using static EL.MessageCenter;
using System.Collections.Generic;
using System.Collections;
using EL;
using fat.conf;
using Config;
using Spine.Unity;
using CenturyGame.Framework.UI;

namespace FAT
{
    public class UIMultiplyRankingMain : UIBase
    {
        [SerializeField] private TextMeshProUGUI _cdText;
        [SerializeField] private MapButton _confirmBtn;
        [SerializeField] private MapButton _helpBtn;
        [SerializeField] private MapButton _progressBtn;
        [SerializeField] private MapButton _closeBtn;
        [SerializeField] private Button TurntableBtn;
        [SerializeField] private Transform TurntableParent;
        [SerializeField] private Transform TurntableTipTrans;
        [SerializeField] private Transform TurntableTrans;
        [SerializeField] private Transform TurntableArrow;
        [SerializeField] private UIMultiplyRankingListScroll _scroll;
        [SerializeField] private UIVisualGroup _vGroup;
        [SerializeField] private MBRewardProgress _mBprogress;
        [SerializeField] private MBRewardIcon _mBProgressReward;
        [SerializeField] private Transform _endUITrans;
        [SerializeField] private Transform _mainTopBgTrans;
        [SerializeField] private Transform _topCdTrans;
        [SerializeField] private TextMeshProUGUI _endRankNumText;
        [SerializeField] private MBRewardLayout _endRankReward;
        [SerializeField] private TextMeshProUGUI _rankEndText;
        [SerializeField] private SkeletonGraphic _pandaSpineAnim;
        [SerializeField] private Transform _pandaTrailFx;
        [SerializeField] private Transform _progressRewardFx;
        [SerializeField] private Transform _topRawImageTrans;
        [SerializeField] private Transform _bottomImageTrans;
        [SerializeField] private Animator _progressAnim;
        [SerializeField] private RawImageUVScroll _topRawImageScroll;
        [SerializeField] private Transform _tipPos;
        [SerializeField] private Animator _animatorArrow;

        private Action WhenTick;
        private bool _isInit;
        private IEnumerator _progressAnimCor;
        private RankingOpenType _openUIType;
        private ActivityMultiplierRanking _activity;
        private List<int> _milestoneScoreList = new();
        private List<RewardCommitData> rewardCommitList = new();
        private const float progressBarFillTime = 0.5f;
        private WaitForSeconds _waitTime = new WaitForSeconds(progressBarFillTime);
        private WaitForSeconds _fxWaitTime = new WaitForSeconds(progressBarFillTime / 2f);
        private bool stateShow = false;

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            transform.Access(out _vGroup);
            var root = transform.Find("Content");
            _vGroup.Prepare(root.Access<TextMeshProUGUI>("TitleImg/title"), "mainTitle");
            _vGroup.Prepare(root.Access<TextMeshProUGUI>("TitleImg/Bg/_cd/text"), "time");
            _vGroup.Prepare(root.Access<UIImageRes>("TitleImg/Bg/_cd/frame"), "frame");
            _vGroup.CollectTrim();
        }

        protected override void OnCreate()
        {
            WhenTick ??= RefreshCD;
            _progressBtn.WhenClick = OnClickMilestone;
            _closeBtn.WhenClick = OnClose;
            _helpBtn.WhenClick = HelpClick;
            _confirmBtn.WhenClick = ConfirmClick;
            TurntableBtn.onClick.AddListener(OnTurntableClick);
            _isInit = true;
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length > 0)
            {
                _activity = (ActivityMultiplierRanking)items[0];
                _openUIType = (RankingOpenType)items[1];
                _scroll.InitContext(_activity, _openUIType, _isInit);
            }
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            RefreshList();
            RefreshProgress();
            TurntableParent.gameObject.SetActive(_openUIType == RankingOpenType.Main);
            _endUITrans.gameObject.SetActive(_openUIType == RankingOpenType.End);
            _confirmBtn.gameObject.SetActive(_openUIType == RankingOpenType.End);
            _mainTopBgTrans.gameObject.SetActive(false);
            _topCdTrans.gameObject.SetActive(_openUIType == RankingOpenType.Main);
            _pandaSpineAnim.gameObject.SetActive(_openUIType == RankingOpenType.Main);
            _topRawImageTrans.gameObject.SetActive(_openUIType == RankingOpenType.Main);
            _bottomImageTrans.gameObject.SetActive(_openUIType == RankingOpenType.End);
            _progressRewardFx.gameObject.SetActive(false);
            if (_openUIType == RankingOpenType.Main)
            {
                //主界面
                RefreshCD();
                var multiplierIndex = _activity.GetMultiplierIndex();
                int slotNum = multiplierIndex + 1;
                RefreshTurntable(slotNum);
            }
            else
            {
                //结算
                _endRankNumText.text = I18N.FormatText("#SysComDesc1480", _activity.CurRank);
                _endRankReward.gameObject.SetActive(_activity.IsHasRankingReward());
                _rankEndText.gameObject.SetActive(!_activity.IsHasRankingReward());
                if (_activity.IsHasRankingReward())
                {
                    var rewardList = new List<RewardCommitData>();
                    rewardList.AddRange(_activity.GetRankingReward());
                    rewardList.Reverse();
                    _endRankReward.Refresh(rewardList);
                }
                else
                {
                    _rankEndText.text = I18N.Text("#SysComDesc1481");

                }
            }
            OnPlayPandaAnim();
            _topRawImageScroll.SetMultiplierNum(_activity.GetMultiplierIndex() + 1);
            MessageCenter.Get<MSG.UI_TOP_BAR_PUSH_STATE>().Dispatch(UIStatus.LayerState.SubStatus);
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(false);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
            stateShow = false;
        }

        protected override void OnPostOpen()
        {
            if (_openUIType == RankingOpenType.Main)
            {

            }
        }

        protected override void OnAddListener()
        {
            Get<GAME_ONE_SECOND_DRIVER>().AddListener(WhenTick);
            Get<FLY_ICON_START>().AddListener(CheckShowRes);
        }

        protected override void OnRemoveListener()
        {
            Get<GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenTick);
            Get<FLY_ICON_START>().RemoveListener(CheckShowRes);
        }

        protected override void OnPreClose()
        {
            if (_progressAnimCor != null)
            {
                StopCoroutine(_progressAnimCor);
                _progressAnimCor = null;
            }
        }

        protected override void OnPostClose()
        {
            MessageCenter.Get<MSG.UI_TOP_BAR_POP_STATE>().Dispatch();
            MessageCenter.Get<MSG.GAME_SHOP_ENTRY_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_LEVEL_GO_STATE_CHANGE>().Dispatch(true);
            MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
        }

        public void RefreshTheme()
        {
            var visual = _activity.VisualUIRankingMain;
            visual.Refresh(_vGroup);
        }

        public void RefreshCD()
        {
            var v = _activity?.Countdown ?? 0;
            UIUtility.CountDownFormat(_cdText, v);
            if (v <= 0)
            {
                if (_openUIType == RankingOpenType.Main)
                {
                    Close();
                }
            }
        }

        public void RefreshList()
        {
            _scroll.UpdateInfoList(_activity);
        }

        /// <summary>
        /// 刷新进度
        /// </summary>
        private void RefreshProgress()
        {
            if (_activity.CheckWeatherMilestoneComplete())
            {
                GetMilestoneData();
                if (_milestoneScoreList.Count > 0)
                {
                    //打开刷新进度，从上次进度开始
                    _mBprogress.Refresh(0, _milestoneScoreList[0], 0);
                }
                //是否存在里程碑奖励待领取
                if (_progressAnimCor != null)
                {
                    StopCoroutine(_progressAnimCor);
                    _progressAnimCor = null;
                }
                _progressAnimCor = RefreshProgressAnimCor();
                StartCoroutine(_progressAnimCor);
            }
            else
            {
                //无待领取的里程碑奖励
                _mBprogress.Refresh(0, _activity.GetMilestoneTarget(), 0);
                var curLastScore = _activity.GetMilestoneScore();
                var targeLastScore = _activity.GetMilestoneTarget();
                _mBprogress.Refresh(curLastScore, targeLastScore);
                //刷新奖励:显示目标奖励
                int curProgressPhase = _activity.GetCurProgressPhase();
                RefreshProgressReward(curProgressPhase);
            }
        }

        /// <summary>
        /// 刷新进度动画
        /// </summary>
        /// <returns></returns>
        private IEnumerator RefreshProgressAnimCor()
        {
            LockEvent();
            if (_milestoneScoreList.Count > 0)
            {
                for (int i = 0; i < _milestoneScoreList.Count; i++)
                {
                    //刷新奖励
                    var reward = rewardCommitList[i];
                    _mBProgressReward.Refresh(reward.rewardId, reward.rewardCount);
                    //刷新进度
                    int targetScore = _milestoneScoreList[i];
                    _mBprogress.RefreshWithTextAnimation(targetScore, targetScore, progressBarFillTime);
                    yield return _waitTime;
                    _progressRewardFx.gameObject.SetActive(false);
                    _progressRewardFx.gameObject.SetActive(true);
                    _progressAnim.SetTrigger("Punch");
                    //1.发奖表现
                    if (rewardCommitList.Count > 0 && i < rewardCommitList.Count)
                    {
                        UIFlyUtility.FlyReward(rewardCommitList[i], _mBProgressReward.transform.position);
                    }
                    //2.刷新奖励
                    if (rewardCommitList.Count > 0 && i + 1 < rewardCommitList.Count)
                    {
                        var reward1 = rewardCommitList[i + 1];
                        _mBProgressReward.Refresh(reward1.rewardId, reward1.rewardCount);
                    }
                    _mBprogress.Refresh(0, targetScore);
                    yield return _fxWaitTime;
                }
                _mBprogress.Refresh(0, _activity.GetMilestoneTarget(), 0);
                var curLastScore = _activity.GetMilestoneScore();
                var targeLastScore = _activity.GetMilestoneTarget();
                _mBprogress.RefreshWithTextAnimation(curLastScore, targeLastScore, progressBarFillTime);
                //刷新奖励
                int curProgressPhase = _activity.GetCurProgressPhase();
                RefreshProgressReward(curProgressPhase);
                yield return new WaitForSeconds(progressBarFillTime * 2);
                if (_activity.IsMilestoneAllComplete())
                {
                    SetProgressText(I18N.Text("#SysComDesc1537"));
                }
                _progressRewardFx.gameObject.SetActive(false);
            }
            if (stateShow)
            {
                yield return new WaitForSeconds(0.7f);
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(false);
                yield return new WaitForSeconds(0.3f);
            }
            UnlockEvent();
        }

        /// <summary>
        /// 刷新进度奖励
        /// </summary>
        /// <param name="milestonePhase"></param>
        private void RefreshProgressReward(int milestonePhase)
        {
            if (_activity.IsMilestoneAllComplete())
            {
                SetProgressText(I18N.Text("#SysComDesc1537"));
            }
            _mBProgressReward.gameObject.SetActive(!_activity.IsMilestoneAllComplete());
            if (_activity.IsMilestoneAllComplete())
            {
                DebugEx.Info("里程碑奖励已全部领取");
                return;
            }
            var id = _activity.detail.MilestoneRewardGroup[milestonePhase];
            var milestoneData = MultiRankMilestoneVisitor.Get(id);
            if (milestoneData != null)
            {
                var reward = milestoneData.MilestoneReward[0].ConvertToRewardConfig();
                _mBProgressReward.Refresh(reward);
            }
        }

        /// <summary>
        /// 获取里程碑数据
        /// </summary>
        private void GetMilestoneData()
        {
            _activity.ClaimMilestoneReward(rewardCommitList, _milestoneScoreList);
        }

        /// <summary>
        /// 刷新转盘
        /// </summary>
        /// <param name="multiplierNum"></param>
        private void RefreshTurntable(int multiplierNum)
        {
            RankingUIUtility.RefreshTurntableByNum(multiplierNum, TurntableTrans);
            RankingUIUtility.PointerToSlot(multiplierNum, TurntableTrans, TurntableArrow, action: () => _animatorArrow.enabled = multiplierNum >= TurntableTrans.childCount);
        }

        public void HelpClick()
        {
            _activity.VisualUIRankingHelp.res.ActiveR.Open(_activity);
        }

        private void OnClickMilestone()
        {
            _activity.VisualUIRankingMilestone.res.ActiveR.Open(_activity);
        }

        public void ConfirmClick()
        {
            if (_activity._finalReward.Count > 0)
            {
                _activity.VisualUIRankingEndReward.res.ActiveR.Open(_activity, _activity._finalReward);
            }
            Close();
        }

        public void GuideTurntableClick() => OnTurntableClick();
        private void OnTurntableClick()
        {
            var tokenId = _activity.conf.Token;
            var str = UIUtility.FormatTMPString(tokenId);
            var itemRect = _tipPos.transform as RectTransform;
            var itemHeight = itemRect.rect.height * 0.5f;
            UIManager.Instance.OpenWindow(UIConfig.UIRankingTurntableTips, _tipPos.transform.position, itemHeight, str);
        }

        private void OnClose()
        {
            Get<MULTIPLY_RANKING_ENTRY_REFRESH_RED_DOT>().Dispatch();
            if (_activity._finalReward.Count > 0 && _openUIType == RankingOpenType.End)
            {
                _activity.VisualUIRankingEndReward.res.ActiveR.Open(_activity, _activity._finalReward);
            }
            Close();
        }

        private void OnPlayPandaAnim()
        {
            var multiplierIndex = _activity.GetMultiplierIndex();
            int slotNum = multiplierIndex + 1;
            if (RankingUIUtility.GetAnimNameBySlotNum(slotNum) != null)
            {
                _pandaSpineAnim.AnimationState.SetAnimation(0, RankingUIUtility.GetAnimNameBySlotNum(slotNum), true);
                RankingUIUtility.SetTrailFxBySlotNum(slotNum, _pandaTrailFx);
            }
            else
            {
                DebugEx.Info("RankingUIUtility.GetAnimNameBySlotNum(slotNum) is null");
            }
        }

        private void CheckShowRes(FlyableItemSlice slice)
        {
            if (Game.Manager.objectMan.IsType(slice.ID, ObjConfigType.Coin) || slice.ID == Constant.kMergeEnergyObjId)
            {
                if (stateShow) return;
                MessageCenter.Get<MSG.GAME_STATUS_UI_STATE_CHANGE>().Dispatch(true);
                stateShow = true;
            }
        }

        private void SetProgressText(string str)
        {
            var text = _mBprogress.transform.Find("text");
            if (text != null)
            {
                var textComponent = text.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = str;
                }
            }
        }
    }
}