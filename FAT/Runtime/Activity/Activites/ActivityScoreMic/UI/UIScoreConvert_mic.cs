namespace FAT
{
    public class UIScoreConvert_mic : UIActivityConvert {
        private ActivityScoreMic activity;
        public override ActivityVisual Visual => activity.SettlePopup.visual;
        public override bool Complete => activity.IsComplete();

        protected override void OnParse(params object[] items) {
            activity = (ActivityScoreMic)items[0];
            base.OnParse(items);
        }

        protected override void OnPreOpen()
        {
            RefreshTheme();
            RefreshUI();
        }

        private void RefreshUI()
        {
            var anyConvert = result.Count > 0;
            convert.gameObject.SetActive(anyConvert);
            var rSize = root.sizeDelta;
            if (anyConvert) {
                root.sizeDelta = new(rSize.x, size[0]);
                MBI18NText.SetFormatKey(desc.gameObject, Complete ? "#SysComDesc1714" : "#SysComDesc307");
                convert.Refresh(result);
                confirm.text.Select(0);
            }
            else {
                root.sizeDelta = new(rSize.x, size[1]);
                MBI18NText.SetFormatKey(desc.gameObject, Complete ? "#SysComDesc1242" : "#SysComDesc1713");
                confirm.text.Select(Complete ? 1 : 2);
            }
        }
    }
}