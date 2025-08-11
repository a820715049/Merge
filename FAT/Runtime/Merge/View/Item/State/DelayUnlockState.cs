/*
 * @Author: tang.yan
 * @Description: 棋子延迟解锁状态 
 * @Date: 2025-04-01 10:04:16
 */
using FAT.Merge;

namespace FAT
{
    public class DelayUnlockState : MergeItemBaseState
    {
        private float _delayTime = 0f;
        
        public DelayUnlockState(MBItemView v) : base(v) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _delayTime = 0f;
            var from = view.stateChangeContext?.fromView;
            if (from != null)
            {
                var resHolderTrig = from.GetResHolder() as MBResHolderTrig;
                if (resHolderTrig != null)
                {
                    _delayTime = resHolderTrig.SpawnDelayTime;
                }
            }
        }

        public override void OnLeave()
        {
            base.OnLeave();
            _delayTime = 0f;
        }
        
        public override ItemLifecycle Update(float dt)
        {
            _delayTime -= dt;
            if (_delayTime < 0f)
            {
                _DelayUnlock();
                return ItemLifecycle.Idle;
            }
            return base.Update(dt);
        }

        private void _DelayUnlock()
        {
            var isInBoxBefore = view.isInBox;
            view.RefreshOnComponentChange();
            // 开箱
            if (isInBoxBefore != view.isInBox)
            {
                if (view.data.unLockLevel > 0)
                {
                    // 等级解锁
                    var res = BoardUtility.GetLevelLockBg();
                    BoardViewManager.Instance.ShowUnlockLevelEffect(view.data.coord, res, view.data.unLockLevel);
                }
                else
                {
                    // 自然解锁
                    BoardViewManager.Instance.ShowUnlockNormalEffect(view.data.coord);
                }
            }
        }
    }
}