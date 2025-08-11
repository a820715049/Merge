/*
 *@Author:chaoran.zhang
 *@Desc:管理config的类，小游戏中需要读取配置时，在该类中声明并获取
 *@Created Time:2024.09.25 星期三 11:34:24
 */

using System.Collections.Generic;
using fat.rawdata;
using Google.Protobuf.Collections;
using data = fat.conf.Data;

namespace MiniGame
{
    public class MiniGameConfig
    {
        private static MiniGameConfig _instance;

        public static MiniGameConfig Instance
        {
            get { return _instance ??= new MiniGameConfig(); }
        }

        public IEnumerable<MiniGameSheet> GetMiniGameMap()
        {
            return data.GetMiniGameSheetMap().Values;
        }

        public MiniGameBeadsLevel GetBeadsLevelConf(int level)
        {
            return data.GetMiniGameBeadsLevel(level);
        }

        public IEnumerable<MiniGameBeadsLevel> GetBeadsLevels()
        {
            return data.GetMiniGameBeadsLevelMap().Values;
        }

        public MiniGameBeadsStage GetBeadsStage(int id)
        {
            return data.GetMiniGameBeadsStage(id);
        }

        public MiniGameBeadsBase GetBeadsBase(int id)
        {
            return data.GetMiniGameBeadsBase(id);
        }

        public MiniGameBeadsCell GetBeadsCell(int id)
        {
            return data.GetMiniGameBeadsCell(id);
        }

        #region SlideMerge
        public MiniGameSlideMergeLevel GetMiniGameSlideMergeLevel(int id)
        {
            return data.GetMiniGameSlideMergeLevel(id);
        }

        public MiniGameSlideMergeStage GetMiniGameSlideMergeStage(int id)
        {
            return data.GetMiniGameSlideMergeStage(id);
        }
        
        public MiniGameSlideMergeItem GetMiniGameSlideMergeItem(int id)
        {
            return data.GetMiniGameSlideMergeItem(id);
        }
        #endregion
    }
}