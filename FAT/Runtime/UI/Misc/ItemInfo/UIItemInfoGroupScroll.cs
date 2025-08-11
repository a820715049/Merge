/*
 * @Author: tang.yan
 * @Description: 物品信息scroll 
 * @Date: 2023-11-24 14:11:15
 */

using System.Collections.Generic;
using Config;

namespace FAT
{
    public class UIItemInfoGroupScroll : UICommonScrollRect<List<int>, UICommonScrollRectDefaultContext>
    {
        private int _maxCount;
        protected override bool Scrollable => ItemsSource.Count > _maxCount;

        public void SetScrollableMax(int count)
        {
            _maxCount = count;
        }
    }
}