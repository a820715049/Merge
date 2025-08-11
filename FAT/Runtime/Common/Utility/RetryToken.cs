using System;
using UnityEngine;

namespace FAT {
    public class RetryToken {
        public int n;
        public int max;
        public float delay;
        public float delayN;
        public bool WillRetry => n < max;

        public void Reset() => n = 0;

        public WaitForSecondsRealtime Retry() {
            ++n;
            return new(delay + delayN * Math.Min(n - 1, max));
        }
    }
}