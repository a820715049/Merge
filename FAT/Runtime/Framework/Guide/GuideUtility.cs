/*
 * @Author: qun.chao
 * @Date: 2023-11-22 15:59:27
 */
using UnityEngine;
using FAT.Merge;

namespace FAT
{
    public static class GuideUtility
    {
        #region board item event
        public static void OnItemMerge(Item src, Item dst, Item result)
        {
            Game.Manager.guideMan.OnItemMerge?.Invoke(src, dst, result);
        }
        public static void OnItemConsume(Item src, Item dst)
        {
            Game.Manager.guideMan.OnItemConsume?.Invoke(src, dst);
        }

        public static void OnItemPutIntoInventory(Item item)
        {
            Game.Manager.guideMan.OnItemPutIntoInventory?.Invoke(item);
        }
        #endregion

        public static void TriggerGuide()
        {
            _TriggerGuide();
        }

        public static void OnExpChange(int change)
        {
            _TriggerGuide();
        }

        public static void OnMergeLevelChange()
        {
            _TriggerGuide();
        }

        public static void OnMainOrderFinished()
        {
            _TriggerGuide();
        }

        private static void _TriggerGuide()
        {
            Game.Manager.guideMan.TriggerGuide();
        }

        public static Vector2 CalcRectTransformScreenPos(Camera cam, Transform trans)
        {
            if (trans is RectTransform rectTrans)
            {
                var wp = rectTrans.TransformPoint(rectTrans.rect.center);
                return RectTransformUtility.WorldToScreenPoint(cam, wp);
            }
            else
            {
                return RectTransformUtility.WorldToScreenPoint(cam, trans.position);
            }
        }

        public static (float w, float h) CalcRectTransformScreenSize(Camera cam, RectTransform rectTrans)
        {
            var min = rectTrans.TransformPoint(rectTrans.rect.min);
            var max = rectTrans.TransformPoint(rectTrans.rect.max);
            var sp_min = RectTransformUtility.WorldToScreenPoint(cam, min);
            var sp_max = RectTransformUtility.WorldToScreenPoint(cam, max);
            return (sp_max.x - sp_min.x, sp_max.y - sp_min.y);
        }

        public static bool CheckIsAncestor(Transform ancestor, Transform descendant)
        {
            var p = descendant;
            if (p == null || ancestor == null)
                return false;
            do
            {
                if (p == ancestor)
                    return true;
                p = p.parent;
            }
            while (p != null);
            return false;
        }

        public static bool IsBoardReady(UIResource res)
        {
            var uiMan = UIManager.Instance;
            var root = uiMan.GetLayerRootByType(UILayer.AboveStatus);
            return uiMan.IsOpen(res) && !uiMan.IsPause(res) && root.childCount < 1;
        }
    }
}