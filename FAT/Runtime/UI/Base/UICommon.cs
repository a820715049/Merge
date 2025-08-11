/*
 * @Author: qun.chao
 * @Date: 2023-10-12 10:51:17
 */
namespace FAT
{
    //目前游戏中使用的各个UILayer层级 自上而下层级依次增大
    public enum UILayer
    {
        Scene,
        // 常驻UI
        Hud,
        // 位于状态栏下方的常规弹窗 (棋盘相关界面)
        BelowStatus,
        // 状态栏 层级特殊 单独给一层
        Status,
        // 位于Status和AboveStatus之间的层级
        MiddleStatus,
        // 显示位于状态栏上方的界面，该层级中各个界面互斥，会独立显示(OnPause OnResume) 
        AboveStatus,
        // 显示位于AboveStatus层级上方的界面，该层级中各界面按顺序叠加显示 不互斥
        SubStatus,
        // 显示优先级最高的界面 如 系统级提示框(message/error)、引导等
        Top,
        // modal窗口常用于衔接状态转换 单独给一层方便判断用户是否在自由态
        Modal,
        // effect
        Effect,
        // loading
        Loading,
        // 点击屏蔽
        Block,
        BlockUser,
        // cache
        Cache,
        Max,
    }

    //界面可能会处于的各种状态枚举
    //目前认为prefab准备好后UIBase才会存在 即数据和prefab是同步生效的(而非先数据再prefab)
    public enum UIWindowState
    {
        None,
        Ready,
        Opening,
        Open,
        Pause,
        Closing,
        Close,
    }
}