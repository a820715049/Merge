/**
 * @Author: zhangpengjian
 * @Date: 2024-3-20 12:30:23
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/21 18:39:17
 * Description: 订单额外奖励活动入口
 */

using System;
using EL;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderExtraEntry : MonoBehaviour, IActivityBoardEntry
    {
        [SerializeField] private TMP_Text cd;
        [SerializeField] private TMP_Text entryName;
        [SerializeField] private GameObject group;
        [SerializeField] private LayoutElement element;
        [SerializeField] private UIImageRes image;
        [SerializeField] private UIImageRes icon;
        
        private Action WhenCD;
        private ActivityExtraRewardOrder orderExtra;

        private void OnEnable()
        {
            WhenCD ??= RefreshCD;
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().AddListener(WhenCD);
            var button = group.GetComponent<Button>().WithClickScale().FixPivot();;
            button.onClick.AddListener(EntryClick);
        }

        private void OnDisable()
        {
            var button = group.GetComponent<Button>();;
            button.onClick.RemoveListener(EntryClick);
            MessageCenter.Get<MSG.GAME_ONE_SECOND_DRIVER>().RemoveListener(WhenCD);
        }

        public void RefreshEntry(ActivityLike activity)
        {
            if (activity == null)
            {
                Visible(false);     
                return;
            }
            if (activity is not ActivityExtraRewardOrder)
                return;
            orderExtra = (ActivityExtraRewardOrder)activity;
            var valid = orderExtra is { Valid: true, IsUnlock: true, IsShowEntry: true };
            Visible(valid);
            if (!valid) return;
            var visualEntry = orderExtra.VisualEntry;
            visualEntry.Refresh(image, "bgImage");
            if (icon != null)
            {
                visualEntry.Refresh(icon, "titleImage");
            }
            visualEntry.Refresh(entryName, "mainTitle");
            visualEntry.RefreshStyle(cd, "time");
            //刷新倒计时
            RefreshCD();
        }
        
        private void RefreshCD() 
        {
            if (!group.activeSelf)
                return;
            var v = orderExtra.Countdown;
            UIUtility.CountDownFormat(cd, v);
            if(v <= 0)
                Visible(false);
        }
        
        private void Visible(bool v_)
        {
            group.SetActive(v_);
            element.ignoreLayout = !v_;
        }
        
        private void EntryClick() {
            // UIManager.Instance.OpenWindow(UIConfig.UIOrderExtra, orderExtra);
            orderExtra.Open();
        }
    }
}
