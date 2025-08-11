/**
 * @Author: handong.liu
 * @Date: 2021-03-22 17:21:40
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public static class EffectUtility
    {
        public static int CalculateMilliBySpeedEffect(ItemComponentBase com, int milli)
        {
            // 项目里没有真正实现IEffectReceiver
            return milli;
            var receiver = com as IEffectReceiver;
            if(receiver == null)
            {
                return milli;
            }
            int add = 0;
            using(ObjectPool<List<SpeedEffect>>.GlobalPool.AllocStub(out var speedEffects))
            {
                com.item.FillAllEffects(speedEffects);
                foreach(var e in speedEffects) {
                    if(receiver.WillReceiveEffect(e))
                    {
                        int baseMilli = milli;
                        if(baseMilli > e.milliLeft)
                        {
                            baseMilli = e.milliLeft;
                        }
                        add += baseMilli * e.speedPercent / 100;
                    }
                }
            }
            return milli + add;
        }
    }
}