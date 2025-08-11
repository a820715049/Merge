/*
 * @Author: qun.chao
 * @Date: 2023-10-17 16:41:14
 */
using System;
using System.Collections.Generic;

namespace FAT
{
    public interface IGameLoading
    {
        void OnPreFadeIn();
        bool HasFadeIn();
        void OnPostFadeIn();

        void OnPreFadeOut();
        bool HasFadeOut();
        void OnPostFadeOut();

        void SetProgress(float p);
        void SetProgress(float from, float to, Func<float> reporter);
    }
}