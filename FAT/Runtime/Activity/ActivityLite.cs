using System;
using System.Collections.Generic;
using fat.rawdata;
using fat.gamekitdata;
using static fat.conf.Data;
using EL;
using fat.msg;

namespace FAT {
    public readonly ref struct LiteInfo {
        public readonly int id;
        public readonly int from;
        public readonly int param;
        public readonly object info;

        public LiteInfo(int id_, int from_, int param_, object info_)
            => (id, from, param, info) = (id_, from_, param_, info_);
    }

    public abstract class ActivityLite {
        public const int FromEventTime = 0;
        public const int FromEventTrigger = 1;
        public const int FlexEventTime = 10;
        public const int FlexEventTrigger = 11;
        public const int FromInternal = 100;

        public static ActivityLite Default { get; } = new ActivityLiteDummy();

        public virtual int Id { get; set; }
        public (int, int) Id2 => (Id, From);
        public virtual EventType Type { get; set; }
        public virtual int Param { get; set; }
        public int From { get; set; }
        public int OpenCount { get; set; }  //活动累计开启次数
        public virtual long StartTS { get; set; }
        public virtual long EndTS { get; set; }
        public abstract bool Valid { get; }
        public virtual bool WillRecord { get; set; }

        public static bool Exist((int, int) id_) {
            static bool Unknown(int i_, int f_) {
                DebugEx.Error($"{nameof(ActivityLite)} unrecognized activity from:{f_} id:{i_}");
                return false;
            }
            var (id, from) = id_;
            return from switch {
                FromEventTime or FlexEventTime => ActivityLiteTime.Exist(id, from),
                FromEventTrigger or FlexEventTrigger => ActivityLiteTrigger.Exist(id, from),
                FromInternal => true,
                _ => Unknown(id, from),
            };
        }

        public static (bool, string) ReadyToCreate((int, int) id_, in ActivityGroup.Option option_, out LiteInfo info_) {
            static (bool, string) Unknown(int i_, int f_, out LiteInfo o_) {
                DebugEx.Error($"{nameof(ActivityLite)} unrecognized activity from:{f_} id:{i_}");
                o_ = default;
                return (false, "unrecognized");
            }
            var (id, from) = id_;
            return from switch {
                FromEventTime or FlexEventTime => ActivityLiteTime.TryCreateInfo(id, from, option_, out info_),
                FromEventTrigger or FlexEventTrigger => ActivityLiteTrigger.TryCreateInfo(id, from, option_, out info_),
                FromInternal => ActivityLiteFlex.CreateInfo(id, from, option_, out info_),
                _ => Unknown(id, from, out info_),
            };
        }

        public static (bool, string) TryCreate(LiteInfo info_, EventType type_, out ActivityLite lite_) {
            static (bool, string) Unknown(int i_, int f_, out ActivityLite l_) {
                DebugEx.Error($"{nameof(ActivityLite)} unrecognized activity from:{f_} id:{i_}");
                l_ = null;
                return (false, "unrecognized");
            }
            var r = info_.from switch {
                FromEventTime => ActivityLiteTime.TryCreateInstance(info_, out lite_),
                FromEventTrigger => ActivityLiteTrigger.TryCreateInstance(info_, out lite_),
                FlexEventTime => ActivityLiteTimeFlex.TryCreateInstance(info_, out lite_),
                FlexEventTrigger => ActivityLiteTriggerFlex.TryCreateInstance(info_, out lite_),
                FromInternal => ActivityLiteFlex.CreateInstance(info_, type_, out lite_),
                _ => Unknown(info_.id, info_.from, out lite_),
            };
            return r switch {
                (true, _) => CheckType(lite_, type_),
                _ => r,
            };
        }

        public static ActivityLite TrySetup(ActivityLite lite_, LiteInfo info_, EventType type_, bool replace_ = false) {
            var ((valid, rs), lite) = (lite_, info_.from) switch {
                (ActivityLiteDummy, _) => (TryCreate(info_, type_, out var liteV), liteV),
                (ActivityLiteTime, FromEventTime) => (CheckType(lite_, type_), lite_),
                (ActivityLiteTrigger, FromEventTrigger) => (CheckType(lite_, type_), lite_),
                (ActivityLiteTimeFlex, FlexEventTime) => (CheckType(lite_, type_), lite_),
                (ActivityLiteTriggerFlex, FlexEventTrigger) => (CheckType(lite_, type_), lite_),
                (ActivityLiteFlex, FromInternal) => (CheckType(lite_, type_), lite_),
                _ when replace_ => (TryCreate(info_, type_, out var liteV), liteV),
                _ => ((false, "unrecognized"), lite_),
            };
            if (!valid) {
                DebugEx.Error($"{nameof(ActivityLite)} info setup failed: {rs} {lite_.GetType()} {info_.from} {info_.id} {info_.param}");
                return lite_;
            }
            lite.Setup(info_, type_);
            return lite;
        }

        public static (bool, string) CheckType(ActivityLite l_, EventType t_) {
            return l_.Type == t_ ? (true, null) : (false, $"type mismatch expect:{t_} actual:{l_.Type}"); 
        }

        public static int IdCompact(IGiftPackLike acti_)
            => IdCompact(acti_.Id, acti_.From);
        public static int IdCompact(int id_, int from_)
            => id_ + from_ * 1_0000_0000;
        public static (int, int) IdUnwrap(int id_) {
            var f = 1_0000_0000;
            return (id_ % f, id_ / f);
        }

        public static string InfoCompact(IGiftPackLike acti_)
            => InfoCompact(acti_.Id, acti_.From, (acti_ as ActivityLike)?.Type ?? EventType.Default, (acti_ as IActivityRedirect)?.SubType);
        public static string InfoCompact(int id_, int from_, EventType type_, string sub_)
            => $"{id_}_{from_}_{(int)type_}_{sub_}";
        public static (bool, int, int, EventType, string) InfoUnwrap(string s_) {
            try {
                var s = s_.AsSpan();
                var c = '_';
                var p = s.IndexOf(c);
                var id = int.Parse(s[..p]);
                s = s[(p + 1)..];
                p = s.IndexOf(c);
                var from = int.Parse(s[..p]);
                s = s[(p + 1)..];
                p = s.IndexOf(c);
                var (type, sub) = p switch {
                    < 0 => (int.Parse(s), string.Empty),
                    _ => (int.Parse(s[..p]), s[(p + 1)..].ToString())
                };
                return (true, id, from, (EventType)type, sub);
            }
            catch (Exception e) {
                DebugEx.Warning($"failed to parse compact info {s_}, exception:{e}");
                return default;
            }
        }

        public virtual LiteInfo ToInfo() => default;
        public abstract void Setup(LiteInfo info_, EventType type_);
        public virtual bool Match(LiteInfo info_) => false;
        public abstract (long, long) SetupTS(long sTS_, long eTS_);
        public abstract void Clear();
    }

    public class ActivityLiteDummy : ActivityLite {
        public override bool Valid => false;
        public override void Setup(LiteInfo info_, EventType type_) {}
        public override (long, long) SetupTS(long sTS_, long eTS_) => (sTS_, eTS_);
        public override void Clear() {}

    }

    public class ActivityLiteTime : ActivityLite {
        public EventTime conf;
        public override long StartTS { get => conf.StartTime; set {} }
        public override long EndTS { get => conf.EndTime; set {} }
        public override bool Valid => conf != null;

        public static bool Exist(int id_, int _)
            => GetEventTime(id_) != null;
        public static (bool, string) TryCreateInfo(int id_, int from_, in ActivityGroup.Option option_, out LiteInfo info_) {
            static (bool, string) F(string m_, out LiteInfo o_) {
                o_ = default;
                return (false, m_);
            }
            var conf = ConfigOf(id_, option_.searchAB);
            if (conf == null) return F($"config EventTime {id_} not found", out info_);
            info_ = new(id_, from_, conf.EventParam, conf);
            return (true, null);
        }
        public static (bool, string) TryCreateInstance(LiteInfo info_, out ActivityLite lite_) {
            if (info_.info is not EventTime conf) return ((lite_ = null) != null, $"config invalid {info_.info}");
            lite_ = new ActivityLiteTime() {
                conf = conf, Id = info_.id, From = info_.from, Type = conf.EventType, Param = conf.EventParam,
                WillRecord = true,
            };
            return (true, null);
        }

        public static EventTime ConfigOf(int id_, bool searchAB_) {
            if (!searchAB_) return GetEventTime(id_);
            var map = fat.conf.conf_loader.ConfManager.GetCurrent().EventTimeMap;
            foreach(var v in map.EventTimeMapAB) {
                if (v.Value.EventTimeMap.TryGetValue(id_, out var conf)) return conf;
            }
            return null;
        }

        public override LiteInfo ToInfo() => new(Id, From, Param, conf);

        public override void Setup(LiteInfo info_, EventType type_) {
            if (info_.info is not EventTime conf_) return;
            conf = conf_;
            Id = conf_ != null ? conf_.Id : -1;
            Type = conf_.EventType;
            Param = conf_.EventParam;
        }

        public override bool Match(LiteInfo info_) => info_.info is EventTime confE && confE == conf;

        public override (long, long) SetupTS(long sTS_, long eTS_) => ( conf.StartTime, conf.EndTime );

        public override void Clear() {
            conf = null;
        }
    }

    public class ActivityLiteTrigger : ActivityLite {
        public EventTrigger conf;
        public override bool Valid => conf != null;
        public override bool WillRecord => false;

        public static bool Exist(int id_, int _)
            => Game.Manager.activityTrigger.info.ContainsKey(id_);
        public static (bool, string) TryCreateInfo(int id_, int from_, in ActivityGroup.Option option_, out LiteInfo info_) {
            static (bool, string) F(string m_, out LiteInfo o_) {
                o_ = default;
                return (false, m_);
            }
            if (!Game.Manager.activityTrigger.info.TryGetValue(id_, out var info)) return F($"trigger {id_} not found", out info_);
            if (!info.SetupValid) return F($"trigger {id_} config invalid", out info_);
            if (info.Abandon) return F($"trigger {id_} abandoned", out info_);
            info_ = new(id_, from_, info.conf.EventParam, info);
            return (true, null);
        }
        public static (bool, string) TryCreateInstance(LiteInfo info_, out ActivityLite lite_) {
            if (info_.info is not ActivityTrigger.Trigger infoT || !infoT.SetupValid) return ((lite_ = null) != null, $"config invalid {info_.info}");
            var conf = infoT.conf;
            lite_ = new ActivityLiteTrigger() {
                conf = conf, Id = info_.id, From = info_.from, Type = conf.EventType, Param = conf.EventParam,
                StartTS = infoT.start, EndTS = infoT.end, OpenCount = infoT.count,
            };
            return (true, null);
        }

        public override LiteInfo ToInfo() => new(Id, From, Param, conf);

        public override void Setup(LiteInfo info_, EventType type_) {
            if (info_.info is not ActivityTrigger.Trigger infoT) return;
            var conf_ = infoT.conf;
            conf = conf_;
            Id = conf_ != null ? conf_.Id : -1;
            Type = conf_.EventType;
            Param = conf_.EventParam;
        }

        public override bool Match(LiteInfo info_) => info_.info is ActivityTrigger.Trigger infoT && infoT.conf == conf;

        public override (long, long) SetupTS(long sTS_, long eTS_)
            => sTS_ > 0 ? (sTS_, eTS_) : (StartTS, EndTS);

        public override void Clear() {
            conf = null;
        }
    }

    public class ActivityLiteTimeFlex : ActivityLiteTime {
        public new static (bool, string) TryCreateInstance(LiteInfo info_, out ActivityLite lite_) {
            if (info_.info is not EventTime conf) return ((lite_ = null) != null, $"config invalid {info_.info}");
            lite_ = new ActivityLiteTimeFlex() {
                conf = conf, Id = info_.id, From = info_.from, Type = conf.EventType, Param = conf.EventParam,
                WillRecord = false,
            };
            return (true, null);
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
            => (StartTS, EndTS) = sTS_ > 0 ? (sTS_, eTS_) : (StartTS, EndTS);
    }

    public class ActivityLiteTriggerFlex : ActivityLiteTrigger {
        public new static (bool, string) TryCreateInstance(LiteInfo info_, out ActivityLite lite_) {
            if (info_.info is not ActivityTrigger.Trigger infoT || !infoT.SetupValid) return ((lite_ = null) != null, $"config invalid {info_.info}");
            var conf = infoT.conf;
            lite_ = new ActivityLiteTrigger() {
                conf = conf, Id = info_.id, From = info_.from, Type = conf.EventType, Param = conf.EventParam,
                StartTS = infoT.start, EndTS = infoT.end,
                WillRecord = false,
            };
            return (true, null);
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
            => (StartTS, EndTS) = sTS_ > 0 ? (sTS_, eTS_) : (StartTS, EndTS);
    }

    public class ActivityLiteFlex : ActivityLite {
        public override bool Valid => true;
 
        public static (bool, string) CreateInfo(int id_, int from_, in ActivityGroup.Option option_, out LiteInfo info_) {
            info_ = new(id_, from_, 0, null);
            return (true, null);
        }
 
        public static (bool, string) CreateInstance(LiteInfo info_, EventType type_, out ActivityLite lite_) {
            lite_ = new ActivityLiteFlex();
            lite_.Setup(info_, type_);
            return (true, null);
        }

        public override void Setup(LiteInfo info_, EventType type_) {
            Id = info_.id;
            Type = type_;
            Param = info_.id;
        }

        public override (long, long) SetupTS(long sTS_, long eTS_)
            => (StartTS, EndTS) = sTS_ > 0 ? (sTS_, eTS_) : (StartTS, EndTS);

        public override void Clear() {}
    }
}