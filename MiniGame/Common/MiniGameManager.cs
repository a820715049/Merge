/*
 *@Author:chaoran.zhang
 *@Desc:所有小游戏的管理类
 *@Created Time:2024.09.26 星期四 17:49:04
 */

using System.Collections.Generic;
using System.Linq;
using EL;
using FAT;
using fat.rawdata;

namespace MiniGame
{
    public class MiniGameManager
    {
        private static MiniGameManager _instance;

        public static MiniGameManager Instance
        {
            get { return _instance ??= new MiniGameManager(); }
        }

        public MiniGameBase CurMiniGame { get; private set; }
        public readonly List<MiniGameSheet> ActiveMiniGameSheets = new();
        private readonly Dictionary<MiniGameType, MiniGameBase> _allGameBases = new();

        /// <summary>
        /// 初始化Manager数据
        /// </summary>
        public void InitManager()
        {
            var map = MiniGameConfig.Instance.GetMiniGameMap();
            ActiveMiniGameSheets.Clear();
            _allGameBases.Clear();
            ActiveMiniGameSheets.AddRange(map.Where(sheet => sheet.IsActive));
        }

        public MiniGameSheet GetMiniGameConfByType(MiniGameType type)
        {
            return ActiveMiniGameSheets.FindEx(conf => conf.MiniGameType == type);
        }

        public void TryCloseMiniGame()
        {
            CurMiniGame?.CloseUI();
        }

        /// <summary>
        /// 初始化小游戏实例
        /// </summary>
        /// <param name="type">游戏类型</param>
        /// <param name="index">关卡序号</param>
        public void TryStartMiniGame(MiniGameType type, int index)
        {
            var finishGuide = Game.Manager.miniGameDataMan.AllGameSheets.TryGetValue(type, out var info) &&
                              info.FinishGuide;
            var level = Instance.ActiveMiniGameSheets.FirstOrDefault(kv => kv.MiniGameType == type)
                ?.LevelId[index] ?? -1;
            if (CheckDicMiniGameBase(type))
            {
                CurMiniGame.InitData(index, !finishGuide, level);
                CurMiniGame.OpenUI();
                DataTracker.TrackMiniGameStart(type, index, level);
                return;
            }
            CurMiniGame = CreateMiniGame(type);
            if (CurMiniGame == null) return;
            CurMiniGame.InitData(index, !finishGuide, level);
            DataTracker.TrackMiniGameStart(type, index, level);
            _allGameBases.Add(CurMiniGame.Type, CurMiniGame);
            CurMiniGame.OpenUI();
        }

        /// <summary>
        /// 尝试从Dictionary中获取活动实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool CheckDicMiniGameBase(MiniGameType type)
        {
            if (!_allGameBases.ContainsKey(type)) return false;
            CurMiniGame = _allGameBases[type];
            return true;
        }

        /// <summary>
        /// 根据活动类型创建对应的实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private MiniGameBase CreateMiniGame(MiniGameType type)
        {
            return type switch
            {
                MiniGameType.MiniGameBeads => new MiniGameBeads(),
                MiniGameType.MiniGameSlideMerge => new SlideMerge.MiniGameSlideMerge(),
                _ => null
            };
        }

        /// <summary>
        /// 清除当前活动
        /// </summary>
        public void EndCurMiniGame()
        {
            var index = CurMiniGame?.Index ?? -1;
            CurMiniGame?.DeInit();
            CurMiniGame = null;
            MessageCenter.Get<MSG.MINIGAME_QUIT>().Dispatch(index);
        }

        /// <summary>
        /// 获取小游戏关卡激活等级
        /// </summary>
        /// <param name="type">小游戏类型</param>
        /// <param name="levelId">关卡id</param>
        /// <returns></returns>
        public int GetActiveLevel(MiniGameType type, int levelId)
        {
            return type switch
            {
                MiniGameType.MiniGameBeads => MiniGameConfig.Instance.GetBeadsLevelConf(levelId)?.ActiveLv ?? 0,
                MiniGameType.MiniGameSlideMerge => MiniGameConfig.Instance.GetMiniGameSlideMergeLevel(levelId)?.ActiveLv ?? 0,
                _ => 0
            };
        }

        /// <summary>
        /// 判断关卡是否解锁
        /// </summary>
        /// <param name="type">小游戏类型</param>
        /// <param name="index">关卡序号</param>
        /// <returns></returns>
        public bool IsLevelUnlock(MiniGameType type, int index)
        {
            if (!Game.Manager.miniGameDataMan.IsMiniGameUnlock())
                return false;
            switch (type)
            {
                case MiniGameType.MiniGameBeads:
                {
                    var data = MiniGameConfig.Instance.GetBeadsLevelConf(ActiveMiniGameSheets
                        .FirstOrDefault(kv => kv.MiniGameType == MiniGameType.MiniGameBeads)?.LevelId[index] ?? 0);
                    return data != null && Game.Manager.mergeLevelMan.level >= data.ActiveLv;
                }
                case MiniGameType.MiniGameSlideMerge:
                {
                    var data = MiniGameConfig.Instance.GetMiniGameSlideMergeLevel(ActiveMiniGameSheets
                        .FirstOrDefault(kv => kv.MiniGameType == MiniGameType.MiniGameSlideMerge)?.LevelId[index] ?? 0);
                    return data != null && Game.Manager.mergeLevelMan.level >= data.ActiveLv;
                }
                default:
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 判断关卡是否完成过
        /// </summary>
        /// <param name="type">游戏类型</param>
        /// <param name="index">关卡序号</param>
        /// <returns></returns>
        public bool IsLevelComplete(MiniGameType type, int index)
        {
            return Game.Manager.miniGameDataMan.IsLevelComplete(type, index);
        }

        public void CompleteLevel(MiniGameType type, int levelIdx)
        {
            if (!CurMiniGame.IsGuide)
            {
                Game.Manager.miniGameDataMan.CompleteLevel(type, levelIdx);
            }
            else
                Game.Manager.miniGameDataMan.CompleteGuide(type);
        }


        /// <summary>
        /// 判断关卡是否有红点显示
        /// </summary>
        /// <param name="type"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool CheckLevelHasRP(MiniGameType type, int level)
        {
            return Game.Manager.miniGameDataMan.CheckLevelHasRP(type, level);
        }

        /// <summary>
        /// 清理红点
        /// </summary>
        /// <param name="type">游戏类型</param>
        public void ClearLevelRP(MiniGameType type)
        {
            Game.Manager.miniGameDataMan.ClearLevelRP(type);
        }

        public void TrackGameResult(MiniGameType type, int index, int level, bool result, int step)
        {
            DataTracker.TrackMiniGameResult(type, index, level, result, step);
        }
    }
}