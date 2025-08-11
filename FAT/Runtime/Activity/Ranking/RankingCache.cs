using System;
using GameNet;
using EL;
using Google.Protobuf.Collections;
using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.msg;
using Google.Protobuf;
using static fat.conf.Data;
using Config;
using fat.gamekitdata;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace FAT
{
    using NetTask = SimpleResultedAsyncTask<IMessage>;

    public class RankingCache
    {
        public RankingType Type { get; }
        public RankingInfoResp Last { get; private set; } //last data with rank changed
        public RankingInfoResp Data { get; private set; }
        public Action<RankingCache> WhenRefresh { get; set; }
        public long TSData { get; private set; }
        private long TSRefresh;
        private const int SyncInterval = 10;
        public int RefreshInterval { get; set; } = 60;
        public bool IsScoreChange;

        public RankingCache(RankingType type_)
        {
            Type = type_;
        }

        public void Test(int rank_, int count_)
        {
            var data = new RankingInfoResp();
            var info = new PlayerOpenInfo()
            {
                Uid = Game.Manager.networkMan.remoteUid
            };
            var me = data.Me = new PlayerRankingInfo { Player = info };
            me.RankingOrder = (ulong)rank_;
            me.Score = 100 * rank_;
            var list = data.Players;
            for (var i = 0; i < count_; ++i)
            {
                //if (i + 1 == rank_) continue;
                var copy = me.Clone();
                copy.Player.Uid = (ulong)(1000 + i);
                copy.RankingOrder = (ulong)i + 1;
                copy.Score = 100 * i;
                list.Add(copy);
            }

            Replace(data);
        }

        public void CheckRefresh(ActivityRanking e_, int delay_) {
            var now = Game.TimestampNow();
            if (now >= TSRefresh + RefreshInterval) {
                SyncRanking(e_, delay_:delay_);
            }
        }

        public bool RankUpAfter(ref long ts_) {
            var v = (Data?.Me?.RankingOrder ?? int.MaxValue) < (Last?.Me?.RankingOrder ?? 0) 
                && ts_ > 0 && TSData > ts_;
            ts_ = TSData;
            return v;
        }

        public void Replace(RankingInfoResp data_)
        {
            Last = Data;
            Data = data_;
            TSData = Game.TimestampNow();
            WhenRefresh?.Invoke(this);
            IsScoreChange = true;
            DebugEx.Info("ranking cacheData replace: " + Data?.Me?.RankingOrder);
            DataTracker.TrackLogInfo("ranking SeverDataCallBack replace--->" + Data?.Me?.RankingOrder);
        }

        public bool SyncRanking(ActivityRanking e_, int interval_ = -1, int delay_ = 0)
        {
            if (!e_.RankingValid(Type)) return false;
            return SyncRanking(e_, ref TSRefresh, interval_, delay_, Game.Manager.networkMan.RequestRanking,
                t => { Replace((RankingInfoResp)t.result); });
        }

        private void Cleanup(RepeatedField<PlayerRankingInfo> list_)
        {
            for (var i = 0; i < list_.Count; ++i)
            {
                var d = list_[i];
                if (d.RankingOrder == 0)
                {
                    list_.RemoveAt(i);
                    --i;
                }
            }
        }

        private bool SyncRanking(ActivityRanking e_, ref long ts_, int interval_, int delay_,
            Func<ActivityRanking, RankingType, NetTask> net_, Action<NetTask> WhenComplete_)
        {
            static async UniTask R(int delay_, ActivityRanking e_, RankingType type_, Func<ActivityRanking, RankingType, NetTask> net_, Action<NetTask> WhenComplete_)
            {
                if (delay_ > 0) await Task.Delay(delay_);
                var net = net_(e_, type_);
                DebugEx.Info($"{nameof(RankingCache)} sync {type_}");
                await net;
                if (net.isSuccess)
                {
                    DebugEx.Info($"{nameof(RankingCache)} sync {type_} success");
                    WhenComplete_(net);
                }
                else
                {
                    DebugEx.Error($"{nameof(RankingCache)} sync {type_} fail:{net.error}");
                }
            }

            var time = Game.Instance.GetTimestampSeconds();
            var interval = interval_ < 0 ? SyncInterval : interval_;
            if (time + delay_ * 1000 - ts_ < interval_) return false;
            ts_ = time;
            _ = R(delay_, e_, Type, net_, WhenComplete_);
            return true;
        }

        public (bool, int) QueryReward(ActivityRanking e_, IList<RewardConfig> list_, int asRank_ = 0,
            bool immediate_ = false)
        {
            switch (Type)
            {
                case RankingType.RankingGroup:
                {
                    if (immediate_ && TSRefresh < e_.endTS)
                    {
                        SyncRanking(e_, interval_:1);
                        return (false, 0);
                    }

                    return QueryReward(e_, list_, asRank_);
                }
            }

            return (true, 0);
        }

        public (bool, int) QueryReward(ActivityRanking e_, IList<RewardConfig> list_, int asRank_ = 0)
        {
            var rank = 0;
            if (asRank_ <= 0)
            {
                var data = Data?.Me;
                if (data == null)
                {
                    var sent = SyncRanking(e_);
                    return (!sent, rank);
                }

                rank = (int)data.RankingOrder;
            }
            else
            {
                rank = asRank_;
            }

            var rList = e_.reward;
            if (rank < 0 || rank >= rList.Count) goto end;
            var rL = rList[rank];
            if (rL == null) goto end;
            foreach (var s in rL) list_.Add(s);
            end:
            return (true, rank);
        }

        public void ResetSync()
        {
            TSRefresh = 0;
        }
    }
}