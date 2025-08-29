using System;
using fat.conf;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;
using static fat.conf.Data;
using static EL.MessageCenter;
using Config;

namespace FAT
{
    /// <summary>
    /// LandMark 活动逻辑：
    /// - 维护 token 计数、小奖/大奖的领取与缓存。
    /// - 登录弹窗（受限）与触发弹窗（不限）的队列管理。
    /// - 达成大奖时，对齐 OrderDash：分发成功、结束活动，并由界面承接表现。
    /// </summary>
    public class LandMarkActivity : ActivityLike
    {
        // 基础可见性由上层活动系统掌控（时间窗、功能开关等）
        public override bool Valid => ConfD != null && ConfDetail != null;
        public PopupActivity popup { get; private set; }
        private PopupLandMarkForce _forcePopup; // 触发用强制弹窗（不受次数限制）
        public UIResAlt Res { get; } = new(UIConfig.UILandMark);
        // 配置
        private EventLandmark ConfD;
        private LandmarkDetail ConfDetail
        {
            get
            {
                return GetLandmarkDetail(m_detailId);
            }
        }
        public bool CollectedFinal { get => m_bigClaimed; }
        #region View-only
        public int TokenCount_View;
        public int ShowedTokenCount_View;
        #endregion
        // 进度与状态
        private int m_tokenCount;          // 已收集 token 总数
        private bool m_smallClaimed;       // 小奖是否已领取（一次性）
        private bool m_bigClaimed;         // 大奖是否已领取（一次性，领取后活动可视为完成）
        private int m_showedTokenCount;    // 已弹出 UI 的 token 数量
        private int m_detailId;
        private RewardCommitData _smallReward;
        private RewardCommitData _bigReward;
        private bool _pendingForcePopup;   // 防抖：已入队强制弹窗，避免重复入队
        public LandMarkActivity(ActivityLite lite_)
        {
            Lite = lite_;
            // 读取活动主配置
            ConfD = GetEventLandmark(lite_.Param);
            Visual.Setup(ConfD.ThemeId, Res);
            popup = new(this, Visual, Res, false);
            _forcePopup = new PopupLandMarkForce(this, Res);
            Get<MSG.UI_SINGLE_REWARD_CLOSE_FEEDBACK>().AddListener(OnTriggerToken);
        }
        public override void SetupFresh()
        {
            base.SetupFresh();
            var grpId = int.TryParse(ConfD.InfoGrp, out var parsed) ? parsed : 0;
            m_detailId = Game.Manager.userGradeMan.GetTargetConfigDataId(grpId);
            m_tokenCount = 0;
            m_smallClaimed = false;
            m_bigClaimed = false;
            m_showedTokenCount = 0;
        }
        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            m_tokenCount = ReadInt(1, any);
            m_smallClaimed = ReadBool(2, any);
            m_bigClaimed = ReadBool(3, any);
            m_showedTokenCount = ReadInt(4, any);
            m_detailId = ReadInt(5, any);
            TokenCount_View = m_tokenCount;
            ShowedTokenCount_View = m_showedTokenCount;
        }
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(1, m_tokenCount));
            any.Add(ToRecord(2, m_smallClaimed));
            any.Add(ToRecord(3, m_bigClaimed));
            any.Add(ToRecord(4, m_showedTokenCount));
            any.Add(ToRecord(5, m_detailId));
        }
        /// <summary>
        /// 打开活动主界面。
        /// </summary>
        public override void Open()
        {
            UIManager.Instance.OpenWindow(Res.ActiveR, this);
        }
        /// <summary>
        /// 加入弹窗队列（登录弹窗，受限）。
        /// </summary>
        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            popup_.TryQueue(popup, state_);
        }

        /// <summary>
        /// 活动激活：
        /// - 登录态：走受限弹窗。
        /// - 非首次且有新进度：走强制弹窗（不限次）。
        /// </summary>
        public override void WhenActive(bool new_)
        {
            if (new_)
            {
                Game.Manager.screenPopup.TryQueue(popup, PopupType.Login);
            }
            else
            {
                if (HasNewTokenProgress())
                {
                    // 非首次激活且有新进度：强制弹出（不受次数限制）
                    Game.Manager.screenPopup.Queue(_forcePopup);
                    _pendingForcePopup = true;
                }
            }
        }
        public override void WhenReset()
        {
            base.WhenReset();
            Get<MSG.UI_SINGLE_REWARD_CLOSE_FEEDBACK>().RemoveListener(OnTriggerToken);
        }
        /// <summary>
        /// 主界面弹出回调：
        /// - 同步已展示进度；
        /// - 清理防抖标记。
        /// </summary>
        public void OnMainUIPop()
        {
            m_showedTokenCount = m_tokenCount;
            ShowedTokenCount_View = m_showedTokenCount;
            _pendingForcePopup = false;
        }
        /// <summary>
        /// 当关闭单一奖励后（含随机宝箱单品），若是本活动的 token，触发强制弹窗。
        /// </summary>
        private void OnTriggerToken(int tokenId)
        {
            if (tokenId != ConfD.TokenId)
            {
                return;
            }
            // Trigger：强制弹出，直接入队，不依赖当前PopupType，且不受次数限制
            if (_pendingForcePopup) { return; }
            Game.Manager.screenPopup.Queue(_forcePopup);
            _pendingForcePopup = true;
            if (m_bigClaimed)
            {
                Get<MSG.UI_SINGLE_REWARD_CLOSE_FEEDBACK>().RemoveListener(OnTriggerToken);
            }
        }
        private bool HasNewTokenProgress()
        {
            return m_tokenCount > m_showedTokenCount;
        }
        /// <summary>
        /// 数据层加 token，达到步数后准备小奖/大奖（缓存 CommitData）。
        /// </summary>
        public void AddToken(int rewardId, int rewardCount)
        {
            if (!Valid)
            {
                return;
            }
            if (rewardId != ConfD.TokenId || rewardCount <= 0)
            {
                return;
            }

            // 计数（不做UI弹出）
            m_tokenCount += rewardCount;
            TokenCount_View = m_tokenCount;
            // 埋点：获得活动token
            var totalNeed = ConfD.LandmarkBig;
            var gotNum = m_tokenCount;
            var diff = ConfDetail.Diff;
            var isFinal = gotNum >= totalNeed;
            DataTracker.event_landmark_token.Track(this, 1, isFinal, gotNum, totalNeed, diff);
            // 数据层仅做奖励准备缓存
            TryPrepareSmallRewards();
            TryPrepareBigRewards();
        }
        /// <summary>
        /// 配置：小奖达成步数。
        /// </summary>
        public int GetSmallRewardStep()
        {
            return ConfD.LandmarkSmall;
        }

        // 准备小奖到缓存（一次性），由表现层拉取并执行飞行
        /// <summary>
        /// 达成小奖：准备到缓存，仅一次。
        /// </summary>
        private bool TryPrepareSmallRewards()
        {
            if (!Valid || m_smallClaimed)
            {
                return false;
            }
            if (m_tokenCount < ConfD.LandmarkSmall)
            {
                return false;
            }
            if (_smallReward != null)
            {
                return true;
            }
            var rewardStr = ConfDetail?.RewardSmall;
            var reward = rewardStr.ConvertToRewardConfigIfValid();
            if (reward == null)
            {
                return false;
            }
            _smallReward = Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.landmark_reward);
            m_smallClaimed = true;
            return true;
        }
        // 准备大奖到缓存（一次性），由表现层拉取并执行飞行
        /// <summary>
        /// 达成大奖：准备到缓存，仅一次；对齐 OrderDash：分发成功并结束活动。
        /// </summary>
        private bool TryPrepareBigRewards()
        {
            if (!Valid || m_bigClaimed)
            {
                return false;
            }
            if (m_tokenCount < ConfD.LandmarkBig)
            {
                return false;
            }
            int rewardId = GetBigRewardConfigId();
            _bigReward = Game.Manager.rewardMan.BeginReward(rewardId, 1, ReasonString.landmark_reward);
            m_bigClaimed = true;
            Get<MSG.ACTIVITY_SUCCESS>().Dispatch(this);
            Game.Manager.activity.EndImmediate(this, false);
            return true;
        }
        // 表现层调用：拉取并清空待飞的小奖（数据层内部创建并返回 Ref）
        /// <summary>
        /// UI 拉取小奖 CommitData（可为 null）。
        /// </summary>
        public RewardCommitData GetSmallRewardCommitData()
        {
            return _smallReward;
        }
        /// <summary>
        /// UI 拉取大奖 CommitData（可为 null）。
        /// </summary>
        public RewardCommitData GetBigRewardCommitData()
        {
            return _bigReward;
        }
        /// <summary>
        /// UI 侧完成小奖表现后回调，清除缓存引用。
        /// </summary>
        public void OnCollectedSmallReward()
        {
            _smallReward = null;
        }
        /// <summary>
        /// UI 侧完成大奖表现后回调，清除缓存引用。
        /// </summary>
        public void OnCollectedBigReward()
        {
            _bigReward = null;
        }
        /// <summary>
        /// 读取小奖配置。
        /// </summary>
        public RewardConfig GetSmallRewardConfig()
        {
            var rewardStr = ConfDetail?.RewardSmall;
            return rewardStr.ConvertToRewardConfigIfValid();
        }
        /// <summary>
        /// 读取大奖配置 Id（随机宝箱）。
        /// </summary>
        public int GetBigRewardConfigId()
        {
            var rewardStr = ConfDetail?.RewardBig[0];
            return rewardStr.ConvertToInt();
        }
        /// <summary>
        /// 获取金色标记起始索引（用于 UI 展示）。
        /// </summary>
        public int GetGoldenStartIndex()
        {
            return ConfD.LandmarkBig - ConfDetail.GoldTokenNum;
        }
        public int GetTokenId()
        {
            return ConfD.TokenId;
        }
    }
    // 强制弹窗：不绑定 PopupConf，忽略次数限制，直接打开 UILandMark
    /// <summary>
    /// 强制弹窗：不绑定 PopupConf，忽略次数限制，直接打开 UILandMark。
    /// </summary>
    internal sealed class PopupLandMarkForce : IScreenPopup
    {
        private readonly ActivityLike _activity;
        private readonly UIResAlt _res;
        public override int PopupWeight => int.MaxValue;
        public override int PopupLimit => -1; // 不受次数限制
        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup();
        public PopupLandMarkForce(ActivityLike activity, UIResAlt res)
        {
            _activity = activity;
            _res = res;
            PopupId = activity.Visual.PopupId; // 记录用，但不会触发限制
            // 提供 PopupRes 给队列系统，否则会报 "no ui to popup"
            PopupRes = _res.ActiveR;
            // 强制弹窗通常不受次数限制清理影响
            option.ignoreLimit = true;
        }
        public override bool CheckValid(out string _)
        {
            _ = null; // 不依赖 PopupConf
            return true;
        }
        public override bool OpenPopup()
        {
            UIManager.Instance.OpenWindow(_res.ActiveR, _activity, Custom);
            Custom = null;
            DataTracker.event_popup.Track(_activity);
            return true;
        }
    }
}
