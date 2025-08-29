// ================================================
// File: MBCloudBoardItemHolder.cs
// Author: yueran.li
// Date: 2025/04/25 16:15:38 星期五
// Desc: 云层
// ================================================

using System.Collections.Generic;
using FAT.Merge;
using UnityEngine;

namespace FAT
{
    public class MBBoardCloudHolder : MonoBehaviour
    {
        [SerializeField] private GameObject cloudViewPrefab;
        [SerializeField] private RectTransform mRoot;

        private int width;
        private int height;

        private Dictionary<(int, int), MBCloudView> mCellDict = new(); // 表现层
        public List<Cloud> curClouds = new(); // 数据层

        private FarmBoardActivity _activity;

        public void SetUp()
        {
            GameObjectPoolManager.Instance.PreparePool(PoolItemType.MERGE_CLOUD_VIEW, cloudViewPrefab);
        }

        public void InitOnPreOpen(FarmBoardActivity act)
        {
            _activity = act;
            
            var board = BoardViewManager.Instance.board;
            // var world = BoardViewManager.Instance.world;

            width = board.size.x;
            height = board.size.y;

            _PrepareGrid();
        }

        public void Cleanup()
        {
            // var board = BoardViewManager.Instance.board;
            // var world = BoardViewManager.Instance.world;

            _ReleaseGrid();
        }

        private void _PrepareGrid()
        {
            float cellSize = BoardUtility.cellSize;
            float halfSize = cellSize * 0.5f;
            var clouds = GetCurClouds();

            GameObject go;
            // 先height后width 为了处理相邻云层叠加时显示问题
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (TryGetBelongCloud(clouds, i, j, out var cloud))
                    {
                        go = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MERGE_CLOUD_VIEW, mRoot);
                        go.SetActive(true);
                        var cloudView = go.GetComponent<MBCloudView>();
                        cloudView.Init(cloud, i, j);

                        var trans = go.transform as RectTransform;
                        trans.anchoredPosition = new Vector2(i * cellSize + halfSize, -j * cellSize - halfSize);
                        trans.sizeDelta = new Vector2(cellSize, cellSize) * 1.1f; // 云层要比格子大一点

                        mCellDict.Add((i, j), cloudView);
                    }
                }
            }
        }

        public void ReFillCloud()
        {
            float cellSize = BoardUtility.cellSize;
            float halfSize = cellSize * 0.5f;
            var clouds = GetCurClouds();

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    if (mCellDict.TryGetValue((i, j), out var view))
                    {
                        var trans = view.transform as RectTransform;
                        trans.SetParent(mRoot);
                        trans.anchoredPosition = new Vector2(i * cellSize + halfSize, -j * cellSize - halfSize);
                    }
                    else
                    {
                        if (TryGetBelongCloud(clouds, i, j, out var cloud))
                        {
                            var go = GameObjectPoolManager.Instance.CreateObject(PoolItemType.MERGE_CLOUD_VIEW,
                                mRoot);
                            go.SetActive(true);
                            var cloudView = go.GetComponent<MBCloudView>();
                            cloudView.Init(cloud, i, j);

                            var trans = go.transform as RectTransform;
                            trans.anchoredPosition = new Vector2(i * cellSize + halfSize, -j * cellSize - halfSize);
                            trans.sizeDelta = new Vector2(cellSize, cellSize) * 1.1f; // 云层要比格子大一点

                            mCellDict.Add((i, j), cloudView);
                        }
                    }
                }
            }
        }

        private void _ReleaseGrid()
        {
            foreach (var coord in mCellDict.Keys)
            {
                GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MERGE_CLOUD_VIEW, mCellDict[coord].gameObject);
            }

            mCellDict.Clear();
        }

        public void ReleaseView((int, int)coord)
        {
            GameObjectPoolManager.Instance.ReleaseObject(PoolItemType.MERGE_CLOUD_VIEW, mCellDict[coord].gameObject);
            mCellDict.Remove(coord);
        }

        // 获得下一个解锁的一片云 不刷新数据
        public Cloud GetNextCloud()
        {
            int level = int.MaxValue;
            Cloud cloud = null;
            foreach (var area in curClouds)
            {
                if (area.UnlockLevel < level)
                {
                    level = area.UnlockLevel;
                    cloud = area;
                }
            }

            return cloud;
        }

        // 获得当前棋盘中所有云
        private List<Cloud> GetCurClouds()
        {
            FillCurShowCloud();
            return curClouds;
        }

        public void FillCurShowCloud()
        {
            var board = BoardViewManager.Instance.board;
            curClouds.Clear();
            board.FillCurShowCloud(curClouds);
        }

        private bool TryGetBelongCloud(List<Cloud> clouds, int x, int y, out Cloud cloud)
        {
            cloud = null;
            foreach (var area in clouds)
            {
                if (area.CloudArea.Contains((x, y)))
                {
                    cloud = area;
                    return true;
                }
            }

            return false;
        }

        // 获得某个云 所在的 云组
        public bool TryGetCloudViews(int x, int y, out List<MBCloudView> views)
        {
            views = new();
            Cloud cloudArea = null;
            var clouds = GetCurClouds();
            foreach (var area in clouds)
            {
                if (area.UnlockLevel < _activity.UnlockMaxLevel)
                {
                    continue;
                }

                if (area.CloudArea.Contains((x, y)))
                {
                    cloudArea = area;
                    break;
                }
            }

            if (cloudArea == null)
            {
                return false;
            }

            foreach (var coord in cloudArea.CloudArea)
            {
                views.Add(mCellDict[coord]);
            }

            return false;
        }

        // 获得一个云的表现
        public bool GetCloudViewByCoord(int x, int y, out MBCloudView view)
        {
            return mCellDict.TryGetValue((x, y), out view);
        }
    }
}