/*
 * @Author: tang.yan
 * @Description: 商店物品cell结构
 * @Date: 2023-11-07 20:11:09
 */
using System;
using EL;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using System.Collections.Generic;
using TMPro;

namespace FAT
{
    [Serializable]
    public class UIShopChessCell
    {
        //背景图
        [SerializeField] public GameObject bgNormalGo;
        [SerializeField] public GameObject bgHighlightGo;
        //棋子
        [SerializeField] public GameObject chessGo;
        [SerializeField] public UIImageState chessFrameBg;  //棋子icon背景Frame 只有底部的棋子才会显示
        [SerializeField] public UIImageRes chessIcon;
        [SerializeField] public TMP_Text chessStock; //库存
        [SerializeField] public TMP_Text chessStockHighlight; //库存
        [SerializeField] public TMP_Text chessName;
        //通用
        [SerializeField] public Button tipsBtn;
        [SerializeField] public Button buyBtn;
        [SerializeField] public GameObject normalGo;
        [SerializeField] public TMP_Text normalPrice;
        [SerializeField] public GameObject soldOutGo;
    }
    
    [Serializable]
    public class UIShopEnergyCell
    {
        //能量
        [SerializeField] public UIImageRes energyIcon;
        [SerializeField] public TMP_Text energyNum; //数量
        [SerializeField] public TMP_Text energyName;
        //通用
        [SerializeField] public Button buyBtn;
        [SerializeField] public TMP_Text normalPrice;
    }
}
