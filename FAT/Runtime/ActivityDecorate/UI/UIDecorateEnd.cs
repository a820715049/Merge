using EL;
using fat.rawdata;
using TMPro;

namespace FAT
{
    public class UIDecorateEnd : UIBase
    {
        private UIImageRes _bg;
        private UIImageRes _titleBg;
        private TextProOnACircle _title;
        private DecorateActivity _activity;
        private TextMeshProUGUI _desc;

        protected override void OnCreate()
        {
            _bg = transform.Find("Content/Panel/bg").GetComponent<UIImageRes>();
            _titleBg = transform.Find("Content/Panel/bg1").GetComponent<UIImageRes>();
            _title = transform.Find("Content/Panel/bg1/title").GetComponent<TextProOnACircle>();
            transform.AddButton("Content/Panel/confirm", Close);
            _desc = transform.Find("Content/Panel/desc1").GetComponent<TextMeshProUGUI>();
        }

        protected override void OnParse(params object[] items)
        {
            _activity = items[0] as DecorateActivity;
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
        }

        private void RefreshTheme()
        {
        }
    }
}
