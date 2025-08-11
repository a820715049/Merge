using EL;
using fat.rawdata;

namespace MiniGame
{
    namespace MSG
    {
        //关卡序号 是否成功
        public class MINIGAME_RESULT : MessageBase<int, bool> { }
        //当直接退出迷你游戏时
        public class MINIGAME_QUIT : MessageBase<int> { }
        //串珠子小游戏中某个基座完成
        public class MINIGAME_BEADS_BASE_COMPLETE : MessageBase<int> { }
    }
}