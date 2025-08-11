/*
 * @Author: tang.yan
 * @Description: 弹珠游戏中的飘字cell 
 * @Date: 2024-12-18 14:12:30
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
    public class UIPachinkoPopFlyCell : MonoBehaviour
    {
        [SerializeField] private RectTransform posTrans;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private UIImageRes rewardIcon;
        [SerializeField] private TMP_Text rewardNum;

        public void ShowFlyTips(int flyIconId, string flyNum)
        {
            var conf = Game.Manager.objectMan.GetBasicConfig(flyIconId);
            if (conf == null) return;
            //刷新icon
            rewardIcon.SetImage(conf.Icon);
            rewardNum.text = flyNum;
        }

        public void SetVisible(bool isVisible)
        {
            canvasGroup.alpha = isVisible ? 1 : 0;
        }

        public void RefreshTipsPos(Vector3 showPos)
        {
            //获取当前tips宽度
            var curTipsWidth = 110;
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