/*
 * @Author: qun.chao
 * @Date: 2022-03-07 10:25:30
 */
using UnityEngine;

namespace FAT
{
    public class GuideActImpBase : CustomYieldInstruction
    {
        public override bool keepWaiting
        {
            get
            {
                return IsWaiting();
            }
        }

        protected bool mIsWaiting = true;

        protected virtual bool IsWaiting()
        {
            return mIsWaiting;
        }

        public virtual void Clear()
        { }

        public virtual void Play(string[] param)
        { }
    }
}