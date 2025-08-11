/*
 * @Author: qun.chao
 * @Date: 2022-09-29 16:44:17
 */
using System;
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public class UIImageResHelper
    {
        public static int cullingMask { get; set; }

        public enum Tag
        {
            None,
            BoardItem = 0x1,
        }

        public static void BindFallback(UIImageRes res, int id, Tag tag = Tag.None)
        {
#if UNITY_EDITOR
            res.OnImageLoadFail = _res => _OnFallback(_res, id, (int)tag);
#endif
        }

        private static void _OnFallback(UIImageRes res, int id, int tag)
        {
            res.OnImageLoadFail = null;
#if UNITY_EDITOR
            UIImageResEditorFallback.Apply(res, id, tag);
#endif
        }
    }

    public class UIImageResEditorFallback : MonoBehaviour
    {
        public static void Apply(UIImageRes res, int id, int tag)
        {
            if (res != null)
            {
                var fb = res.GetComponent<UIImageResEditorFallback>();
                if (fb == null)
                    fb = res.gameObject.AddComponent<UIImageResEditorFallback>();
                fb.Refresh(id, tag);
            }
        }

        private static Color[] colorCollection = { Color.black, Color.blue, Color.cyan, Color.gray, Color.green, Color.magenta, Color.red, Color.white, Color.yellow };
        private static Color _PickColor(int id)
        {
            var idx = id % colorCollection.Length;
            return colorCollection[idx];
        }

        private static GUIStyle customStyle = null;
        private static void _EnsureStyle()
        {
            if (customStyle == null)
            {
                customStyle = new GUIStyle(GUI.skin.box);
                customStyle.fontSize = 28;
                customStyle.fontStyle = FontStyle.Bold;
                customStyle.alignment = TextAnchor.MiddleCenter;
                customStyle.normal.background = Texture2D.whiteTexture;
                customStyle.normal.textColor = Color.white;
            }
        }

        private static Rect _RectTransformToScreenRect(RectTransform trans)
        {
            Vector2 size = Vector2.Scale(trans.rect.size, trans.lossyScale);
            return new Rect(trans.position.x - size.x * 0.5f, Screen.height - (trans.position.y + size.y * 0.5f), size.x, size.y);
        }

        private int itemId;
        private int tag;

        public void Refresh(int id, int _tag)
        {
            itemId = id;
            tag = _tag;
        }

        private void OnDisable()
        {
            Destroy(this);
        }

        private void OnGUI()
        {
            _EnsureStyle();

            if ((UIImageResHelper.cullingMask & tag) != 0)
                return;

            if (Game.Manager.objectMan.IsType(itemId, ObjConfigType.MergeItem))
            {
                _ShowMergeItem(itemId);
            }
        }

        private void _ShowMergeItem(int itemId)
        {
            var cat = Env.Instance.GetCategoryByItem(itemId);
            if (cat == null)
                return;
            var lv = cat.Progress.IndexOf(itemId) + 1;
            var ori = GUI.backgroundColor;
            GUI.skin.box.wordWrap = true;
            GUI.backgroundColor = _PickColor(cat.Id) * .75f;
            GUI.Box(_RectTransformToScreenRect(transform as RectTransform), $"{EL.I18N.Text(cat.Name)}{lv}", customStyle);
            GUI.backgroundColor = ori;
        }
    }
}