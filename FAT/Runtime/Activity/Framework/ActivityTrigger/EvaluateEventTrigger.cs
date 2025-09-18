using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using fat.rawdata;

namespace FAT
{
    using static ConditionExpression;
    using static EventTriggerState;

    public class EvaluateEventTrigger : IConditionEvaluate {
        public readonly Dictionary<string, float> cache = new();
        public ActivityTrigger target;
        public ActivityTrigger Target => target ?? Game.Manager.activityTrigger;

        public bool TryEval(ConditionExpression o, Node n, out bool r) {
            bool v;
            (v, r) = (n.op, n.Type) switch {
                (OpEQ, nameof(TriggerState)) => (true, TriggerState(n)),
                (OpEQ, nameof(TriggerExpired)) => (true, TriggerExpired(n)),
                _ => (false, false),
            };
            return v;
        }

        public (float, bool) Value(IExpr e, string t) {
            return (e, t) switch {
                (ValueS s, _) => (s.v, true),
                (ValueN n, _) when cache.TryGetValue(n.v, out var v) => (v, true),
                (ValueN n, nameof(Lv)) => (Lv(), true),
                (ValueN n, nameof(LT)) => (LT(), true),
                (ValueN n, nameof(AD)) => (AD(), true),
                (ValueN n, nameof(UTC)) => (UTC(), true),
                (ValueN n, nameof(IAPTotal)) => (IAPTotal(), true),
                (ValueN n, nameof(LoginLv)) => LoginLv(),
                (ValueN n, nameof(LastLoginLT)) => LastLoginLT(),
                (ValueN _, nameof(LvUp)) => LvUp(),
                (ValueN _, nameof(PayLT)) => PayLT(),
                (ValueN _, nameof(LastPayLT)) => LastPayLT(),
                (ValueN n, _) => throw new Exception($"unrecognized named value {n}"),
                (ValueC c, nameof(TriggerCount)) => (TriggerCount(c), true),
                (ValueC c, nameof(LvLT)) => LvLT(c),
                (ValueC c, _) => throw new Exception($"unrecognized complex value {c}"),
                _ => throw new Exception($"unrecognized value expression {e}"),
            };
        }
        
        public static long LT(long ts_) {
            var d = Game.TimeOf(ts_);
            var rH = Game.Manager.configMan.globalConfig.RequireTypeUtcClock;
            d = Game.NextTimeOfDay(d, hour_:rH, offset_: -1);
            return (Game.TimestampNow() - Game.Timestamp(d)) / (3600 * 24);
        }
        public static float Lv() => Game.Manager.mergeLevelMan.displayLevel;
        public static float LT() => LT(Game.Manager.accountMan.createAt);
        public static float AD() {
            return Game.Manager.accountMan.playDayUTC10;
        }
        public static float UTC() => Game.TimestampNow() / 3600 % 24;
        public static float IAPTotal() => Game.Manager.iap.TotalIAPServer;

        public float TriggerCount(ValueC c) {
            var id = (int)ValueR(c.child, 1);
            return Target != null && Target.info.TryGetValue(id, out var info) ? info.count : 0;
        }

        public static (float, bool) LvLT() => LvLT((int)Lv());
        public static (float, bool) LvLT(ValueC c) => LvLT((int)ValueR(c.child, 1));
        public static (float, bool) LvLT(int lv) {
            var lvM = Game.Manager.mergeLevelMan;
            long ts;
            if (lvM == null || lvM.displayLevel < lv || (ts = lvM.RecordOf(lv)) <= 0) return (-1, false);
            return (LT(ts), true);
        }

        public static (float, bool) PayLT() {
            var iap = Game.Manager.iap;
            long ts;
            if (iap == null || (ts = iap.FirstPayTS) <= 0) return (-1, false);
            return (LT(ts), true);
        }

        public static (float, bool) LastPayLT() {
            var iap = Game.Manager.iap;
            long ts;
            if (iap == null || (ts = iap.LastPayTS) <= 0) return (-1, false);
            return (LT(ts), true);
        }

        public static (float, bool) LoginLv() => (Game.Manager.archiveMan.LoginLevel, true);
        public static (float, bool) LastLoginLT() => (Game.Manager.archiveMan.OfflineDays, true);
        // 正在升级
        public static (float, bool) LvUp() => (Game.Manager.mergeLevelMan.level, Game.Manager.mergeLevelMan.isLevelUp);

        public bool Ready(Expr e) {
            if ((e.token.Contains(nameof(IAPTotal)) || e.token.Contains(nameof(PayLT)))
                && !Game.Manager.iap.DataReady) return false;
            return true;
        }

        public static ActivityTrigger.Trigger TryAccess(ActivityTrigger t, int id) {
            if (t == null || !t.info.TryGetValue(id, out var info)) {
                throw new Exception($"trigger {id} not found");
            }
            return info;
        }

        public bool TriggerState(Node n) {
            static (int, int) L(ValueC c)
                => ((int)ValueR(c.child, 1), (int)ValueO(c.child, 2, 0));
            static int R(ValueN n)
                => n.v switch {
                    nameof(Waiting) => (int)Waiting,
                    nameof(Pending) => (int)Pending,
                    nameof(Activating) => (int)Activating,
                    nameof(Failed) => (int)Failed,
                    nameof(Succeed) => (int)Succeed,
                    _ => throw new Exception($"unknown {nameof(TriggerState)} {n.v}"),
                };
            static bool Match(ActivityTrigger t, (int, int) l, int r) {
                var (id, ts) = l;
                var info = TryAccess(t, id);
                return (int)info.state == r && info.ts + ts <= Game.TimestampNow();
            }
            if(!(n.child.Count == 2 && n.child[0] is ValueC l && n.child[1] is ValueN r)) {
                throw new Exception($"node format mismatch: {n}");
            }
            return Match(Target, L(l), R(r));
        }

        public bool TriggerExpired(Node n) {
            static (int, int) L(ValueC c)
                => ((int)ValueR(c.child, 1), (int)ValueO(c.child, 2, 0));
            static bool R(ValueN n)
                => n.v switch {
                    "TRUE" => true,
                    "FALSE" => false,
                    _ => throw new Exception($"unknown {nameof(TriggerExpired)} value {n.v}"),
                };
            static bool Match(ActivityTrigger t, (int, int) l, bool r) {
                var (id, ts) = l; 
                var info = TryAccess(t, id);
                return info.ConfEnd + ts <= Game.TimestampNow();
            }
            if(!(n.child.Count == 2 && n.child[0] is ValueC l && n.child[1] is ValueN r)) {
                throw new Exception($"node format mismatch: {n}");
            }
            return Match(Target, L(l), R(r));
        }
    }
}