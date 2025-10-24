/*
 * @Author: tang.yan
 * @Description: 活动token翻倍 - 全局效果 
 * @Doc: https://centurygames.feishu.cn/wiki/FCr6wUVEZiwH77kZn6pcjmTxn1g
 * @Date: 2025-09-16 12:09:14
 */
using System;
using EL;

namespace FAT.Merge
{
    //TokenMulti和JumpCD类似 是单个item发起的棋盘全局效果
    public class TokenMulti
    {
        public int activeTokenMultiId => mActiveTokenMultiId;
        public bool hasActiveTokenMulti => mTokenMultiDurationMilli > 0;
        public int countdown => Math.Max(0, mTokenMultiDurationMilli - mTokenMultiLifeCountMilli);
        public int tokenMultiDurationMilli => mTokenMultiDurationMilli;
        public int tokenMultiLifeCountMilli => mTokenMultiLifeCountMilli;

        // 当前激活的唯一id
        private int mActiveTokenMultiId = 0;
        private int mTokenMultiLifeCountMilli;
        private int mTokenMultiDurationMilli;

        private MergeWorld mWorld;

        public TokenMulti(MergeWorld world)
        {
            mWorld = world;
        }

        public void Deserialize(fat.gamekitdata.Merge data)
        {
            if (data.TokenMulti != null)
            {
                var id = data.TokenMulti.TokenMultiItemId;
                var item = mWorld.activeBoard.FindItemById(id);
                if (item != null && TryActivateTokenMulti(item))
                {
                    mTokenMultiLifeCountMilli = data.TokenMulti.LifeCounter;
                }
            }
        }

        public void Serialize(fat.gamekitdata.Merge data)
        {
            data.TokenMulti ??= new fat.gamekitdata.TokenMulti();
            data.TokenMulti.LifeCounter = mTokenMultiLifeCountMilli;
            data.TokenMulti.TokenMultiItemId = mActiveTokenMultiId;
        }

        public void Update(int milli)
        {
            if (mActiveTokenMultiId <= 0)
                return;
            mTokenMultiLifeCountMilli += milli;
            if (mTokenMultiLifeCountMilli >= mTokenMultiDurationMilli)
            {
                _RemoveCurrentTokenMulti();
                _TryActivateNextTokenMulti();
            }
        }

        public bool TryActivateTokenMulti(Item item)
        {
            if (!item.TryGetItemComponent(out ItemTokenMultiComponent com))
                return false;
            _Reset();
            mActiveTokenMultiId = item.id;
            mTokenMultiDurationMilli = com.config.Time;
            return true;
        }

        public void ClearTokenMulti()
        {
            _Reset();
            mWorld.activeBoard.TriggerTokenMultiEnd();
        }

        private void _RemoveCurrentTokenMulti()
        {
            DebugEx.Info($"TokenMulti::_RemoveCurrentTokenMulti expired {mTokenMultiDurationMilli}@{mActiveTokenMultiId}");
            var curItemId = mActiveTokenMultiId;
            ClearTokenMulti();
            mWorld.OnTokenMultiItemExpired(curItemId);
        }

        private void _Reset()
        {
            mActiveTokenMultiId = 0;
            mTokenMultiLifeCountMilli = 0;
            mTokenMultiDurationMilli = 0;
        }

        private bool _TryActivateNextTokenMulti()
        {
            var item = mWorld.activeBoard.FindAnyNormalItemByComponent<ItemTokenMultiComponent>();
            if (item != null && mWorld.activeBoard.UseTokenMulti(item))
            {
                DebugEx.Info($"TokenMulti::_TryActivateNextTokenMulti activated {mTokenMultiDurationMilli}@{mActiveTokenMultiId}");
                return true;
            }
            return false;
        }
    }
}