/**
 * @Author: handong.liu
 * @Date: 2020-09-29 17:51:46
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
    public interface IProtobufCodec
    {
        void PreProcessRequest(HttpWebRequest request);
        void PreProcessRequest(UnityWebRequest request);
        void MarshalToStream(Stream target, IMessage msg);
        ByteString MarshalToByteString(IMessage msg);
        T UnmarshalFromStream<T>(Stream byteStream) where T : IMessage, new();
        T UnmarshalFromByteString<T>(ByteString buffer) where T : IMessage, new();
        string Dump(IMessage msg);
        IProtobufCodec Clone();
    }
    public class ProtobufCodec : IProtobufCodec
    {
        private JsonFormatter mFormatter = new JsonFormatter(JsonFormatter.Settings.Default.WithFormatEnumsAsIntegers(true));
        private Dictionary<System.Type, IMessage> mInstanceMap = new Dictionary<System.Type, IMessage>();
        private byte[] mUTF8Buffer = new byte[256];
        public void PreProcessRequest(HttpWebRequest request)
        {
            request.ContentType = "application/json";
            request.Accept = "application/json";
        }
        public void PreProcessRequest(UnityWebRequest request)
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
        }
        public void MarshalToStream(Stream target, IMessage msg)
        {
            int len = _Marshal(msg);
            target.Write(mUTF8Buffer, 0, len);
        }
        public ByteString MarshalToByteString(IMessage msg)
        {
            int len = _Marshal(msg);
            return ByteString.CopyFrom(mUTF8Buffer, 0, len);
        }
        public T UnmarshalFromStream<T>(Stream byteStream) where T : IMessage, new()
        {
            if(byteStream.Length > mUTF8Buffer.Length)
            {
                _Expand(byteStream.Length);
            }
            _Expand(256);
            int count = byteStream.Read(mUTF8Buffer, 0, 256);
            int total = 0;
            while(count > 0)
            {
                total += count;
                _Expand(total + 256);
                count = byteStream.Read(mUTF8Buffer, total, 256);
            }
            if(total <= 0)
            {
                return default(T);
            }
            IMessage inst = null;
            if(!mInstanceMap.TryGetValue(typeof(T), out inst))
            {
                inst = new T();
                mInstanceMap[typeof(T)] = inst;
            }
            return (T)_Unmarshal(inst.Descriptor, total);
        }
        public T UnmarshalFromByteString<T>(ByteString buffer) where T : IMessage, new()
        {
            if(buffer.Length > mUTF8Buffer.Length)
            {
                _Expand(buffer.Length);
            }
            buffer.CopyTo(mUTF8Buffer, 0);
            IMessage inst = null;
            if(!mInstanceMap.TryGetValue(typeof(T), out inst))
            {
                inst = new T();
                mInstanceMap[typeof(T)] = inst;
            }
            return (T)_Unmarshal(inst.Descriptor, buffer.Length);
        }

        private IMessage _Unmarshal(MessageDescriptor desc, int len)
        {
            string json = Encoding.UTF8.GetString(mUTF8Buffer, 0,  len);
            try
            {
                var ret = JsonParser.Default.Parse(json, desc);
                if(ret == null)
                {
                    DebugEx.FormatWarning("ProtobufCodecJson ----> unmarshal fail, message tyep:{0}, json:{1}", desc.FullName, json);
                }
                return ret;
            } 
            catch (System.Exception ex)
            {
                DebugEx.FormatWarning("ProtobufCodecJson ----> unmarshal fail, message tyep:{0}, json:{1}, ex:{2}", desc.FullName, json, ex.ToString());
                return null;
            }
        }

        private int _Marshal(IMessage msg)
        {
            var jsonStr = mFormatter.Format(msg);
            int len = 0;
            try
            {
                len = Encoding.UTF8.GetBytes(jsonStr, 0, jsonStr.Length, mUTF8Buffer, 0);
            }
            catch(System.Exception ex)
            {
                //expand capacity 
                len = Encoding.UTF8.GetByteCount(jsonStr);
                _Expand(len);
                len = Encoding.UTF8.GetBytes(jsonStr, 0, jsonStr.Length, mUTF8Buffer, 0);
            }
            return len;
        }
        
        string IProtobufCodec.Dump(IMessage msg)
        {
            var rawPacket = msg as gen.netutils.RawPacket;
            if(rawPacket != null)
            {
                StringBuilder sb = new StringBuilder();
                for(int i = 0; i < rawPacket.RawAny.Count; i++)
                {
                    var len = rawPacket.RawAny[i].Raw.Length;
                    _Expand(len);
                    rawPacket.RawAny[i].Raw.CopyTo(mUTF8Buffer, 0);
                    string str = Encoding.UTF8.GetString(mUTF8Buffer, 0, len);
                    sb.AppendFormat("{0}:{1}", rawPacket.RawAny[i].Uri, str);
                    if(i + 1 < rawPacket.RawAny.Count)
                    {
                        sb.Append(",");
                    }
                }
                return sb.ToString();
            }
            else
            {
                return mFormatter.Format(msg);
            }
        }

        IProtobufCodec IProtobufCodec.Clone()
        {
            return new ProtobufCodec();
        }


        private void _Expand(long sizeNeeded)
        {
            System.Array.Resize(ref mUTF8Buffer, (int)sizeNeeded * 3/2);
        }
    }
}