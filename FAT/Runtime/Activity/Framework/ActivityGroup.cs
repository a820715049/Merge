using System;
using fat.rawdata;
using fat.gamekitdata;
using System.Collections.Generic;

namespace FAT {
    public abstract class ActivityGroup {
        public readonly ref struct Option {
            public readonly bool flex;
            public readonly bool searchAB;
            public readonly Action<ActivityLike> Create;

            public Option(bool flex_ = false, bool searchAB_ = false, Action<ActivityLike> Create_ = null) {
                flex = flex_;
                searchAB = searchAB_;
                Create = Create_;
            }

            public void Apply(ActivityLike acti_) {
                Create?.Invoke(acti_);
            }
        }

        public Activity Activity => Game.Manager.activity;

        public virtual (bool, string) TryAdd(Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_) {
            if (activity_.IsActive(id_)) return (true, "active");
            if (activity_.IsExpire(id_)) return (false, "expire");
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            (r, rs) = TryCreateByType(activity_, id_, type_, lite, data_, option_, out var pack);
            if (!r) return (r, rs);
            pack.LoadData(data_);
            if (!pack.Valid) return activity_.Invalid(id_, $"{id_} detail invalid");
            option_.Apply(pack);
            activity_.AddActive(id_, pack, new_: data_ == null);
            return (true, null);
        }

        public virtual (bool, string) TryAddTo(Action<ActivityLike> Add_, Activity activity_, (int, int) id_, EventType type_, ActivityInstance data_, in Option option_) {
            var (r, rs) = ActivityLite.ReadyToCreate(id_, option_, out var lite);
            if (!r) return activity_.Invalid(id_, $"{id_} not available reason:{rs}");
            (r, rs) = TryCreateByType(activity_, id_, type_, lite, data_, option_, out var pack);
            if (!r) return (r, rs);
            pack.LoadData(data_);
            if (!pack.Valid) return activity_.Invalid(id_, $"{id_} detail invalid");
            option_.Apply(pack);
            Add_(pack);
            return (true, null);
        }

        public virtual (bool, string) TryCreateByType(Activity activity_, (int, int) id_, EventType type_, LiteInfo lite_, ActivityInstance data_, in Option option_, out ActivityLike acti_) {
            acti_ = null;
            if (activity_.IsInvalid(id_, out var rsi)) return (false, rsi);
            var (valid, rs) = CreateCheck(type_, lite_);
            valid = valid || option_.flex;
            if (!valid) return (false, rs);
            (valid, rs) = ActivityLite.TryCreate(lite_, type_, out var lite);
            if (!valid) return (false, rs);
            acti_ = Create(type_, lite);
            if (acti_ == null) return activity_.Invalid(id_, $"unrecognized activity type {type_} id:{id_}");
            return (true, null);
        }

        public virtual (bool, string) CreateCheck(EventType type_, LiteInfo lite_) => (true, null);
        public virtual ActivityLike Create(EventType type_, ActivityLite lite_) => null;

        public virtual (bool, string) Filter(int filter_, (int, int) id_, EventType type_) {
            return filter_ switch {
                < 0 => (true, null),
                0 => FilterOne(id_, type_),
                _ => ((EventType)filter_ == type_, "type filtered")
            };
        }
        public virtual (bool, string) FilterOne((int, int) id_, EventType type_) => (true, null);
        public virtual void End(Activity activity_, ActivityLike acti_, bool expire_) { }
    }
}