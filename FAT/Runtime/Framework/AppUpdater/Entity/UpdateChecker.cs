/*
 * @Author: qun.chao
 * @Date: 2025-06-12 11:52:35
 */
using System.Threading;
using Cysharp.Threading.Tasks;
using CenturyGame.AppUpdaterLib.Runtime;
using static FAT.AppUpdater.UpdateCheckerUtil;
using EL;
using fat.rawdata;

namespace FAT.AppUpdater
{
    public class UpdateChecker
    {
        private CancellationTokenSource cts;
        private bool isChecking = false;
        private EntryPointRequester entryPointRequester = new();
        private UpdateRemindPopup updateRemindPopup = new();

        public void ToForeground()
        {
            TryCancel();

            if (UIManager.Instance.IsOpen(UIConfig.UIWait))
            {
                Info("waiting, maybe in iap, skip check");
                return;
            }

            cts ??= new CancellationTokenSource();
            CheckWrapper(cts.Token).AttachExternalCancellation(cts.Token);
        }

        public void ToBackground()
        {
            TryCancel();
        }

        public void Cancel()
        {
            TryCancel();
        }

        private async UniTask CheckWrapper(CancellationToken token)
        {
            isChecking = true;
            await Check(cts.Token).AttachExternalCancellation(token);
            isChecking = false;
        }

        private async UniTask Check(CancellationToken token)
        {
            // 获取lighthouse
            var lighthouseConfig = await GetLighthouseConfig(token);
            if (lighthouseConfig == null)
            {
                Error("config null");
                return;
            }

            // 检查是否强更
            if (IsForceUpdate(lighthouseConfig))
            {
                Info("force update");
                UIBridgeUtility.ForceUpdate();
                return;
            }

            // 检查是否存在热更
            entryPointRequester.Clear();
            var server = GetCurrentServerData(lighthouseConfig);
            var reqId = entryPointRequester.SendEntryPointRequest(server.Url);
            await UniTask.WaitUntil(() => entryPointRequester.RequestId != reqId, cancellationToken: token);
            if (HasHotUpdate(entryPointRequester.DataVersion, entryPointRequester.ResVersion))
            {
                // track
                DataTracker.hotfix_popup.Track();
                Info("hot update");
                Game.Manager.commonTipsMan.ShowMessageTipsCustom(I18N.Text("#SysComDesc1233"),
                                                            I18N.Text("#SysComDesc4"),
                                                            I18N.Text("#SysComBtn3"),
                                                            GameProcedure.RestartGame);
                return;
            }

            // 检查是否需要更新提醒
            if (ShouldRemindNewVersion(lighthouseConfig.UpdateData.Versoin))
            {
                Info("update remind");
                // 设置更新提醒弹窗的数据
                updateRemindPopup.Setup();
                // 将弹窗加入popup系统队列
                Game.Manager.screenPopup.Queue(updateRemindPopup);
                return;
            }
        }

        private bool ShouldRemindNewVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }
            var targetVersion = new Version(version);
            var currentVersion = new Version(Game.Instance.appSettings.version);
            if (currentVersion.CompareTo(targetVersion) == Version.VersionCompareResult.LowerForMajor)
            {
                // 大版本低，需要提醒
                return true;
            }
            return false;
        }

        private void TryCancel()
        {
            // 中断正在进行的检查
            if (isChecking)
            {
                Info("cancel");
                cts.Cancel();
                cts.Dispose();
                cts = null;
                isChecking = false;
            }
        }
    }

    // 更新提醒弹窗类
    public class UpdateRemindPopup : IScreenPopup
    {
        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup();

        private bool initialized = false;
        public void Setup()
        {
            if (initialized)
                return;
            initialized = true;
            var popupId = Game.Manager.configMan.globalConfig.UpdateRemindPopupId;
            PopupConf = fat.conf.Data.GetPopup(popupId);
            PopupRes = UIConfig.UIUpdate;
        }

        public override bool CheckValid(out string _)
        {
            _ = null;
            return true;
        }
        
        public override bool OpenPopup()
        {
            UIConfig.UIUpdate.Open(false);
            return true;
        }
    }
}