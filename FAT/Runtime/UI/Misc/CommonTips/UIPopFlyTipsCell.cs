/*
 * @Author: tang.yan
 * @Description: UI飘字提示Cell
 * @Date: 2023-12-08 18:12:24
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using EL;

namespace FAT
{
    public class UIPopFlyTipsCell : MonoBehaviour
    {
        [Serializable]
        private class ToastType
        {
            [SerializeField] public GameObject toastType;
            [SerializeField] public TextProOnACircle curveText;
            [SerializeField] public TMP_Text standardText;
            [SerializeField] public GameObject maxGo;
        }
        
        [SerializeField] private RectTransform posTrans;
        [SerializeField] private ToastType toastCurve;      //文本弯曲样式
        [SerializeField] private ToastType toastStandard;   //文本水平样式
        [SerializeField] private CanvasGroup canvasGroup;
        private ToastType _curToast;   //当前tips
        //策划配置与文本描边颜色样式配置的对应
        private Dictionary<int, (int, string)> _fontStyleMap = new Dictionary<int, (int, string)>()
        {
            [1] = (0, ""),    //横幅样式 不在此使用
            [2] = (17, ""),   //白色样式 对应 Font_MaterialRes index = 17
            [3] = (9, "#89f0ff"),    //蓝色样式 对应 Font_MaterialRes index = 9
            [4] = (33, ""),   //紫色样式 对应 Font_MaterialRes index = 33
            [5] = (32, ""),   //黄色样式 对应 Font_MaterialRes index = 32
        };

        public void ShowFlyTips1(string info, int type, int style)
        {
            switch (type)
            {
                case 2: _curToast = toastCurve; break;
                case 3: _curToast = toastStandard; break;
                default: _curToast = null; return;
            }
            //选择文本样式
            toastCurve.toastType.SetActive(type == 2);
            toastStandard.toastType.SetActive(type == 3);
            //设置文本信息
            TMP_Text textComp;
            switch (type)
            {
                case 2: textComp = _curToast.curveText.m_TextComponent; break;
                case 3: textComp = _curToast.standardText; break;
                default: return;
            }
            textComp.text = info;
            //设置文本颜色材质
            if (_fontStyleMap.TryGetValue(style, out var fontRes))
            {
                var config = FontMaterialRes.Instance.GetFontMatResConf(fontRes.Item1);
                config?.ApplyFontMatResConfig(textComp);
                if (!string.IsNullOrEmpty(fontRes.Item2))
                {
                    if (ColorUtility.TryParseHtmlString(fontRes.Item2, out var col))
                    {
                        textComp.color = col;
                    }
                }
            }
            //5代表Max样式 会额外显示一个icon
            if (_curToast.maxGo != null)
            {
                _curToast.maxGo.SetActive(style == 5);
            }
        }

        public void SetVisible(bool isVisible)
        {
            canvasGroup.alpha = isVisible ? 1 : 0;
        }

        public void ShowFlyTips2(string info)
        {
            if (_curToast == null)
                return;
            if (_curToast.curveText != null)
            {
                _curToast.curveText.SetText(info);
            }
            if (_curToast.standardText != null)
            {
                _curToast.standardText.text = info;
            }
        }
        
        public void RefreshTipsPos(Vector3 showPos)
        {
            //获取当前tips宽度
            var curTipsWidth = _curToast?.toastType.GetComponent<RectTransform>().rect.width ?? 300;
            var localPos = UIManager.Instance.TransWorldPosToLocal(showPos);
            //设置最终位置
            var pos = new Vector3(localPos.x, localPos.y, 0);
            //面板宽度适配
            pos = UIManager.Instance.FitUITipsPosByWidth(pos, curTipsWidth);
            //适配后的值回传给localPosition
            posTrans.localPosition = pos;
        }
    }
}