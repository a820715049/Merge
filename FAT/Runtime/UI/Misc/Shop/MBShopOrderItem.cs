/*
 * @Author: tang.yan
 * @Description: 商店棋子页面售卖订单棋子对应的MonoBehaviour
 * @Date: 2025-09-19 17:09:00
 */

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EL;

namespace FAT
{
    public class MBShopOrderItem : MonoBehaviour
    {
        //背景图
        [SerializeField] private GameObject bgNormalGo;
        [SerializeField] private GameObject bgHighlightGo;
        //棋子
        [SerializeField] private GameObject chessGo;
        [SerializeField] private UIImageState chessFrameBg;  //棋子icon背景Frame 只有底部的棋子才会显示
        [SerializeField] private UIImageRes chessIcon;
        [SerializeField] private TMP_Text chessStock; //库存
        [SerializeField] private TMP_Text chessStockHighlight; //库存
        [SerializeField] private TMP_Text chessName;
        //通用
        [SerializeField] private Button tipsBtn;
        [SerializeField] private Button buyBtn;
        [SerializeField] private GameObject normalGo;
        [SerializeField] private TMP_Text normalPrice;
        [SerializeField] private GameObject soldOutGo;
        //活动挂token相关
        [SerializeField] private UIImageRes scoreMicIcon;   //左下角-积分活动(麦克风版)

        private ShopChessOrderData _chessOrderData;
        
        public void Setup()
        {
            buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnOrderChessBtnClick); 
            tipsBtn.WithClickScale().FixPivot().onClick.AddListener(_OnOrderChessTipsBtnClick);
        }

        public void SetVisible(bool isVisible)
        {
            chessGo.SetActive(isVisible);
        }
        
        public void Refresh(ShopChessOrderData chessOrderData)
        {
            _chessOrderData = chessOrderData;
            if (_chessOrderData == null)
                return;
            bool isNeedInOrder = chessOrderData.CheckIsNeedInOrder();
            bgHighlightGo.SetActive(false);
            bgNormalGo.SetActive(true);
            var image = chessOrderData.GetCurSellGoodsImage();
            if (image != null)
            {
                chessIcon.SetImage(image.Group, image.Asset);
            }
            chessName.text = chessOrderData.GetCurSellGoodsName();
            chessStock.text = I18N.FormatText("#SysComDesc72", chessOrderData.GetStockNum());
            tipsBtn.gameObject.SetActive(true);
            if (!chessOrderData.CheckCanBuy())
            {
                buyBtn.interactable = false;
                GameUIUtility.SetGrayShader(buyBtn.image);
                normalGo.SetActive(false);
                soldOutGo.SetActive(true);
                chessFrameBg.Setup(1);
            }
            else
            {
                buyBtn.interactable = true;
                GameUIUtility.SetDefaultShader(buyBtn.image);
                normalGo.SetActive(true);
                soldOutGo.SetActive(false);
                normalPrice.text = chessOrderData.CurSellGoodsPrice.ToString();
                chessFrameBg.Setup(!isNeedInOrder ? 1 : 2);
            }
            //刷新ScoreMic相关UI
            _RefreshScoreMic();
        }
    
        //持有麦克风活动实例 避免频繁查找
        private ActivityScoreMic _activityScoreMic;
        private void _RefreshScoreMic()
        {
            scoreMicIcon.gameObject.SetActive(false);
            if (_chessOrderData == null)
                return;
            if (_activityScoreMic == null || !_activityScoreMic.Active)
            {
                Game.Manager.activity.LookupAny(fat.rawdata.EventType.MicMilestone, out var activity);
                if (activity == null || !(activity is ActivityScoreMic _activity))
                {
                    return;
                }
                _activityScoreMic = _activity;
            }
           
            var tokenId = _activityScoreMic.GetTokenIdForShopItem(_chessOrderData);
            var cfg = Game.Manager.objectMan.GetBasicConfig(tokenId);
            if (cfg != null)
            {
                scoreMicIcon.gameObject.SetActive(true);
                var isMulti = _activityScoreMic.CheckMainBoardTokenMultiRate(tokenId, out _);
                //非翻倍时读小图Image，翻倍时借用BlackIcon字段
                var image = !isMulti ? cfg.Image : cfg.BlackIcon;
                scoreMicIcon.SetImage(image);
            }
        }

        private void _OnOrderChessBtnClick()
        {
            if (_chessOrderData == null)
                return;
            var trans = chessIcon.transform as RectTransform;
            var from = trans != null ? (trans.position - new Vector3(0, trans.sizeDelta.y / 2, 0)) : chessIcon.transform.position;
            Game.Manager.shopMan.TryBuyShopChessOrderGoods(_chessOrderData, from, 196f, () =>
            {
                Game.Manager.shopMan.TryCollectActivityToken(_chessOrderData, scoreMicIcon.transform.position);
            });
        }
        
        private void _OnOrderChessTipsBtnClick()
        {
            if (_chessOrderData == null)
                return;
            UIItemUtility.ShowItemPanelInfo(_chessOrderData.CurSellGoodsId);
        }
    }
}