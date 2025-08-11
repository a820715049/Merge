/**
 * @Author: handong.liu
 * @Date: 2021-02-02 18:28:03
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

public interface ILoadingProgress
{
    float progress01 {get;}
}

public interface ICompositeLoadingProgress : ILoadingProgress
{
    void AddLoader(float startProgress, ILoadingProgress progress);
    void Reset();
}