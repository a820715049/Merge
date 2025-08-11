/*
 * @Author: pengjian.zhang
 * @Description: 寻宝教学弹窗
 * @Date: 2024-04-24 20:00:00
 */

using EL;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UITreasureHuntHelpPopup : UIBase
    {
        [SerializeField] private TextMeshProUGUI help1;
        [SerializeField] private TextMeshProUGUI help2;
        [SerializeField] private TextMeshProUGUI helpTitle;

        protected override void OnCreate()
        {
            transform.AddButton("Content/page1/BtnClaim", OnClose).FixPivot();
        }

        protected override void OnPreOpen()
        {
            var actInst = Game.Manager.activity.LookupAny(fat.rawdata.EventType.Treasure) as ActivityTreasure;
            var c = Game.Manager.objectMan.GetTokenConfig(actInst.ConfD.RequireCoinId);
            help2.SetText(I18N.FormatText("#SysComDesc768", c.SpriteName));
            help1.SetText(I18N.FormatText("#SysComDesc767", c.SpriteName));
            transform.GetComponent<Animator>().SetTrigger("Show");
            actInst.VisualHelp.Refresh(helpTitle, "helpTitle");
        }
        
        private void OnClose()
        {
            UIUtility.FadeOut(this, transform.GetComponent<Animator>());
        }
    }
}