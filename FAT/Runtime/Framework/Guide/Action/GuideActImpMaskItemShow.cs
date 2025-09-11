/*
 * @Author: qun.chao
 * @Date: 2023-11-24 12:15:16
 */
using System.Collections.Generic;
using FAT.Merge;
using EL;

namespace FAT
{
    public class GuideActImpMaskItemShow : GuideActImpBase
    {
        public override void Play(string[] param)
        {
            float offsetCoe = 0f;
            if (param.Length > 1)
            {
                float.TryParse(param[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out offsetCoe);
            }
            using (ObjectPool<List<int>>.GlobalPool.AllocStub(out var list))
            {
                var strs = param[0].Split(':');
                foreach (var str in strs)
                {
                    if (int.TryParse(str, out var itemId) && itemId > 0)
                    {
                        list.Add(itemId);
                    }
                }
                //新增3个参数时的处理逻辑 会在param[0]的基础上尝试增加棋子到list
                if (param.Length > 2)
                {
                    var extraParam = param[2];
                    if (extraParam == "FrozenItem")
                    {
                        var frozenItemId = BoardViewManager.Instance.GetFirstFrozenItemId();
                        if (frozenItemId > 0)
                        {
                            list.Add(frozenItemId);
                        }
                    }
                }
                Game.Manager.guideMan.ActiveGuideContext?.ShowBoardCommonMask(list, null, offsetCoe);
            }
            mIsWaiting = false;
        }
    }
}