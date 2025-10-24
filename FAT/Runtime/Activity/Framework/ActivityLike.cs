using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using EL.Resource;
using EL;

namespace FAT
{
    using static PoolMapping;

    public abstract class ActivityLike : IAssetDependent
    {
        public int dataIndex;
        public ActivityLite Lite { get; set; } = ActivityLite.Default;
        public (int, int, int) Info3 => (Id, From, Param);
        public int Id => Lite.Id;
        public (int, int) Id2 => Lite.Id2;
        public EventType Type => Lite.Type;
        public int Param => Lite.Param;
        public int From => Lite.From;

        //对外暴露当前活动的累计开启次数 以供打点
        //目前仅支持Trigger类活动 默认从1开始
        //其他类活动默认返回0
        public int OpenCount =>
            (From == ActivityLite.FromEventTrigger && Lite is ActivityLiteTrigger liteTrigger)
            ? liteTrigger.OpenCount
            : 0;

        public virtual bool Valid => Lite.Valid;
        public virtual ActivityVisual Visual { get; } = new();
        public virtual VisualRes GuideRes { get; } = new();
        public virtual bool EntryVisible => Visual.EntryVisible;
        public virtual string EntryIcon => Visual.EntryIcon;
        public virtual int Priority => Visual.Priority;
        public int phase;
        public long startTS;
        public long endTS;
        public long Countdown => endTS - Game.TimestampNow();
        public bool Active => Valid && Countdown > 0;
        public ActivityAsset Asset { get; } = new();
        public static Action<IAssetDependent> WhenResReady;

        public override string ToString() => $"{Info3}";

        public virtual ActivityInstance SaveData()
        {
            var data = new ActivityInstance()
            {
                Id = Id,
                StartTS = startTS,
                EndTS = endTS,
                Phase = phase,
                ActId = (int)Type,
                From = Lite.From,
            };
            dataIndex = 0;
            SaveSetup(data);
            return data;
        }

        public virtual void LoadData(ActivityInstance data_)
        {
            if (data_ != null)
            {
                phase = data_.Phase;
                dataIndex = 0;
                LoadSetup(data_);
                RefreshTS(data_.StartTS, data_.EndTS);
            }
            else
            {
                RefreshTS(0, 0);
                SetupFresh();
            }
            AfterLoad(data_);
        }

        public abstract void SaveSetup(ActivityInstance data_);
        public abstract void LoadSetup(ActivityInstance data_);

        public virtual void AfterLoad(ActivityInstance data_) { }
        public virtual bool SetupPending() => false;
        public virtual bool ResPending()
        {
            Asset.Setup(this, 0);
            return false;
            // return ResManager.Prepare(Asset);
        }
        public virtual void SetupFresh() { }
        public virtual void SetupClear()
        {
            Lite?.Clear();
        }

        public void RefreshTS(long sTS_, long eTS_) => (startTS, endTS) = SetupTS(sTS_, eTS_);
        public virtual (long, long) SetupTS(long sTS_, long eTS_) => Lite.SetupTS(sTS_, eTS_);
        public virtual void WhenActive(bool new_) { }
        public virtual void WhenEnd() { }
        public virtual void WakeLimbo() { }
        public virtual void WhenReset() { }
        public virtual bool WhenObserve(string e_) => false;

        public virtual void TryPopup(ScreenPopup popup_, PopupType state_) { }
        public virtual void ResetPopup() { }

        public abstract void Open();
        public void Open(in VisualPopup vi_) => Open(vi_.res);
        public void Open(in VisualRes vi_) => Open(vi_.res);
        public void Open(UIResAlt ui_) => OpenRes(ui_.ActiveR, this);
        public static void OpenRes(UIResAlt ui_) => OpenRes(ui_.ActiveR, null);
        public static void OpenRes(UIResource ui_, ActivityLike acti_)
        {
            UIManager.Instance.OpenWindow(ui_, acti_);
            DataTracker.event_popup.Track(acti_);
        }

        public virtual IEnumerable<(string, AssetTag)> ResEnumerate()
            => Visual.ResEnumerate();
    }
}