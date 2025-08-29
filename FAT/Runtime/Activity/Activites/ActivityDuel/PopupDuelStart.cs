/**
 * @Author: zhangpengjian
 * @Date: 2025/4/14 10:41:16
 * @LastEditors: zhangpengjian
 * @LastEditTime: 2025/4/14 10:41:16
 * Description: 开启duel活动弹窗
 */

namespace FAT
{
    public class PopupDuelStart : IScreenPopup
    {
        public override int PopupWeight => int.MaxValue;
        public override int PopupLimit => -1;
        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup();
        private ActivityLike _activity;

        public PopupDuelStart(ActivityLike activity_, ActivityVisual visual_, UIResAlt ui_)
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