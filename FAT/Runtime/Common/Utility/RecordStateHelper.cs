using System.Collections;
using System.Collections.Generic;
using GameNet;
using UnityEngine;
using Google.Protobuf.Collections;
using fat.gamekitdata;

namespace FAT {
    public static class RecordStateHelper {
        private static bool Find(int id, IList<AnyState> list, out AnyState v) {
            foreach (var r in list) {
                if (r.Id == id) {
                    v = r;
                    return true;
                }
            }
            v = null;
            return false;
        }

        public static void UpdateRecord(int id, int val, IList<AnyState> list) {
            bool found = false;
            foreach (var r in list) {
                if (r.Id == id) {
                    found = true;
                    r.Value = val;
                    break;
                }
            }
            if (!found) {
                list.Add(ToRecord(id, val));
            }
        }

        public static bool RemoveRecord(int id, IList<AnyState> list) {
            foreach (var r in list) {
                if (r.Id == id) {
                    list.Remove(r);
                    return true;
                }
            }
            return false;
        }

        public static AnyState ToRecord(int id, bool v_) => new() { Id = id, Value = v_ ? 1 : 0 };
        public static bool ReadBool(int id, IList<AnyState> list)
            => Find(id, list, out var v) && (v.Value > 0);

        public static AnyState ToRecord(int id, int v_) => new() { Id = id, Value = v_ };
        public static int ReadInt(int id, IList<AnyState> list)
            => Find(id, list, out var v) ? v.Value : 0;

        public static AnyState ToRecord(int id, long ts, long offset) => new() { Id = id, Value = (int)(ts - offset) };
        public static long ReadTS(int id, long offset, IList<AnyState> list)
            => Find(id, list, out var v) ? (v.Value + offset) : 0;
    }
}