using EL;
using TMPro;
using UnityEngine.UI;

namespace FAT
{
    public class UIOrderRAteTip : UITipsBase
    {
        private TextMeshProUGUI _title;
        private TextMeshProUGUI _titleBg;
        private UITextState _desc;
        private UIImageRes _icon;

        protected override void OnCreate()
        {
            transform.Access("root/TitleBg/Title", out _title);
            transform.Access("root/TitleBg/Title2", out _titleBg);
            transform.Access("root/Desc", out _desc);
            transform.Access("root/Reward", out _icon);

        }

        protected override void OnParse(params object[] items)
        {
            _SetCurTipsWidth(906);
            _SetTipsPosInfo(items);
            int.TryParse(items[2].ToString(), out var id);
            var cfg = Game.Manager.configMan.GetEventOrderRateBoxConfig(id);
            if (cfg == null)
                return;
            _icon.SetImage(cfg.EventInfo);
            _title.text = I18N.Text(cfg.OrderInfoKey);
            _titleBg.text = I18N.Text(cfg.OrderInfoKey);
            _desc.Select(id - 1);
        }
        protected override void OnPreOpen()
        {
            //刷新tips位置
            _RefreshTipsPos(18, false);
        }
    }

}