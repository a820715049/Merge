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

namespace Tests {
    using static ConfHelper;

    public class ActivityExpireTest {
        public RepeatedField<RoundExpire> Load() {
            var list = GetRoundExpireSlice();
            if (list != null && list.Count > 0) return list;
            var name = nameof(RoundExpireConf);
            var file = GetTableLoadPath(name, out var fileReadable, false, Constant.kConfExt);
            Debug.Log(file);
            var loader = Loaders[name];
            var data = File.ReadAllBytes(file);
            var bytes = ProtoFileUtility.Decrypt(data);
            loader.Invoke(new CodedInputStream(bytes));
            return GetRoundExpireSlice();
        }

        [Test]
        public void Round() {
            var listR = Load();
            static void T(int input_, int expect) {
                Debug.Log($"input:{input_}");
                var v = ActivityExpire.RoundExpire(input_);
                Assert.AreEqual(expect, v);
            }
            foreach(var c in listR) {
                T(c.From, c.RwdNum);
            }
            for (var k = 1; k < listR.Count; ++k) {
                var c = listR[k];
                var cc = listR[k - 1];
                T(c.From - 1, cc.RwdNum);
            }
            for (var k = 0; k < listR.Count - 1; ++k) {
                var c = listR[k];
                T(c.From + 1, c.RwdNum);
            }
            var l = listR[^1];
            T(l.From * 10, l.RwdNum);
        }
    }
}