
using System;
using EL;

namespace FAT {
    public static class ActivityTransit {
        public static bool fromMerge;
        public static UIResource loading;
        public static ActivityVisual visual;

        public static void Enter(ActivityLike acti_, VisualRes loading_, UIResAlt ui_)
            => Enter(acti_, loading_.res.ActiveR, ui_, visual_:loading_.visual);
        public static void Enter(ActivityLike acti_, UIResource loading_, UIResAlt ui_, ActivityVisual visual_ = null) {
            UIManager.Instance.ChangeIdleActionState(false);
            Game.Manager.screenPopup.Block(delay_: true);
            loading = loading_;
            visual = visual_;
            UIActivityLoading.Open(loading_, () => {
                fromMerge = UIManager.Instance.IsOpen(UIConfig.UIMergeBoardMain);
                UIConfig.UIStatus.Close();
                if (fromMerge)
                {
                    UIConfig.UIMergeBoardMain.Close();
                }
                else
                {
                    Game.Manager.mapSceneMan.Exit();
                }
                acti_.Open(ui_);
            }, visual_:visual_);
            DebugEx.Info($"activity enter {acti_.Info3}");
        }

        public static void Exit(ActivityLike acti_, UIResource ui_, Action afterFadeIn = null) {
            if (UIManager.Instance.IsClosed(ui_)) return;
            UIActivityLoading.Open(loading, () => {
                ui_.Close();
                UIConfig.UIStatus.Open();
                if (fromMerge)
                {
                    UIConfig.UIMergeBoardMain.Open();
                }
                else
                {
                    Game.Manager.mapSceneMan.Enter(null);
                }
                UIManager.Instance.ChangeIdleActionState(true);
                Game.Manager.screenPopup.Block(false, false);
                afterFadeIn?.Invoke();
            }, visual_:visual);
            DebugEx.Info($"activity exit {acti_.Info3}");
        }
    }
}