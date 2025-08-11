/*
 * @Author: tang.yan
 * @Description: 卡册活动预告tips界面 
 * @Date: 2024-04-22 15:04:18
 */
using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;

namespace FAT
{
    public class UICardActivityTips : UITipsBase
    {
        [SerializeField] private TMP_Text desc1;
        [SerializeField] private TMP_Text desc2;
        
        protected override void OnParse(params object[] items)
        {
            if (items.Length >= 2)
            {
                //设置tips位置参数
                _SetTipsPosInfo(items);
            }
        }

        protected override void OnPreOpen()
        {
            Game.Manager.audioMan.TriggerSound("CardActivityTips");
            //设置文本
            desc1.text = I18N.Text("#SysComDesc370");
            var conf = Game.Manager.featureUnlockMan.GetFeatureConfig(FeatureEntry.FeatureCardAlbum);
            if (conf != null)
            {
                desc2.text = I18N.FormatText("#SysComDesc371", conf.Level);
            }
            //刷新tips位置
            _RefreshTipsPos(18);
        }
    }
}