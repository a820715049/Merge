/**
 * @Author: handong.liu
 * @Date: 2021-02-23 11:26:02
 */
using System.Collections;
using System.Collections.Generic;
using Config;

namespace FAT
{
    [System.Flags]
    public enum RewardFlags
    {
        None = 0,
        IsUseIAP = 1,
        IsEventPriority = 1 << 1,

        _IsPriority = IsUseIAP + IsEventPriority,
    }

    public struct RewardContext
    {
        public Merge.MergeWorld targetWorld;
        public IParamProvider paramProvider;
    }

    public interface IParamProvider { }

    public class RewardCommitData
    {
        public int rewardId;
        public ObjConfigType rewardType;
        public int rewardCount;
        public ReasonString reason;
        public RewardContext context;
        public RewardFlags flags;
        public bool isFake = false;     //for first time guide
        //本奖励是否在等待commit 如果奖励有Commit逻辑，则必须要在Begin时将此值设为true
        public bool WaitCommit = false;
        //caller info
        internal int _l;
        internal string _f;
        internal string _m;
        internal long _ts;

        public RewardCommitData(int l_, string f_, string m_) {
            _l = l_;
            _f = f_;
            _m = m_;
            _ts = Game.TimestampNow();
        }

        public override string ToString()
        {
            return $"id:{rewardId}, type:{rewardType}, count:{rewardCount}, waitCommit:{WaitCommit}, isFake:{isFake}";
        }
    }
}