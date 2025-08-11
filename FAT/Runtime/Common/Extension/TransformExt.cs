/**
 * @Author: handong.liu
 * @Date: 2020-07-10 15:57:18
 */
using UnityEngine;
using UnityEngine.UI;
using FAT;

namespace EL
{
    public static class TransformExt
    {   
        public static T FindEx<T>(this Transform target, string path) where T : Component
            => GameUIUtility.Access<T>(target, path);
        private static System.Collections.Generic.Queue<Transform> sCachedTransformQueue = new System.Collections.Generic.Queue<Transform>();
        public static Transform FindInChild(this Transform target, string name)
        {
            sCachedTransformQueue.Clear();
            sCachedTransformQueue.Enqueue(target);
            while(sCachedTransformQueue.Count > 0)
            {
                var t = sCachedTransformQueue.Dequeue();
                int childCount = t.childCount;
                for(int i = 0; i < childCount; i++)
                {
                    var c = t.GetChild(i);
                    if(c.name == name)
                    {
                        return c;
                    }
                    else
                    {
                        sCachedTransformQueue.Enqueue(c);
                    }
                }
            }
            return null;
        }
        public static bool FindEx<T>(this Transform target, string path, out T com) where T : Component
            => GameUIUtility.Access(target, path, out com);
        public static bool FindEx(this Transform target, string path, out GameObject com)
            => (com = GameUIUtility.TryFind(target, path)) != null;
        public static bool FindEx(this Transform target, string path, out Transform com)
            => GameUIUtility.Access(target, path, out com);

        public static Button AddButton(this Transform target, string path, UnityEngine.Events.UnityAction callback)
        {
            var t = target;
            if (!string.IsNullOrEmpty(path))
            {
                t = target.Find(path);
            }

            if (t != null)
            {
                var btn = t.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(callback);
                    ButtonExt.TryAddClickScale(btn);
                    return btn;
                }
            }
            else
            {
                Debug.LogWarning($"button path null : {target.name} - {path}");
            }
            return null;
        }

        public static T GetComponentInParentEx<T>(this Transform target) where T : Component
        {
            target.TryGetComponent<T>(out var mb);
            while (mb == null && target.parent != null)
            {
                target = target.parent;
                target.TryGetComponent(out mb);
            }
            return mb;
        }

        public static string GetPath(this Transform target, System.Text.StringBuilder sb = null)
        {
            sb ??= new();
            if(target.parent != null)
            {
                GetPath(target.parent, sb);
                sb.Append("/");
            }
            sb.Append(target.name);
            return null;
        }

        public static void DestroyAllChildren(this Transform target)
        {
            var count = target.childCount;
            for(var i = count - 1; i >= 0; --i)
            {
                GameObject.Destroy(target.GetChild(i).gameObject);
            }
        }
    }
}