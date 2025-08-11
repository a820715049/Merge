/**
 * @Author: handong.liu
 * @Date: 2021-06-16 16:22:58
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace EL
{
    public class DeterministicRandom
    {
        public int seed => mSeed;
        public const int kRandomMax = int.MaxValue;
        public int Next {
            get {
                _MoveNext();
                return mBuffer[mCurrentLoopedIdx];
            }
        }
        private int[] mBuffer = new int[55];            //this 55 if changed, the 33, 24, 24, 165 constant integer use below should also changed
        private int mCurrentLoopedIdx = 0;
        private int mCurrentIdx = 0;
        private int mSeed = 0;
        public DeterministicRandom()
        {
            ResetWithSeed(0);
        }
        public DeterministicRandom(int seed)
        {
            ResetWithSeed(seed);
        }
        public void ResetWithSeed(int seed, int index = 0)
        {
            seed = Mathf.Abs(seed);
            mSeed = seed;
            int s0 = mBuffer[mBuffer.Length - 1] = seed, s1 = mBuffer[33] = 1;
            for(int i = 2; i < mBuffer.Length; i++)
            {
                int next = EL.MathUtility.AbsMod(s0 - s1, kRandomMax);
                s0 = s1;
                s1 = next;
                mBuffer[_RingIdx(34 * i - 1)] = next;
            }
            mCurrentLoopedIdx = mCurrentIdx = mBuffer.Length - 1;
            for(int i = 0; i < 165; i++)
            {
                _MoveNext();
            }
            while(mCurrentLoopedIdx != 0)
            {
                _MoveNext();
            }
            mCurrentIdx = 0;
            while(mCurrentIdx < index)
            {
                _MoveNext();
            }
        }

        public int GetByIndex(int idx)
        {
            if(idx > mCurrentIdx || idx <= mCurrentIdx - mBuffer.Length)
            {
                ResetWithSeed(mSeed, idx);
            }
            return mBuffer[_RingIdx(idx)];
        }

        private void _MoveNext()
        {
            mCurrentIdx ++;
            int nextIdx = _RingIdx(mCurrentLoopedIdx + 1);
            mBuffer[nextIdx] = EL.MathUtility.AbsMod(mBuffer[_RingIdx(nextIdx - mBuffer.Length)] - mBuffer[_RingIdx(nextIdx - 24)], kRandomMax);
            mCurrentLoopedIdx = nextIdx;
        }

        private int _RingIdx(int idx)
        {
            return EL.MathUtility.AbsMod(idx, mBuffer.Length);
        }
    }
}