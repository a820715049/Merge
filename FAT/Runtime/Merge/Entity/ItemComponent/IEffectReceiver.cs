/**
 * @Author: handong.liu
 * @Date: 2021-03-22 17:05:41
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace FAT.Merge
{
    public interface IEffectReceiver
    {
        bool WillReceiveEffect(SpeedEffect effect);
    }
}