/**
 * @Author: zhangpengjian
 * @Date: 2024/8/24 11:41:19
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2024/8/24 11:41:19
 * Description: 积分活动教学界面
 */

using System;
using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UIScoreGuide : UIBase
    {
        [SerializeField] private TMP_Text helpTip1;
        [SerializeField] private TMP_Text helpTip2;
        [SerializeField] private TMP_Text helpTip3;

        protected override void OnCreate()
        {
            transform.AddButton("Content/BtnClose", OnClose).FixPivot();
        }

        protected override void OnPreOpen()
        {
            transform.GetComponent<Animator>().SetTrigger("Show");
            RefreshTip();
        }

        private void RefreshTip()
        {
            Game.Manager.activity.LookupAny(fat.rawdata.EventType.Score, out var activity);
            var activityScore = (ActivityScore)activity;
            activityScore.Visual.Theme.AssetInfo.TryGetValue("tmpIcon1", out var icon1);
            activityScore.Visual.Theme.AssetInfo.TryGetValue("tmpIcon2", out var icon2);
            var sprite1 = "<sprite name=\"" + icon1.ConvertToAssetConfig().Asset + "\">";
            var sprite2 = "<sprite name=\"" + icon2.ConvertToAssetConfig().Asset + "\">";
            helpTip1.text = I18N.FormatText("#SysComDesc535", sprite1);
            helpTip2.text = I18N.FormatText("#SysComDesc536", sprite2);
            helpTip3.text = I18N.FormatText("#SysComDesc537", sprite2);
        }

        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}
