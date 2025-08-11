/**
 * @Author: handong.liu
 * @Date: 2020-08-13 13:59:23
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;
public static class RendererExt
{
    public static Material GetSharedMaterial(this Renderer rend)
    {
#if UNITY_EDITOR
        return rend.material;
#else
        return rend.sharedMaterial;
#endif
    }
    public static Material[] GetSharedMaterials(this Renderer rend)
    {
#if UNITY_EDITOR
        return rend.materials;
#else
        return rend.sharedMaterials;
#endif
    }
}