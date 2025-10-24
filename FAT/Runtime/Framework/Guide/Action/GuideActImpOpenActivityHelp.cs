
using System;
using System.Linq;
using System.Reflection;
using fat.rawdata;

namespace FAT
{
    public class GuideActImpOpenActivityHelp: GuideActImpBase
    {
        public override void Play(string[] param)
        {
            if (param.Count() == 0)
            {
                mIsWaiting = false;
                return;
            }
            var text = param[0];
            text = text[9..]; //删除EventType前缀
            if (!string.IsNullOrEmpty(text) && Enum.TryParse<EventType>(text,out var r))
            {
                Game.Manager.activity.LookupAny(r, out var acti_);
                if (acti_ == null)
                {
                    mIsWaiting = false;
                    return;
                }
                var ui = acti_.GuideRes.res.ActiveR;
                if (ui == null)
                {
                    mIsWaiting = false;
                    return;
                }
                UIManager.Instance.OpenWindow(ui, acti_);
                mIsWaiting = false;
            }
            else
            {
                mIsWaiting = false;
            }
        }
    }
}