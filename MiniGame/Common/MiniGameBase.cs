/*
 *@Author:chaoran.zhang
 *@Desc:小游戏基类，所有小游戏实例都应该继承该类。
 *@Created Time:2024.09.23 星期一 16:25:04
 */

using FAT;
using fat.rawdata;

namespace MiniGame
{
    public abstract class MiniGameBase
    {
        public int Index; //关卡序号
        public int LevelID; //关卡ID，与配置对应
        public bool IsGuide;
        public MiniGameType Type;

        public abstract void InitData(int index, bool isGuide, int level);
        public abstract void DeInit();
        public abstract void OpenUI();
        public abstract void CloseUI();

        /// <summary>
        /// 检测是否成功，务必在确定完成后调用MiniGameManager中的TrackGameResult方法打点
        /// </summary>
        public abstract void CheckWin();

        /// <summary>
        /// 检测是否失败，务必在确定失败后调用MiniGameManager中的TrackGameResult方法打点
        /// </summary>
        public abstract void CheckLose();
    }
}