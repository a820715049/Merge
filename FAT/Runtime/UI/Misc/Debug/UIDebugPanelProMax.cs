/*
 * @Author: tang.yan
 * @Description: Debug面板ProMax版本
 * @Date: 2025-02-28 10:02:37
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using EL;
using FAT.Merge;
using fat.rawdata;
using CenturyGame.AppUpdaterLib.Runtime.Managers;
using System.Linq;
using System.Text;
using Cysharp.Text;
using EventType = fat.rawdata.EventType;
using TMPro;
using FAT.Platform;

namespace FAT
{
    public class UIDebugPanelProMax : UIBase
    {
        public static bool FightDebug = false;
        private Transform mRootPanel;
        private Transform mRootTL;
        private Transform mRootTR;
        private TMP_Text txtNowTime;

        private GameObject goGroupA;
        private GameObject goGroupB;
        private GameObject goGroupC;
        private GameObject goGroupD;
        private GameObject goItem;
        private GameObject goCompButton;
        private GameObject goCompInput;

        private List<UISimpleToggle> tabToggleList = new List<UISimpleToggle>();
        private List<UIDebugScrollView> scrollViewList = new List<UIDebugScrollView>();
        private UIDebugScrollLog scrollLog;

        private Transform mCurScrollRoot;
        private Transform mCurItemRoot;
        private int curSelectTabIndex = 0;
        private UnityAction lastCmd;
        private Dictionary<TMP_Text, Action<TMP_Text>> mInfoUpdater = new();
        private List<string> _allLanguageList = new();

        protected override void OnCreate()
        {
            mRootPanel = transform.Find("Content/Root");
            mRootTL = transform.Find("Content/TL");
            mRootTR = transform.Find("Content/TR");
            transform.FindEx("Content/Root/item_groupA", out goGroupA);
            transform.FindEx("Content/Root/item_groupB", out goGroupB);
            transform.FindEx("Content/Root/item_groupC", out goGroupC);
            transform.FindEx("Content/Root/item_groupD", out goGroupD);
            transform.FindEx("Content/Root/item_container", out goItem);
            transform.FindEx("Content/Root/item_button", out goCompButton);
            transform.FindEx("Content/Root/item_input", out goCompInput);

            transform.AddButton("Content/TR/BtnMin", _OnBtnMinimize);
            transform.AddButton("Content/TR/BtnClose", Close);
            transform.AddButton("Content/TL/BtnResume", _OnBtnResume);
            transform.AddButton("Content/TL/BtnRepeat", _OnBtnRepeat);
            transform.AddButton("Content/TL/BtnCloseMin", Close);
            txtNowTime = transform.FindEx<TMP_Text>("Content/TL/Time/Text");

            _InitScrollView();
            _InitToggle();
            //依次构建各个页签下的debug功能
            _Build();
        }

        private void _Build()
        {
            //QA专用页签，聚合了QA平时常用的debug功能，更方便QA使用
            _BuildQATab();
            //通用页签，主要包含影响游戏整体状态的通用debug功能
            //划分依据：1.影响游戏全局状态 2.适用于所有玩法、场景 3.调试基础核心机制
            _BuildCommonTab();
            //活动页签，包含活动系统基础的debug功能，以及具体活动业务逻辑要用到的debug功能
            _BuildActivityTab();
            //其他页签，主要包含游戏核心玩法之外的不可或缺的支持类debug功能，如账号个性化设置(如用户分层)、SDK相关、服务器相关等
            _BuildOtherTab();
            //抓取游戏日志页签，用于支持运行时输入关键字检索游戏输出日志，单独成一页
            _BuildLogTab();
        }

        protected override void OnPreOpen()
        {
            lastCmd = null;
            _SetState(true);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(_UpdateSeconds);
        }

        protected override void OnPreClose()
        {
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(_UpdateSeconds);
            BoardQuickMergeTool.Reset();
        }

        public void OnDestroy()
        {
            scrollLog?.Clear();
        }

        public void Update()
        {
#if UNITY_EDITOR
            //只在编辑器环境下检测快捷键
            _CheckCombo();
#endif
            //尝试执行自动快速合成
            BoardQuickMergeTool.Update();
        }

        #region build

        //构建通用页签里的debug功能项
        private void _BuildCommonTab()
        {
            mCurScrollRoot = scrollViewList[1].Content;

            _StartGroupC("游戏控制");
            _RegisterButton("timescale x10", () =>
            {
                if (Time.timeScale != 1f) Time.timeScale = 1f;
                else Time.timeScale = 10f;
            });
            _RegisterButton("timescale /10", () =>
            {
                if (Time.timeScale != 1f) Time.timeScale = 1f;
                else Time.timeScale = 0.1f;
            });
            _RegisterButtonWithInput("Set TimeScale", (string str) =>
            {
                var isNumber = float.TryParse(str, out var scale);
                scale = isNumber ? scale : 1f;
                if (scale >= 100f) scale = 100f;
                if (scale <= 0) scale = 1f;
                Time.timeScale = scale;
                Game.Manager.commonTipsMan.ShowClientTips("Set TimeScale = " + scale);
            });
            _RegisterButton("restart", () => GameProcedure.RestartGame());

            _StartGroupA("货币资源");
            _RegisterButton("+ coin", () => { _OnBtnAddMergeCoin(true); });
            _RegisterButton("- coin", () => { _OnBtnAddMergeCoin(false); });
            _RegisterButton("+ diamond", () => { _OnBtnAddGem(true); });
            _RegisterButton("- diamond", () => { _OnBtnAddGem(false); });
            _RegisterButton("+ energy", () => { _OnBtnAddEnergy(true); });
            _RegisterButton("- energy", () => { _OnBtnAddEnergy(false); });
            _RegisterButton("levelup", _OnBtnTryLevelUp);
            _RegisterButton("level+1", _OnBtnLevelUp1);
            _RegisterButton("openlevelup", _OnBtnOpenLevelUp);
            _RegisterButton("reset level", () => Game.Manager.mergeLevelMan.DebugReset());

            _StartGroupC("发资源/发道具/发棋子/发活动Token");
            _RegisterButtonWithInput("get item", _OnBtnAddItem);
            _RegisterButtonWithInput("add to board", (idStr) =>
            {
                if (int.TryParse(idStr, out var id))
                {
                    var board = Game.Manager.mergeBoardMan.activeWorld;
                    if (board != null)
                        board.activeBoard.SpawnItemMustWithReason(id,
                            ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false, false);
                }
            });
            //图鉴按链条id发送链上所有棋子到主棋盘
            _RegisterButtonWithInput("get handbook item", _OnBtnHandBook);
            //打印没有正确被commit的奖励
            _RegisterButton("reward info", () => Game.Manager.rewardMan.ReportCommit());

            _StartGroupB("棋盘/订单/meta场景操作/guide");
            _RegisterButton("orderpass", () => OrderUtility.SetDebug(!OrderUtility.isDebug));
            _RegisterButton("orderitem", _OnBtnAddOrderItem);
            _RegisterButton("clear board", _OnBtnClearBoard);
            _RegisterButton("quick merge", _OnBtnQuickMerge);
            _RegisterButton("auto quick merge", _OnBtnAutoQuickMerge);
            _RegisterButton("delete select item", _OnBtnDeleteSelectItem);
            _RegisterButton("Freeze Item", _OnBtnFreezeItem);               // 锁住选中的item
            _RegisterButtonWithInput("guide flip", _OnBtnGuideFlip);        // 切换guide状态
            _RegisterButtonWithInput("board step(s)", _OnUpdateBoard);
            _RegisterButtonWithInput("claim all bonus", (idStrs) =>
            {
                var ids = idStrs.Split(',');
                var idsSet = new HashSet<int>();
                foreach (var idStr in ids)
                    if (int.TryParse(idStr, out var id))
                        idsSet.Add(id);

                _OnBtnMinimize();
                Game.Manager.mergeBoardMan.ClaimAllBonus(idsSet);
            });
            _RegisterButton("log board item", _OnBtnLogBoardItem);
            _RegisterButton("log all item", _OnBtnLogAllItem);
            _RegisterButton("map reset", () => Game.Manager.mapSceneMan.DebugReset());

            _StartGroupC("修改游戏时间");
            _RegisterButtonWithInput("+ day", str => _SetBiasOffset(str, 86400));
            _RegisterButtonWithInput("+ hour", str => _SetBiasOffset(str, 3600));
            _RegisterButtonWithInput("+ min", str => _SetBiasOffset(str, 60));
            _RegisterButtonWithInput("set net bias", _OnBtnSetNetBias, _NetBiasDisplay);

            _StartGroupC("测试棋子产出分布");
            _RegisterButtonWithInput("output simulate", _OnBtnOutputTest);

            _StartGroupD("当前游戏时间");
            _RegisterInfo("time", _UpdateTimeBias);
        }

        //构建活动页签里的debug功能项
        private void _BuildActivityTab()
        {
            mCurScrollRoot = scrollViewList[2].Content;

            _StartGroupB("活动加/减");
            _RegisterButtonWithInput("+activity", Game.Manager.activity.DebugActivate);
            _RegisterButtonWithInput("++activity", Game.Manager.activity.DebugInsert);
            _RegisterButtonWithInput("--activity", Game.Manager.activity.DebugEnd);

            _StartGroupB("活动状态管理");
            _RegisterButton("pack reset", () => Game.Manager.activity.DebugReset());
            _RegisterButton("popup reset", () => Game.Manager.screenPopup.DebugReset());
            _RegisterButton("activity reset", () =>
            {
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            });
            _RegisterButton("activity ready info", () => Game.Manager.activity.DebugReportReady());
            _RegisterButton("eval info", () => Game.Manager.activityTrigger.DebugEvalauteInfo());
            _RegisterButton("trigger info", () => Game.Manager.activityTrigger.DebugReportState());
            _RegisterButton("ETrigger reset", () => Game.Manager.activityTrigger.DebugReset());

            _StartGroupA("每日任务");
            _RegisterButton("daily reset", () => Game.Manager.dailyEvent.DebugReset());
            _RegisterButton("daily complete", () => Game.Manager.dailyEvent.DebugClaimNext());
            _RegisterButton("daily almost", () => Game.Manager.dailyEvent.DebugAlmostCompleteNext());
            _RegisterButton("weekly complete", () => (Game.Manager.activity.LookupAny(EventType.WeeklyTask) as ActivityWeeklyTask)?.DebugCompleteTask());
            _RegisterButtonWithInput("weekly id", (str) =>
            {
                if (int.TryParse(str, out var id))
                    (Game.Manager.activity.LookupAny(EventType.WeeklyTask) as ActivityWeeklyTask)?.DebugCompleteTaskById(id);
            });

            _StartGroupA("卡册");
            _RegisterButton("card reset", () => Game.Manager.cardMan.DebugClearCardData());
            _RegisterButton("card draw", () => Game.Manager.cardMan.OpenDrawCardDebugUI());

            _StartGroupA("装饰区");
            _RegisterButton("deco cloud reset", () => Game.Manager.decorateMan.DebugResetCloud());
            _RegisterButton("deco complete", () => Game.Manager.decorateMan.DebugComplete());
            _RegisterButton("reset dec", Game.Manager.decorateMan.ClearUnlock);
            _RegisterButton("deco overview", () =>
            {
                UIManager.Instance.OpenWindow(UIConfig.UIDecorateOverview);
                GameProcedure.MergeToSceneArea(Game.Manager.decorateMan.Activity.CurArea, overview_: true);
            });

            _StartGroupA("阶梯活动");
            _RegisterButton("step complete",
                () => (Game.Manager.activity.LookupAny(EventType.Step) as ActivityStep)?.DebugComplete());
            _RegisterButton("step reset",
                () => (Game.Manager.activity.LookupAny(EventType.Step) as ActivityStep)?.DebugReset());

            _StartGroupA("热气球");
            _RegisterButton("AddScore", () => RaceManager.GetInstance().Race.AddScore());
            _RegisterButton("BotAddScore", () => RaceManager.GetInstance().Race.AddScoreBot());
            _RegisterButtonWithInput("jump to", str => RaceManager.GetInstance().Race.JumpToNext(str));

            _StartGroupC("排行榜/弹珠活动/积分活动/迷你游戏");
            //排行榜
            _RegisterButtonWithInput("ranking", (str) =>
            {
                int.TryParse(str, out var index);
                (Game.Manager.activity.LookupAny(EventType.Rank) as ActivityRanking)?.Test(index);
                Close();
            });
            //弹珠活动
            _RegisterButton("Pachinko Debug", _OnBtnPachinkoDebug);
            //积分活动
            _RegisterButton("ScoreActivity +100", () =>
            {
                var act = Game.Manager.activity.LookupAny(EventType.Score) as ActivityScore;
                if (act != null) act.DebugAddScore(ScoreEntity.ScoreType.OrderRight, 100);
            });
            _RegisterButtonWithInput("ScoreActivity +N", (str) =>
            {
                int.TryParse(str, out var score);
                var act = Game.Manager.activity.LookupAny(EventType.Score) as ActivityScore;
                if (act != null) act.DebugAddScore(ScoreEntity.ScoreType.OrderRight, score);
            });
            //迷你游戏
            _RegisterButtonWithInput("MiniGame", _OnBtnSetMIniGame);

            _StartGroupB("棋盘活动/冰冻棋子");
            _RegisterButton("ResetMiniBoard", () =>
            {
                Game.Manager.miniBoardMan.DebugResetMiniBoard();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
                Game.Manager.screenPopup.DebugReset();
            });
            _RegisterButton("ResetMiniBoardMulti", () =>
            {
                Game.Manager.miniBoardMultiMan.DebugResetMiniBoard();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
                Game.Manager.screenPopup.DebugReset();
            });
            _RegisterButton("ResetMineBoard", () =>
            {
                Game.Manager.mineBoardMan.DebugResetMineBoard();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
                Game.Manager.screenPopup.DebugReset();
            });
            //设置体力消耗次数
            _RegisterButtonWithInput("ErgTapCount", (numStr) =>
            {
                if (int.TryParse(numStr, out var num) && (Game.Manager.activity.LookupAny(EventType.FrozenItem) is ActivityFrozenItem frozen))
                {
                    frozen.DebugSetTapCount(num);
                    Game.Manager.commonTipsMan.ShowMessageTips($"Set ErgConsumeTimes success for {num}", isSingle: true);
                }
            });
            //设置体力消耗总数
            _RegisterButtonWithInput("ErgTotalNum", (numStr) =>
            {
                if (int.TryParse(numStr, out var num) && (Game.Manager.activity.LookupAny(EventType.FrozenItem) is ActivityFrozenItem frozen))
                {
                    frozen.DebugSetEnergyConsumed(num);
                    Game.Manager.commonTipsMan.ShowMessageTips($"Set ErgConsumeNum success for {num}", isSingle: true);
                }
            });
            //设置是否必出冰冻棋子
            _RegisterButton("MustSpawnFrozenItem",() =>
            {
                if ((Game.Manager.activity.LookupAny(EventType.FrozenItem) is ActivityFrozenItem frozen))
                {
                    frozen.DebugMustSpawnFrozenItem();
                    Game.Manager.commonTipsMan.ShowMessageTips($"Set MustSpawnFrozenItem success for {frozen.MustSpawnFrozenItem}", isSingle: true);
                }
            });

            _StartGroupA("猜颜色");
            _RegisterButton("guess milestone", ActivityGuess.DebugLastM);
            _RegisterButton("guess ready", ActivityGuess.DebugReady);
            _RegisterButton("invite reset", ActivityInvite.DebugReset);
            _RegisterButton("invite advance", ActivityInvite.DebugAdvance);
            _RegisterButton("duel score", ActivityDuel.DebugAddScore);
            _RegisterButton("duel robot", ActivityDuel.DebugAddRobotScore);

            _StartGroupA("火车");
            _RegisterButton("order item", _DebugTrainMissionOrderItem);
            _RegisterButton("order need", _DebugTrainMissionOrderNeed);
            _RegisterButton("finish train", _DebugTrainMissionFinishTrain);
            _RegisterButton("recycle", _DebugTrainMissionRecycle);

            _StartGroupA("打怪");
            _RegisterDisplayButton("一刀999:" + FightDebug, _ChangeFightDebugState);

            _StartGroupA("Bingo Item");
            _RegisterButton("一键发棋子", ItemBingoUtility.DebugBingoItem);


            _StartGroupA("签到");
            _RegisterButton("Show", () => UIManager.Instance.OpenWindow(UIConfig.UISignInpanel));
            _RegisterButton("Reset", () => Game.Manager.loginSignMan.DebugReset());
            _RegisterButtonWithInput("SetTotalSign", (str) =>
            {
                int.TryParse(str, out var day);
                Game.Manager.loginSignMan.DebugSetTotalSign(day);
            });

            _StartGroupA("ClawOrder");
            _RegisterButton("clear", ActivityClawOrder.DebugReset);
            _StartGroupA("倍率排行榜");
            _RegisterButtonWithInput("Add Score", (string str) =>
            {
                if (Game.Manager.activity.LookupAny(EventType.MultiplierRanking, out var multi))
                {
                    (multi as ActivityMultiplierRanking).AddScoreDebug(str.ConvertToInt());
                }
            });
            _RegisterButtonWithInput("Set Energy", (string str) =>
            {
                if (Game.Manager.activity.LookupAny(EventType.MultiplierRanking, out var multi))
                {
                    (multi as ActivityMultiplierRanking).SetEnergy(str.ConvertToInt());
                }
            });
        }

        private void _UnfrozenAndUnlockAllItems()
        {
            var world = Game.Manager.mergeBoardMan.activeWorld;
            if (world == null || world.activeBoard == null)
            {
                return;
            }
            var board = world.activeBoard;
            var size = board.size;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var item = board.GetItemByCoord(x, y);
                    if (item != null && (item.isLocked || item.isFrozen))
                    {
                        // 先解除锁定状态
                        if (item.isLocked)
                        {
                            item.SetState(false, item.isFrozen);
                            board.TriggerItemStatusChange(item);
                        }
                        // 再解除冻结状态
                        if (item.isFrozen)
                        {
                            board.UnfrozenItem(item);
                        }
                    }
                }
            }
        }

        private string _ChangeFightDebugState()
        {
            FightDebug = !FightDebug;
            return "一刀999:" + FightDebug;
        }

        //构建其他杂项页签里的debug功能项
        private void _BuildOtherTab()
        {
            mCurScrollRoot = scrollViewList[3].Content;

            _StartGroupC("账户信息/IAP");
            _RegisterButton("accountinfo", _OnBtnShowInfo);
            _RegisterButton("login info", () =>
            {
                var sdk = PlatformSDK.Instance.Adapter;
                var str = $"login:{sdk.LoginType} cache:{sdk.LoginCache} session:{sdk.SessionId} session_last:{sdk.LoginId1}";
                DebugEx.Info(str);
                Game.Manager.commonTipsMan.ShowMessageTips(str, isSingle: true);
            });
            _RegisterButtonWithInput("set iapamount(cent)", Game.Manager.iap.TotalIAPServer.ToString(),
                _OnBtnSetIAPCent);
            _RegisterButton("iap history", Game.Manager.iap.TestHistory);
            _RegisterButton("report iap history", Game.Manager.iap.ReportHistory);

            _StartGroupC("AB测试分组");
            //展示用户AB测试信息
            _RegisterButton("ABTest info", _ShowABTestInfo);
            //修改用户AB测试分组
            _RegisterButtonWithInput("ABTest change", _OnBtnChangeABTest);
            //展示用户AB测试Tag信息
            _RegisterButton("ABTest tagInfo", _ShowABTestTagInfo);

            _StartGroupC("用户分层");
            //是否忽略服务器传来的用户分层tag标签 以及 是否允许自定义tag
            _RegisterDisplayButton("IgnoreTagExpire : " + Game.Manager.userGradeMan.IsIgnoreTagExpire,
                _OnBtnSetIgnoreTagExpire);
            //修改用户分层grade标签
            _RegisterButtonWithInput("UserGrade change", _OnBtnChangeUserTag);
            //展示用户分层grade标签信息
            _RegisterButton("UserGrade info", _ShowUserTagInfo);
            //重置难度API结果过期时间
            _RegisterButton("Reset Api ExpireTime", _OnBtnResetApiExpireTs);
            //设置难度API请求延迟时间
            _RegisterButtonWithInput("SetApiDelayTime(s)", _OnBtnSetApiDelayTime, Game.Manager.userGradeMan.DebugDelayTime.ToString);

            _StartGroupC("Light House");
            _RegisterButtonWithInput("set channel", _OnBtnSetChannel);

            _RegisterDisplayButton($"channel: {_GetLightHouseOverrideChannel(false)}", () =>
            {
                var next = _GetLightHouseOverrideChannel(true);
                if (next == "online")
                    GameUpdateManager.SetOverrideChannel(string.Empty);
                else
                    GameUpdateManager.SetOverrideChannel(next);
                return $"channel: {next}";
            });
            _RegisterDisplayButton($"LH client: {_GetLightHouseClientId(false)}", () =>
            {
                var next = _GetLightHouseClientId(true);
                if (next == "normal")
                    GameUpdateManager.SetClientId(string.Empty);
                else
                    GameUpdateManager.SetClientId(next);
                return $"LH client: {next}";
            });

            _StartGroupD("游戏版本");
            _RegisterInfo("version", _UpdateVersion1);
            _RegisterInfo("version", _UpdateVersion2);

            _StartGroupA("多语言切换");
            _allLanguageList.Clear();
            I18N.GetAllLanguage(_allLanguageList);
            foreach (var language in _allLanguageList)
                _RegisterButton(language, () => { GameI18NHelper.GetOrCreate().SwitchTargetLanguage(language); });

            _StartGroupA("SDK相关");
            _RegisterButton("adsdebug", _AdsDebug);
            _RegisterButton("adsplaytest", _AdsPlay);
            _RegisterButton("notification", () => Game.Manager.notification.DebugTest());
            _RegisterButton("notification info", () => Game.Manager.notification.DebugInfo());
            _RegisterButton("deeplink share", () => PlatformSDK.Instance.shareLink.TestSend(true));
            _RegisterButton("deeplink send", () => PlatformSDK.Instance.shareLink.TestSend(false));
            _RegisterButton("deeplink payload", () => PlatformSDK.Instance.shareLink.LinkPayload(null));
            _RegisterButtonWithInput("cdkey", Game.Manager.cdKeyMan.DebugExchangeGiftCode);
            _RegisterDisplayButton("switch track", () =>
            {
                DataTracker.TrackEnable = !DataTracker.TrackEnable;
                return DataTracker.TrackEnable ? "track ON" : "track OFF";
            });
            _RegisterButton("clear LinkData", () => Game.Manager.communityLinkMan.DebugClearCommunityLinkData());
            _RegisterButtonWithInput("RankNum", (str) =>
            {
                int.TryParse(str, out var rankNum);
                MessageCenter.Get<MSG.MULTIPLIER_RANKING_SLOTS_CHANGE>().Dispatch(rankNum);
            });
        }

        //构建日志页签里的相关功能
        private void _BuildLogTab()
        {
            _InitScrollLog();
        }

        //构建QA专用页签里的debug功能项
        private void _BuildQATab()
        {
            mCurScrollRoot = scrollViewList[0].Content;

            _StartGroupC("游戏控制");
            _RegisterButton("timescale x10", () =>
            {
                if (Time.timeScale != 1f) Time.timeScale = 1f;
                else Time.timeScale = 10f;
            });
            _RegisterButton("timescale /10", () =>
            {
                if (Time.timeScale != 1f) Time.timeScale = 1f;
                else Time.timeScale = 0.1f;
            });
            _RegisterButtonWithInput("Set TimeScale", (string str) =>
            {
                var isNumber = float.TryParse(str, out var scale);
                scale = isNumber ? scale : 1f;
                if (scale >= 100f) scale = 100f;
                if (scale <= 0) scale = 1f;
                Time.timeScale = scale;
                Game.Manager.commonTipsMan.ShowClientTips("Set TimeScale = " + scale);
            });
            _RegisterButton("restart", () => GameProcedure.RestartGame());

            _StartGroupA("货币资源");
            _RegisterButton("+ coin", () => { _OnBtnAddMergeCoin(true); });
            _RegisterButton("- coin", () => { _OnBtnAddMergeCoin(false); });
            _RegisterButton("+ diamond", () => { _OnBtnAddGem(true); });
            _RegisterButton("- diamond", () => { _OnBtnAddGem(false); });
            _RegisterButton("+ energy", () => { _OnBtnAddEnergy(true); });
            _RegisterButton("- energy", () => { _OnBtnAddEnergy(false); });
            _RegisterButton("levelup", _OnBtnTryLevelUp);
            _RegisterButton("level+1", _OnBtnLevelUp1);
            _RegisterButton("openlevelup", _OnBtnOpenLevelUp);

            _StartGroupC("发资源/发道具/发棋子/发活动Token");
            _RegisterButtonWithInput("add to board", (idStr) =>
            {
                if (int.TryParse(idStr, out var id))
                {
                    var board = Game.Manager.mergeBoardMan.activeWorld;
                    if (board != null)
                        board.activeBoard.SpawnItemMustWithReason(id,
                            ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false, false);
                }
            });
            _RegisterButtonWithInput("get item", _OnBtnAddItem);
            _RegisterButtonWithInput("Update Task", _OnBtnUpdateTask);
            //图鉴按链条id发送链上所有棋子到主棋盘
            _RegisterButtonWithInput("get handbook item", _OnBtnHandBook);

            _StartGroupC("修改游戏时间");
            _RegisterButtonWithInput("+ day", str => _SetBiasOffset(str, 86400));
            _RegisterButtonWithInput("+ hour", str => _SetBiasOffset(str, 3600));
            _RegisterButtonWithInput("+ min", str => _SetBiasOffset(str, 60));
            _RegisterButtonWithInput("set net bias", _OnBtnSetNetBias, _NetBiasDisplay);

            _StartGroupC("AB测试分组");
            //展示用户AB测试信息
            _RegisterButton("ABTest info", _ShowABTestInfo);
            //修改用户AB测试分组
            _RegisterButtonWithInput("ABTest change", _OnBtnChangeABTest);

            _StartGroupC("用户分层");
            //是否忽略服务器传来的用户分层tag标签 以及 是否允许自定义tag
            _RegisterDisplayButton("IgnoreTagExpire : " + Game.Manager.userGradeMan.IsIgnoreTagExpire,
                _OnBtnSetIgnoreTagExpire);
            //修改用户分层grade标签
            _RegisterButtonWithInput("UserGrade change", _OnBtnChangeUserTag);
            //展示用户分层grade标签信息
            _RegisterButton("UserGrade info", _ShowUserTagInfo);
            //重置难度API结果过期时间
            _RegisterButton("Reset API ExpireTime", _OnBtnResetApiExpireTs);
            //设置难度API请求延迟时间
            _RegisterButtonWithInput("SetApiDelayTime(s)", _OnBtnSetApiDelayTime, Game.Manager.userGradeMan.DebugDelayTime.ToString);

            _StartGroupB("棋盘/订单操作");
            _RegisterButton("orderpass", () => OrderUtility.SetDebug(!OrderUtility.isDebug));
            _RegisterButton("orderitem", _OnBtnAddOrderItem);
            _RegisterButton("clear board", _OnBtnClearBoard);
            _RegisterButton("quick merge", _OnBtnQuickMerge);
            _RegisterButton("auto quick merge", _OnBtnAutoQuickMerge);
            _RegisterButton("unlock board", _UnfrozenAndUnlockAllItems);
            _RegisterButton("delete select item", _OnBtnDeleteSelectItem);

            _StartGroupB("活动重置");
            _RegisterButton("pack reset", () => Game.Manager.activity.DebugReset());
            _RegisterButton("popup reset", () => Game.Manager.screenPopup.DebugReset());
            _RegisterButton("activity reset", () =>
            {
                Game.Manager.decorateMan.ClearUnlock();
                Game.Manager.activityTrigger.DebugReset();
                Game.Manager.activity.DebugReset();
            });

            _StartGroupA("多语言切换");
            _allLanguageList.Clear();
            I18N.GetAllLanguage(_allLanguageList);
            _DebugChangeLanguage();

            _StartGroupD("资源显示检查");
            _RegisterInfo("reward", _UpdateCoin);
        }

        #region 基础Add方法

        //以A类型开始一个组结构
        private void _StartGroupA(string groupTitle = "Default Title")
        {
            var go = Instantiate(goGroupA, mCurScrollRoot);
            go.SetActive(true);
            var trans = go.transform;
            var titleText = trans.FindEx<Text>("Title");
            titleText.text = groupTitle;
            mCurItemRoot = trans.Find("Content");
        }

        //以B类型开始一个组结构
        private void _StartGroupB(string groupTitle = "Default Title")
        {
            var go = Instantiate(goGroupB, mCurScrollRoot);
            go.SetActive(true);
            var trans = go.transform;
            var titleText = trans.FindEx<Text>("Title");
            titleText.text = groupTitle;
            mCurItemRoot = trans.Find("Content");
        }

        //以C类型开始一个组结构
        private void _StartGroupC(string groupTitle = "Default Title")
        {
            var go = Instantiate(goGroupC, mCurScrollRoot);
            go.SetActive(true);
            var trans = go.transform;
            var titleText = trans.FindEx<Text>("Title");
            titleText.text = groupTitle;
            mCurItemRoot = trans.Find("Content");
        }

        //以C类型开始一个组结构
        private void _StartGroupD(string groupTitle = "Default Title")
        {
            var go = Instantiate(goGroupD, mCurScrollRoot);
            go.SetActive(true);
            var trans = go.transform;
            var titleText = trans.FindEx<Text>("Title");
            titleText.text = groupTitle;
            mCurItemRoot = trans.Find("Content");
        }

        private Transform _AddItem(Transform root)
        {
            var go = Instantiate(goItem, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddButton(Transform root)
        {
            var go = Instantiate(goCompButton, root);
            go.SetActive(true);
            return go.transform;
        }

        private Transform _AddInput(Transform root)
        {
            var go = Instantiate(goCompInput, root);
            go.SetActive(true);
            return go.transform;
        }

        private void _RegisterButton(string desc, Action act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            btn.FindEx<TMP_Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act.Invoke();
                lastCmd = cb;
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterDisplayButton(string desc, Func<string> f)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var text = btn.FindEx<TMP_Text>("Text");
            text.text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                text.text = f.Invoke();
                lastCmd = cb;
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, Action<string> act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input = _AddInput(item).GetComponent<TMP_InputField>();
            btn.FindEx<TMP_Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act.Invoke(input.text);
                lastCmd = cb;
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, Action<string> act, Func<string> display)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input = _AddInput(item).GetComponent<TMP_InputField>();
            btn.FindEx<TMP_Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act.Invoke(input.text);
                lastCmd = cb;
                input.text = display.Invoke();
            };
            input.text = display.Invoke();
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, Action<string, string> act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input1 = _AddInput(item).GetComponent<TMP_InputField>();
            var input2 = _AddInput(item).GetComponent<TMP_InputField>();
            btn.FindEx<TMP_Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () =>
            {
                act.Invoke(input1.text, input2.text);
                lastCmd = cb;
            };
            btn.AddButton(null, cb);
        }

        private void _RegisterButtonWithInput(string desc, string inputDesc, Action<string> act = null)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var input = _AddInput(item).GetComponent<TMP_InputField>();
            input.text = inputDesc;
            btn.FindEx<TMP_Text>("Text").text = desc;
            UnityAction cb = null;
            cb = () => { act?.Invoke(input.text); };
            btn.AddButton(null, cb);
        }

        private void _RegisterInfo(string desc, Action<TMP_Text> act)
        {
            var item = _AddItem(mCurItemRoot);
            var btn = _AddButton(item);
            var area = btn.FindEx<TMP_Text>("Text");
            btn.AddButton(null, () =>
            {
                GUIUtility.systemCopyBuffer = area.text;
                Game.Manager.commonTipsMan.ShowPopTips(Toast.CopySuccess);
            });
            Action<TMP_Text> imp = text =>
            {
                act.Invoke(text);
                text.text = $"{desc} : {text.text}";
            };
            area.enableAutoSizing = true;
            imp.Invoke(area);
            mInfoUpdater.Add(area, imp);
        }

        private void _UpdateSeconds()
        {
            foreach (var entry in mInfoUpdater) entry.Value(entry.Key);

            if (txtNowTime.gameObject.activeInHierarchy)
            {
                var dt = TimeUtility.GetDateTimeFromEpoch(Game.Instance.GetTimestampSeconds());
                txtNowTime.text = $"{dt:yyyy-MM-dd HH:mm:ss}";
            }
        }

        #endregion

        #endregion

        #region 具体debug功能实现逻辑

        private void _OnBtnAddCoin(CoinType ct, bool add)
        {
            if (!add)
            {
                var c = Mathf.Max(1, Game.Manager.coinMan.GetCoin(ct));
                Game.Manager.coinMan.UseCoin(ct, c, ReasonString.cheat).Execute();
            }
            else
            {
                Game.Manager.coinMan.AddCoin(ct, 1000, ReasonString.cheat);
            }
        }

        private void _OnBtnAddMergeCoin(bool add)
        {
            _OnBtnAddCoin(CoinType.MergeCoin, add);
        }

        private void _OnBtnAddGem(bool add)
        {
            _OnBtnAddCoin(CoinType.Gem, add);
        }

        private void _OnBtnAddEnergy(bool add)
        {
            if (!add)
                Game.Manager.mergeEnergyMan.UseEnergy(Game.Manager.mergeEnergyMan.Energy, ReasonString.cheat);
            else
                Game.Manager.mergeEnergyMan.DebugAddEnergy(100, ReasonString.cheat);
        }

        private void _OnBtnClearBoard()
        {
            var world = Game.Manager.mergeBoardMan.activeWorld;
            var board = world?.activeBoard;
            if (board != null)
            {
                var items = new List<Item>();
                board.WalkAllItem((item) => items.Add(item));
                foreach (var item in items) board.DisposeItem(item);

                while (world.rewardCount > 0) world.RemoveItem(world.nextReward);
            }
        }

        //一键把当前场上所有相同的棋子合成1次
        private void _OnBtnQuickMerge()
        {
            BoardQuickMergeTool.QuickMergeOnce();
            //自动缩小面板
            _SetState(false);
        }

        //打开自动快速合成功能
        private void _OnBtnAutoQuickMerge()
        {
            var isOpen = BoardQuickMergeTool.SwitchAutoQuickMerge();
            if (isOpen)
                _SetState(false);
        }

        private void _OnBtnShowInfo()
        {
            var info = "";
            if (Game.Manager.networkMan.isInSync) info += ".isInSync";

            info += "." + Game.Manager.networkMan.state.ToString();

            info += string.Format(".day{0}", Game.Manager.accountMan.playDay);
            info += string.Format(".nextday{0}",
                TimeUtility.GetDateTimeFromEpoch(Game.Manager.accountMan.nextRefreshTime).ToLocalTime().ToString());
            // info += string.Format(".order{0}.compatOrder{1}", Game.Instance.schoolMan.FillTotalCompletedTask(), Game.Instance.schoolMan.GetTotalTaskProgress());
            Game.Manager.commonTipsMan.ShowMessageTips(info);
        }

        private void _OnBtnLevelUp1()
        {
            var mgr = Game.Manager.mergeLevelMan;
            mgr.AddExp(mgr.nextLevelConfig.Exp - mgr.exp, ReasonString.cheat);
            using var _ = ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards);
            if (mgr.TryLevelup(rewards)) UIFlyUtility.FlyRewardList(rewards, Vector3.zero);

            var map = Game.Manager.mapSceneMan;
            if (map.scene.Active) map.RefreshLocked();
        }

        private void _OnBtnOpenLevelUp()
        {
            var mgr = Game.Manager.mergeLevelMan;
            mgr.AddExp(mgr.nextLevelConfig.Exp - mgr.exp, ReasonString.cheat);
            var rewards = new List<RewardCommitData>();
            if (mgr.TryLevelup(rewards))
            {
                _OnBtnMinimize();
                UIManager.Instance.OpenWindow(UIConfig.UILevelUp, rewards);
            }

            Game.Manager.mapSceneMan.RefreshLocked();
        }

        private void _OnBtnTryLevelUp()
        {
            if (Game.Manager.mergeLevelMan.canLevelup)
            {
                Close();
                Game.Instance.StartCoroutineGlobal(_CoTryLevelUp());
            }
        }

        private void _UpdateCoin(TMP_Text text)
        {
            var logInfo = new StringBuilder();
            var energyMan = Game.Manager.mergeEnergyMan;
            if (energyMan != null)
            {
                logInfo.AppendLine(energyMan.EnergyAfterFly == energyMan.Energy
                    ? $"Energy true: {energyMan.EnergyAfterFly}, display: {energyMan.Energy}"
                    : $"Energy true: <color=#9f3a2c>{energyMan.EnergyAfterFly}</color>, display: {energyMan.Energy}");
            }

            var coinMan = Game.Manager.coinMan;
            if (coinMan != null)
            {
                var displayGem = coinMan.GetDisplayCoin(CoinType.Gem);
                var trueGem = coinMan.GetCoin(CoinType.Gem);

                var displayCoin = coinMan.GetDisplayCoin(CoinType.MergeCoin);
                var trueCoin = coinMan.GetCoin(CoinType.MergeCoin);

                logInfo.AppendLine(trueCoin == displayCoin
                    ? $"Coin true: {trueCoin}, display: {displayCoin}"
                    : $"Coin true: <color=#9f3a2c>{trueCoin}</color>, display: {displayCoin}");

                logInfo.AppendLine(trueGem == displayGem
                    ? $"Gem true: {trueGem}, display: {displayGem}"
                    : $"Gem true: <color=#9f3a2c>{trueGem}</color>, display: {displayGem}");
            }
            text.text = logInfo.ToString();
        }

        private IEnumerator _CoTryLevelUp()
        {
            var mgr = Game.Manager.mergeLevelMan;
            using (ObjectPool<List<RewardCommitData>>.GlobalPool.AllocStub(out var rewards))
            {
                while (mgr.canLevelup)
                {
                    if (mgr.TryLevelup(rewards)) UIFlyUtility.FlyRewardList(rewards, Vector3.zero);

                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        private void _AdsDebug()
        {
#if !UNITY_EDITOR
            centurygame.CGAdvertising.instance.ShowMaxDebuggerTool();
#endif
        }

        private void _AdsPlay()
        {
            Game.Manager.adsMan.TestPlayAds();
        }

        private void _OnBtnAddItem(string str, string strCount)
        {
            if (strCount.StartsWith("m")) strCount = strCount.Substring(1);

            if (!int.TryParse(strCount, out var count)) count = 1;

            var isNumber = ulong.TryParse(str, out var id);
            if (isNumber)
            {
                var rewards = new List<RewardCommitData>()
                    { Game.Manager.rewardMan.BeginReward((int)id, count, ReasonString.cheat) };
                UIFlyUtility.FlyRewardList(rewards, Vector3.zero);
            }
        }
        
        private void _AddItemToBoard(int rewardID)
        {
            var board = Game.Manager.mergeBoardMan.activeWorld;
            if (board != null)
                board.activeBoard.SpawnItemMustWithReason(rewardID,
                    ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false, false);
        }

        private void _OnBtnUpdateTask(string str, string strCount)
        {
            if (strCount.StartsWith("m")) strCount = strCount.Substring(1);

            if (!int.TryParse(strCount, out var count)) count = 1;

            var isNumber = ulong.TryParse(str, out var id);
            if (isNumber)
            {
                Game.Manager.taskMan.DebugUpdateTask((TaskType)id, count);
            }
        }

        private void _OnBtnSetMIniGame(string str, string strIndex)
        {
            if (!int.TryParse(str, out var type)) type = 1;

            if (!int.TryParse(strIndex, out var index)) index = -1;

            Game.Manager.miniGameDataMan.SetIndex((MiniGameType)type, index);
        }

        //设置累充金额 单位美分
        private void _OnBtnSetIAPCent(string param)
        {
            Game.Manager.iap.DebugSetIAPCent(ulong.Parse(param));
        }

        private void _OnBtnGuideFlip(string param)
        {
            if (int.TryParse(param, out var gid))
            {
                var mgr = Game.Manager.guideMan;
                if (mgr.IsGuideFinished(gid))
                {
                    mgr.UnfinishGuideAndRefresh(gid);
                }
                else
                {
                    mgr.FinishGuideAndMoveNext(gid);
                }
            }
        }

        private void _OnUpdateBoard(string seconds)
        {
            if (int.TryParse(seconds, out var intSeconds))
            {
                if (Game.Manager.mergeBoardMan.activeWorld != null)
                    Game.Manager.mergeBoardMan.activeWorld.Update(intSeconds * 1000);

                Game.Manager.mergeEnergyMan.TickRecover(intSeconds);
            }
        }

        //删除当前选中的棋子
        private void _OnBtnDeleteSelectItem()
        {
            var item = BoardViewManager.Instance?.GetCurrentBoardInfoItem();
            if (item != null) 
                Game.Manager.mergeBoardMan.activeWorld?.activeBoard?.DisposeItem(item);
        }
        
        private void _OnBtnFreezeItem()
        {
            var item = BoardViewManager.Instance?.GetCurrentBoardInfoItem();
            if (item != null) Game.Manager.mergeBoardMan.activeWorld?.activeBoard.FreezeItem(item);
        }

        private void _OnBtnOutputTest(string countStr, string roundStr)
        {
            var item = BoardViewManager.Instance?.GetCurrentBoardInfoItem();
            if (item != null)
            {
                int.TryParse(countStr, out var count);
                int.TryParse(roundStr, out var round);
                if (item.TryGetItemComponent(out ItemClickSourceComponent click))
                {
                    click.SimulateOutput(count, round);
                }
            }
        }

        private void _OnBtnPachinkoDebug()
        {
            Close();
            Game.Manager.pachinkoMan.EnterMainScene();
            UIManager.Instance.OpenWindow(UIConfig.UIPachinkoDebug);
        }

        private void _OnBtnLogBoardItem()
        {
            Game.Manager.mergeBoardMan.activeWorld?.currentTracer?.DebugBoardItemCount();
        }

        private void _OnBtnLogAllItem()
        {
            Game.Manager.mergeBoardMan.activeWorld?.currentTracer?.DebugAllItemCount();
        }

        private void _SetBiasOffset(string strCount, long offset)
        {
            if (!long.TryParse(strCount, out var count))
                // 数量1
                count = 1;

            var bias = Game.Manager.networkMan.debugBias;
            Game.Manager.networkMan.DebugSetNetworkBias(bias + offset * count);
        }

        private string _NetBiasDisplay()
        {
            // 此处已经包含bias
            var now = Game.Instance.GetTimestampSeconds();
            var dt = TimeUtility.GetDateTimeFromEpoch(now);
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void _OnBtnSetNetBias(string str)
        {
            if (!long.TryParse(str, out var bias))
            {
                // 按照日期解析
                if (DateTime.TryParse(str, out var dt))
                {
                    var tar = TimeUtility.GetSecondsSinceEpoch(dt.Ticks);
                    bias = tar - Game.Instance.GetTimestampSeconds() + Game.Manager.networkMan.networkBias;
                }
                else
                {
                    return;
                }
            }

            Game.Manager.networkMan.DebugSetNetworkBias(bias);
        }

        private void _OnBtnSetChannel(string channel)
        {
            GameUpdateManager.SetOverrideChannel(channel);
        }

        private string _GetLightHouseOverrideChannel(bool next)
        {
            var channels = new List<string>() { "online", "fat.global.staging" };
            var current = PlayerPrefs.GetString("____LH___O__V__R", string.Empty);
            if (string.IsNullOrEmpty(current))
                current = "online";
            var idx = channels.FindIndex(x => x == current);
            var nextIdx = (idx + 1) % channels.Count;
            if (next)
                return channels[nextIdx];
            return current;
        }

        private string _GetLightHouseClientId(bool next)
        {
            var channels = new List<string>() { "normal", "test" };
            var current = PlayerPrefs.GetString("____LH___C__I__D", string.Empty);
            if (string.IsNullOrEmpty(current))
                current = "normal";
            var idx = channels.FindIndex(x => x == current);
            var nextIdx = (idx + 1) % channels.Count;
            if (next)
                return channels[nextIdx];
            return current;
        }

        private void _OnBtnHandBook(string chainIdStr)
        {
            if (!int.TryParse(chainIdStr, out var chainId)) chainId = 1;

            var config = Game.Manager.mergeItemMan.GetCategoryConfig(chainId);
            if (config != null)
            {
                var rewardList = new List<RewardCommitData>();
                var rewardMan = Game.Manager.rewardMan;
                foreach (var itemId in config.Progress)
                    rewardList.Add(rewardMan.BeginReward(itemId, 1, ReasonString.cheat));

                UIFlyUtility.FlyRewardList(rewardList, Vector3.zero);
            }
        }

        private void _ShowABTestInfo()
        {
            var info = "";
            var groupInfo = Game.Manager.playerGroupMan.DebugABTestInfo();
            if (!string.IsNullOrEmpty(groupInfo)) info += groupInfo;
            Game.Manager.commonTipsMan.ShowMessageTipsFullScreen(info, null, null, true);
        }

        private void _ShowABTestTagInfo()
        {
            var info = "";
            info += string.Format("ConfigTag: \n{0}", Game.Manager.configMan.abTags?.ToStringEx());
            Game.Manager.commonTipsMan.ShowMessageTips(info, null, null, true);
        }

        private void _OnBtnChangeABTest(string abTestId, string groupIndex)
        {
            if (!int.TryParse(abTestId, out var testId) || testId <= 0)
                return;
            if (!int.TryParse(groupIndex, out var index) || index <= 0)
                return;
            Game.Manager.playerGroupMan.DebugChangeGroup(testId, index);
        }

        private string _OnBtnSetIgnoreTagExpire()
        {
            Game.Manager.userGradeMan.DebugSetIgnoreTagExpire();
            return "IgnoreTagExpire : " + Game.Manager.userGradeMan.IsIgnoreTagExpire;
        }

        private void _OnBtnChangeUserTag(string id, string value)
        {
            if (!int.TryParse(id, out var userGradeId) || userGradeId <= 0)
                return;
            if (!int.TryParse(value, out var userGradeValue) || userGradeValue <= 0)
                return;
            Game.Manager.userGradeMan.DebugChangeUserTag(userGradeId, userGradeValue);
        }

        private void _ShowUserTagInfo()
        {
            var info = "";
            var tagInfo = Game.Manager.userGradeMan.DebugUserTagInfo();
            if (!string.IsNullOrEmpty(tagInfo)) info += tagInfo;
            info += string.Format("\n\nIsIgnoreTagExpire = {0}", Game.Manager.userGradeMan.IsIgnoreTagExpire);
            Game.Manager.commonTipsMan.ShowMessageTips(info, null, null, true);
        }

        private void _OnBtnResetApiExpireTs()
        {
            Game.Manager.userGradeMan.DebugResetApiExpireTs();
        }

        private void _OnBtnSetApiDelayTime(string str)
        {
            var isNumber = float.TryParse(str, out var delayTime);
            if (isNumber)
            {
                Game.Manager.userGradeMan.DebugSetAPIDelayTime(delayTime);
                Game.Manager.commonTipsMan.ShowMessageTips($"Set Success, delayTime = {delayTime}", null, null, true);
            }
        }

        /// <summary>
        /// 模拟积分活动加分
        /// </summary>
        /// <param name="scoreStr">num</param>
        private void _AddScore(string scoreStr)
        {
            if (!int.TryParse(scoreStr, out var score)) score = 1;

            Game.Manager.activity.LookupAny(EventType.Score, out var activity);
            if (activity == null)
                return;
            var activityScore = (ActivityScore)activity;
            activityScore.DebugAddScore(ScoreEntity.ScoreType.Merge, score);
        }

        private void _UpdateTimeBias(TMP_Text text)
        {
            text.text = string.Format("bias:{0}s \n {1} | {2}", Game.Manager.networkMan.networkBias,
                TimeUtility.GetDateTimeFromEpoch(Game.Instance.GetTimestampSeconds()).ToString(),
                TimeUtility.GetDateTimeFromEpoch(
                    TimeUtility.ConvertUTCSecToLocalSec(Game.Instance.GetTimestampSeconds())).ToString());
        }

        private void _UpdateVersion1(TMP_Text text)
        {
            var settings = Game.Instance.appSettings;
            var info = $"{settings.variant}@{settings.version}({settings.versionCode})";
            var appInfoAfterUpdate = AppUpdaterManager.AppUpdaterGetAppInfoManifest();
            info = $"{info}[{appInfoAfterUpdate?.dataResVersion}][{appInfoAfterUpdate?.unityDataResVersion}]";
            if (Hotfix.HotfixManager.Instance.isHotfix) info += ".hotfixed";

            text.text = info;
        }

        private void _UpdateVersion2(TMP_Text text)
        {
            var info = "";
            if (EL.Resource.ResManager.IsSupportAppUpdater())
                info += string.Format(".lighthouse@{0}",
                    AppUpdaterManager.AppUpdaterGetLHConfig()?.MetaData?.lighthouseId);

            info += string.Format(".http@{0}", Game.Instance.appSettings.httpServer.urlRoot);
            text.text = info;
        }

        private void _OnBtnAddOrderItem()
        {
            using (ObjectPool<List<IOrderData>>.GlobalPool.AllocStub(out var container))
            {
                var board = Game.Manager.mergeBoardMan.activeWorld;
                BoardViewWrapper.FillBoardOrder(container);
                if (container.Count > 0)
                    foreach (var order in container)
                        if (order.State == OrderState.OnGoing && !order.IsCounting)
                        {
                            foreach (var req in order.Requires)
                                for (var i = req.CurCount; i < req.TargetCount; i++)
                                    board.activeBoard.SpawnItemMustWithReason(req.Id,
                                        ItemSpawnContext.CreateWithType(ItemSpawnContext.SpawnType.Cheat), 0, 0, false,
                                        false);

                            break;
                        }
            }
        }

        private void _DebugChangeLanguage()
        {
            foreach (var language in _allLanguageList)
            {
                _RegisterButton(language, () =>
                {
                    var content = ZString.Format("Switch Language form {0} to {1}", I18N.GetLanguage(), language);
                    Game.Manager.commonTipsMan.ShowMessageTipsCustom(content, "", "OK", UIUtility.DebugResetCacheStr);
                    GameI18NHelper.GetOrCreate().SwitchTargetLanguage(language);
                });
            }
        }

        private void _DebugTrainMissionOrderItem()
        {
            if (Game.Manager.activity.LookupAny(EventType.TrainMission, out var act) &&
                act is TrainMissionActivity _activity)
            {
                var top = _activity.topOrder;
                foreach (var info in top.ItemInfos)
                {
                    _AddItemToBoard(info.itemID);
                }

                var bottom = _activity.bottomOrder;
                foreach (var info in bottom.ItemInfos)
                {
                    _AddItemToBoard(info.itemID);
                }
            }
        }

        private void _DebugTrainMissionOrderNeed()
        {
            if (Game.Manager.activity.LookupAny(EventType.TrainMission, out var act) &&
                act is TrainMissionActivity _activity)
            {
                var top = _activity.topOrder;
                for (var i = 0; i < top.ItemInfos.Count; i++)
                {
                    var info = top.ItemInfos[i];
                    if (_activity.CheckMissionState(top, i) == 1)
                    {
                        _AddItemToBoard(info.itemID);
                    }
                }

                var bottom = _activity.bottomOrder;
                for (var i = 0; i < bottom.ItemInfos.Count; i++)
                {
                    var info = bottom.ItemInfos[i];
                    if (_activity.CheckMissionState(bottom, i) == 1)
                    {
                        _AddItemToBoard(info.itemID);
                    }
                }
            }
        }

        private void _DebugTrainMissionFinishTrain()
        {
            var ui = UIManager.Instance.TryGetUI(UIConfig.UITrainMissionMain);
            if (ui == null)
            {
                return;
            }

            var main = (UITrainMissionMain)ui;
            foreach (var pair in main.TrainModule.TopTrain.ItemMap)
            {
                var index = pair.Key;
                var item = pair.Value;
                if (item is MBTrainMissionTrainItemCarriage carriage)
                {
                    carriage.OnClickGO();
                }
            }

            foreach (var pair in main.TrainModule.BottomTrain.ItemMap)
            {
                var index = pair.Key;
                var item = pair.Value;
                if (item is MBTrainMissionTrainItemCarriage carriage)
                {
                    carriage.OnClickGO();
                }
            }
        }

        private void _DebugTrainMissionRecycle()
        {
            if (Game.Manager.activity.LookupAny(EventType.TrainMission, out var act) &&
                act is TrainMissionActivity _activity)
            {
                _activity.FinishRoundDebug();
            }
        }

        #endregion

        #region 内部界面初始化逻辑

        #region toggle

        private void _InitToggle()
        {
            tabToggleList.Clear();
            var togglePath = "Content/Root/TabGo/TabGroup/Tab";
            for (var i = 0; i < 5; i++)
            {
                var path = ZString.Concat(togglePath, i);
                var toggle = transform.FindEx<UISimpleToggle>(path);
                var index = i;
                toggle.onValueChanged.AddListener(isSelect => _OnToggleSelect(index, isSelect));
                tabToggleList.Add(toggle);
            }
            curSelectTabIndex = 0;
            tabToggleList[curSelectTabIndex].SetIsOnWithoutNotify(true);
        }

        private void _OnToggleSelect(int index, bool isSelect)
        {
            if (isSelect && curSelectTabIndex != index)
            {
                curSelectTabIndex = index;
                tabToggleList[curSelectTabIndex].SetIsOnWithoutNotify(true);
                for (var i = 0; i < 4; i++)
                {
                    scrollViewList[i].SetRootActive(curSelectTabIndex == i);
                }
                scrollLog?.SetRootActive(curSelectTabIndex == 4);
            }
        }

        #endregion

        #region UIDebugScrollView

        private void _InitScrollView()
        {
            scrollViewList.Clear();
            var scrollPath = "Content/Root/ScrollViewGroup/ScrollView";
            for (var i = 0; i < 4; i++)
            {
                var path = ZString.Concat(scrollPath, i);
                var scroll = new UIDebugScrollView(transform.Find(path));
                scroll.SetRootActive(i == 0);
                scrollViewList.Add(scroll);
            }
        }

        private class UIDebugScrollView
        {
            private Transform root;
            public Transform Content;

            public UIDebugScrollView(Transform uiRoot)
            {
                root = uiRoot;
                Content = root.Find("Viewport/Content");
            }

            public void SetRootActive(bool isActive)
            {
                root.gameObject.SetActive(isActive);
            }
        }

        #endregion

        #region UIDebugScrollLog

        private void _InitScrollLog()
        {
            var path = "Content/Root/ScrollViewGroup/ScrollLog";
            scrollLog = new UIDebugScrollLog(transform.Find(path));
            scrollLog.InitLog();
            scrollLog.SetRootActive(false);
        }

        private class UIDebugScrollLog
        {
            private Transform root;
            public List<TMP_Text> logList = new List<TMP_Text>();
            public TMP_InputField logFilterInput;
            public Button logClear;
            private int logCount;
            private List<string> logFilter = new();
            private Action ClearLog;

            public UIDebugScrollLog(Transform uiRoot)
            {
                root = uiRoot;
                var logText = root.FindEx<TMP_Text>("Viewport/Content/log");
                logList.Add(logText);
                logFilterInput = root.FindEx<TMP_InputField>("filter");
                logClear = root.FindEx<Button>("clear");
            }

            public void SetRootActive(bool isActive)
            {
                root.gameObject.SetActive(isActive);
            }

            public void InitLog()
            {
                TMP_Text Create()
                {
                    var template = logList[0];
                    var obj = Instantiate(template);
                    var t = obj.GetComponent<TMP_Text>();
                    logList.Add(t);
                    obj.transform.SetParent(template.transform.parent, false);
                    return t;
                }

                void LogEntry(string condi_, string st_, LogType type_)
                {
                    if (logFilter.Count == 0 || logFilter.All(s => !condi_.Contains(s))) return;
                    var c = 100;
                    var n = logCount % c;
                    ++logCount;
                    var t = n >= logList.Count ? Create() : logList[n];
                    t.gameObject.SetActive(true);
                    t.transform.SetAsLastSibling();
                    t.text = $"{condi_}\n<size=80%><color=#6889B3>{st_}</color></size>";
                }

                void Clear()
                {
                    logCount = 0;
                    foreach (var e in logList) e.gameObject.SetActive(false);
                }

                void Filter(string str_)
                {
                    logFilter.Clear();
                    var seg = str_.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    logFilter.AddRange(seg);
                }

                Application.logMessageReceived -= LogEntry;
                Application.logMessageReceived += LogEntry;
                logClear.onClick.RemoveAllListeners();
                logClear.onClick.AddListener(Clear);
                logFilterInput.onSubmit.RemoveAllListeners();
                logFilterInput.onSubmit.AddListener(Filter);
                ClearLog = () => { Application.logMessageReceived -= LogEntry; };
            }

            public void Clear()
            {
                ClearLog?.Invoke();
            }
        }

        #endregion

        #region min / resume / repeat

        private void _SetState(bool isMaximize)
        {
            mRootPanel.gameObject.SetActive(isMaximize);
            mRootTR.gameObject.SetActive(isMaximize);
            mRootTL.gameObject.SetActive(!isMaximize);
        }

        private void ToggleState()
        {
            _SetState(!mRootPanel.gameObject.activeSelf);
        }

        private void _OnBtnMinimize()
        {
            _SetState(false);
        }

        private void _OnBtnResume()
        {
            _SetState(true);
        }

        private void _OnBtnRepeat()
        {
            lastCmd?.Invoke();
        }

        private void _CheckCombo()
        {
            if (!Input.anyKey ||
#if UNITY_EDITOR_OSX
                !(Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
#else
                !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
#endif
               ) return;
            if (Input.GetKeyDown(KeyCode.R))
            {
                _OnBtnRepeat();
                return;
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleState();
                return;
            }
        }

        #endregion

        #endregion
    }
}
