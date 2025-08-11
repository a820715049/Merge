/*
 * @Author: pengjian.zhang
 * @Description: 社区跳转Item（JoinUs）
 * @Date: 2024-07-09 17:36:23
 */

using EL;
using fat.rawdata;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FAT
{
    public class UISettingCommunityItem : UIGenericItemBase<SettingsCommunity>
    {
        protected override void InitComponents()
        {
            transform.GetChild(0).GetComponent<Button>().onClick.AddListener(_OnBtnClick);
        }

        protected override void UpdateOnDataChange()
        {
            var btnTxt = transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>();
            var text = transform.GetChild(1).GetComponent<TMP_Text>();
            var text2 = transform.GetChild(2).GetComponent<TMP_Text>();
            var image = transform.GetChild(3).GetComponent<UIImageRes>();
            text.text = I18N.Text(mData.Title);
            btnTxt.text = I18N.Text(mData.Btn);
            text2.text = I18N.Text(mData.Desc);
            image.SetImage(mData.Image);
        }

        private void _OnBtnClick()
        {
            UIBridgeUtility.OpenURL(mData.BtnLink);
            DataTracker.settings_community.Track(mData.Id);
        }
    }
}