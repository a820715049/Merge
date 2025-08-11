using EL;
using TMPro;
using UnityEngine;
using static fat.conf.Data;

namespace FAT
{
    public class UIJokerCardTips : UITipsBase
    {
        [SerializeField] private GameObject _normalBg;
        [SerializeField] private GameObject _shinyBg;
        [SerializeField] private GameObject _normalImg;
        [SerializeField] private GameObject _shinyImg;
        [SerializeField] private TextMeshProUGUI _titleBg;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _desc;

        private readonly string commonKey = "#SysComDesc1057";
        private readonly string shinyKey = "#SysComDesc327";

        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(934);
            _SetTipsPosInfo(items);
            int.TryParse(items[2].ToString(), out var id);
            var info = GetObjCardJoker(id);
            if (info == null)
                return;
            var obj = GetObjBasic(id);
            setBg(info.IsOnlyNormal);
            setDesc(info.ChestTipsStyle, info.IsOnlyNormal);
            setTitle(obj.Name);
        }

        private void setBg(bool normal)
        {
            _normalBg.SetActive(normal);
            _normalImg.SetActive(normal);
            _shinyBg.SetActive(!normal);
            _shinyImg.SetActive(!normal);
        }

        private void setTitle(string text)
        {
            _title.text = I18N.Text(text);
            _titleBg.text = I18N.Text(text);
        }

        private void setDesc(int id, bool isOnlyNormal)
        {
            var config = FontMaterialRes.Instance.GetFontMatResConf(id);
            if (config == null) return;
            config.ApplyFontMatResConfig(_desc);
            _desc.SetText(I18N.Text(isOnlyNormal ? commonKey : shinyKey));
        }

        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18, false);
        }
    }
}