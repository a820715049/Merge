/**
 * @Author: handong.liu
 * @Date: 2022-01-17 16:37:24
 */
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EL;

namespace GameNet
{
    public interface IHttpGameClient
    {
        void SetErrorRetryCB(System.Action<long> cb);          //error code
        IProtobufCodec protocolCodec {get;}
        void SetHobbitsName(int hn);            //=SetEnableProtocolSignature, 1 = enable， 0 = disable

        void InitWithConfig(HttpClientConfig config);

        void Dispose();

        void SetToken(string token);

        void RequestPost(string path, bool needToken, System.Action<NetResponse> onResponse, Google.Protobuf.IMessage data, int retryCount);

        void AddAdditionalHeader(string key, string val);
        
        void RemoveAdditionalHeader(string key);

        void Update(float sec);
    }
}