// ===================================================
// Author: mengqc
// Date: 2025/09/15
// ===================================================

using System.Collections.Generic;
using System.Linq;
using Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIVineLeapChoiceTip : UITipsBase
    {
        public RectTransform layoutGroup;
        public TMP_Text title;
        private RewardConfig[] reward;
        private float _baseOffset = 18f; //竖直方向固定偏移(下方箭头宽度的一半)
        private float _cellWidth = 152f; //面板中cell的宽度
        private float _cellHeight = 152f; //面板中cell的宽度
        private bool isShowTitle;

        protected override void OnCreate()
        {
            UIUtility.CommonItemSetup(layoutGroup);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 3)
            {
                //设置tips位置参数
                _SetTipsPosInfo(items);
                //设置界面自定义参数
                reward = (RewardConfig[])items[2];
                if (items.Length == 4)
                {
                    isShowTitle = (bool)items[3];
                }
                else
                {
                    isShowTitle = false;
                }
            }
        }

        protected override void OnPreOpen()
        {
            base.OnPreOpen();
            _RefreshShowReward();
            //刷新tips位置
            _RefreshTipsPos(_baseOffset);
            title.gameObject.SetActive(isShowTitle);
        }

        protected override void OnPostClose()
        {
            base.OnPreClose();
        }

        private void _RefreshShowReward()
        {
            UIUtility.CommonItemRefresh(layoutGroup, reward.ToList());
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
            _SetCurTipsWidth(layoutGroup.rect.width);
            _SetCurTipsHeight(layoutGroup.rect.height);
        }
    }
}