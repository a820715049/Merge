/*
 *@Author:chaoran.zhang
 *@Desc:装饰活动实例：管理弹窗、积分数量
 *@Created Time:2024.05.14 星期二 16:05:54
 */

using System;
using System.Collections.Generic;
using Config;
using EL;
using EL.Resource;
using fat.gamekitdata;
using fat.rawdata;
using FAT.MSG;
using UnityEngine;
using static FAT.RecordStateHelper;

namespace FAT
{
    using static MessageCenter;
    using static ListActivity;
    using static PoolMapping;

    public class DecorateActivity : ActivityLike, IRankingSource
    {
        public EventDecorate confD;
        public override bool EntryVisible => CurGroupConf != null;
        public override ActivityVisual Visual => StartRemindVisual;
        public int CurArea;
        public int RankingScore => _totalToken;
        public Action<int> RankingScoreChange { get; set; }

        //结束预告弹版
        public ActivityVisual EndRemindVisual = new();
        public PopupActivity EndRemindPop = new();

        //活动回收弹板
        public ActivityVisual ReContinueVisual = new();
        public PopupActivity ReContinuePop = new();

        //活动开启弹板
        public ActivityVisual StartRemindVisual = new();
        public PopupActivity StartRemindPop = new();

        //新一轮弹板
        public ActivityVisual ReStartVisual = new();
        public PopupDeco RestartPop = new();

        //选择页面
        public ActivityVisual ChoiceVisual = new();

        //帮助页面
        public ActivityVisual HelpVisual = new();

        public EntryDecorate Entry;


        //当前的活动组利用phase字段存储
        //当前阶段奖励领取情况
        public int CurLevel => _curLevel;
        private int _curLevel => Game.Manager.decorateMan.UnlockDecoration.Count / 3;
        private int _hasCompleteLevel;
        private int _totalToken; //总装饰代币获取量

        //当前积分
        private int _score = 0;

        public int Score => _score;

        //是否需要进行新一轮开启弹脸
        private bool _needPop;

        public bool NeedComplete => _needComplete;

        private bool _needComplete;

        //是否需要重新显示云的遮挡动画
        private bool _needCloud;

        private bool _hasPop;


        //当前活动组配置
        public EventDecorateGroup CurGroupConf { get; private set; }

        //当前活动组阶段配置
        public EventDecorateLevel CurLevelConf { get; private set; }


        //弹板ui资源字段
        public UIResAlt EndRemindUI = new(UIConfig.UIDecorateEndNotice);
        public UIResAlt StartRemindUI = new(UIConfig.UIDecorateStartNotice);
        public UIResAlt ReContinueUI = new(UIConfig.UIDecorateConvert);
        public UIResAlt DecoratePanel = new(UIConfig.UIDecoratePanel);
        public UIResAlt RestartUI = new(UIConfig.UIDecorateRestartNotice);
        public UIResAlt HelpUI = new(UIConfig.UIDecorateHelp);

        #region 保存、加载、初始化流程

        public override IEnumerable<(string, AssetTag)> ResEnumerate()
        {
            if (!Valid) yield break;
            foreach (var v in StartRemindVisual.ResEnumerate()) yield return v;
            foreach (var v in EndRemindVisual.ResEnumerate()) yield return v;
            foreach (var v in HelpVisual.ResEnumerate()) yield return v;
            foreach (var v in ReStartVisual.ResEnumerate()) yield return v;
            foreach (var v in ChoiceVisual.ResEnumerate()) yield return v;
        }

        public void SetUp(ActivityLite lite_, EventDecorate confD_)
        {
            Lite = lite_;
            confD = confD_;
        }

        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _score));
            any.Add(ToRecord(1, _needPop));
            any.Add(ToRecord(3, _needCloud));
            any.Add(ToRecord(4, _needComplete));
            any.Add(ToRecord(5, _hasCompleteLevel));
            any.Add(ToRecord(6, _totalToken));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            //phase字段用来存储当前活动组
            phase = data_.Phase;
            _score = ReadInt(0, data_.AnyState);
            _needPop = ReadBool(1, data_.AnyState);
            _needCloud = ReadBool(3, data_.AnyState);
            _needComplete = ReadBool(4, data_.AnyState);
            _hasCompleteLevel = ReadInt(5, data_.AnyState);
            _totalToken = ReadInt(6, data_.AnyState);
            //初始化当前活动组config
            if (confD.IncludeGrpId.Count <= phase)
                return;
            CurGroupConf = Game.Manager.configMan.GetEventDecorateGroupConfig(confD.IncludeGrpId[phase]);
            CurLevelConf = Game.Manager.configMan.GetEventDecorateLevelConfig(CurGroupConf.IncludeLvId[_curLevel >= CurGroupConf.IncludeLvId.Count ? CurGroupConf.IncludeLvId.Count - 1 : _curLevel]);
            CurArea = Game.Manager.configMan.GetEventDecorateInfo(CurLevelConf.DecorateID[0]).IslandId;
        }

        public override void LoadData(ActivityInstance data_)
        {
            base.LoadData(data_);
            _RefreshPopup();
        }

        public override void SetupFresh()
        {
            Game.Manager.decorateMan.PrepareNewData(this);
            CurGroupConf = Game.Manager.configMan.GetEventDecorateGroupConfig(confD.IncludeGrpId[phase]);
            CurLevelConf = Game.Manager.configMan.GetEventDecorateLevelConfig(CurGroupConf.IncludeLvId[_curLevel]);
            CurArea = Game.Manager.configMan.GetEventDecorateInfo(CurLevelConf.DecorateID[0]).IslandId;
            _needCloud = true;
            _needPop = true;
            Game.Manager.decorateMan.RefreshData(this);
            UpdateScore(confD.ScoreNum, ReasonString.decorate_reward);
            _RefreshPopup();
            Game.Manager.screenPopup.TryQueue(StartRemindPop, PopupType.Login);
            _hasPop = true;
            // UIManager.Instance.RegisterIdleAction("ui_idle_decorate_begin", 203, () =>
            // {
            //     if (_needPop)
            //         UIManager.Instance.OpenWindow(StartRemindUI);
            // });
        }

        #endregion

        #region 弹脸

        private void _RefreshPopup()
        {
            if (EndRemindVisual.Setup(confD.EndTipTheme, EndRemindUI))
                EndRemindPop.Setup(this, EndRemindVisual, EndRemindUI);
            if (ReContinueVisual.Setup(confD.RecontinueTheme, ReContinueUI))
                ReContinuePop.Setup(this, ReContinueVisual, ReContinueUI, false, false);
            if (StartRemindVisual.Setup(confD.EventTheme, StartRemindUI))
                StartRemindPop.Setup(this, StartRemindVisual, StartRemindUI);
            if (ReStartVisual.Setup(confD.AgainTheme, RestartUI))
                RestartPop.Setup(this, ReStartVisual, RestartUI);
            ChoiceVisual.Setup(confD.EventChoiceTheme, DecoratePanel);
            HelpVisual.Setup(confD.EventHelpTheme, HelpUI);
            StartRemindPop.option = new IScreenPopup.Option() { ignoreLimit = true };
            RestartPop.option = new IScreenPopup.Option() { ignoreLimit = true };
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (!Valid)
                return;

            if (CheckEndRemind() && EndRemindPop.PopupValid) popup_.TryQueue(EndRemindPop, state_);

            if (_needPop && phase == 0 && !_hasPop)
                popup_.TryQueue(StartRemindPop, state_);
            if (_needPop && phase > 0 && !_needComplete)
                popup_.TryQueue(RestartPop, state_);
        }

        private bool CheckEndRemind()
        {
            var time = Game.Instance.GetTimestampSeconds();
            return time > endTS - confD.EndRemindTime && time < endTS;
        }

        #endregion

        #region API

        /// <summary>
        /// 新一轮开启弹版只会弹一次，所以需要该接口修改弹板状态
        /// </summary>
        public void ChangePopState(bool b)
        {
            _needPop = b;
        }

        /// <summary>
        /// 领取阶段奖励接口
        /// </summary>
        public List<RewardCommitData> ClaimLevelReward()
        {
            var list = new List<RewardCommitData>();
            var level = Game.Manager.configMan.GetEventDecorateLevelConfig(CurGroupConf.IncludeLvId[_curLevel - 1]);
            foreach (var variable in level.Reward)
            {
                var reward = ConvertToRewardConfig(variable);
                list.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.decorate_reward));
            }

            return list;
        }

        /// <summary>
        /// 领取活动组里程碑奖励
        /// </summary>
        /// <returns></returns>
        public List<RewardCommitData> ClaimGroupReward()
        {
            var list = new List<RewardCommitData>();
            foreach (var variable in CurGroupConf.MilestoneReward)
            {
                var reward = variable.ConvertToRewardConfig();
                list.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.decorate_reward));
            }

            return list;
        }

        /// <summary>
        /// 开启新一轮活动组
        /// </summary>
        public void StartNewGroup()
        {
            CurGroupConf = null;
            CurGroupConf = Game.Manager.configMan.GetEventDecorateGroupConfig(phase);
        }

        /// <summary>
        /// 活动积分变化接口
        /// </summary>
        /// <param name="num"></param>
        public void UpdateScore(int num, ReasonString reasonString = null)
        {
            _score += num;
            if (num > 0 && reasonString != ReasonString.decorate_reward)
            {
                _totalToken += num;
                RankingScoreChange?.Invoke(num);
            }

            if (_score < 0)
                DebugEx.Error("Decorate score less than zero");
            if (reasonString == null)
                DataTracker.token_change.Track(confD.RequireScoreId, num, _score, ReasonString.decorate_reward);
            else
                DataTracker.token_change.Track(confD.RequireScoreId, num, _score, reasonString);
            Get<DECORATE_SCORE_UPDATE>().Dispatch(this, num);
        }

        /// <summary>
        /// 尝试进入下一阶段
        /// </summary>
        public void TryGotoNextLevel()
        {
            _hasCompleteLevel++;
            DataTracker.event_decorate_levelcomp.Track(Id, Param, From, phase + 1, _hasCompleteLevel, _curLevel, CurGroupConf.IncludeLvId.Count, 1, _curLevel == CurGroupConf.IncludeLvId.Count);
            if (_curLevel < CurGroupConf.IncludeLvId.Count)
            {
                CurLevelConf = Game.Manager.configMan.GetEventDecorateLevelConfig(CurGroupConf.IncludeLvId[_curLevel]);
            }
            else
            {
                CurLevelConf = null;
            }
            Get<DECORATE_SCORE_UPDATE>().Dispatch(this, 0);
        }

        /// <summary>
        /// 尝试进入下一轮
        /// </summary>
        /// <param name="str"></param>
        public void TryGotoNextGroup()
        {
            phase++;
            if (phase < confD.IncludeGrpId.Count)
            {
                CurGroupConf = Game.Manager.configMan.GetEventDecorateGroupConfig(confD.IncludeGrpId[phase]);
                CurLevelConf = Game.Manager.configMan.GetEventDecorateLevelConfig(CurGroupConf.IncludeLvId[0]);
                CurArea = Game.Manager.configMan.GetEventDecorateInfo(CurLevelConf.DecorateID[0]).IslandId;
                if (phase == 1)
                    Get<ACTIVITY_SUCCESS>().Dispatch(this);
            }
            else
            {
                CurGroupConf = null;
            }
        }

        public void ChangeCloudState(bool state)
        {
            _needCloud = state;
        }

        public bool GetCloudState()
        {
            return _needCloud;
        }

        public void SetCompleteState(bool b)
        {
            _needComplete = b;
        }

        public bool ShowEntryDot()
        {
            if (CurLevelConf == null)
                return false;
            foreach (var kv in CurLevelConf.DecorateID)
            {
                var conf = Game.Manager.configMan.GetEventDecorateInfo(kv);
                if (conf.Price <= _score && !Game.Manager.decorateMan.UnlockDecoration.Contains(conf.Id))
                    return true;
            }

            return false;
        }

        public bool EndNoticeJump()
        {
            return _needComplete || _needPop;
        }

        public void ResetRankingScore()
        {
            _totalToken = 0;
        }

        //检查当前轮次是否可以全览整个装饰区
        public bool CheckCanPreview()
        {
            if (confD == null)
                return false;
            return phase >= confD.PreviewParm;
        }

        #endregion

        #region 内部实现

        //转换规则参照配表备注
        private RewardConfig ConvertToRewardConfig(string str)
        {
            var config = new RewardConfig();
            var split = str.ConvertToInt3();
            config.Id = split.Item1;
            config.Count = Game.Manager.rewardMan.CalcDailyEventTaskRequireCount(split.Item2, split.Item3);
            return config;
        }

        #endregion

        public override void WhenEnd()
        {
            UIManager.Instance.CloseWindow(UIConfig.UIDecorateComplete);
            UIManager.Instance.CloseWindow(UIConfig.UIDecorateRestartNotice);
            var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            var needConvert = false;
            using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            map[confD.RequireScoreId] = _score;
            DataTracker.expire.Track(confD.RequireScoreId, _score);
            if (_score >= 0)
            {
                ActivityExpire.ConvertToReward(confD.ExpirePopup, list, ReasonString.decorate_reward, map);
                _score = 0;
                needConvert = list.Count > 0;
            }

            if (UIManager.Instance.IsOpen(UIConfig.UIDecorateComplete) || _needComplete)
            {
                UIManager.Instance.RegisterIdleAction("decorate_end", 201, () =>
                {
                    if (needConvert) Game.Manager.screenPopup.Queue(ReContinuePop, listT);
                });
            }
            else
            {
                if (needConvert) Game.Manager.screenPopup.Queue(ReContinuePop, listT);
            }
        }

        public override void Open()
        {
            if (CurGroupConf == null)
                return;
            var uiMgr = UIManager.Instance;
            var mapScene = Game.Manager.mapSceneMan;
            uiMgr.Block(true);
            var areaId = fat.conf.Data.GetEventDecorateInfo(CurLevelConf.DecorateID[0]).IslandId;
            mapScene.TryFocus(areaId, 0.5f, 100f, () =>
            {
                uiMgr.Block(false);
                uiMgr.OpenWindow(DecoratePanel.ActiveR);
            });
        }

        public bool RankingValid(RankingType type_)
        {
            return true;
        }

        public void InitTotalToken()
        {
            _totalToken = 0;
        }
    }

    public class EntryDecorate : IEntrySetup
    {
        public Entry Entry => e;
        private readonly Entry e;
        private readonly DecorateActivity p;

        public EntryDecorate(Entry e_, DecorateActivity p_)
        {
            (e, p) = (e_, p_);
            e_.token.gameObject.SetActive(true);
            p_.StartRemindVisual.RefreshStyle(e_.token, "entrance");
            RefreshToken(p_, 0);
            Get<DECORATE_SCORE_UPDATE>().AddListenerUnique(RefreshToken);
            p_.Entry = this;
        }

        public override void Clear(Entry e_)
        {
            Get<DECORATE_SCORE_UPDATE>().RemoveListener(RefreshToken);
        }

        public void RefreshToken(DecorateActivity p_, int _)
        {
            if (p != p_) return;
            e.token.text = $"{p_.Score}";
            e.dot.SetActive(p.ShowEntryDot());
        }

        public override string TextCD(long diff_)
        {
            return UIUtility.CountDownFormat(diff_);
        }
    }
}
