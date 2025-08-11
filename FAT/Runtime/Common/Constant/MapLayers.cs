/**
 * @Author: handong.liu
 * @Date: 2020-12-17 18:07:59
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;


public static class MapLayers
{
    public enum OccupyMaskType
    {
        All,
        AllExceptRole,
        Carpet,
        Furniture,
        Role
    }
    private static uint[] OccupyMask = new uint[]
    {
        kAllLayerMask,
        kAllLayerMask & (~kLayer2),
        kLayer0,
        kLayer1 | kLayer2,
        kLayer2
    };
    public static uint GetMask(OccupyMaskType type)
    {
        return OccupyMask[(int)type];
    }
    public const uint kAllLayerMask = 0xffffffff;
    public static readonly uint kRoleLayerMask = GetMask(OccupyMaskType.Role);
    public static readonly uint kFurnitureLayerMask = GetMask(OccupyMaskType.Furniture);
    private const uint kLayer2 = ((uint)1)<<2;
    private const uint kLayer1 = ((uint)1)<<1;
    private const uint kLayer0 = ((uint)1)<<0;
}