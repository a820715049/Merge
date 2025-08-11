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
                    if (int.TryParse(str, out var itemId))
                    {
                        list.Add(itemId);
                    }
                }
                Game.Manager.guideMan.ActiveGuideContext?.ShowBoardCommonMask(list, null, offsetCoe);
            }
            mIsWaiting = false;
        }
    }
}