/*
 * @Author: qun.chao
 * @doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/odbgv0ep73p98ggw
 * @Date: 2024-03-05 14:21:23
 */
using System;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    /*
    jumpcd和orderbox类似 是单个item发起的棋盘全局效果
    */
    public class JumpCD
    {
        public int activeJumpCDId => mActiveJumpCDId;
        public bool hasActiveJumpCD => mJumpCDDurationMilli > 0;
        public int countdown => Math.Max(0, mJumpCDDurationMilli - mJumpCDLifeCountMilli);
        public int jumpCDDurationMilli => mJumpCDDurationMilli;
        public int jumpCDLifeCountMilli => mJumpCDLifeCountMilli;

        // 当前激活的唯一id
        private int mActiveJumpCDId = 0;
        private int mJumpCDLifeCountMilli;
        private int mJumpCDDurationMilli;

        private MergeWorld mWorld;

        public JumpCD(MergeWorld world)
        {
            mWorld = world;
        }

        public void Deserialize(fat.gamekitdata.Merge data)
        {
            if (data.JumpCD != null)
            {
                var id = data.JumpCD.JumpCDItemId;
                var item = mWorld.activeBoard.FindItemById(id);
                if (item != null && TryActivateJumpCD(item))
                {
                    mJumpCDLifeCountMilli = data.JumpCD.LifeCounter;
                }
            }
        }

        public void Serialize(fat.gamekitdata.Merge data)
        {
            data.JumpCD ??= new fat.gamekitdata.JumpCD();
            data.JumpCD.LifeCounter = mJumpCDLifeCountMilli;
            data.JumpCD.JumpCDItemId = mActiveJumpCDId;
        }

        public void Update(int milli)
        {
            if (mActiveJumpCDId <= 0)
                return;
            mJumpCDLifeCountMilli += milli;
            if (mJumpCDLifeCountMilli >= mJumpCDDurationMilli)
            {
                _RemoveCurrentJumpCD();
                _TryActivateNextJumpCD();
            }
        }

        public bool TryActivateJumpCD(Item item)
        {
            if (!item.TryGetItemComponent(out ItemJumpCDComponent com))
                return false;
            _Reset();
            mActiveJumpCDId = item.id;
            mJumpCDDurationMilli = com.config.Time;
            return true;
        }

        public void ClearJumpCD()
        {
            _Reset();
            mWorld.activeBoard.TriggerJumpCDEnd();
        }

        private void _RemoveCurrentJumpCD()
        {
            DebugEx.Info($"JumpCD::_RemoveCurrentJumpCD expired {mJumpCDDurationMilli}@{mActiveJumpCDId}");
            var curItemId = mActiveJumpCDId;
            ClearJumpCD();
            mWorld.OnJumpCDItemExpired(curItemId);
        }

        private void _Reset()
        {
            mActiveJumpCDId = 0;
            mJumpCDLifeCountMilli = 0;
            mJumpCDDurationMilli = 0;
        }

        private bool _TryActivateNextJumpCD()
        {
            var item = mWorld.activeBoard.FindAnyNormalItemByComponent<ItemJumpCDComponent>();
            if (item != null && mWorld.activeBoard.UseJumpCD(item))
            {
                DebugEx.Info($"JumpCD::_TryActivateNextJumpCD activated {mJumpCDDurationMilli}@{mActiveJumpCDId}");
                return true;
            }
            return false;
        }
    }
}