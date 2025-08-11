/**
 * @Author: handong.liu
 * @Date: 2021-04-12 16:21:50
 */
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System.Collections;
using System.Net;
using System.Collections.Generic;
using EL;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace GameNet
{
    public class ProtobufCodecBin : IProtobufCodec
    {
        private Dictionary<System.Type, IMessage> mInstanceMap = new Dictionary<System.Type, IMessage>();
        private MemoryStream mUTF8Buffer = new MemoryStream();

        public void PreProcessRequest(HttpWebRequest request)
        {
            request.ContentType = "application/x-protobuf";
            request.Accept = "application/x-protobuf";
        }
        public void PreProcessRequest(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/x-protobuf");
            request.SetRequestHeader("Accept", "application/x-protobuf");
        }
        public void MarshalToStream(Stream target, IMessage msg)
        {
            mUTF8Buffer.Position = 0;
            mUTF8Buffer.SetLength(0);
            msg.WriteTo(target);
        }
        public ByteString MarshalToByteString(IMessage msg)
        {
            mUTF8Buffer.Position = 0;
            mUTF8Buffer.SetLength(0);
            msg.WriteTo(mUTF8Buffer);
            mUTF8Buffer.Position = 0;
            return ByteString.FromStream(mUTF8Buffer);
        }
        public T UnmarshalFromStream<T>(Stream byteStream) where T : IMessage, new()
        {
            T inst = new T();
            inst.MergeFrom(byteStream);
            return inst;
        }
        public T UnmarshalFromByteString<T>(ByteString buffer) where T : IMessage, new()
        {
            T inst = new T();
            inst.MergeFrom(buffer);
            return inst;
        }
        
        string IProtobufCodec.Dump(IMessage msg)
        {
            var rawPacket = msg as gen.netutils.RawPacket;
            if(rawPacket != null)
            {
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < rawPacket.RawAny.Count; i++)
                {
                    string serialized = "TODO";
#if UNITY_EDITOR
                    string typeName = rawPacket.RawAny[i].Uri.Substring(rawPacket.RawAny[i].Uri.LastIndexOf(".") + 1);
                    var myName = GetType();
                    var type = GetType().Assembly.GetType("GameNet." + typeName);
                    IMessage instance = null;
                    if(type != null && !mInstanceMap.TryGetValue(type, out instance))
                    {
                        instance = System.Activator.CreateInstance(type) as IMessage;
                        mInstanceMap[type] = instance;
                    }
                    if(instance != null)
                    {
                        instance.MergeFrom(rawPacket.RawAny[i].Raw);
                        serialized = instance.ToString();
                    }
#endif
                    sb.AppendFormat("{0}:{1}", rawPacket.RawAny[i].Uri, serialized);
                    if(i + 1 < rawPacket.RawAny.Count)
                    {
                        sb.Append(",");
                    }
                }
                return sb.ToString();
            }
            else
            {
                return msg.ToString();
            }
        }

        IProtobufCodec IProtobufCodec.Clone()
        {
            return new ProtobufCodecBin();
        }
    }
}