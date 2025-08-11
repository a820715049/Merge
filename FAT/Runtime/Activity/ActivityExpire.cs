using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static EL.MessageCenter;
using FAT.MSG;
using System.Text;
using System.Linq;
using Cysharp.Threading.Tasks;
using EL.Resource;

namespace FAT {
    using static ActivityLite;
    using static PoolMapping;

    public static class ActivityExpire {
        public static int ConvertExpire(IDictionary<int, int> map_, Merge.MergeWorld world_, IDictionary<int, int> result_ = null) {
            var c = 0;
            foreach (var (f, t) in map_) {
                var count = world_.ConvertItem(f, t);
                c += count;
                if (result_ != null && t > 0) {
                    result_.TryGetValue(t, out var v);
                    result_[t] = v + count;
                }
            }
            return c;
        }

        public static int RemoveExpire(IDictionary<int, string> map_, Merge.MergeWorld world_, IDictionary<int, int> result_ = null, IDictionary<int, int> token_ = null) {
            var c = 0;
            foreach (var (f, t) in map_) {
                var tt = t.AsSpan();
                var count = (token_?.TryGetValue(f, out var cc) ?? false) ? cc : world_.RemoveItem(f);
                if (count > 0) {
                    c += count;
                    var s = tt.IndexOf('=');
                    var rId = int.Parse(tt[..s]);
                    var rCount = int.Parse(tt[(s + 1)..]);
                    if (result_ != null && rId > 0 && rCount > 0) {
                        result_.TryGetValue(rId, out var v);
                        result_[rId] = v + rCount * count;
                    }
                }
            }
            return c;
        }

        public static void ConvertToReward(IDictionary<int, string> map_, List<RewardCommitData> list_, ReasonString reason_, IDictionary<int, int> token_ = null) {
            using var _ = PoolMappingAccess.Borrow<Dictionary<int, int>>(out var map);
            var c = RemoveExpire(map_, Game.Manager.mainMergeMan.world, map, token_);
            DebugEx.Info($"{nameof(ActivityStep)} remove {c} expire items");
            var rewardMan = Game.Manager.rewardMan;
            foreach(var (k, v) in map) {
                var vv = RoundExpire(v);
                if (vv == 0) continue;
                var d = rewardMan.BeginReward(k, vv, reason_);
                list_.Add(d);
            }
        }

        public static int RoundExpire(int v_) {
            var listR = GetRoundExpireSlice();
            if (listR.Count == 0) return v_;
            var n = listR[0];
            for(var k = listR.Count - 1; k >= 0; --k) {
                n = listR[k];
                if (v_ >= n.From) break;
            }
            return n.RwdNum;
        }
    }
}