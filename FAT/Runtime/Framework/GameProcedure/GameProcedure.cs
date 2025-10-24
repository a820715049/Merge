/*
 * @Author: qun.chao
 * @Date: 2023-10-11 19:59:54
 */

using System;
using System.Collections;
using EL;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace FAT
{
    public static partial class GameProcedure
    {
        public enum LoadingPhase
        {
            BeginLoading,
            PreSDK,
            PostSDK,
            PreUpdate,
            PostUpdate,
            PreLoadArchive,
            PostLoadArchive,
            PreLogin,
            PostLogin,
            EndLoading,
        }

        public static bool IsInGame { get; private set; }
        private static CancellationToken tokenForLoading => ctsForLoading.Token;
        private static CancellationTokenSource ctsForLoading = new();
        private static TimeoutController timeoutController = new();

        private static void CancelWhenRestart()
        {
            Cancel();
        }

        public static void CancelWhenError(string content, Action cb)
        {
            Cancel();
            Game.Instance.AbortContinue(content, 0, cb, string.Empty, false);
        }

        private static void Cancel()
        {
            // 中断
            ctsForLoading?.Cancel();
            ctsForLoading.Dispose();
            ctsForLoading = new();
        }

        public static void MergeToScene(MapBuilding focus_ = null)
        {
            // 场景还没有准备好
            if (!Game.Manager.mapSceneMan.scene.Ready)
            {
                return;
            }

            UIConfig.UIMergeBoardMain.Close();
            Game.Manager.mapSceneMan.Enter(focus_);
            //棋盘进场景时播音效
            Game.Manager.audioMan.TriggerSound("BoardClose");
            MessageCenter.Get<MSG.MERGE_TO_SCENE>().Dispatch();

            // 切换到meta时尝试发起推送开启提醒
            Game.Manager.notification.TryRemindSwitchScene();
        }

        public static void MergeToSceneArea(int area_ = 0, Action callback = null, bool overview_ = false)
        {
            var ui = UIManager.Instance;
            var scene = Game.Manager.mapSceneMan;
            if (!scene.scene.Ready)
                return;

            void WhenFocus()
            {
                ui.Block(false);
                var acti = Game.Manager.decorateMan.Activity;
                if (acti == null || !acti.GetCloudState())
                {
                    callback?.Invoke();
                    return;
                }
                acti.ChangeCloudState(false);
                Game.Instance.StartCoroutineGlobal(scene.AreaCloudVisual(acti.CurArea, callback));
            }

            UIConfig.UIMergeBoardMain.Close();
            scene.Enter();
            scene.scene.RefreshArea();
            ui.Block(true);
            scene.TryFocus(area_, WhenFocus_: WhenFocus, overview_: overview_);
            //棋盘进场景时播音效
            Game.Manager.audioMan.TriggerSound("BoardClose");
            MessageCenter.Get<MSG.MERGE_TO_SCENE>().Dispatch();
        }

        public static void SceneToMerge()
        {
            if (Game.Manager.mergeBoardMan.activeWorld != null && Game.Manager.mergeBoardMan.activeWorld.activeBoard.EquivalentToMain) return;
            Game.Manager.mapSceneMan.Exit();
            UIConfig.UIMergeBoardMain.Open();
            //场景进棋盘时播音效
            Game.Manager.audioMan.TriggerSound("BoardOpen");
            MessageCenter.Get<MSG.SCENE_TO_MERGE>().Dispatch();
        }

        private static async UniTask ALoadingImp(IGameLoading loading,
            Func<bool> onLoadingFinished,
            CancellationToken token,
            Action onPreFadeIn = null,
            Action onPostFadeIn = null,
            Action onPreFadeOut = null,
            Action onPostFadeOut = null)
        {
            // 淡入
            onPreFadeIn?.Invoke();
            loading.OnPreFadeIn();
            await UniTask.WaitUntil(() => loading.HasFadeIn()).AttachExternalCancellation(token);
            loading.OnPostFadeIn();
            onPostFadeIn?.Invoke();
            // loading中
            await UniTask.WaitUntil(() => onLoadingFinished?.Invoke() ?? true).AttachExternalCancellation(token);
            // 淡出
            onPreFadeOut?.Invoke();
            loading.OnPreFadeOut();
            await UniTask.WaitUntil(() => loading.HasFadeOut()).AttachExternalCancellation(token);
            loading.OnPostFadeOut();
            onPostFadeOut?.Invoke();
        }
    }
}