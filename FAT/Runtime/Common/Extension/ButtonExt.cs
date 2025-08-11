/*
 * @Author: qun.chao
 * @Date: 2024-02-02 15:21:04
 */
using UnityEngine;
using UnityEngine.UI;

public static class ButtonExt
{
    public static Button WithClickScale(this Button btn)
    {
        TryAddClickScale(btn);
        return btn;
    }

    public static FAT.MapButton WithClickScale(this FAT.MapButton btn)
    {
        TryAddClickScale(btn);
        return btn;
    }

    public static T FixPivot<T>(this T mb) where T : MonoBehaviour
    {
        if (mb.transform is RectTransform trans)
        {
            trans.anchoredPosition += trans.rect.center;
            trans.pivot = Vector2.one * 0.5f;
        }
        return mb;
    }


    #region 点击反馈

    /*
        按钮点击反馈统一
        1. 点击反馈不应使用变色效果(Tint)
        2. 点击反馈按需添加, 通过ButtonScaleFAT脚本实现. 参数 enter 1 / down 0.96 / duration 0.05
    */

    public static void TryAddClickScale(FAT.MapButton mb)
    {
        var btnScale = mb.GetComponent<uTools.ButtonScaleFAT>();
        if (btnScale != null)
            return;
        btnScale = mb.gameObject.AddComponent<uTools.ButtonScaleFAT>();
        btnScale.scaleNormal = 1f;
        btnScale.scaleDown = 0.96f;
        btnScale.duration = 0.05f;
    }

    public static void TryAddClickScale(Button btn, bool force = false)
    {
        if (btn.transition != Selectable.Transition.ColorTint && !force)
            return;
        var btnScale = btn.GetComponent<uTools.ButtonScaleFAT>();
        if (btnScale != null && !force)
            return;
        btn.transition = Selectable.Transition.None;
        if (btnScale == null)
            btnScale = btn.gameObject.AddComponent<uTools.ButtonScaleFAT>();
        btnScale.scaleNormal = 1f;
        btnScale.scaleDown = 0.96f;
        btnScale.duration = 0.05f;
    }

    #endregion
}