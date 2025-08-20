/*
 * @Author: yanfuxing
 * @Date: 2025-07-03 10:20:05
 */

namespace FAT
{
    public class CommunityPlanRewardPop : IScreenPopup
    {
        public override bool Ready() => UIManager.Instance.CheckUIIsIdleStateForPopup(true);
        public override int PopupWeight => int.MaxValue;
        public CommunityPlanRewardPop(int popupId_, UIResAlt ui_)
        {
            PopupId = popupId_;
            PopupConf = Game.Manager.configMan.GetPopupConfig(popupId_);
            PopupRes = ui_.ActiveR;
        }

        public override bool CheckValid(out string _)
        {
            _ = null;
            return true;
        }

        public override bool OpenPopup()
        {
            UIManager.Instance.OpenWindow(PopupRes, Custom);
            Custom = null;
            return true;
        }
    }
}