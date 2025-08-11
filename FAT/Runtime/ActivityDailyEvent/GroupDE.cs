using System.Collections;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using System;
using DataDE = fat.gamekitdata.DailyEvent;
using static DataTracker;

namespace FAT {
    public class GroupDE : ActivityGroup {
        public override (bool, string) FilterOne((int, int) id_, EventType type_) {
            return type_ switch {
                EventType.Dem => (false, "filter manual"),
                _ => (true, null),
            };
        }

        public override (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_) {
            if (activity_.IsInvalid(id_, out var rsi)) return (false, rsi);
            if (activity_.LookupAny(type_) != null) return (true, "active");
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            var de = Game.Manager.dailyEvent;
            var check = data_ == null;
            var (acti, valid, match, msg) = type_ switch {
                EventType.De => de.SetupActivityD(lite, check),
                EventType.Dem => de.SetupActivityM(lite, check),
                _ => (null, false, false, null),
            };
            if (acti == null) return activity_.Invalid(id_, $"failed to create instance for {id_}: {msg}");
            if (!valid || !acti.Valid) return (false, "rejected"); 
            acti.LoadData(data_);
            option_.Apply(acti);
            activity_.AddActive(id_, acti, new_: data_ == null);
            if (!match && type_ == EventType.De) activity_.CheckEventTime((int)EventType.Dem);
            return (true, null);
        }
    }
}