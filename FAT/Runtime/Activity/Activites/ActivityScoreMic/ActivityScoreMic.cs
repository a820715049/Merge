/*
 * @Author: tang.yan
 * @Description: 积分活动-麦克风版(积分活动的变种，把积分直接挂在棋子上，让玩家直观地看到积分来源)
 * @Doc: https://centurygames.feishu.cn/wiki/FCr6wUVEZiwH77kZn6pcjmTxn1g
 * @Date: 2025-09-02 14:09:55
 */

using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class ActivityScoreMic : ActivityLike, IBoardEntry
    {
        public override bool Valid => Lite.Valid && Conf != null;
        public MicMilestone Conf { get; private set; }

        #region 活动基础

        //用户分层 对应MicMilestoneDetail.id
        private int _detailId;
        
        //外部调用需判空
        public MicMilestoneDetail GetCurDetailConfig()
        {
            return Game.Manager.configMan.GetMicMilestoneDetailConfig(_detailId);
        }
        
        public ActivityScoreMic(ActivityLite lite_)
        {
            Lite = lite_; Conf = Game.Manager.configMan.GetMicMilestoneConfig(lite_.Param);
        }
        
        // 活动首次初始化 | 此时不走读档流程 不会调用LoadSetup
        public override void SetupFresh()
        {
            _detailId = Game.Manager.userGradeMan.GetTargetConfigDataId(Conf.EventGroup);
            //刷新弹脸信息
            _RefreshPopupInfo();
        }
        
        public override void SaveSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            any.Add(ToRecord(0, _detailId));
            any.Add(ToRecord(1, HasTriggerGuide));
        }

        public override void LoadSetup(ActivityInstance data_)
        {
            var any = data_.AnyState;
            _detailId = ReadInt(0, any);
            HasTriggerGuide = ReadBool(1, any);
            //刷新弹脸信息
            _RefreshPopupInfo();
        }

        public override void WhenEnd()
        {
            // UIManager.Instance.CloseWindow(UIConfig.UIDecorateComplete);
            // UIManager.Instance.CloseWindow(UIConfig.UIDecorateRestartNotice);
            // var listT = PoolMappingAccess.Take(out List<RewardCommitData> list);
            // var needConvert = false;
            // using var _ = PoolMappingAccess.Borrow(out Dictionary<int, int> map);
            // map[confD.RequireScoreId] = _score;
            // DataTracker.expire.Track(confD.RequireScoreId, _score);
            // if (_score >= 0)
            // {
            //     ActivityExpire.ConvertToReward(confD.ExpirePopup, list, ReasonString.decorate_reward, map);
            //     _score = 0;
            //     needConvert = list.Count > 0;
            // }
            //
            // if (UIManager.Instance.IsOpen(UIConfig.UIDecorateComplete) || _needComplete)
            // {
            //     UIManager.Instance.RegisterIdleAction("decorate_end", 201, () =>
            //     {
            //         if (needConvert) Game.Manager.screenPopup.Queue(ReContinuePop, listT);
            //     });
            // }
            // else
            // {
            //     if (needConvert) Game.Manager.screenPopup.Queue(ReContinuePop, listT);
            // }
        }
        
        #region 界面 入口 换皮 弹脸

        public override ActivityVisual Visual => MainPopup.visual;
        public VisualPopup MainPopup { get; } = new(UIConfig.UIScore_Mic); // 主界面
        public VisualPopup SettlePopup { get; } = new(UIConfig.UIScoreConvert_Mic); //活动结算界面

        public override void Open() => Open(MainPopup);

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            MainPopup.Setup(Conf.EventMainTheme, this);
            SettlePopup.Setup(Conf.EventSettleTheme, this, false, false);
        }

        string IBoardEntry.BoardEntryAsset()
        {
            MainPopup.visual.AssetMap.TryGetValue("boardEntry", out var key);
            return key;
        }
        
        //是否已触发过新手引导(进存档)
        public bool HasTriggerGuide = false;

        #endregion
        
        #endregion
    }
}