/*
 * @Author: qun.chao
 * @Date: 2023-12-06 19:20:00
 */
using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using EL;

namespace FAT
{
    public static partial class GameProcedure
    {
        private static bool isRestarting;

        public static void RestartGame()
        {
            GameUpdateManager.Instance.OnRestartGame();
            CancelWhenRestart();
            DataTracker.TrackRestart();
            AsyncRestart().Forget();
        }

        private static async UniTaskVoid AsyncRestart()
        {
            try
            {
                isRestarting = true;
                Game.Instance.SetNotRunning();
                var token = tokenForLoading;
                // 需要等待AppUpdater内部正在进行的网络请求结束 强行reset会报错
                await UniTask.WaitUntil(() => GameUpdateManager.Instance.AppUpdaterHttpRequestDone);
                await AsyncStartGame(null, null, token).AttachExternalCancellation(token);
                isRestarting = false;
            }
            catch (OperationCanceledException)
            {
                DebugEx.Info("[GameProcedure] canceled");
            }
        }
    }
}