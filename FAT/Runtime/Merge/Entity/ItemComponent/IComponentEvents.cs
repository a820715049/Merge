/**
 * @Author: handong.liu
 * @Date: 2021-03-22 15:05:55
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
namespace FAT.Merge
{
    public interface IComponentEventsEffectChange
    {
        void OnEffectChanged(SpeedEffect effect);
    }

    public interface IComponentEventsItemMove
    {
        void OnItemMove();
    }

    public interface IComponentEventsItemStatusChange
    {
        void onItemStatusChange();
    }
}