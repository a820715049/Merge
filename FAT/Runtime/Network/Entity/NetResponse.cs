/**
 * @Author: handong.liu
 * @Date: 2020-09-28 13:52:06
 */
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using EL;
using EL.SimpleJSON;
using Google.Protobuf;

namespace GameNet
{
    public class NetResponse
    {
        public DateTime requestStart;
        public DateTime requestEnd;
        public IMessage rawBody { get {return mBody;} }
        public string err { get { return mError; } }
        public long errCode { get { return mErrorCode; } }           //<0 means network error, >0 means server logic error, == 0 means success
        private static byte[] sStreamBuffer = new byte[256];
        private long mErrorCode;
        private string mError;
        private IMessage mBody;
        private IMessage mCachedResponseBody;
        private IProtobufCodec mCodec;
        public T GetBody<T>() where T:class, IMessage, new()
        {
            T body = mCachedResponseBody as T;
            if(body != null)
            {
                return body;
            }
            var rawAny = mBody as gen.netutils.RawAny;
            if(rawAny == null)
            {
                return default(T);
            }
            else
            {
                if(mCodec == null)
                {
                    DebugEx.FormatInfo("NetResponse.GetBody ----> no codec!");
                    return default(T);
                }
                else
                {
                    var ret = mCodec.UnmarshalFromByteString<T>(rawAny.Raw);
                    mCachedResponseBody = ret;
                    return ret;
                }
            }
        }
        public void SetError(gen.netutils.ErrorResponse error)
        {
            mBody = error;
            mError = error.Message;
            mErrorCode = ErrorCodeUtility.ConvertToCommonCode(error.Code, ErrorCodeType.ServerError);
        }
        public void SetError(long code, ErrorCodeType codeType, string err)
        {
            mBody = null;
            mError = err;
            mErrorCode = ErrorCodeUtility.ConvertToCommonCode(code, codeType);
        }
        public void SetBody(IMessage msg)
        {
            mCachedResponseBody = msg;
            mError = null;
            mErrorCode = (int)fat.gamekitdata.ErrorCode.Ok;
        }
        public void SetMessage(IMessage msg, IProtobufCodec codec)
        {
            mCodec = codec;
            mBody = msg;
            mError = null;
            mErrorCode = (int)fat.gamekitdata.ErrorCode.Ok;
        }
    }
}