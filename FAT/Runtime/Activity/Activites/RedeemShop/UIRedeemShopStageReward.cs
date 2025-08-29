/*
 * @Author: yanfuxing
 * @Date: 2025-05-08 11:25:10
 */
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    /// <summary>
    /// 兑换商店阶段奖励
    /// </summary>
    public class UIRedeemShopStageReward : UIBase
    {
        [SerializeField] private TextMeshProUGUI _StageNumText; //奖励数量
        //当前轮次阶段数
        private int _curStageIndex;
        protected override void OnCreate()
        {
            base.OnCreate();
            transform.AddButton("Mask", OnCloseBtnClick);
        }

        protected override void OnParse(params object[] items)
        {
            base.OnParse(items);
            if (items.Length > 0)
            {
                _curStageIndex = (int)items[1];
            }
        }
        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _StageNumText.text = _curStageIndex.ToString();
            ActivityRedeemShopLike.PlaySound(AudioEffect.RedeemComplete);
        }
        protected override void OnAddListener()
        {
            base.OnAddListener();
        }

        private void OnCloseBtnClick()
        {
            Close();
        }
    }

}

