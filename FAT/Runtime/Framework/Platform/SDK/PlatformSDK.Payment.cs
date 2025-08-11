/*
 * @Author: qun.chao
 * @Date: 2023-10-31 14:22:28
 */
using System;
using System.Collections;
using UnityEngine;

namespace FAT.Platform {
    public readonly ref struct Transaction {
        public readonly string productId;
        public readonly uint serverId;
        public readonly string cargo;

        public Transaction(string productId_, uint serverId_, string cargo_) {
            productId = productId_;
            serverId = serverId_;
            cargo = cargo_;
        }
    }

    public partial class PlatformSDK {
        public void Purchase(Transaction target_, Action<string, bool, SDKError> WhenComplete_) {
            #if UNITY_EDITOR
            IEnumerator Delay(string pId_) {
                yield return new WaitForSeconds(1.5f);
                WhenComplete_?.Invoke(pId_, true, null);
            }
            Game.Instance.StartCoroutineGlobal(Delay(target_.productId));
            return;
            #endif
            #pragma warning disable 0162
            Adapter.Buy(target_, WhenComplete_);
            #pragma warning restore 0162
        }
    }
}