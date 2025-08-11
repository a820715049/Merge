using UnityEngine;
using FAT;
using System.Text;
using NUnit.Framework;
using static fat.conf.Data;
using fat.rawdata;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Tests {
    using static ConfHelper;

    public class ActivityGuessTest {
        public void Generate(ActivityGuess acti_, IList<int> weight_) {
            var listA = acti_.answer;
            var listH = acti_.hand;
            acti_.Generate(weight_);
            var (valid, rs) = acti_.ValidateLevel();
            Assert.True(valid, rs);
            Assert.AreEqual(null, rs);
            Assert.AreEqual(listA.Count, listH.Count);
            Assert.AreEqual(listA.Count, weight_.Sum());
        }

        //ref https://stackoverflow.com/questions/11208446/generating-permutations-of-a-set-most-efficiently
        private static bool NextPermutation(int[] numList)
        {
            /*
            Knuths
            1. Find the largest index j such that a[j] < a[j + 1]. If no such index exists, the permutation is the last permutation.
            2. Find the largest index l such that a[j] < a[l]. Since j + 1 is such an index, l is well defined and satisfies j < l.
            3. Swap a[j] with a[l].
            4. Reverse the sequence from a[j + 1] up to and including the final element a[n].

            */
            var largestIndex = -1;
            for (var i = numList.Length - 2; i >= 0; i--)
            {
                if (numList[i] < numList[i + 1]) {
                    largestIndex = i;
                    break;
                }
            }

            if (largestIndex < 0) return false;

            var largestIndex2 = -1;
            for (var i = numList.Length - 1 ; i >= 0; i--) {
                if (numList[largestIndex] < numList[i]) {
                    largestIndex2 = i;
                    break;
                }
            }

            (numList[largestIndex2], numList[largestIndex]) = (numList[largestIndex], numList[largestIndex2]);
            for (int i = largestIndex + 1, j = numList.Length - 1; i < j; i++, j--) {
                (numList[j], numList[i]) = (numList[i], numList[j]);
            }

            return true;
        }

        [Test]
        public void Validate() {
            var acti = new ActivityGuess();
            var wA = new int[] { 1, 2, 3, 4, 5};
            var wH = new int[wA.Length];

            do {
                var aK = acti.PackKey(wA);
                for (var k = 0; k < wH.Length; ++k) wH[k] = k + 1;
                do {
                    var hK = acti.PackKey(wH);
                    acti.ApplyGenerate(wA.Length, aK, 0, 0, 0, hK, 0);
                    var (valid, rs) = acti.ValidateLevel();
                    Assert.True(valid);
                    Assert.AreEqual(null, rs);
                } while (NextPermutation(wH));
            } while (NextPermutation(wA));
        }

        [Test]
        public void Invalidate() {
            var acti = new ActivityGuess();
            var listA = acti.answer;
            var listH = acti.hand;
            var w = new List<int> { 1, 2, 2};

            Generate(acti, w);

            var i = 0;
            listA[i].value = 0;
            var (valid, rs) = acti.ValidateLevel();
            Assert.False(valid);
            Assert.AreEqual($"invalid answer at {i}", rs);

            acti.Generate(w);
            var ii = 0;
            var vv = listH[ii].value;
            listH[ii].value = 0;
            (valid, rs) = acti.ValidateLevel();
            Assert.False(valid);
            Assert.AreEqual($"answer not fullfillable card {vv} diff:{1}", rs);
        }

        [Test]
        public void Generate1() {
            var acti = new ActivityGuess();

            var weight = new List<int>() { 1, 1, 1, 1, 1 };
            Generate(acti, weight);

            acti.Generate(weight);
            var (valid, rs) = acti.ValidateLevel();
            Assert.True(valid);
            Assert.AreEqual(null, rs);
        }

        [Test]
        public void Generate2() {
            var acti = new ActivityGuess();

            var weight = new List<int>() { 2, 2, 2 };
            Generate(acti, weight);

            acti.Generate(weight);
            var (valid, rs) = acti.ValidateLevel();
            Assert.True(valid);
            Assert.AreEqual(null, rs);
        }

        [Test]
        public void FirstInTypeHack() {
            var acti = new ActivityGuess();
            var listA = acti.answer;
            var wA = new int[] { 1, 2, 3, 4 };
            var wH = new int[wA.Length];

            do {
                var aK = acti.PackKey(wA);
                for (var k = 0; k < wH.Length; ++k) wH[k] = k + 1;
                do {
                    var hK = acti.PackKey(wH);
                    acti.ApplyGenerate(wA.Length, aK, hK, 0, 0, 0, 0);
                    acti.FirstInTypeHack();
                    var (valid, rs) = acti.ValidateLevel();
                    Assert.True(valid, rs);
                    Assert.AreEqual(null, rs);
                    var count = listA.Count - 1;
                    for (var k = 0; k < count; ++k) {
                        var a = listA[k];
                        Assert.AreNotEqual(a.bet, a.value);
                    }
                    var aa = listA[^1];
                    Assert.AreEqual(aa.bet, aa.value);
                } while (NextPermutation(wH));
            } while (NextPermutation(wA));
        }
    }
}