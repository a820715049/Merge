//#define LOG_ACCESS
/**
 * @Author: handong.liu
 * @Date: 2020-10-22 15:50:35
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EL {
    using static PoolMapping;

    public interface IObjectPool {
        void Free(object item);
    }

    public partial class PoolMapping : IObjectPool {
        public readonly struct Auto : IDisposable {
            internal readonly IObjectPool target;
            public readonly object obj;

            public Auto(IObjectPool pool_, object obj_) {
                obj = obj_;
                target = pool_;
            }

            void IDisposable.Dispose() {
                target.Free(obj);
            }

            public T Access<T>() where T: class => obj as T;
        }

        public readonly struct Ref {
            internal readonly IObjectPool target;
            public readonly object obj;
            public bool Valid => obj != null;

            public Ref(IObjectPool pool_, object obj_) {
                obj = obj_;
                target = pool_;
            }

            public void Free() => target.Free(obj);

            public T Access<T>() where T: class => obj as T;
            public Auto ToAuto() => new(target, obj);
        }

        public readonly struct Ref<T> {
            internal readonly IObjectPool target;
            public readonly T obj;
            public bool Valid => obj != null;

            public Ref(IObjectPool pool_, T obj_) {
                obj = obj_;
                target = pool_;
            }

            public void Free() => target.Free(obj);

            public Auto ToAuto() => new(target, obj);
        }
    }

    public partial class PoolMapping : IObjectPool {
        private static PoolMapping _instance;
        public static PoolMapping PoolMappingAccess => _instance ??= new();
        public static readonly Dictionary<Type, ObjectPool> map = new();

        public Auto Borrow<T>(out T o_) => Access(typeof(T)).Borrow(out o_);
        public Auto Borrow(Type type_) => Access(type_).Borrow();
        public Ref<T> Take<T>(out T o_) => Access(typeof(T)).Take(out o_);
        public Ref<T> Take<T>() => Access(typeof(T)).Take<T>();
        public Ref Take(Type type_) => Access(type_).Take();

        public ObjectPool Access(Type type_) {
            if (!map.TryGetValue(type_, out var list)) {
                list = new(type_);
                map[type_] = list;
            }
            return list;
        }

        internal object Next(Type type_) => Access(type_).Next();

        public void Free(object obj_) {
            var type = obj_.GetType();
            if (!map.TryGetValue(type, out var list)) {
                DebugEx.Warning($"returning untracked object of type:{type}, will ignore");
                return;
            }
            list.Free(obj_);
        }
    }

    public class ObjectPool : IObjectPool {
        public readonly Type type;
        public readonly Stack<object> list = new();
        public Func<object> create;
        public Action<object> clear;

        public Auto Borrow<T>(out T o_) => new(this, o_ = (T)Next());
        public Auto Borrow() => new(this, Next());
        public Ref<T> Take<T>(out T o_) => new(this, o_ = (T)Next());
        public Ref<T> Take<T>() => new(this, (T)Next());
        public Ref Take() => new(this, Next());

        public ObjectPool(Type type_) => type = type_;

        internal object Next() {
            if (list.Count > 0) {
                #if LOG_ACCESS
                DebugEx.Info($"pool object borrow {type}");
                #endif
                return list.Pop();
            }
            var n = Create();
            #if LOG_ACCESS
            DebugEx.Info($"pool object create {type}");
            #endif
            return n;
        }

        private object Create() => create?.Invoke() ?? Activator.CreateInstance(type);

        public void Free(object obj_) {
            var t = obj_.GetType();
            if (t != type) {
                DebugEx.Warning($"returning object of mismatched type:{t} to pool of type:{type}, will ignore");
                return;
            }
            if (list.Contains(obj_)) {
                DebugEx.Error($"returning object exists, will ignore");
                return;
            }
            #if LOG_ACCESS
            DebugEx.Info($"pool object return {t}");
            #endif
            TryClear(obj_);
            list.Push(obj_);
        }

        public void TryClear(object obj_) {
            if (obj_.GetType() != type) return;
            if (clear != null) {
                clear(obj_);
                return;
            }
            ClearAny(obj_);
        }

        public static void ClearAny(object obj_) {
            switch(obj_) {
                case IList list: list.Clear(); return;
                case IDictionary dict: dict.Clear(); return;
                case ISet<int> set: set.Clear(); return;
                case ISet<string> set: set.Clear(); return;
                case StringBuilder builder: builder.Clear(); return;
                case MemoryStream mem: mem.SetLength(0); return;
                case DeterministicRandom: return;
            }
            DebugEx.Warning($"pool object not cleared type:{obj_.GetType()}");
        }

        public void Reserve(int n_) {
            for (var k = 0; k < n_; ++k) {
                list.Push(Create());
            }
            #if LOG_ACCESS
            DebugEx.Info($"pool object create {n_} {type}");
            #endif
        }
    }
    
    public class ObjectPool<T> : IObjectPool where T : class, new()
    {
        public static ObjectPool<T> GlobalPool = new();
        
        public Auto AllocStub(out T ret)
        {
            ret = (T)PoolMappingAccess.Next(typeof(T));
            return new(PoolMappingAccess, ret);
        }

        public T Alloc()
        {
            return (T)PoolMappingAccess.Next(typeof(T));
        }

        public void Free(object elem)
        {
            PoolMappingAccess.Free(elem);
        }
    }
}