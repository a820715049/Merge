/*
 * @Author: tang.yan
 * @Description: 集卡活动-活动相关-数据管理类 
 * @Doc: https://centurygames.yuque.com/ywqzgn/ne0fhm/wz88ugv6graz2uq0#BQkOq
 * @Date: 2024-01-15 17:01:35
 */

using fat.gamekitdata;
using fat.rawdata;
using System.Collections.Generic;
using EL.Resource;

namespace FAT
{
    public class CardActivity : ActivityLike
    {
        public EventCardAlbum confD;
        //只有在活动结束预告时间段内才会显示集卡结束预告入口
        public override ActivityVisual Visual => _CheckIsInEndRemindTime() ? VisualEnd : VisualStart;
        public ActivityVisual VisualStart { get; } = new();
        public ActivityVisual VisualEnd { get; } = new();
        public ActivityVisual VisualEndCompletely = new();  //活动彻底结束
        public ActivityVisual VisualRestart = new();        //卡册重玩
        //弹脸相关
        private long startRemindTs; //活动开始后多久之内需要弹出宣传界面（秒）
        private long endRemindTs;   //活动结束前多久之内需要弹出活动结束预告（秒）
        public PopupCard StartRemindPopup { get; } = new PopupCard();  //活动开始后宣传界面弹脸相关数据
        public PopupCard EndRemindPopup { get; } = new PopupCard();    //活动结束前预告界面弹脸相关数据
        public PopupCardEnd EndCompletely { get; internal set; }   //活动彻底结束
        public PopupCard RestartRemindPopup { get; } = new PopupCard();    //卡册重玩弹板 只有上线时检测到能重玩但玩家还没确认时才弹
        
        public void Setup(ActivityLite lite_, EventCardAlbum confD_) {
            Lite = lite_;
            confD = confD_;
        }

        //顺序上在Setup之后
        public override void LoadData(ActivityInstance data_)
        {
            base.LoadData(data_);
            //上层逻辑在调用LoadData时才会设置好活动的开始结束时间
            //所以在此逻辑之后设置弹脸时间
            _RefreshPopupInfo();
        }
        
        public override void WhenEnd()
        {
            Game.Manager.screenPopup.Queue(EndCompletely);
        }
        
        public override void SetupClear() {
            base.SetupClear();
            confD = null;
            startRemindTs = 0;
            endRemindTs = 0;
            StartRemindPopup.Clear();
            EndRemindPopup.Clear();
        }
        
        public override void SaveSetup(ActivityInstance data_) { }

        public override void LoadSetup(ActivityInstance data_) { }
        
        public override IEnumerable<(string, AssetTag)> ResEnumerate() {
            if (!Valid) yield break;
            foreach(var v in VisualStart.ResEnumerate()) yield return v;
            foreach(var v in VisualEnd.ResEnumerate()) yield return v;
            foreach(var v in VisualEndCompletely.ResEnumerate()) yield return v;
            foreach(var v in VisualRestart.ResEnumerate()) yield return v;
            foreach(var v in Visual.ResEnumerate()) yield return v;
        }

        public override void TryPopup(ScreenPopup popup_, PopupType state_) {
            if (StartRemindPopup.PopupValid)
                popup_.TryQueue(StartRemindPopup, state_);
            if (EndRemindPopup.PopupValid)
                popup_.TryQueue(EndRemindPopup, state_);
            //检测到卡册能重玩但玩家还没确认时才弹
            if (RestartRemindPopup.PopupValid && Game.Manager.cardMan.CheckCanRestartAlbum())
                popup_.TryQueue(RestartRemindPopup, state_);
        }

        public override void Open()
        {
            //只有在活动结束预告时间段内才会显示入口icon  点击后会打开活动结束预告界面  并不会直接打开集卡界面
            if (_CheckIsInEndRemindTime())
            {
                UIManager.Instance.OpenWindow(UIConfig.UICardActivityEndNotice);
            }
        }

        //检查目前是否在活动结束预告时间段内
        private bool _CheckIsInEndRemindTime()
        {
            if (Valid)
            {
                var t = Game.Instance.GetTimestampSeconds();
                return t >= endRemindTs && t < endTS;
            }
            return false;
        }

        public void RefreshAlbumConfig(EventCardAlbum confD_)
        {
            if (confD_ != null && confD_.Id != confD.Id)
            {
                confD = confD_;
                _RefreshPopupInfo();
            }
        }

        private void _RefreshPopupInfo()
        {
            if (!Valid)
                return;
            startRemindTs = startTS + confD.StartRemindTime;
            endRemindTs = endTS - confD.EndRemindTime;
            var ui = new UIResAlt(UIConfig.UICardActivityStartNotice);
            if (VisualStart.Setup(confD.StartRemindTheme, ui)) {
                StartRemindPopup.Setup(this, VisualStart, startTS, startRemindTs, ui);
            }
            ui = new UIResAlt(UIConfig.UICardActivityEndNotice);
            if (VisualEnd.Setup(confD.EndRemindTheme, ui)) {
                EndRemindPopup.Setup(this, VisualEnd, endRemindTs, endTS, ui);
            }
            ui = new UIResAlt(UIConfig.UICardActivityEnd);
            if (VisualEndCompletely.Setup(confD.OverTheme, ui))
            {
                EndCompletely = new PopupCardEnd(this, VisualEndCompletely, ui, false, active_: false);
            }
            ui = new UIResAlt(UIConfig.UICardAlbumRestart);
            if (VisualRestart.Setup(confD.RestartTheme, ui)) {
                RestartRemindPopup.Setup(this, VisualRestart, startTS, endTS, ui);
            }
        }
    }
}

