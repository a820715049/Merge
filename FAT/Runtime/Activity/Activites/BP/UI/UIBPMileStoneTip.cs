// ==================================================
// // File: UIBPScrollTip.cs
// // Author: liyueran
// // Date: 2025-06-19 15:06:19
// // Desc: BP 里程碑点击Tip
// // ==================================================

using System.Collections.Generic;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EventType = fat.rawdata.EventType;

namespace FAT
{
    public class UIBPMileStoneTip : UITipsBase
    {
        private UITextState _contentState;
        private TextMeshProUGUI _content;
        private bool _isFreeTip;
        private int id;

        private UIImageState bgState;
        private UIImageState upState;
        private UIImageState downState;

        private string claimKey = "#SysComDesc1354"; // 你已领取过该奖励
        private string unAchieveKey = "#SysComDesc1353"; // 完成任务并升级以获得该奖励！
        private string buyNormalKey = "#SysComDesc1355"; // 购买黄金通行证解锁奖励！


        protected override void OnCreate()
        {
            transform.Access("Panel/Content", out _contentState);
            transform.Access("Panel/Content", out _content);
            transform.Access("Panel/Bg", out bgState);
            transform.Access("Panel/Bg/Arrow", out downState);
            transform.Access("Panel/Bg/ArrowUp", out upState);
        }

        protected override void OnParse(params object[] items)
        {
            if (items.Length < 2)
            {
                return;
            }

            // items[0] Vector3 位置
            // items[1] float 偏移参数
            _SetTipsPosInfo(items);

            _isFreeTip = (bool)items[2];
            id = (int)items[3];
        }

        protected override void OnPreOpen()
        {
            // 刷新tips位置
            _RefreshTipsPos(18);
            _contentState.Select(_isFreeTip ? 0 : 1);
            bgState.Select(_isFreeTip ? 0 : 1);
            downState.Select(_isFreeTip ? 0 : 1);
            upState.Select(_isFreeTip ? 0 : 1);

            if (Game.Manager.activity.LookupAny(EventType.Bp, out var act) && act is BPActivity _activity)
            {
                var (normalClaim, luxuryClaim, _) = _activity.RewardClaimStateDict[id];

                if ((normalClaim && _isFreeTip) || (luxuryClaim && !_isFreeTip))
                {
                    // 已经领取
                    _content.SetText(I18N.Text(claimKey));
                }
                else
                {
                    _content.SetText(I18N.Text(unAchieveKey));
                }

                if (!_isFreeTip && _activity.PurchaseState == BPActivity.BPPurchaseState.Free)
                {
                    _content.SetText(I18N.Text(buyNormalKey));
                }
            }
        }


        protected override void OnPostClose()
        {
        }
    }
}