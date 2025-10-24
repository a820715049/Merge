/*
 * @Author: tang.yan
 * @Description: 积分活动变种(麦克风版) - 棋子左下角积分获取处理器 
 * @Date: 2025-09-12 15:09:33
 */

using UnityEngine;

namespace FAT.Merge
{
    public class ScoreMicDisposeBonusHandler : IDisposeBonusHandler
    {
        public int priority;
        int IDisposeBonusHandler.priority => priority;        //越小越先出
        
        private ActivityScoreMic _actInst;
        private bool _isValid => _actInst != null && _actInst.Active;
        
        public ScoreMicDisposeBonusHandler(ActivityScoreMic act)
        {
            _actInst = act;
        }
        
        void IDisposeBonusHandler.Process(DisposeBonusContext context)
        {
            //活动实例非法时返回
            if (!_isValid)
                return;
            var selfItem = context.item;
            var targetItem = context.dieToTarget;
            if (selfItem == null || targetItem == null)
                return;
            var dt = context.deadType;
            if (dt == ItemDeadType.BubbleTimeout || dt == ItemDeadType.None || dt == ItemDeadType.Sell || dt == ItemDeadType.Delete)
            {
                //泡泡棋子因超时自然销毁时,棋子被卖掉或直接删掉时 不给发上面可能带着的积分
                //因为棋子积分挂接的来源由ScoreMicSpawnBonusHandler决定，所以这里不过度对ItemDeadType的类型做判断，只要判断棋子身上有没有积分组件就好
                return;
            }
            //没有ItemActivityTokenComponent或左下角没有数据时返回
            if (!selfItem.TryGetItemComponent<ItemActivityTokenComponent>(out var comp, true) || !comp.CanShow_BL)
                return;
            var isMulti = _actInst.CheckTokenMultiRate(comp.TokenId_BL, out var rate);
            //发麦克风积分
            var num = isMulti ? comp.TokenNum_BL * rate : comp.TokenNum_BL;
            var reward = Game.Manager.rewardMan.BeginReward(comp.TokenId_BL, num, ReasonString.score_mic_board);
            Vector3 pos;
            if (selfItem.parent != null)
                pos = BoardUtility.GetWorldPosByCoord(targetItem.coord);
            else
            {
                //parent为空时 认为在背包里 起始飞行位置设置为背包icon位置
                var sp = BoardViewManager.Instance.inventoryEntryScreenPos;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(UIManager.Instance.CanvasRoot, sp, null, out pos);
            }
            UIFlyUtility.FlyReward(reward, pos);
            //积分发完后清理组件上的分数
            comp.ClearActivityInfo_BL();
        }

        void IDisposeBonusHandler.OnRegister()
        {
            
        }

        void IDisposeBonusHandler.OnUnRegister()
        {
            
        }
    }
}