using System;

namespace EL
{
    public interface IGameMessage {
        void Clear();
    }

    public class MessageBase : IGameMessage
    {
        // protected event Action mMessageHandler;
        protected Action mMessageHandler = null;

        public void AddListener(Action func)
        {
            mMessageHandler += func;
        }

        public void AddListenerUnique(Action func) {
            mMessageHandler -= func;
            mMessageHandler += func;
        }

        public void RemoveListener(Action func)
        {
            if (func == null) return;
            mMessageHandler -= func;
        }

        public void Dispatch()
        {
            mMessageHandler?.Invoke();
        }

        public void Clear() => mMessageHandler = null;
    }

    public class MessageBase<T> : IGameMessage
    {
        protected Action<T> mMessageHandler = null;

        public void AddListener(Action<T> func)
        {
            mMessageHandler += func;
        }

        public void AddListenerUnique(Action<T> func) {
            mMessageHandler -= func;
            mMessageHandler += func;
        }

        public void RemoveListener(Action<T> func)
        {
            if (func == null) return;
            mMessageHandler -= func;
        }

        public void Dispatch(T val)
        {
            mMessageHandler?.Invoke(val);
        }

        public void Clear() => mMessageHandler = null;
    }

    public class MessageBase<T1,T2> : IGameMessage
    {
        protected Action<T1,T2> mMessageHandler = null;

        public void AddListener(Action<T1,T2> func)
        {
            mMessageHandler += func;
        }

        public void AddListenerUnique(Action<T1, T2> func) {
            mMessageHandler -= func;
            mMessageHandler += func;
        }

        public void RemoveListener(Action<T1,T2> func)
        {
            if (func == null) return;
            mMessageHandler -= func;
        }

        public void Dispatch(T1 val1, T2 val2)
        {
            mMessageHandler?.Invoke(val1, val2);
        }

        public void Clear() => mMessageHandler = null;
    }
    
    public class MessageBase<T1,T2,T3> : IGameMessage
    {
        protected Action<T1,T2,T3> mMessageHandler = null;

        public void AddListener(Action<T1,T2,T3> func)
        {
            mMessageHandler += func;
        }

        public void AddListenerUnique(Action<T1, T2, T3> func) {
            mMessageHandler -= func;
            mMessageHandler += func;
        }

        public void RemoveListener(Action<T1,T2,T3> func)
        {
            if (func == null) return;
            mMessageHandler -= func;
        }

        public void Dispatch(T1 val1, T2 val2, T3 val3)
        {
            mMessageHandler?.Invoke(val1, val2, val3);
        }

        public void Clear() => mMessageHandler = null;
    }
}