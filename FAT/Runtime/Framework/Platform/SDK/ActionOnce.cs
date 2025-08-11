using System;

namespace FAT {
    public class ActionOnce {
        public Action action;
        public bool Valid => action != null;

        public void Setup(Action action_) => action = action_;

        public void Invoke() {
            action?.Invoke();
            action = null;
        }

        public void Cancel() => action = null;
    }

    public class ActionOnce<T1, T2> {
        public Action<T1, T2> action;
        public bool Valid => action != null;

        public void Setup(Action<T1, T2> action_) => action = action_;

        public void Invoke(T1 t1, T2 t2) {
            action?.Invoke(t1, t2);
            action = null;
        }

        public void Cancel() => action = null;
    }

    public class ActionOnce<T1, T2, T3> {
        public Action<T1, T2, T3> action;
        public bool Valid => action != null;

        public void Setup(Action<T1, T2, T3> action_) => action = action_;

        public void Invoke(T1 t1, T2 t2, T3 t3) {
            action?.Invoke(t1, t2, t3);
            action = null;
        }

        public void Cancel() => action = null;
    }
}