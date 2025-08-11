using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FAT
{
    public interface IConditionEvaluate {
        public bool TryEval(ConditionExpression o, ConditionExpression.Node n, out bool r);
        public (float, bool) Value(ConditionExpression.IExpr e, string t);
    }

    public partial class ConditionExpression {
        public (bool, bool) Evaluate(IConditionEvaluate m, Expr n) => Evaluate(m, n.root);
        public (bool, bool) Evaluate(IConditionEvaluate m, Node n) {
            try {
                var r = Eval(m, n);
                return (r, true);
            }
            catch (Exception e) {
                Debug.LogError($"faild to evaluate:{n}\n{e.Message}\n{e.StackTrace}");
                return (false, false);
            }
        }

        private bool Eval(IConditionEvaluate m, Node n) {
            static bool EvalAnd(ConditionExpression o, IConditionEvaluate m, Node e) {
                var v = e.child.Count > 0;
                for(var c = 0; c < e.child.Count; ++c) {
                    v = v && e.child[c] is Node n && o.Eval(m, n);
                    if (!v) return v;
                }
                return v;
            }
            static bool EvalOr(ConditionExpression o, IConditionEvaluate m, Node e) {
                var v = false;
                for(var c = 0; c < e.child.Count; ++c) {
                    v = v || (e.child[c] is Node n && o.Eval(m, n));
                    if (v) return v;
                }
                return v;
            }
            return n.op switch {
                OpAnd => EvalAnd(this, m, n),
                OpOr => EvalOr(this, m, n),
                OpNot => n.child.Count > 0 && n.child[0] is Node nn && !Eval(m, nn),
                0 when n.Empty => false,
                _ when m.TryEval(this, n, out var r) => r,
                _ when n.child.Count != 2 => throw new Exception($"element to compare count invalid {n.child.Count}"),
                OpGT => Value(m, n, 0, out var l) && Value(m, n, 1, out var r) && l > r,
                OpGE => Value(m, n, 0, out var l) && Value(m, n, 1, out var r) && l >= r,
                OpLT => Value(m, n, 0, out var l) && Value(m, n, 1, out var r) && l < r,
                OpLE => Value(m, n, 0, out var l) && Value(m, n, 1, out var r) && l <= r,
                OpEQ => Value(m, n, 0, out var l) && Value(m, n, 1, out var r) && l == r,
                _ => throw new Exception($"unknown condition op {OpSymbol(n.op)} {n.op}"),
            };
        }

        public static bool Op(int op, float l, float r) {
            return op switch {
                OpGT => l > r,
                OpGE => l >= r,
                OpLT => l < r,
                OpLE => l <= r,
                OpEQ => l == r,
                _ => throw new Exception($"unrecognized op {OpSymbol(op)} {op}"),
            };
        }

        public static bool Value(IConditionEvaluate m, Node n, int i, out float v) {
            bool r;
            (v, r) = m.Value(n.child[i], n.Type);
            return r;
        }

        public static float ValueR(IList<IExpr> l, int i) {
            return ((ValueS)l[i]).v;
        }

        public static float ValueO(IList<IExpr> l, int i, float d_) {
            return i >= 0 && i < l.Count && l[i] is ValueS s ? s.v : d_;
        }
    }
}