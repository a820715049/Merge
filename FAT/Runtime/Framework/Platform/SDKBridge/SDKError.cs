using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FAT.Platform {
    public class SDKError
    {
        public long ErrorCode { get; set; }
        public string Message { get; set; }

        public SDKError() { }

        public SDKError(long err, string msg)
        {
            ErrorCode = err;
            Message = msg;
        }

        public override string ToString() {
            return $"{Message}({ErrorCode})";
        }
    }
}