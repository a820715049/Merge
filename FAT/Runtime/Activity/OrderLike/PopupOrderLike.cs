/*
 * @Author: qun.chao
 * @Date: 2025-04-08 14:03:54
 */
namespace FAT
{
    public class PopupOrderLike : IScreenPopup
    {
        public override int PopupWeight => int.MaxValue;
        public override int PopupLimit => -1;
        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup();
        private ActivityLike _activity;

        public PopupOrderLike(ActivityLike activity_, ActivityVisual visual_, UIResAlt ui_)
        {
            _activity = activity_;
            PopupId = visual_.PopupId;
            PopupConf = visual_.Popup;
            PopupRes = ui_.ActiveR;
        }

        public override bool CheckValid(out string _)
        {
            _ = null;
            return true;
        }

        public override bool OpenPopup()
        {
            UIManager.Instance.OpenWindow(PopupRes, _activity, Custom);
            Custom = null;
            DataTracker.event_popup.Track(_activity);
            return true;
        }
    }
}