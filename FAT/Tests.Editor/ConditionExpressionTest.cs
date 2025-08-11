using UnityEngine;
using FAT;
using System.Text;
using NUnit.Framework;

namespace Tests {
    using static ConditionExpression;

    public class ConditionExpressionTest {
        [Test]
        public void AllInOne() {
            var cond = new ConditionExpression();
            var eval = new EvaluateEventTrigger() { target = new() };
            var info = eval.target.info;
            var builder = new StringBuilder();
            Expr Match(string str, string expectE) {
                Debug.Log($"input:{str}");
                builder.Clear();
                var (expr, valid) = cond.Parse(str);
                Assert.True(valid, $"invalid input: {str}");
                cond.Log(expr.root, builder, 0);
                var v = builder.ToString();
                Assert.AreEqual(expectE, v);
                return expr;
            }
            void Set(string k, float v) {
                eval.cache[k] = v;
            }
            void Value(Expr n, bool expect) {
                var (r, valid) = cond.Evaluate(eval, n);
                Assert.True(valid, $"value evalute failed");
                Assert.AreEqual(expect, r);
            }
            Expr n;
            n = Match("", "[? ]");
            Value(n, false);
            n = Match("Lv>=10",
                    "[>= Lv 10]");
            Set("Lv", 9); Value(n, false);
            Set("Lv", 10); Value(n, true);
            n = Match("Lv>=10 and Lv<=20",
                    "[&& [>= Lv 10] [<= Lv 20]]");
            Set("Lv", 9); Value(n, false);
            Set("Lv", 20); Value(n, true);
            Set("Lv", 21); Value(n, false);
            n = Match("((Lv>=10 and Lv<=20) or AD>=2) and not [TriggerState,1,86400]=Succeed",
                    "[&& [|| [&& [>= Lv 10] [<= Lv 20]] [>= AD 2]] [! [= TriggerState,1,86400 Succeed]]]");
            info[1] = new();
            Set("Lv",19); Set("AD", 2); Value(n, true);
            Set("AD", 1); Value(n, true);
            Set("Lv", 9); Value(n, false);
            n = Match("(Lv>=10 and Lv<=20 and AD>=2)",
                    "[&& [>= Lv 10] [<= Lv 20] [>= AD 2]]");
            n = Match("(Lv>=10 and Lv<=20) or AD>=2",
                    "[|| [&& [>= Lv 10] [<= Lv 20]] [>= AD 2]]");
            n = Match("Lv>=10 and Lv<=20 or AD>=2",
                    "[&& [>= Lv 10] [|| [<= Lv 20] [>= AD 2]]]");
            n = Match("Lv>=10 and (Lv<=20 or AD>=2)",
                    "[&& [>= Lv 10] [|| [<= Lv 20] [>= AD 2]]]");
            n = Match("PayLT >= 0",
                    "[>= PayLT 0]");
            var iap = Game.Manager.iap;
            Value(n, (iap?.FirstPayTS ?? 0) > 0);
            eval.cache.Clear();
        }
    }
}