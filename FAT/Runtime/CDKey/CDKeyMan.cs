/*
 * @Author: qun.chao
 * @Date: 2025-07-01 18:29:24
 */
using Cysharp.Threading.Tasks;
using EL;
using FAT.Platform;
using System.Collections.Generic;
using System.Threading;
using fat.msg;
using CDKeyReward = fat.gamekitdata.Reward;

namespace FAT
{
    public class CDKeyMan : IGameModule
    {
        public const long ErrorCode_InvalidKey = -1;
        public const long ErrorCode_CantMakePurchases = -2;
        public const long ErrorCode_ServerMakeThroughCargoFailed = -3;
        public const long ErrorCode_ServerExchangeFailed = -4;

        private List<RewardCommitData> rewardsToCommit = new();
        private List<CDKeyReward> rewardsToShow = new();
        private CancellationTokenSource cts;

        void IGameModule.Reset()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            rewardsToCommit.Clear();
        }

        void IGameModule.LoadConfig()
        { }

        void IGameModule.Startup()
        { }

        public void DebugExchangeGiftCode(string code)
        {
            UniTask.Void(async () =>
            {
                using var _ = PoolMapping.PoolMappingAccess.Borrow<List<RewardCommitData>>(out var container);
                await ExchangeGiftCode(code, container);
                UIFlyUtility.FlyRewardList(container, UIUtility.GetScreenCenterWorldPosForUICanvas());
            });
        }

        public int FillRewards(List<RewardCommitData> container)
        {
            if (container == null)
                return 0;
            var count = rewardsToCommit.Count;
            container.AddRange(rewardsToCommit);
            rewardsToCommit.Clear();
            return count;
        }

        /// <summary>
        /// 兑换礼品码
        /// </summary>
        public async UniTask<(bool suc, long errCode, List<CDKeyReward> rewardsRaw)> ExchangeGiftCode(string code, List<RewardCommitData> container = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                Error($"cdkey invalid");
                return (false, ErrorCode_InvalidKey, null);
            }
            if (!PlatformSDK.Instance.Adapter.CDKeyCanMakePurchases())
            {
                Error($"cant make purchases");
                return (false, ErrorCode_CantMakePurchases, null);
            }

            rewardsToShow.Clear();
            cts ??= new CancellationTokenSource();

            // 1. 生成透传参数
            var (suc_cargo, cargo, serverId) = await MakeThroughCargo(cts.Token);
            if (!suc_cargo)
            {
                Error($"makethroughcargo failed");
                return (false, ErrorCode_ServerMakeThroughCargoFailed, null);
            }

            // 2. SDK验证礼品码
            var (suc_verify, err) = await VerifyGiftCode(code, cargo, serverId, cts.Token);
            if (!suc_verify)
            {
                Error($"verify gift code failed");
                // 2.5 SDK验证失败，尝试从服务器获取奖励内容用于展示
                await RequestDeliverGiftCode(code, cts.Token, rewardsToShow, null);
                return (false, err.ErrorCode, rewardsToShow);
            }

            // 3. 服务器兑换奖励
            var suc_reward = await RequestDeliverGiftCode(code, cts.Token, rewardsToShow, rewardsToCommit);
            if (!suc_reward)
            {
                Error($"request deliver gift code failed {code}");
                return (false, ErrorCode_ServerExchangeFailed, rewardsToShow);
            }
            FillRewards(container);
            return (true, 0, rewardsToShow);
        }

        /// <summary>
        /// 生成透传参数
        /// </summary>
        private async UniTask<(bool suc, string cargo, string serverId)> MakeThroughCargo(CancellationToken token)
        {
            var task = Game.Manager.networkMan.MakeThroughCargo(0, ThroughCargoType.Gift, new PayContext());
            await UniTask.WaitWhile(() => task.keepWaiting, cancellationToken: token);
            if (task.isSuccess && task.result is MakeThroughCargoResp resp)
            {
                return (true, resp.ThroughCargo, resp.ServerId.ToString());
            }
            return (false, null, null);
        }

        /// <summary>
        /// 兑换CDKey
        /// </summary>
        private async UniTask<(bool suc, SDKError err)> VerifyGiftCode(string giftCode, string cargo, string serverId, CancellationToken token)
        {
            var exchanging = true;
            var exchangeSuccess = false;
            SDKError err = null;
            PlatformSDK.Instance.Adapter.CDKeyExchange(giftCode, string.Empty, string.Empty, cargo, serverId, false, (result, error) =>
            {
                exchanging = false;
                exchangeSuccess = result;
                err = error;
            });

            await UniTask.WaitWhile(() => exchanging, cancellationToken: token);
            if (!exchangeSuccess)
            {
                Error($"sdk exchange failed: {err}");
                return (false, err);
            }
            Info($"sdk exchange success: {giftCode}");
            return (true, null);
        }

        /// <summary>
        /// 请求服务器兑换奖励
        /// </summary>
        private async UniTask<bool> RequestDeliverGiftCode(string code, CancellationToken token, List<CDKeyReward> rewardsRaw, List<RewardCommitData> rewardsToCommit)
        {
            var task = Game.Manager.networkMan.RequestDeliverGiftCode(code);
            await UniTask.WaitWhile(() => task.keepWaiting, cancellationToken: token);
            if (task.isSuccess && task.result is DeliverGiftCodeResp resp)
            {
                DebugEx.Info($"RequestDeliverGiftCode resp.Rewards: {resp.Rewards.Count}");
                rewardsRaw?.AddRange(resp.Rewards);
                if (rewardsToCommit != null)
                {
                    foreach (var reward in resp.Rewards)
                    {
                        rewardsToCommit.Add(Game.Manager.rewardMan.BeginReward(reward.Id, reward.Count, ReasonString.giftLink_reward));
                    }
                }
                return true;
            }
            return false;
        }

        private void Info(string msg)
        {
            DebugEx.Info($"[CDKeyMan] {msg}");
        }

        private void Error(string msg)
        {
            DebugEx.Error($"[CDKeyMan] {msg}");
        }
    }
}