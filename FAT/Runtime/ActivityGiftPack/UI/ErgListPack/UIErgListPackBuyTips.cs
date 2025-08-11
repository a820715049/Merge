/*
 * @Author: tang.yan
 * @Description: 体力列表礼包购买提示界面 
 * @Date: 2025-04-16 18:04:00
 */
using UnityEngine;
using UnityEngine.UI;
using EL;

namespace FAT
{
    public class UIErgListPackBuyTips : UIBase
    {
        [SerializeField] private Button buyBtn;
        [SerializeField] private UITextState iapPriceState;
        [SerializeField] private GameObject labelGo;
        //性价比标签
        private UIIAPLabel _iapLabel = new();
        
        private PackErgList pack;
        
        protected override void OnCreate()
        {
            transform.AddButton("Content/Panel/BtnClose/Btn", base.Close);
            buyBtn.WithClickScale().FixPivot().onClick.AddListener(_OnClickBtnBuy);
        }

        protected override void OnParse(params object[] items)
        {
            pack = (PackErgList)items[0];
        }

        protected override void OnPreOpen()
        {
            _RefreshPrice();
            var labelId = pack.GetCurDetailConfig()?.Label ?? 0;
            _iapLabel.Setup(labelGo.transform, labelId, pack.PackId);
        }

        protected override void OnAddListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().AddListener(_RefreshPrice);
            MessageCenter.Get<MSG.ACTIVITY_END>().AddListener(_RefreshEnd);
        }

        protected override void OnRemoveListener()
        {
            MessageCenter.Get<MSG.IAP_INIT>().RemoveListener(_RefreshPrice);
            MessageCenter.Get<MSG.ACTIVITY_END>().RemoveListener(_RefreshEnd);
        }

        protected override void OnPostClose()
        {
            _iapLabel.Clear();
        }
        
        private void _RefreshPrice()
        {
            if (pack == null)
                return;
            var iap = Game.Manager.iap;
            var valid = iap.Initialized;
            iapPriceState.Enabled(valid, iap.PriceInfo(pack.Content.IapId));
            if (valid)
            {
                buyBtn.interactable = true;
                GameUIUtility.SetDefaultShader(buyBtn.image);
            }
            else
            {
                buyBtn.interactable = false;
                GameUIUtility.SetGrayShader(buyBtn.image);
            }
        }
        
        private void _OnClickBtnBuy()
        {
            if (pack == null)
                return;
            pack.TryPurchasePack();
            Close();
        }
        
        private void _RefreshEnd(ActivityLike pack_, bool expire_) {
            if (pack_ != pack) return;
            Close();
        }
    }
}