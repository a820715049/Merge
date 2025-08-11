/*
 *@Author:chaoran.zhang
 *@Desc:登入赠品活动，主要管理是否领取了奖励，时候需要弹窗
 *@Created Time:2024.03.21 星期四 16:04:37
 */

using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;

namespace FAT
{
    public class LoginGiftActivity : ActivityLike
    {
        public EventLoginGift confD;
        public UIResAlt Res = new(UIConfig.UILoginGift);
        public override bool Valid => confD != null;
        public PopupActivity Popup { get; internal set; }

        public LoginGiftActivity(ActivityLite lite_) {
            Lite = lite_;
            confD = GetEventLoginGift(lite_.Param);
            if (confD != null && Visual.Setup(confD.EventTheme, Res))
            {
                Popup = new PopupActivity(this, Visual, Res, false);
            }
        }

        public override void SaveSetup(ActivityInstance data_)
        {
        }

        public override void LoadSetup(ActivityInstance data_)
        {
        }
        
        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            popup_.TryQueue(Popup, state_);
        }
        
        public override void Open() => Open(Res);
    }
}