/*
 * @Author: tang.yan
 * @Description: 触发耗体行为时，尝试记录生成冰冻棋子所需的必要参数 只认主棋盘 只认耗体(id=31)
 * @Date: 2025-08-14 18:08:51
 */

namespace FAT.Merge
{
    //触发耗体行为时，尝试记录生成冰冻棋子所需的必要参数 只认主棋盘 只认耗体(id=31)
    public class FrozenItemSpawnBonusHandler : ISpawnBonusHandler
    {
        public int priority;
        int ISpawnBonusHandler.priority => priority;        //越小越先出
        
        private ActivityFrozenItem _actInst;
        private bool _isValid => _actInst != null && _actInst.Active;
        
        public FrozenItemSpawnBonusHandler(ActivityFrozenItem act)
        {
            _actInst = act;
        }
        
        void ISpawnBonusHandler.Process(SpawnBonusContext context)
        {
            //活动实例非法时返回
            if (!_isValid)
                return;
            //限制生产来源只能是ClickSource或DieOutput
            if (context.reason != ItemSpawnReason.ClickSource && context.reason != ItemSpawnReason.DieOutput) 
                return;
            var from = context.from;
            //没有来源或者没有消耗体力时返回
            if(from == null || context.energyCost <= 0)
                return;
            //只认主棋盘
            var boardId = context.world?.activeBoard?.boardId;
            if (boardId != Constant.MainBoardId)
                return;
            //没有click source组件时返回
            if (!from.TryGetItemComponent<ItemClickSourceComponent>(out var comp, true))
                return;
            //click source组件的消耗id和活动配置的消耗id不一致时返回
            if (_actInst.Conf.Cost != comp.firstCost.Cost)
                return;
            // —— 通过全部校验：累计一次有效耗体 —— //
            _actInst.OnEnergySpent(context.energyCost);
        }

        void ISpawnBonusHandler.OnRegister()
        {
            
        }

        void ISpawnBonusHandler.OnUnRegister()
        {
            
        }
    }
}