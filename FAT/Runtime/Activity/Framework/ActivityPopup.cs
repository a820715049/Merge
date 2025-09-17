using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using static EL.MessageCenter;
using FAT.MSG;
using Cysharp.Threading.Tasks;

namespace FAT
{
    public class ActivityPopup
    {
        public Activity activity;
        public PopupOOE popupOOE = new() { option = new IScreenPopup.Option { ignoreDelay = true } };

        public ActivityPopup(Activity activity_)
        {
            activity = activity_;
            Get<SCREEN_POPUP_QUERY>().AddListenerUnique(PopupQuery);
        }

        public void PopupQuery(ScreenPopup popup_, PopupType state_)
        {
            void Query()
            {
                foreach (var (_, a) in activity.map)
                {
                    a.TryPopup(popup_, state_);
                }
                foreach (var (_, a) in activity.pending)
                {
                    a.TryPopup(popup_, state_);
                }
            }
            int Any()
            {
                Query();
                return 0;
            }
            void LoginP(ActivityLike a_)
            {
                var asset = a_.Asset;
                if (!asset.Pending) a_.TryPopup(popup_, state_);
                else asset.popupLogin = true;
            }
            int Login()
            {
                foreach (var (_, a) in activity.map)
                {
                    LoginP(a);
                }
                foreach (var (_, a) in activity.pending)
                {
                    LoginP(a);
                }
                return 0;
            }
            int OutOfEnergy()
            {
                popup_.Queue(popupOOE);
                activity.CheckEventTime((int)EventType.Energy);
                activity.CheckEventTime((int)EventType.GemEndlessThree);
                activity.CheckEventTime((int)EventType.EnergyMultiPack);
                Query();
                return 0;
            }
            var _ = state_ switch
            {
                PopupType.Energy => OutOfEnergy(),
                PopupType.Login => Login(),
                _ => Any(),
            };
        }

        public void WhenEnterGame()
        {
            async UniTask W()
            {
                await activity.waitRes;
                Game.Manager.screenPopup.WhenEnterGame();
            }
            _ = W();
        }
    }
}