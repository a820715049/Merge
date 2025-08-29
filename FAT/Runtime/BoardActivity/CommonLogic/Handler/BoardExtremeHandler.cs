/*
 * @Author: tang.yan
 * @Description: 棋盘卡死处理者
 * @Date: 2025-08-06 18:08:18
 */

using EL;
using fat.rawdata;

namespace FAT
{
    public class BoardExtremeHandler
    {
        public enum BoardExtremeType
        {
            None = 0,
            CycleUp = 1,        //带循环的棋盘 整个棋盘向上移动 如矿车棋盘
            CloudDown = 2,      //带云层的棋盘 整个棋盘向下移动 如农场棋盘、许愿棋盘
        }
        
        //各个活动继承的接口
        private readonly IBoardExtremeAdapter _adapter;
        //棋盘卡死的类型，不同类型决定了不同的处理逻辑
        private BoardExtremeType _type = BoardExtremeType.None;

        public BoardExtremeHandler(ActivityLike activity, BoardExtremeType type)
        {
            _adapter = (IBoardExtremeAdapter)activity;
            if (_adapter == null)
            {
                DebugEx.Error($"BoardMoveHandler error! activity = {activity?.Type}");
            }
            _type = type;
        }

        //需在活动的OnBoardItemChange方法中调用
        public void OnBoardItemChange()
        {
            _CheckBoardExtremeCase();
        }

        //需在活动的OnActivityUpdate方法中调用
        public void OnActivityUpdate(float deltaTime)
        {
            if (_isWaitExtremeCase)
            {
                _extremeCaseTime += deltaTime;
                if (_extremeCaseTime > 1)
                {
                    _ExecuteExtremeCase();
                    _isWaitExtremeCase = false;
                }
            }
        }
        
        //外部可以在做完表现后主动调用此方法，用于检查棋盘是否卡死
        public void CheckBoardExtremeCase()
        {
            _CheckBoardExtremeCase();
        }

        #region 内部逻辑

        //检测到有极限情况时，统一在1秒后处理
        private float _extremeCaseTime = 0;
        //是否正在等待处理卡死
        private bool _isWaitExtremeCase = false;

        private void _CheckBoardExtremeCase()
        {
            //棋盘上升时和等待处理极限情况时不检查
            if (!_adapter.CanCheckExtreme() || _isWaitExtremeCase)
                return;
            //检查棋盘是否卡死
            if (!_CheckHasExtremeCase())
                return;
            //如果卡死了 在1s后处理
            _extremeCaseTime = 0;
            _isWaitExtremeCase = true;
        }
        
        private bool _CheckHasExtremeCase()
        {
            if (_type == BoardExtremeType.None)
                return false;
            //获取棋盘
            var board = _adapter.GetBoard();
            if (board == null)
                return false;
            //棋盘还有空格子时return
            var hasEmptyGrid = board.CheckHasEmptyIdx();
            if (hasEmptyGrid)
                return false;
            //检测棋盘上是否有活跃的bonus棋子 有的话 不算卡死
            var hasBonus = board.CheckHasBonusItem();
            if (hasBonus)
                return false;
            //检查目前棋盘上是否有合成可能
            var checker = BoardViewManager.Instance.checker;
            checker.FindMatch(true);
            //如果还有 说明还没卡死 让玩家继续操作
            return !checker.HasMatchPair();
        }

        //执行棋盘卡死流程
        private void _ExecuteExtremeCase()
        {
            //暂时只有一种卡死逻辑
            if (_type == BoardExtremeType.None)
                return;
            //获取棋盘
            var board = _adapter.GetBoard();
            if (board == null)
                return;
            //再次确认棋盘是否卡死
            if (!_CheckHasExtremeCase())
                return;
            //通知对应棋盘界面开启一段时间的block
            MessageCenter.Get<MSG.UI_ACTIVITY_BOARD_EXTREME>().Dispatch();
            //确认卡死后弹提示并执行两件事
            Game.Manager.commonTipsMan.ShowPopTips(Toast.NoMerge);
            //1、将场上所有活跃的棋子收到奖励箱
            board.WalkAllItem((item) =>
            {
                if (item.isActive)
                {
                    board.MoveItemToRewardBox(item, true);
                }
            });
            //2、将棋盘最底下两行的所有棋子收进奖励箱
            var boardSize = board.size;
            var cols = boardSize.x;
            var rows = boardSize.y;
            for (int i = 0; i < 2; i++)
            {
                // 根据方向，计算要处理的行
                int row = _GetRowNumByType(rows, i);
                for (int col = 0; col < cols; col++)
                {
                    var item = board.GetItemByCoord(col, row);
                    if (item != null)
                    {
                        board.MoveItemToRewardBox(item, true);
                    }
                }
            }
        }

        private int _GetRowNumByType(int totalRow, int index)
        {
            return _type switch
            {
                BoardExtremeType.CloudDown => totalRow - 1 - index,
                BoardExtremeType.CycleUp => index,
                _ => 0
            };
        }

        #endregion
    }
}