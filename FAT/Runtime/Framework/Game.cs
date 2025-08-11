/*
 * @Author: qun.chao
 * @Date: 2023-10-11 14:24:44
 */
using EL;
using UnityEngine;
using EL.Resource;
using System;
using System.Collections;
using FAT.Platform;

namespace FAT
{
    public class Game : Singleton<Game>
    {
        public MainEntry mainEntry;
        public static ModuleContainer Manager => Instance.container;
        public GameModuleManager moduleMan => moduleManager;

        // FAT_TODO
        private AppSettings innerSetting = AppVariant.Load();
        public AppSettings appSettings => innerSetting;

        private GameModuleManager moduleManager = new GameModuleManager();
        private ModuleContainer container = new ModuleContainer();
        // 秒级时间驱动
        private float _secondDriver = 0.0f;
        //弱网检查秒级驱动
        private float _netWeakDriver = 0.0f;

        public static DateTime UtcNow => DateTime.UtcNow.AddSeconds(Manager.networkMan.networkBias);

        public void Init(MainEntry main)
        {
            mainEntry = main;
            GameProcedure.LaunchGame();
        }

        public long GetTimestamp()
        {
            var local = GetTimestampLocal();
            if (Game.Manager.networkMan != null)
            {
                local += Game.Manager.networkMan.networkBias * 1000;
            }
            return local;
        }

        public long GetTimestampSeconds()
        {
            var local = GetTimestampLocalSeconds();
            if (Game.Manager.networkMan != null)
            {
                local += Game.Manager.networkMan.networkBias;
            }
            return local;
        }

        public long GetTimestampLocal()
        {
            return TimeUtility.GetTickSinceEpoch(System.DateTime.UtcNow.Ticks) / System.TimeSpan.TicksPerMillisecond;
        }

        public long GetTimestampLocalSeconds()
        {
            return TimeUtility.GetTickSinceEpoch(System.DateTime.UtcNow.Ticks) / System.TimeSpan.TicksPerSecond;
        }

        public static DateTime NextTimeOfDay(int hour_, int min_ = 0, int sec_ = 0, int offset_ = 1) => NextTimeOfDay(UtcNow, hour_, min_, sec_, offset_);
        public static DateTime NextTimeOfDay(DateTime t_, int hour_, int min_ = 0, int sec_ = 0, int offset_ = 1) {
            var t = new DateTime(t_.Year, t_.Month, t_.Day, hour_, min_, sec_);
            t = offset_ switch {
                var _ when offset_ > 0 && t < t_ => t.AddDays(offset_),
                var _ when offset_ < 0 && t > t_ => t.AddDays(offset_),
                _ => t,
            };
            return t;
        }

        public static long TimestampNow() => Timestamp(DateTime.UtcNow) + Manager.networkMan.networkBias;
        public static long Timestamp(DateTime t_) => (long)(t_ - DateTime.UnixEpoch).TotalSeconds;
        public static DateTime TimeOf(long ts_) => DateTime.UnixEpoch.AddSeconds(ts_);

        // public long GetGameTimestamp()
        // {
        //     return gameTimeMan.GetGameMilli();
        // }

        public static Coroutine StartCoroutine(IEnumerator routine)
            => Instance.mainEntry.StartCoroutine(routine);
        
        public static void StopCoroutine(Coroutine routine)
            => Instance.mainEntry.StopCoroutine(routine);

        public Coroutine StartCoroutineGlobal(IEnumerator routine)
        {
            return mainEntry.StartCoroutine(routine);
        }
        
        public void StopCoroutineGlobal(Coroutine routine)
        {
            mainEntry.StopCoroutine(routine);
        }

        public T StartAsyncTaskGlobal<T>(IEnumerator routine) where T : AsyncTaskBase
        {
            return mainEntry.StartAsyncTask<T>(routine);
        }

        public AsyncTaskBase StartAsyncTaskGlobal(IEnumerator routine)
        {
            return mainEntry.StartAsyncTask(routine);
        }

        public void OnNetworkStateChanged(bool isSynced, bool firstSync = false)
        {
            if (firstSync)
            {
                GameSwitchManager.Instance.Refresh();
                MessageCenter.Get<MSG.GAME_LOGIN_FIRST_SYNC>().Dispatch();
            }
            PlatformSDK.Instance.UpdateGameUserInfo();
            Game.Manager.userGradeMan.MarkTagExpire(isSynced);
        }

        public void OnNetworkWeakChanged()
        {
            if (Game.Manager.networkMan.isWeakNetwork)
            {
                if (isRunning)
                {
                    UIManager.Instance.OpenWindow(UIConfig.UINetWarning);
                }
            }
            else
            {
                UIManager.Instance.CloseWindow(UIConfig.UINetWarning);
            }
            MessageCenter.Get<MSG.GAME_NETWORK_WEAK>().Dispatch(Manager.networkMan.isWeakNetwork);
        }

        private void _RunCoroutine(Func<IEnumerator> co)
        {
            mainEntry.StartCoroutine(co());
        }

        private void _OnSecondUpdate(float dt)
        {
            _secondDriver += dt;
            if (_secondDriver > 1f)
            {
                _secondDriver -= 1f;
                if (_secondDriver > 1f)
                    _secondDriver = 0f;
                moduleMan.SecondUpdateAll(GameModuleManager.ModuleScope.AppLaunch, 1, true);
                MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().Dispatch();
            }
        }

        private void _OnSecondNetWeakUpdate(float dt)
        {
            _netWeakDriver += dt;
            if (_netWeakDriver > 1f)
            {
                _netWeakDriver -= 1f;
                if (_netWeakDriver > 1f)
                    _netWeakDriver = 0f;
                var networkMan = Game.Manager.networkMan;
                //loading过程中如果检测到弱网 则持续检测 直到弱网状态结束或者到点弹窗
                if (networkMan != null && networkMan.isWeakNetwork && !Game.Instance.gameStopped)
                {
                    networkMan.CheckNetworkWeakInLoading(true, 10);//这个时机还没读到配置
                }
            }
        }

        #region sync
        private long mLastUpdate = 0;
        private long mLastBackgroundBack = 0;

        public void Update(float dt)
        {
            var restart = false;
            if (mLastBackgroundBack > 0)
            {
                if (mLastUpdate > 0)
                {
                    var delta = Mathf.Max(0, (mLastBackgroundBack - mLastUpdate) / 1000f);
                    DebugEx.FormatInfo("Game::PostUpdate ----> add background seconds {0}", delta);
                    MessageCenter.Get<MSG.GAME_BACKGROUND_BACK>().Dispatch(delta);
                    dt += delta;
                    if (delta > Game.Manager.configMan.globalConfig.RestartTime)
                    {
                        restart = true;
                    }
                }
                mLastBackgroundBack = 0;            //consumed
            }
            mLastUpdate = GetTimestamp();

            if (isRunning)
            {
                moduleMan.UpdateAll(GameModuleManager.ModuleScope.AppLaunch, dt, true);
                _OnSecondUpdate(dt);
            }
            else
            {
                _OnSecondNetWeakUpdate(dt);
            }

            if (restart)
            {
                GameProcedure.RestartGame();
            }
        }

        public void AppWillEnterBackground()
        {
            DataTracker.TrackLogInfo($"[Game] AppWillEnterBackground {isRunning}");
            if (isRunning)
            {
                // 存档无法在彻底切换到后台前完成上传操作
                // 切换回前台时, 上传操作会继续完成, 但延后达成的上传操作无法通过服务器的时间校验
                // 改为不再进行此操作
                // Game.Manager.archiveMan.SendImmediately(true);

                Game.Manager.notification.ToBackground();
                Game.Manager.mergeEnergyMan.ToBackground();
                GameUpdateManager.Instance.ToBackground();
            }
        }

        public void AppDidEnterForeground()
        {
            DataTracker.TrackLogInfo($"[Game] AppDidEnterForeground {isRunning}");
            if (isRunning)
            {
                Game.Manager.networkMan.CancelOngoingSyncTime();
                TimeUtility.ResetTimeZoneData();
                mLastBackgroundBack = GetTimestamp();
                Game.Manager.networkMan.SetServerUnsync();
                Game.Manager.notification.ToForeground();
                Game.Manager.mergeEnergyMan.ToForeground();
                Game.Manager.remoteApiMan.ToForeground();
                GameUpdateManager.Instance.ToForeground();
            }
            DeviceNotificationHelper.CheckRespondedNotification();
        }
        #endregion

        // FAT_TODO 拆分逻辑
        #region game flag

        public enum GameFlag
        {
            DataInit,
            CommonResInit,
            TimeSynced,
            WaitClose
        }
        private readonly ulong kGameEnteredFlag = _PrecalculateGameFlag(GameFlag.DataInit, GameFlag.CommonResInit);
        private readonly ulong kGameRunningFlag = _PrecalculateGameFlag(GameFlag.DataInit, GameFlag.CommonResInit);
        private readonly ulong kGameStopFlag = _PrecalculateGameFlag(GameFlag.WaitClose);
        public bool isRunning => _HasAllGameFlags(kGameRunningFlag) && !_HasOneOfGameFlags(kGameStopFlag);
        public bool isLoginSuccess => (mReadyFlag & kGameEnteredFlag) == kGameEnteredFlag;
        public bool isStateReady { get; private set; }
        public bool gameStopped => _HasOneOfGameFlags(kGameStopFlag);
        protected ulong mReadyFlag = 0;

        private static ulong _PrecalculateGameFlag(params GameFlag[] flags)
        {
            ulong ret = 0;
            foreach (var f in flags)
            {
                ret |= (1UL << (int)f);
            }
            return ret;
        }

        private bool _HasAllGameFlags(ulong flags)
        {
            return (mReadyFlag & flags) == flags;
        }

        private bool _HasOneOfGameFlags(ulong flags)
        {
            return (mReadyFlag & flags) != 0;
        }

        private bool _HasGameFlag(GameFlag flag)
        {
            return (mReadyFlag & (1UL << (int)flag)) != 0;
        }

        protected void _SetGameFlag(GameFlag flag)
        {
            mReadyFlag |= 1UL << (int)flag;
        }

        private void _UnsetGameFlag(GameFlag flag)
        {
            mReadyFlag &= ~(1UL << (int)flag);
        }

        private IEnumerator _CoWaitForGameFlag(GameFlag flag)
        {
            while (!_HasGameFlag(flag))
            {
                yield return null;
            }
        }
        #endregion

        #region abort

        public void RestartGame()
        {
            GameProcedure.RestartGame();
        }

        public void Abort(string noticeContent, long errorCode, string noticeTitle = "", bool isShowErrorCode = true)
        {
            _Abort(noticeContent, errorCode, noticeTitle, isShowErrorCode);
        }

        public void AbortRestart(string noticeContent, long errorCode, string noticeTitle = "", bool isShowErrorCode = true, string btnName = "")
        {
            if (!_HasGameFlag(GameFlag.DataInit))
            {
                _Abort(noticeContent, errorCode, noticeTitle, isShowErrorCode, btnName);
            }
            else
            {
                _AbortRestart(noticeContent, errorCode, noticeTitle, isShowErrorCode, btnName);
            }
        }

        public void AbortContinue(string noticeContent, long errorCode, Action cb = null, string noticeTitle = "", bool isShowErrorCode = true)
        {
            _AbortContinue(noticeContent, errorCode, cb, noticeTitle, isShowErrorCode);
        }

        public void ShowAppForceUpdate()
        {
            if (!_HasGameFlag(GameFlag.WaitClose))
            {
                _SetGameFlag(GameFlag.WaitClose);
                UIBridgeUtility.ForceUpdate();
            }
        }

        private void _Abort(string noticeContent, long errorCode, string noticeTitle = "", bool isShowErrorCode = true, string btnName = "")
        {
            if (!_HasGameFlag(GameFlag.WaitClose))
            {
                _SetGameFlag(GameFlag.WaitClose);
                string content = isShowErrorCode ? $"{noticeContent}\n{errorCode}" : noticeContent;
                Manager.commonTipsMan.ShowLoadingTipsWithHelp(content, noticeTitle, GameProcedure.QuitGame, btnName);
            }
        }

        private void _AbortRestart(string noticeContent, long errorCode, string noticeTitle = "", bool isShowErrorCode = true, string btnName = "")
        {
            if (!_HasGameFlag(GameFlag.WaitClose))
            {
                _SetGameFlag(GameFlag.WaitClose);
                string content = isShowErrorCode ? $"{noticeContent}\n{errorCode}" : noticeContent;
                Manager.commonTipsMan.ShowLoadingTipsWithHelp(content, noticeTitle, () =>
                {
                    RestartGame();
                    _UnsetGameFlag(GameFlag.WaitClose);
                }, btnName);
            }
        }
        
        private void _AbortContinue(string noticeContent, long errorCode, Action cb = null, string noticeTitle = "", bool isShowErrorCode = true)
        {
            if (!_HasGameFlag(GameFlag.WaitClose))
            {
                _SetGameFlag(GameFlag.WaitClose);
                string content = isShowErrorCode ? $"{noticeContent}\n{errorCode}" : noticeContent;
                Manager.commonTipsMan.ShowLoadingTipsWithHelp(content, noticeTitle, () =>
                {
                    cb?.Invoke();
                    _UnsetGameFlag(GameFlag.WaitClose);
                });
            }
        }

        //删号流程单独用的Abort
        public void AbortRemoveCount(Action finishCb)
        {
            //提示账号正在删除中，提供是否撤销删除的按钮 点左边的按钮退游戏  右边的按钮会请求撤销删除 并正常进游戏
            Action CancelRemoveAccount = () =>
            {
                StartCoroutineGlobal(_TryProcessCancelRemoveUser(finishCb));
            };
            Action SureRemoveAccount = () =>
            {
                StartCoroutineGlobal(_TryProcessSureRemoveUser());
            };
            Game.Manager.commonTipsMan.ShowLoadingTips(I18N.Text("#SysComDesc180"), SureRemoveAccount, CancelRemoveAccount);
        }

        public void AbortRemovedAccount()
        {
            StartCoroutineGlobal(_TryProcessSureRemoveUser());
        }

        private IEnumerator _TryProcessCancelRemoveUser(Action finishCb)
        {
            var task = AccountDelectionUtility.TryProcessCancelRemoveUser();
            yield return task;
            if (task.isSuccess)
            {
                //弹提示 撤销删除成功 直接进游戏
                Game.Manager.commonTipsMan.ShowLoadingTips(I18N.Text("#SysComDesc181"), null, ()=>finishCb?.Invoke(), true);
            }
            else
            {
                //弹提示 撤销删除失败 直接退出游戏
                Game.Manager.commonTipsMan.ShowLoadingTips(I18N.Text("#SysComDesc182"), null, GameProcedure.QuitGame, true);
            }
        }
        
        private IEnumerator _TryProcessSureRemoveUser()
        {
            yield return AccountDelectionUtility.TryProcessSureRemoveUser();
            yield return null;
            CommonUtility.QuitApp();
        }

        #endregion

        public void SetRunning()
        {
            _SetGameFlag(GameFlag.DataInit);
            _SetGameFlag(GameFlag.CommonResInit);
        }

        public void SetNotRunning()
        {
            _UnsetGameFlag(GameFlag.DataInit);
            _UnsetGameFlag(GameFlag.CommonResInit);
        }
    }
}