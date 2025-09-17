/*
 * @Author: tang.yan
 * @Description: 积分活动变种(麦克风版) - 棋子左下角积分获取处理器 
 * @Date: 2025-09-12 15:09:33
 */

namespace FAT.Merge
{
    public class ScoreMicDisposeBonusHandler : IDisposeBonusHandler
    {
        public int priority;
        int IDisposeBonusHandler.priority => priority;        //越小越先出
        
        private ActivityFrozenItem _actInst;
        private bool _isValid => _actInst != null && _actInst.Active;
        
        public ScoreMicDisposeBonusHandler(ActivityFrozenItem act)
        {
            _actInst = act;
        }
        
        void IDisposeBonusHandler.Process(DisposeBonusContext context)
        {
            //活动实例非法时返回
            if (!_isValid)
                return;
        }

        void IDisposeBonusHandler.OnRegister()
        {
            
        }

        void IDisposeBonusHandler.OnUnRegister()
        {
            
        }
    }
}