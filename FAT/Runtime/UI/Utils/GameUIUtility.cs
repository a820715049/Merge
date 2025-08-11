/**
 * @Author: handong.liu
 * @Date: 2020-09-03 18:33:13
 */
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using EL;


namespace FAT
{
    public static class GameUIUtility
    {
        public static void SetFrozenItem(Graphic target)
        {
            target.material = CommonRes.Instance.frozenItemMat;
        }
        public static void SetGrayShader(Graphic target)
        {
            target.material = CommonRes.Instance.grayImageMat;
        }
        public static void SetGrayAlphaShader(Graphic target)
        {
            target.material = CommonRes.Instance.grayWithAlphaImageMat;
        }
        public static void SetMonoShader(Image target)
        {
            target.material = CommonRes.Instance.monoImageMat;
        }
        public static void SetMonoShader(Graphic target)
        {
            target.material = CommonRes.Instance.monoImageMat;
        }
        public static void SetCommonMonoShader(Graphic target)
        {
            target.material = CommonRes.Instance.monoImageMatCommon;
        }
        public static void SetDefaultShader(Graphic target)
        {
            target.material = target.defaultMaterial;
        }
        public static void SetButtonEnable(Button button, bool enabled)
        {
            button.enabled = enabled;
            var gray = button.transform.Find("gray");
            if (gray != null)
            {
                gray.gameObject.SetActive(!enabled);
            }
        }

        public static T Access<T>(this Component root_, string path_, bool try_ = false)
            where T : Component {
            Access(root_, path_, out T r, try_:try_);
            return r;
        }
        public static bool Access<T>(this Component root_, string path_, out T r_, bool try_ = false)
            where T : Component {
            r_ = default;
            if (root_ == null) {
                if (!try_) DebugEx.Error($"try access null transform");
                return false;
            }
            var t = root_.transform;
            var rr = t.Find(path_);
            if (rr == null) {
                if (!try_) DebugEx.Error($"path {path_} not found on transform {t.name}({t.root.name})");
                return false;
            }
            var e = rr.TryGetComponent(out r_);
            if (!e && !try_) DebugEx.Error($"expected component {typeof(T).Name} not found on {t.name}/{path_}({t.root.name})");
            return e;
        }

        public static T Access<T>(this Component root_, bool try_ = false)
            where T : Component {
            Access(root_, out T r, try_:try_);
            return r;
        }
        public static bool Access<T>(this Component root_, out T r_, bool try_ = false)
            where T : Component {
                r_ = default;
            if (root_ == null) {
                if (!try_) DebugEx.Error($"try access null transform");
                return false;
            }
            var t = root_.transform;
            var e = t.TryGetComponent(out r_);
            if (!e && !try_) DebugEx.Error($"expected component {typeof(T).Name} not found on {t.name}({t.root.name})");
            return e;
        }

        public static GameObject TryFind(this Component root_, string path_) {
            var rr = root_.transform.Find(path_);
            if (rr == null) return null;
            return rr.gameObject;
        }
    }
}