using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FAT
{
    using static StringComparison;

    public partial class ConditionExpression {
        public const int TraverseLimit = 16;
        public const int OpAnd = 1;
        public const int OpOr = 2;
        public const int OpNot = 3;
        public const int OpGT = 10;
        public const int OpLT = 11;
        public const int OpEQ = 100;
        public const int OpGE = OpGT + OpEQ;
        public const int OpLE = OpLT + OpEQ;

        public abstract class IExpr {
            public abstract string Type { get; }
            public IExpr parent;
            public virtual void Push(IExpr v) { Debug.LogError($"push not supported on {this}"); }
            public virtual void Remove(IExpr v) { Debug.LogError($"remove not supported on {this}"); }
        }

        public class ValueS : IExpr {
            public override string Type => nameof(Single);
            public float v;
            public override string ToString() => $"{v}";
        }

        public class ValueN : IExpr {
            public override string Type => v;
            public string v;
            public override string ToString() => v;
        }

        public class ValueC : IExpr {
            public override string Type => child.Count > 0 ? child[0].Type : null;
            public List<IExpr> child = new();
            public override void Push(IExpr v) => child.Add(v);
            public override string ToString() {
                var b = new StringBuilder();
                foreach(var e in child) {
                    b.Append(e).Append(',');
                }
                --b.Length;
                return b.ToString();
            }
        }

        public class Node : IExpr {
            public override string Type => child != null && child.Count > 0 ? child[0].Type : null;
            public int op;
            public List<IExpr> child;
            public bool Empty => parent == null && child == null;
            public override void Push(IExpr v) => (child ??= new()).Add(v);
            public override void Remove(IExpr v) => child.Remove(v);
            public override string ToString() {
                var b = new StringBuilder();
                b.Append('[');
                b.Append(OpSymbol(op));
                foreach(var e in child) {
                    b.Append(' ').Append(e);
                }
                b.Append(']');
                return b.ToString();
            }
        }

        public class Expr {
            public Node root;
            public HashSet<string> token = new();
        }

        public static string OpSymbol(int op)
            => op switch {
                OpAnd => "&&",
                OpOr => "||",
                OpNot => "!",
                OpGT => ">",
                OpGE => ">=",
                OpLT => "<",
                OpLE => "<=",
                OpEQ => "=",
                _ => "?"
            };

        public (Expr, bool) Parse(ReadOnlySpan<char> expr_) {
            try {
                var n = ParseExpr(expr_);
                return (n, true);
            }
            catch (Exception e) {
                Debug.LogError($"failed to parse:{expr_.ToString()}\n{e.Message}\n{e.StackTrace}");
                return (null, false);
            }
        }
        private Expr ParseExpr(ReadOnlySpan<char> expr_) {
            static void Push(ref IExpr e, IExpr ee = null) {
                ee ??= new Node();
                ee.parent = e;
                e.Push(ee);
                e = ee;
            }
            static void Pop(ref IExpr e) {
                if (e.parent == null) throw new Exception("won't pop root node");
                e = e.parent;
            }
            static void Encase(ref IExpr e) {
                IExpr nn = new Node();
                Push(ref nn, e);
                e = e.parent;
            }
            static void Insert(ref IExpr e) {
                var ee = e.parent ?? throw new Exception("won't insert at root node");
                ee.Remove(e);
                Push(ref ee);
                Push(ref ee, e);
                e = e.parent;
            }
            static void Write(char c, Span<char> buffer, ref int b) {
                buffer[b] = c;
                ++b;
            }
            static void Op(IExpr e, int op) {
                if (e is not Node n) throw new Exception($"op not supported on {e}");
                n.op = op;
            }
            static void OpMod(IExpr e, int op) {
                if (e is not Node n) throw new Exception($"op not supported on {e}");
                n.op += op;
            }
            static void Op2(ref IExpr e, int op) {
                var ee = e.parent;
                switch(ee) {
                    case null: Encase(ref e); break;
                    case Node n when n.op == 0 || n.op == op: e = ee; break;
                    case Node: Insert(ref e); break;
                    default: throw new Exception($"op not supported on {e}");
                }
                Op(e, op);
                Push(ref e);
            }
            static void Op1(ref IExpr e, int op) {
                Op(e, op); Push(ref e);
            }
            Span<char> buffer = stackalloc char[256];
            var b = 0;
            IExpr n = new Node();
            var expr = new Expr();
            var token = expr.token;
            void Read(ReadOnlySpan<char> buffer) {
                if (b == 0) return;
                var s = buffer[..b];
                b = 0;
                switch(s) {
                    case var _ when s.Equals("and", Ordinal): Op2(ref n, OpAnd); break;
                    case var _ when s.Equals("or", Ordinal): Op2(ref n, OpOr); break;
                    case var _ when s.Equals("not", Ordinal): Op1(ref n, OpNot); break;
                    default: {
                        switch(s) {
                            case var _ when float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vf):
                                n.Push(new ValueS { v = vf }); break;
                            default: {
                                var ss = s.ToString();
                                if (!token.Contains(ss)) token.Add(ss);
                                n.Push(new ValueN() { v = ss });
                            } break;
                        };
                    } break;
                };
            }
            for (var k = 0; k < expr_.Length; ++k) {
                var c = expr_[k];
                switch (c) {
                    case '\n':
                    case '\r':
                    case '\t':
                    case ' ': Read(buffer); break;
                    case '[': Read(buffer); Push(ref n, new ValueC()); break;
                    case '(': Read(buffer); Push(ref n); break;
                    case ']':
                    case ')': Read(buffer); Pop(ref n); break;
                    case '>': Read(buffer); Op(n, OpGT); break;
                    case '<': Read(buffer); Op(n, OpLT); break;
                    case '=': Read(buffer); OpMod(n, OpEQ); break;
                    case ',': Read(buffer); break;
                    default: Write(c, buffer, ref b); break;
                }
            }
            Read(buffer);
            var l = 0;
            while(n.parent != null && ++l < TraverseLimit) n = n.parent;
            if (l >= TraverseLimit) throw new Exception("loop in node chain");
            expr.root = (Node)n;
            return expr;
        }

        public void Log(IExpr e, StringBuilder b, int d) {
            if (e == null || d > TraverseLimit) return;
            b ??= new StringBuilder();
            switch (e) {
                case Node n:
                    b.Append('[');
                    b.Append(OpSymbol(n.op)).Append(' ');
                    ++d;
                    if (n.child != null && n.child.Count > 0) {
                        foreach(var c in n.child) {
                            Log(c, b, d);
                            b.Append(' ');
                        }
                        --b.Length;
                    }
                    b.Append(']');
                break;
                default:
                    b.Append(e.ToString());
                break;
            }
        }
    }
}