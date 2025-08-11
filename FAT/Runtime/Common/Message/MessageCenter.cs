/*
 * @Author: qun.chao
 * @Date: 2020-07-27 18:42:43
 */
using System;
using System.Collections.Generic;

namespace EL
{
    public static class MessageCenter
    {
        private static Dictionary<Type, IGameMessage> mMessageDict = new();

        public static T Get<T>() where T : IGameMessage, new()
        {
            var type= typeof(T);
            if (!mMessageDict.TryGetValue(type, out var v))
            {
                v = new T();
                mMessageDict.Add(type, v);
            }
            return (T)v;
        }

        public static IGameMessage Get(Type type_) {
            if (!type_.IsSubclassOf(typeof(IGameMessage))) {
                DebugEx.Error($"invalid message type");
                return null;
            }
            if (!mMessageDict.TryGetValue(type_, out var v)) {
                v = (IGameMessage)Activator.CreateInstance(type_);
                mMessageDict.Add(type_, v);
            }
            return v;
        }

        public static void Clear() {
            mMessageDict.Clear();
        }
    }
}