using System;
using System.Collections.Generic;
using fat.rawdata;

namespace MiniGame
{
    #region 珠子

    public class BeadCellInfo
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public int ConfID;

        /// <summary>
        /// 配置类型
        /// </summary>
        public BeadsType Type;

        /// <summary>
        /// 当前处于第几个基座下
        /// </summary>
        public int Belong;

        /// <summary>
        /// 处于当前基座的第几个
        /// </summary>
        public int Index;
    }

    #endregion

    #region 基座

    public class BeadBaseInfo
    {
        /// <summary>
        /// 配置ID
        /// </summary>
        public int ConfID;

        /// <summary>
        /// 配置类型
        /// </summary>
        public BeadsType Type;

        /// <summary>
        /// 当前处在底座下的珠子List
        /// </summary>
        public readonly List<BeadCellInfo> Beads = new();

        /// <summary>
        /// 该底座是否已经完成，若完成则不能再移动了
        /// </summary>
        public bool Complete;
    }

    #endregion

    #region 引导

    public class BeadGuide
    {
        public (int, int) Start;
        public (int, int) End;
    }

    #endregion

    public class MiniGameBeadsData
    {
        public readonly List<BeadBaseInfo> BaseInfos = new();
        public readonly List<BeadCellInfo> CellInfos = new();
        public readonly List<BeadGuide> GuideInfos = new();

        private MiniGameBeadsLevel _level;

        /// <summary>
        /// 初始化关卡
        /// </summary>
        /// <param name="level">关卡ID</param>
        public void Init(int level, bool isGuide)
        {
            ClearData();
            InitData(level, isGuide);
        }

        public void DeInit()
        {
            ClearData();
        }

        /// <summary>
        /// 清理数据
        /// </summary>
        private void ClearData()
        {
            BaseInfos.Clear();
            CellInfos.Clear();
            GuideInfos.Clear();
            _level = null;
        }

        /// <summary>
        /// 获取关卡配置，调用接口创建基座
        /// </summary>
        /// <param name="level">关卡ID</param>
        private void InitData(int level, bool isGuide)
        {
            _level = MiniGameConfig.Instance.GetBeadsLevelConf(level);
            if (isGuide) InitGuide(_level.GuideAction);
            foreach (var stage in isGuide ? _level.GuideStageId : _level.StageId)
                InitStage(stage);
        }

        /// <summary>
        /// 创建基座
        /// </summary>
        /// <param name="baseID">基座的ID</param>
        private void InitStage(int baseID)
        {
            var stage = MiniGameConfig.Instance.GetBeadsStage(baseID);
            var baseConf = MiniGameConfig.Instance.GetBeadsBase(stage.BaseInfo);
            var temp = new BeadBaseInfo() { ConfID = stage.BaseInfo, Type = baseConf.BeadsType };
            BaseInfos.Add(temp);
            foreach (var cell in stage.CellInfo) InitItem(temp, cell);
        }

        /// <summary>
        /// 创建珠子
        /// </summary>
        /// <param name="baseInfo">基座实例</param>
        /// <param name="itemID">珠子ID</param>
        private void InitItem(BeadBaseInfo baseInfo, int itemID)
        {
            var cellConf = MiniGameConfig.Instance.GetBeadsCell(itemID);
            var cell = new BeadCellInfo()
            {
                ConfID = cellConf.Id,
                Belong = BaseInfos.Count - 1,
                Index = baseInfo.Beads.Count,
                Type = cellConf.BeadsType
            };
            baseInfo.Beads.Add(cell);
            CellInfos.Add(cell);
        }

        /// <summary>
        /// 创建引导序列
        /// </summary>
        private void InitGuide(string action)
        {
            var actList = action.Split('#');

            foreach (var act in actList)
            {
                var (start, end) = ParseAction(act);
                var actInfo = new BeadGuide { Start = start, End = end };
                GuideInfos.Add(actInfo);
            }
        }

        /// <summary>
        /// 分离完整操作
        /// </summary>
        /// <param name="act"></param>
        /// <returns>操作元组</returns>
        private ((int, int) Start, (int, int) End) ParseAction(string act)
        {
            var parts = act.Split(':');
            var startCoords = ParseCoordinates(parts[0]);
            var endCoords = ParseCoordinates(parts[1]);
            return (startCoords, endCoords);
        }

        /// <summary>
        /// 分离坐标
        /// </summary>
        /// <param name="coords"></param>
        /// <returns>坐标</returns>
        private (int, int) ParseCoordinates(string coords)
        {
            var values = coords.Split(',');
            int.TryParse(values[0], out var x);
            int.TryParse(values[1], out var y);
            return (x, y);
        }
    }
}