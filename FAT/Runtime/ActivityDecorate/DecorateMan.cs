/*
 *@Author:chaoran.zhang
 *@Desc:装饰区活动manager，管理玩家解锁的奖励，因为改数据需要长期保存与活动生命周期不相同，所以创建该类管理
 *@Created Time:2024.05.21 星期二 11:08:56
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EL;
using fat.gamekitdata;
using fat.rawdata;
using log4net.Core;
using UnityEngine;
using UnityEngine.Assertions.Must;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class DecorateMan : IGameModule, IUserDataHolder, IUpdate
    {
        //装饰区活动是否满足解锁条件
        public bool IsUnlock => Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDecorate);

        public GroupDecorate ActivityGroup = new();

        //展示的装饰列表
        public List<int> UnlockDecoration { get; private set; } = new();

        //活动结束后依旧展示的列表
        public List<int> TotalUnlockDecoration { get; private set; } = new();
        public DecorateActivity Activity; //活动实例
        public int EventID = 0; //当前装饰区活动对应的EventTimeID
        public int GroupID => Activity.CurGroupConf.Id; //当前活动组id
        public int LevelID => Activity.CurLevelConf.Id; //当前阶段id
        public long EndTime; //装饰区数据清除时间
        public bool IsValid => Activity != null && UnlockDecoration.Count != 0;
        public int Score => Activity?.Score ?? 0;
        public int FlyScore;
        public readonly List<RewardCommitData> LevelReward = new();
        public readonly List<RewardCommitData> GroupReward = new();
        public readonly List<(int priority, Action callback)> AnimList = new();
        public bool StartNewGroup;
        public bool AllEnd;
        public bool NeedRefreshUI;
        public Action WaitAction;
        public int CurAnimNum;
        public UIResource Panel => Activity.DecoratePanel.ActiveR;
        private int areaId;
        private int _trackID;
        private int _lastUnlockID = -1; //上次解锁的建筑的id，同一个建筑连续解锁

        public void DebugResetCloud()
        {
            if (Activity == null) return;
            Activity.ChangeCloudState(true);
            GameProcedure.MergeToSceneArea(Activity.CurArea);
        }

        public void DebugComplete()
        {
            if (Activity == null) return;
            Game.Instance.StartCoroutineGlobal(Game.Manager.mapSceneMan.AreaCompleteVisual(Activity.CurArea));
        }

        #region API

        public bool CheckGuideStart()
        {
            if (Activity == null) { return false; }
            return UIManager.Instance.IsOpen(Activity.StartRemindUI.ActiveR);
        }
        public bool GetCloudState()
        {
            if (!Game.Manager.featureUnlockMan.IsFeatureEntryUnlocked(FeatureEntry.FeatureDecorate))
                return true;
            if (Activity == null)
                return false;
            var time = Game.Instance.GetTimestampSeconds();
            return Activity.GetCloudState() && time < EndTime;
        }

        public int GetAreaID()
        {
            if (Activity != null) return Activity.CurArea;
            if (areaId > 0) return areaId;
            return FindAreaID();
        }

        public int FindAreaID()
        {
            var map = Game.Manager.configMan.GetEventTimeConfigsByType(EventType.Decorate);
            var time = Game.TimestampNow();
            var eventTime = map.First();
            foreach (var kv in map)
            {
                if (kv.StartTime > time)
                    continue;
                if (eventTime.StartTime < kv.StartTime)
                    eventTime = kv;
            }

            var confD = fat.conf.Data.GetEventDecorate(eventTime.EventParam);
            if (confD == null) return 0;
            var groupConf = Game.Manager.configMan.GetEventDecorateGroupConfig(confD.IncludeGrpId[0]);
            var levelConf = Game.Manager.configMan.GetEventDecorateLevelConfig(groupConf.IncludeLvId[0]);
            areaId = Game.Manager.configMan.GetEventDecorateInfo(levelConf.DecorateID[0]).IslandId;
            return areaId;
        }

        public void SetCloudState(bool state)
        {
            if (Activity == null)
                return;
            Activity.ChangeCloudState(state);
        }

        /// <summary>
        /// 更新装饰活动数据
        /// </summary>
        public void RefreshData(DecorateActivity activity)
        {
            //若当前活动id与存档中的id不相同，则清空旧数据
            //相同，判断是否保存活动实例，或结束活动
            var lite = activity.Lite;
            if (lite.Id != EventID)
            {
                Activity = activity;
                EventID = lite.Id;
                EndTime = lite.EndTS + activity.confD.DeleteTime;
                UnlockDecoration.Clear();
                TotalUnlockDecoration.Clear();
                MessageCenter.Get<MSG.DECORATE_AREA_REFRESH>().Dispatch();
                MessageCenter.Get<MSG.DECORATE_REFRESH>().Dispatch();
                NeedRefreshUI = true;
            }
            else
            {
                if (activity.CurGroupConf == null)
                    Game.Manager.activity.EndImmediate(activity, false);
                else
                    Activity = activity;
            }
        }

        /// <summary>
        /// 装饰区活动结束：
        /// 两种情况：1.装饰品还在有效期内；2.装饰品不在有效期内
        ///1:在有效期内，则判断完成轮次，若为一轮以上则开放全部装饰品
        ///2:清空所有装饰品数据
        /// </summary>
        /// <returns></returns>
        public void WhenActivityEnd()
        {
            UnlockDecoration.Clear();
            UnlockDecoration.AddRange(TotalUnlockDecoration);
            MessageCenter.Get<MSG.DECORATE_REFRESH>().Dispatch();
            UIManager.Instance.CloseWindow(Panel);
        }

        /// <summary>
        /// 检测装饰是否已经解锁
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CheckDecorationUnlock(int id)
        {
            if (StartNewGroup || AllEnd)
                return true;
            return UnlockDecoration.Contains(id);
        }

        /// <summary>
        /// 阶段是否完成
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CheckLevelComplete(int id)
        {
            if (StartNewGroup || AllEnd)
                return true;
            var list = Activity.CurGroupConf.IncludeLvId;
            return list.IndexOf(Activity.CurLevelConf.Id) > list.IndexOf(id);
        }

        /// <summary>
        /// 阶段是否解锁
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CheckLevelActive(int id)
        {
            if (StartNewGroup || AllEnd)
                return true;
            var list = Activity.CurGroupConf.IncludeLvId;
            return list.IndexOf(Activity.CurLevelConf.Id) >= list.IndexOf(id);
        }

        /// <summary>
        /// 更新积分
        /// </summary>
        /// <returns></returns>
        public void TryUpdateScore(int id, int num, ReasonString reasonString)
        {
            if (Activity == null)
                return;
            if (id != Activity.confD.RequireScoreId)
                return;
            Activity.UpdateScore(num, reasonString);
            if (num > 0)
                FlyScore = num;
            else
                MessageCenter.Get<MSG.DECORATE_SCORE_FLY>().Dispatch();
        }

        /// <summary>
        /// 尝试解锁装饰
        /// </summary>
        /// <param name="id">需要解锁的装饰id</param>
        /// <returns></returns>
        public bool TryUnlock(int id)
        {
            if (Activity == null)
                return false;
            if (!Activity.Active)
                return false;
            var conf = Game.Manager.configMan.GetEventDecorateInfo(id);
            if (Activity.Score < conf.Price)
                return false;
            if (UnlockDecoration.Contains(id))
                return false;
            if (_lastUnlockID == id)
                return false;
            Unlock(id);
            return true;
        }

        public void AfterUnlock()
        {
            if (AnimList.Count > 0)
                AnimList.Clear();
            _lastUnlockID = -1;
            var uiMgr = UIManager.Instance;
            if (uiMgr.IsShow(UIConfig.UIDecorateRes))
            {
                MessageCenter.Get<MSG.DECORATE_RES_UI_STATE_CHANGE>().Dispatch(true);
                uiMgr.Visible(Panel, true);
            }
            else
            {
                uiMgr.OpenWindow(Panel);
            }
        }

        public void TryComplete()
        {
            if (AllEnd)
            {
                UIManager.Instance.CloseWindow(Panel);
                UIManager.Instance.CloseWindow(UIConfig.UIDecorateRes);
                NeedRefreshUI = true;
                Game.Manager.rewardMan.CommitReward(GroupReward[0]);
                GroupReward.Clear();
                UIManager.Instance.RegisterIdleAction("ui_idle_complete", 203,
                    () =>
                    {
                        Game.Instance.StartCoroutineGlobal(
                            Game.Manager.mapSceneMan.AreaCompleteVisual(Activity.CurArea));
                    });
                return;
            }

            if (StartNewGroup)
            {
                StartNewGroup = false;
                UIManager.Instance.CloseWindow(Panel);
                UIManager.Instance.CloseWindow(UIConfig.UIDecorateRes);
                NeedRefreshUI = true;
                Game.Manager.rewardMan.CommitReward(GroupReward[0]);
                GroupReward.Clear();
                UIManager.Instance.RegisterIdleAction("ui_idle_complete", 203,
                    () =>
                    {
                        Game.Instance.StartCoroutineGlobal(
                            Game.Manager.mapSceneMan.AreaCompleteVisual(Activity.CurArea));
                    });
            }
        }

        public void AfterComplete()
        {
            if (!AllEnd)
            {
                if (Activity.Active) UIManager.Instance.OpenWindow(Activity.RestartUI.ActiveR);
            }
            else
            {
                Game.Manager.activity.EndImmediate(Activity, false);
            }
        }

        public void PlayAnim()
        {
            IEnumerator wait()
            {
                yield return new WaitForSeconds(2.5f);
                DebugEx.FormatInfo("Wait Lock Anim End,Set Block False");
                UIManager.Instance.Block(false);
                CurAnimNum = 0;
            }

            if (!UIManager.Instance.IsShow(Panel)) return;
            if (AnimList.Count <= 0)
            {
                if (CurAnimNum == 0)
                    UIManager.Instance.Block(false);
                else
                    Game.Instance.StartCoroutineGlobal(wait());
                TryComplete();
                DebugEx.FormatInfo("All Anim End:Set Block Wait {0}", CurAnimNum != 0);
                return;
            }

            AnimList.First().callback.Invoke();
            DebugEx.FormatInfo("Anim Play End:Anim Type {0}", AnimList.First().priority);
            AnimList.RemoveAt(0);
        }

        public void SortAnimList()
        {
            if (AnimList.Count > 1) AnimList.Sort((a, b) => a.priority - b.priority);
        }

        public void FinishFlyCoin(int amount)
        {
            FlyScore -= amount;
            MessageCenter.Get<MSG.DECORATE_SCORE_FLY>().Dispatch();
        }

        public void TryPlayComplete()
        {
            if (Activity == null)
                return;
            if (!Activity.NeedComplete)
                return;
            Game.Manager.screenPopup.TryQueue(Activity.RestartPop, PopupType.Login);
        }

        public bool CheckCanPreview()
        {
            return Activity != null && Activity.CheckCanPreview();
        }

        #endregion

        #region 内部实现

        //  解锁装饰
        private void Unlock(int id)
        {
            var conf = Game.Manager.configMan.GetEventDecorateInfo(id);
            UnlockDecoration.Add(id);
            if (!TotalUnlockDecoration.Contains(id))
                TotalUnlockDecoration.Add(id);
            _lastUnlockID = id;
            DataTracker.event_decorate.Track(Activity.Lite.Id, Activity.Lite.Param, Activity.phase + 1,
                Activity.CurLevelConf.Id, id, Activity.Lite.From);
            DataTracker.TrackLogInfo(string.Format("track decorate log id: {0}", _trackID));
            _trackID++;
            Activity.UpdateScore(-conf.Price);
            TryClaimReward();
        }

        //发放后续奖励
        private void TryClaimReward()
        {
            if (!CheckLevelReward())
                return;
            Game.Manager.screenPopup.Block(ignore_: true);
            //领取阶段奖励并解锁下一阶段
            if (LevelReward.Count > 0) LevelReward.Clear();
            LevelReward.AddRange(Activity.ClaimLevelReward());
            Activity.TryGotoNextLevel();
            if (!CheckGroupReward())
                goto BlockExpression;
            //领取活动组奖励并解锁下一活动组
            GroupReward.AddRange(Activity.ClaimGroupReward());
            Activity.TryGotoNextGroup();
            StartNew();
        BlockExpression:
            Game.Manager.screenPopup.Block(ignore_: false);
            Game.Manager.archiveMan.SendImmediately(true);
        }

        //检测是否有阶段奖励待领取
        private bool CheckLevelReward()
        {
            if (!Activity.Valid)
                return false;
            if (Activity.CurLevelConf == null)
                return false;
            foreach (var variable in Activity.CurLevelConf.DecorateID)
            {
                if (UnlockDecoration.Contains(variable))
                    continue;
                return false;
            }

            return true;
        }

        //检测是否有里程碑奖励待领取
        private bool CheckGroupReward()
        {
            if (!Activity.Valid)
                return false;

            //领取阶段奖励之后会尝试进入下一阶段，如果发现无法进入下一阶段则CurLevelConf会至空
            return Activity.CurLevelConf == null;
        }

        //检测装饰是否过期
        private void CheckDecorationActive()
        {
            var time = Game.Instance.GetTimestampSeconds();
            if (time < EndTime)
                return;
            ClearUnlock();
        }

        private void StartNew()
        {
            UnlockDecoration.Clear();
            if (Activity.CurGroupConf != null)
            {
                StartNewGroup = true;
                Activity.ChangePopState(true);
                Activity.SetCompleteState(true);
                MessageCenter.Get<MSG.DECORATE_SCORE_UPDATE>().Dispatch(Activity, 0);
            }
            else
            {
                AllEnd = true;
            }
        }

        #endregion

        public void FillData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData;
            data.DecorateActivity ??= new fat.gamekitdata.DecorateActivity();
            data.DecorateActivity.EndTime = EndTime;
            data.DecorateActivity.Id = EventID;
            data.DecorateActivity.Decoration.Clear();
            data.DecorateActivity.Decoration.AddRange(UnlockDecoration);
            data.DecorateActivity.Unlock.Clear();
            data.DecorateActivity.Unlock.AddRange(TotalUnlockDecoration);
        }

        public void SetData(LocalSaveData archive)
        {
            if (archive.ClientData.PlayerGameData.DecorateActivity == null)
                return;
            var data = archive.ClientData.PlayerGameData.DecorateActivity;
            EventID = data.Id;
            EndTime = data.EndTime;
            if (data.Decoration != null)
            {
                UnlockDecoration.Clear();
                UnlockDecoration.AddRange(data.Decoration);
            }

            if (data.Unlock != null)
            {
                TotalUnlockDecoration.Clear();
                TotalUnlockDecoration.AddRange(data.Unlock);
            }

            CheckDecorationActive();
        }

        public void Reset()
        {
            Activity = null;
            EventID = 0;
        }

        public void Startup()
        {
            MessageCenter.Get<MSG.DECORATE_ANIM_END>().AddListenerUnique(PlayAnim);
            MessageCenter.Get<MSG.SCENE_LOAD_FINISH>().AddListenerUnique(TryPlayComplete);
        }

        public void LoadConfig()
        {
        }

        public bool GuideRequireCheck(int param)
        {
            if (Activity == null)
                return false;
            var uiMgr = UIManager.Instance;
            var isShow = uiMgr.IsOpen(Panel) && uiMgr.IsShow(Panel);
            if (param == 0)
            {
                return isShow;
            }
            else
            {
                return isShow && uiMgr.IsVisible(Panel);
            }
        }

        #region Debug

        public void ClearUnlock()
        {
            UnlockDecoration.Clear();
            TotalUnlockDecoration.Clear();
            MessageCenter.Get<MSG.DECORATE_REFRESH>().Dispatch();
        }

        public void Update(float t)
        {
            if (WaitAction == null)
                return;
            if (Game.Manager.specialRewardMan.IsBusyShow())
                return;
            if (UIManager.Instance.GetLayerRootByType(UILayer.AboveStatus).childCount > 0 ||
                UIManager.Instance.GetLayerRootByType(UILayer.SubStatus).childCount > 0)
                return;
            WaitAction?.Invoke();
            WaitAction = null;
        }

        public void RegisterWaitAction(Action callback)
        {
            WaitAction = null;
            WaitAction = callback;
        }

        #endregion
    }
}
