/*
 * @Author: yanfuxing
 * @Date: 2025-07-07 10:20:05
 */
using fat.conf;
using fat.gamekitdata;
using fat.rawdata;
using static FAT.RecordStateHelper;

namespace FAT
{
    public class CommunityMailActivity : ActivityLike
    {
        public CommunityLink CommunityLinkConfig;
        private EventCommunity _eventCommunityConfig;
        public override bool Valid => Lite.Valid && _eventCommunityConfig != null;
        public VisualPopup VisualUICommunityMailStartNotice { get; } = new(UIConfig.UICommunityMailStartNotice);
        public override ActivityVisual Visual => VisualUICommunityMailStartNotice.visual;
        private int _popupCount;
        public CommunityMailActivity(ActivityLite lite)
        {
            Lite = lite;
            _eventCommunityConfig = Data.GetEventCommunity(lite.Param);
            VisualUICommunityMailStartNotice.Setup(_eventCommunityConfig.ThemeId, this, active_: false);
            CommunityLinkConfig = Data.GetCommunityLink(_eventCommunityConfig.Id);
        }

        public override void SetupFresh()
        {
            _popupCount = 0;
        }

        public override void LoadSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            _popupCount = ReadInt(1, any);

        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_)
        {
            if (state_ != PopupType.Login)
                return;
            if (_eventCommunityConfig != null && Active)
            {
                if (Valid)
                {
                    //是否领取过
                    if (Game.Manager.communityLinkMan.IsShowCommunityLinkById(_eventCommunityConfig.BtnLink))
                    {
                        //弹脸次数是否达到上限
                        if (_popupCount >= _eventCommunityConfig.Count)
                        {
                            return;
                        }
                        VisualUICommunityMailStartNotice.Popup(Game.Manager.screenPopup);
                    }
                }
            }
        }

        public override void SaveSetup(ActivityInstance data)
        {
            var any = data.AnyState;
            any.Add(ToRecord(1, _popupCount));
        }

        public override void Open() { }

        public void AddPopupCount()
        {
            _popupCount++;
        }
    }
}