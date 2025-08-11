/**
 * @Author: handong.liu
 * @Date: 2021-07-22 11:41:37
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
using fat.rawdata;

namespace FAT.Merge
{
    public class Area
    {
        public MergeGridArea config => mConfig;
        public RectInt rect => mRect;
        private MergeGridArea mConfig;
        private RectInt mRect;

        public Area(MergeGridArea cfg)
        {
            mConfig = cfg;
            _RefreshArea();
        }

        public void WalkGrid(System.Action<int, int, int> walkFunc)     //param: col, row, tid
        {
            for(int i = 0; i < mConfig.Grid.Count; i++)
            {
                if(mConfig.Grid[i] != 0)
                {
                    walkFunc?.Invoke(mRect.xMin + i % mRect.width, mRect.yMin + i / mRect.width, mConfig.Grid[i]);
                }
            }
        }

        private void _RefreshArea()
        {
            int height = (config.Grid.Count + config.Shape[2] - 1) / config.Shape[2];
            mRect = new RectInt(config.Shape[0], config.Shape[1], config.Shape[2], height);
        }
    }
}