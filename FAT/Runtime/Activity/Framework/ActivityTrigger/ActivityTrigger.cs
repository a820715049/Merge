using System;
using System.Collections.Generic;
using fat.gamekitdata;
using fat.rawdata;
using static fat.conf.Data;
using EL;
using FAT.MSG;
using System.Text;
using System.Diagnostics;

namespace FAT
{
    using static ConditionExpression;
    using static EventTriggerState;
    using static ActivityLite;
    using static MessageCenter;
    using static RecordStateHelper;
    using static EvaluateEventTrigger;

    public class ActivityTrigger : IGameModule, IUserDataHolder
    {
        public class Trigger
        {
            public int id;
            public EventTrigger conf;
            public Expr expr;
            public Expr exprR;
            public EventTriggerState state;
            public long ts;
            public long start;
            public long end;
            public long invalid;
            public int count;
            internal string rs;
            public bool Valid => conf != null && expr != null && invalid == 0;
            public bool SetupValid => conf != null;
            public bool Active => conf != null && conf.IsActive;
            public bool Abandon => conf != null && conf.IsAbandon;
            public long ConfEnd => start + conf.Lifetime;
        }

        public readonly Dictionary<int, Trigger> info = new();
        public readonly List<(int, TriggerInfo)> orphan = new();
        public readonly List<Trigger> waiting = new();
        public readonly List<Trigger> pending = new();
        public readonly List<Trigger> revive = new();
        internal readonly ConditionExpression cond = new();
        internal readonly EvaluateEventTrigger eval = new();
        private readonly Dictionary<string, (Expr, bool)> cache = new();

        public void DebugEvalauteInfo()
        {
            static string V((float v, bool r) v) => v.r ? $"{v.v}" : "-";
            var b = new StringBuilder();
            b.Append(nameof(Lv)).Append(':').Append(Lv()).Append(' ');
            b.Append(nameof(LT)).Append(':').Append(LT()).Append(' ');
            b.Append(nameof(AD)).Append(':').Append(AD()).Append(' ');
            b.Append(nameof(UTC)).Append(':').Append(UTC()).Append(' ');
            b.Append(nameof(IAPTotal)).Append(':').Append(IAPTotal()).Append(' ');
            b.Append(nameof(LvLT)).Append(Lv()).Append(':').Append(V(LvLT())).Append(' ');
            b.Append(nameof(PayLT)).Append(':').Append(V(PayLT())).Append(' ');
            var str = b.ToString();
            DebugEx.Info(str);
            Game.Manager.commonTipsMan.ShowMessageTips(str, isSingle: true);
        }

        public void DebugReset()
        {
            Clear();
            LoadConfig();
            SetupFresh();
        }

        public void DebugResetLegacy()
        {
            Clear();
            LoadConfig();
            SetupLegacy();
            SetupFresh();
        }

        public void DebugReportState()
        {
            var b = new StringBuilder();
            b.Append(nameof(ActivityTrigger)).AppendLine(" state:");
            foreach (var (id, t) in info)
            {
                b.Append(t.id).Append(' ');
                b.Append(t.state);
                switch (t.state)
                {
                    case Waiting: b.Append("(queue=").Append(waiting.Contains(t)).Append(") "); break;
                    case Pending: b.Append("(queue=").Append(pending.Contains(t)).Append(") "); break;
                    default: b.Append(' '); break;
                }
                if (t.start > 0 || t.end > 0)
                {
                    b.Append('[').Append(Game.TimeOf(t.start)).Append("->");
                    b.Append(Game.TimeOf(t.end)).Append(']');
                }
                var special = t switch
                {
                    _ when t.Abandon => "(abandon)",
                    _ when !t.Active => "(inactive)",
                    _ when !t.Valid => "(invalid)",
                    _ => null,
                };
                if (special != null)
                {
                    b.AppendLine(special);
                    continue;
                }
                var expr = t.expr;
                if (revive.Contains(t))
                {
                    expr = t.exprR;
                    b.Append("(revive)");
                }
                if (!eval.Ready(expr))
                {
                    b.Append("(not ready)");
                }
                if (t.count > 1) b.Append(" #").Append(t.count);
                b.AppendLine();
            }
            DebugEx.Info(b.ToString());
        }

        public void Clear()
        {
            waiting.Clear();
            pending.Clear();
            revive.Clear();
            info.Clear();
        }

        public void Reset()
        {
            Clear();
            SetupListener();
        }

        public void SetupListener()
        {
            Get<ACTIVITY_SUCCESS>().AddListenerUnique(ActivitySuccess);
            Get<ACTIVITY_END>().AddListenerUnique(ActivityEnd);
            Get<ACTIVITY_TS_SYNC>().AddListenerUnique(ActivityTSSync);
        }

        public virtual void LoadConfig()
        {
            var ts = Game.TimestampNow();
            foreach (var (id, v) in GetEventTriggerMap())
            {
                if (v.IsAbandon || !v.IsActive)
                {
                    info[id] = new Trigger()
                    {
                        id = id, conf = v, state = Waiting,
                    };
                    continue;
                }
                var (e, valid) = cond.Parse(v.TriggerRequire);
                info[id] = new Trigger()
                {
                    id = id, conf = v, expr = e, state = Waiting,
                    invalid = valid ? 0 : ts,
                };
            }
        }

        public void Startup()
        {
            if (waiting.Count + pending.Count + revive.Count == 0)
            {
                SetupLegacy();
                SetupFresh();
            }
            Check();
        }

        private void SetupFresh()
        {
            foreach (var (_, t) in info)
            {
                Setup(t);
            }
        }

        private void SetupLegacy()
        {
            var ts = Game.TimestampNow();
            var act = Game.Manager.activity;
            //legacy NewUserPack
            var nu1 = act.RecordOf(1001) > 0;
            var nu2 = act.RecordOf(1002) > 0;
            var nu3 = act.RecordOf(1003) > 0;
            var actiNU = act.LookupAny(EventType.NewUser);
            var nu1a = actiNU?.Id == 1001;
            var nu2a = actiNU?.Id == 1002;
            var nu3a = actiNU?.Id == 1003;
            act.EndImmediate(EventType.NewUser);
            if (info.TryGetValue(1, out var t1))
            {
                if (nu1) State(t1, nu1 && !nu2 && !nu3 ? Succeed : Failed, ts);
                if (nu1a) { (t1.start, t1.end) = (actiNU.startTS, actiNU.endTS); Invoke(t1, ts); }
            }
            if (info.TryGetValue(2, out var t2))
            {
                if (nu2) State(t2, nu2 && !nu3 && !nu1a ? Succeed : Failed, ts);
                if (nu2a) { (t2.start, t2.end) = (actiNU.startTS, actiNU.endTS); Invoke(t2, ts); }
            }
            if (info.TryGetValue(3, out var t3))
            {
                if (nu3) State(t3, Failed, ts);
                if (nu3a) { (t3.start, t3.end) = (actiNU.startTS, actiNU.endTS); Invoke(t3, ts); }
            }
            //legacy NewSessionPack
            act.EndImmediate(EventType.NewSession);
        }

        private long TSOffset() => Game.Timestamp(new DateTime(2024, 1, 1));

        public void FillData(LocalSaveData archive)
        {
            var game = archive.ClientData.PlayerGameData;
            var data = game.Trigger ??= new();
            var record = data.Record;
            var tsO = TSOffset();
            foreach (var (id, t) in info)
            {
                var dataT = new TriggerInfo()
                {
                    State = (int)t.state,
                    TS = t.ts,
                };
                record[id] = dataT;
                var any = dataT.AnyState;
                any.Add(ToRecord(0, t.start, tsO));
                any.Add(ToRecord(1, t.end, tsO));
                any.Add(ToRecord(2, t.invalid, tsO));
                any.Add(ToRecord(3, t.count));
            }
            foreach (var (id, t) in orphan)
            {
                record[id] = t;
            }
        }

        public void SetData(LocalSaveData archive)
        {
            var data = archive.ClientData.PlayerGameData.Trigger;
            if (data == null) return;
            var record = data.Record;
            var tsO = TSOffset();
            foreach (var (id, dataT) in record)
            {
                if (!info.TryGetValue(id, out var t))
                {
                    orphan.Add((id, dataT));
                    continue;
                }
                t.state = (EventTriggerState)dataT.State;
                t.ts = dataT.TS;
                var any = dataT.AnyState;
                t.start = ReadTS(0, tsO, any);
                t.end = ReadTS(1, tsO, any);
                t.invalid = ReadTS(2, tsO, any);
                t.count = ReadInt(3, any);
                Setup(t);
            }

            // 配置有 但 存档里没有 也尝试Setup
            foreach (var (id, trigger) in info)
            {
                if (record.ContainsKey(id))
                    continue;
                Setup(trigger);
            }
        }

        public void Setup(Trigger t_)
        {
            switch (t_.state)
            {
                case var _ when !t_.Valid: break;
                case var _ when !t_.Active: break;
                case var _ when t_.Abandon: Abandon(t_); break;
                case Waiting: waiting.Add(t_); break;
                case Pending: Insert(pending, t_); break;
                case Failed:
                case Succeed: TryAppendRevive(t_, t_.state); break;
            }
            ;
        }

        internal void Insert(IList<Trigger> list_, Trigger t_)
        {
            var k = 0;
            for (; k < list_.Count; ++k)
            {
                if (list_[k].conf.Priority > t_.conf.Priority) break;
            }
            list_.Insert(k, t_);
        }

        public void Abandon(Trigger t)
        {
            var conf = t.conf;
            var act = Game.Manager.activity;
            if (conf.Id == 0 || !act.LookupAny(conf.EventType, conf.Id, out var acti)) return;
            act.EndImmediate(acti, true);
            DebugEx.Info($"{nameof(ActivityTrigger)} end activity {acti.Id} {acti.Type} because trigger {conf.Id} is set to abandon");
        }

        public (Expr, bool) Parse(string str_, bool cache_ = true)
        {
            if (cache.TryGetValue(str_, out var v)) return v;
            var (e, r) = cond.Parse(str_);
            if (cache_) cache[str_] = (e, r);
            return (e, r);
        }
        public bool Evaluate(string str_)
        {
            var (e, r) = Parse(str_, cache_: true);
            if (!r) return false;
            var (v, _) = Evaluate(e);
            return v;
        }

        public (bool, bool) Evaluate(Expr e)
        {
            if (!eval.Ready(e)) return (false, true);
            return cond.Evaluate(eval, e);
        }

        public void Check()
        {
            var time = Game.TimestampNow();
            for (var k = 0; k < revive.Count; ++k)
            {
                var t = revive[k];
                var (r, v) = Evaluate(t.exprR);
                if (!v)
                {
                    DebugEx.Error($"{nameof(ActivityTrigger)} revive trigger {t.id} invalid");
                    revive.RemoveAt(k--);
                    continue;
                }
                if (r)
                {
                    revive.RemoveAt(k--);
                    waiting.Add(t);
                    Revive(t, time);
                }
            }
            for (var k = 0; k < waiting.Count; ++k)
            {
                var t = waiting[k];
                var (r, v) = Evaluate(t.expr);
                if (!v)
                {
                    DebugEx.Error($"{nameof(ActivityTrigger)} waiting trigger {t.id} invalid");
                    waiting.RemoveAt(k--);
                    continue;
                }
                if (r)
                {
                    waiting.RemoveAt(k--);
                    Insert(pending, t);
                    Ready(t, time);
                }
            }
            for (var k = 0; k < pending.Count; ++k)
            {
                var t = pending[k];
                if (time >= t.start && Invoke(t, time))
                {
                    pending.RemoveAt(k--);
                }
            }
        }

        public void State(Trigger t_, EventTriggerState s_, long ts_)
        {
            t_.state = s_;
            t_.ts = ts_;
            t_.rs = null;
            TryAppendRevive(t_, s_);
        }

        public void State(int id_, EventTriggerState s_, long ts_)
        {
            if (info.TryGetValue(id_, out var t)) State(t, s_, ts_);
        }

        public void TryAppendRevive(Trigger t_, EventTriggerState s_)
        {
            if (!(s_ == Failed || s_ == Succeed) || string.IsNullOrEmpty(t_.conf.ReviveRequire)) return;
            var (e, valid) = cond.Parse(t_.conf.ReviveRequire);
            if (!valid) return;
            t_.exprR = e;
            revive.Add(t_);
        }

        public void Revive(Trigger t_, long ts_)
        {
            State(t_, Waiting, ts_);
            t_.exprR = null;
            DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} revived");
        }

        public void Ready(Trigger t_, long ts_)
        {
            State(t_, Pending, ts_);
            if (t_.conf.IsUtc)
            {
                var g = Game.Manager.configMan.globalConfig;
                var day = Game.UtcNow.AddHours(-g.RequireTypeUtcClock);
                t_.start = Game.Timestamp(Game.NextTimeOfDay(day, hour_: t_.conf.StartUtc, offset_: 0));
            }
            else t_.start = ts_;
            t_.end = t_.start + t_.conf.Lifetime;
            DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} ready {Game.TimeOf(t_.start)}->{Game.TimeOf(t_.end)}");
        }

        public bool Invoke(Trigger t_, long ts_)
        {
            ++t_.count;
            if (ts_ >= t_.end)
            {
                State(t_, Failed, ts_);
                DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} missed end ts {t_.end} {ts_}");
                return true;
            }
            var conf = t_.conf;
            if (conf.EventParam == 0 && conf.EventType == EventType.Default)
            {
                State(t_, Failed, ts_);
                DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} invoke & skip");
                return true;
            }
            var actMgr = Game.Manager.activity;
            var (r, rs) = actMgr.TryAdd((t_.id, FromEventTrigger), t_.conf.EventType);
            if (r)
            {
                // 成功发起活动 尝试立即终止配置的trigger
                foreach (var shutdownId in t_.conf.ShutdownTrigger)
                {
                    var acti = actMgr.Lookup(shutdownId, FromEventTrigger);
                    if (acti != null)
                    {
                        actMgr.EndImmediate(acti, false);
                    }
                }
            }
            else
            {
                var rm = rs switch
                {
                    _ => false,
                };
                if (rm)
                {
                    State(t_, Failed, ts_);
                    DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} failed to invoke reason:{rs}");
                    return true;
                }
                if (t_.rs != rs)
                {
                    DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} try invoke failed reason:{rs}");
                    t_.rs = rs;
                }
                return false;
            }
            State(t_, Activating, ts_);
            var ss = rs != null ? $" (note:{rs})" : null;
            DebugEx.Info($"{nameof(ActivityTrigger)} {t_.id} invoked{ss}");
            return true;
        }

        public bool InvokeImmediate(int id_)
        {
            if (!info.TryGetValue(id_, out var t)) return false;
            var ts = Game.TimestampNow();
            Ready(t, ts);
            return Invoke(t, ts);
        }

        internal bool ActivityState(ActivityLike acti_, EventTriggerState s_, out Trigger t)
        {
            var id = acti_.Id;
            t = null;
            if (acti_.From != FromEventTrigger || !info.TryGetValue(id, out t)) return false;
            if (!t.Valid)
            {
                DebugEx.Error($"{nameof(ActivityTrigger)} state update to invalid trigger id:{id} type:{acti_.Type} param:{acti_.Param}");
            }
            if (t.state != Activating) return false;
            var ts = Game.TimestampNow();
            DebugEx.Info($"{nameof(ActivityTrigger)} {t.id} state changed to {s_} by activity");
            State(t, s_, ts);
            return true;
        }

        public void ActivitySuccess(ActivityLike acti_)
        {
            ActivityState(acti_, Succeed, out _);
        }

        public void ActivityEnd(ActivityLike acti_, bool expire_)
        {
            if (!ActivityState(acti_, expire_ ? Failed : Succeed, out var t)) return;
            t.end = Game.TimestampNow();
        }

        public void ActivityTSSync(ActivityLike acti_)
        {
            var id = acti_.Id;
            if (!info.TryGetValue(id, out var t)) return;
            if (!t.Valid)
            {
                DebugEx.Error($"{nameof(ActivityTrigger)} sync ts with invalid trigger id:{id} type:{acti_.Type} param:{acti_.Param}");
            }
            t.start = acti_.startTS;
            t.end = acti_.endTS;
            DebugEx.Info($"{nameof(ActivityTrigger)} {t.id} ts sync to {Game.TimeOf(t.start)}-{Game.TimeOf(t.end)} by activity");
        }
    }
}