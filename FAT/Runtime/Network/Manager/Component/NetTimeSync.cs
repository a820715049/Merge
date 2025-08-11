/*
 * @Author: qun.chao
 * @Date: 2024-12-25 16:03:15
 */
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using EL;
using fat.msg;
using fat.gamekitdata;

namespace FAT
{
    public enum SyncStatus
    {
        Unknown,
        InSync,
        ArchiveModified,
        TimeIsDifferent
    }

    public class NetTimeSync
    {
        public long serverTimeBias => _serverTimeBias + _localTimeBias;
        public long localTimeBias => _localTimeBias;
        public bool isSynced => _recentStatus == SyncStatus.InSync;
        public const string local_time_bias_key = nameof(local_time_bias_key);

        private long _serverTimeBias { get; set; }
        private long _localTimeBias { get; set; }
        private bool _isWaitingSync;
        private SyncStatus _recentStatus = SyncStatus.Unknown;

        private CancellationTokenSource _cts = new();

        public void Reset()
        {
            Cancel();
            _recentStatus = SyncStatus.Unknown;
            _isWaitingSync = false;
            _serverTimeBias = 0;
            _localTimeBias = 0;
        }

        public void StartUp()
        {
            DebugLoadTimeBias();
        }

        public void Update()
        {
        }

        public void Cancel()
        {
            _cts?.Cancel();
            _cts.Dispose();
            _cts = new();
        }

        public void SetUnsync()
        {
            _recentStatus = SyncStatus.Unknown;
        }

        public void DebugSetTimeBias(long bias)
        {
            _localTimeBias = bias;
            PlayerPrefs.SetString(local_time_bias_key, bias.ToString());
            MessageCenter.Get<FAT.MSG.TIME_BIAS>().Dispatch(bias);
        }

        private void DebugLoadTimeBias()
        {
            var biasStr = PlayerPrefs.GetString(local_time_bias_key, string.Empty);
            if (!string.IsNullOrEmpty(biasStr) && long.TryParse(biasStr, out var bias))
            {
                _localTimeBias = bias;
            }
        }

        private void SyncTime()
        {
            RequestSyncTime();
        }

        private async void RequestSyncTime()
        {
            _isWaitingSync = true;
            var req = Game.Manager.networkMan.PostMessage_SyncTime();
            try
            {
                await UniTask.WaitWhile(() => req.keepWaiting).AttachExternalCancellation(_cts.Token);
            }
            catch (System.OperationCanceledException)
            {
                DebugEx.Info($"synctime canceled");
            }
            _isWaitingSync = false;
            if (req.isSuccess)
            {
                var resp = req.result as SyncTimeResp;
                var state = CheckSync(resp.ServerSec, null);
                if (state != SyncStatus.InSync)
                {
                    Game.Manager.networkMan.SetState(GameNet.NetworkMan.State.TimeNotSync, 0);
                }
                else
                {
                    Game.Instance.OnNetworkStateChanged(true, false);
                }
                DebugEx.FormatInfo("RequestSyncTime ----> sync time success {0}", _serverTimeBias);
            }
        }

        #region time sync check

        private bool CheckSync_IsArchiveReady(ref SyncStatus status)
        {
            if (!Game.Manager.archiveMan.isArchiveLoaded)
            {
                status = SyncStatus.InSync;
                return true;
            }
            return false;
        }

        private bool CheckSync_Uid(ref SyncStatus status, ulong serverUid)
        {
            if (serverUid > 0 && Game.Manager.accountMan.uid != serverUid)
            {
                status = SyncStatus.ArchiveModified;
                return true;
            }
            return false;
        }

        private bool CheckSync_LastSyncTime(ref SyncStatus status, long serverLastSync)
        {
            if (serverLastSync > 0 && serverLastSync > Game.Manager.archiveMan.lastSyncTime)
            {
                status = SyncStatus.ArchiveModified;
                return true;
            }
            return false;
        }

        private bool CheckSync_Bias(ref SyncStatus status, long serverTime)
        {
            var prevBias = _serverTimeBias;
            var now = Game.Instance.GetTimestampLocalSeconds();
            var realBias = serverTime - now;
            if (Mathf.Abs(prevBias - realBias) > 30f)
            {
                // 与服务器时间差存在跳变 用户可能改时间
                status = SyncStatus.TimeIsDifferent;
                return true;
            }
            if (Game.Manager.archiveMan.lastUpdateTime - 60L > serverTime)
            {
                // 本地记录的存档修改更新时间已超过服务器时间 用户可能改时间
                status = SyncStatus.TimeIsDifferent;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查时间状态
        /// </summary>
        /// <param name="serverTime">ServerSec 服务器时间 / TimeSync协议或者Auth协议里都有</param>
        /// <param name="baseData">玩家基础数据</param>
        public SyncStatus CheckSync(long serverTime, PlayerBaseData baseData)
        {
            var status = SyncStatus.Unknown;
            var userType = baseData?.UserType ?? Game.Manager.archiveMan.userType;
            var isEditor = false;
#if UNITY_EDITOR
            isEditor = true;
#endif
            if (CheckSync_IsArchiveReady(ref status)) { }
            else if (CheckSync_Bias(ref status, serverTime)) { }
            else if (CheckSync_Uid(ref status, baseData?.Uid ?? 0)) { }
            else if (CheckSync_LastSyncTime(ref status, baseData?.LastSync ?? 0)) { }

            if (status == SyncStatus.Unknown)
            {
                // 未被修改 说明是正常login
                status = SyncStatus.InSync;
            }
            else if (status == SyncStatus.TimeIsDifferent)
            {
                // 编辑器环境允许时间偏差 tester用户允许时间偏差
                if (isEditor || userType == UserType.Test)
                {
                    status = SyncStatus.InSync;
                }
            }

            if (!isEditor && userType != UserType.Test)
            {
                // 非editor 非test用户 不允许改本地偏移
                _localTimeBias = 0;
            }

            if (status == SyncStatus.InSync && serverTime > 0)
            {
                // 状态正常 记录服务器时间
                SyncServerTime(serverTime);
            }
            _recentStatus = status;
            return status;
        }

        private void SyncServerTime(long serverSeconds)
        {
            var now = Game.Instance.GetTimestampLocalSeconds();
            _serverTimeBias = serverSeconds - now;
            DebugEx.FormatInfo("NetTimeSync._SyncServerTime ----> serverSeconds:{0}, localSeconds:{1}, bias:{2}", serverSeconds, now, _serverTimeBias);
        }

        #endregion
    }
}