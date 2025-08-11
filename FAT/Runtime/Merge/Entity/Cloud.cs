/**
 * @Author: handong.liu
 * @Date: 2021-07-22 11:41:37
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Config;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public enum CloudType
    {
        Movable,   //可随着棋盘移动而自身坐标发生变化的云层
    }
    
    public class Cloud
    {
        //当前云层是否可以显示，条件为：坐标有效且处于未解锁状态
        public bool CanShow => CloudArea.Count > 0 && !IsUnlock;
        //当前云层的区域范围 使用坐标描述 支持不规则矩形  CloudArea.Count>0 说明当前云层正在棋盘上显示
        public HashSet<(int col, int row)> CloudArea { get; } = new();
        //区域解锁等级 会根据不同情况有不同含义 可能是玩家等级 也可能是某个棋盘的里程碑等级
        public int UnlockLevel { get; private set; }
        public bool IsUnlock { get; private set; }   //是否已解锁
        //区域类型 主要用于区分不同处理逻辑
        public CloudType Type { get; private set; }
        //配置Conf Id
        public int ConfId { get; private set; }
        //该云层配置的坐标区域
        private List<CoordConfig> _coordConfigList = new List<CoordConfig>();

        //创建云层区域 此时还未刷新区域在棋盘上的实际坐标点位
        public static bool CreateCloud(MergeCloud conf, CloudType type, out Cloud cloud)
        {
            cloud = null;
            if (conf == null)
                return false;
            cloud = new Cloud
            {
                ConfId = conf.CloudId,
                Type = type,
                UnlockLevel = conf.UnlockLevel
            };
            cloud._InitCoordConfigList(conf.Coord);
            return true;
        }

        //结合当前棋盘行数和偏移程度 刷新棋盘中云层区域的坐标，对外部来讲，可以通过这个操作来感知到当前棋盘上正在显示哪些云层区域
        public void RefreshCloudArea(int boardRowCount, int boardMoveOffset = 0)
        {
            CloudArea.Clear();
            foreach (var oldCoord in _coordConfigList)
            {
                if (_TransCoord(oldCoord.Col, oldCoord.Row, out var newCol, out var newRow, boardRowCount, boardMoveOffset))
                    CloudArea.Add((newCol, newRow));
                else
                {
                    //一旦有坐标不符合 则整个区域都不算数
                    CloudArea.Clear();
                    break;
                }
            }
        }

        public void RefreshUnlockState(int curLevel)
        {
            IsUnlock = curLevel >= UnlockLevel;
        }

        private void _InitCoordConfigList(IList<string> coordConf)
        {
            _coordConfigList.Clear();
            foreach (var coord in coordConf)
            {
                var c = coord.ConvertToCoord();
                if (c != null)
                {
                    _coordConfigList.Add(c);
                }
            }
        }

        //boardRowCount 棋盘总行数
        //boardMoveOffset 棋盘目前偏移的行数 默认初始为0表示未偏移
        private bool _TransCoord(int oldCol, int oldRow, out int newCol, out int newRow, int boardRowCount, int boardMoveOffset = 0)
        {
            newCol = 0; 
            newRow = 0;
            switch (Type)
            {
                //将逻辑坐标 oldRow 映射到当前棋盘数据结构中的视图行坐标（0 ~ boardRowCount-1）
                //如果在当前棋盘视图的可见范围内，返回 localY；否则返回 -1 表示不可见
                case CloudType.Movable:
                    newCol = oldCol;
                    var localY = oldRow - boardMoveOffset;
                    //坐标反转
                    newRow = (localY >= 0 && localY < boardRowCount) ? (boardRowCount - localY - 1) : -1;
                    //-1的时候返回false
                    return newRow != -1;
                default:
                    return false; 
            }
        }
    }
}