using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace kmty.geom.d2.delaunay {
    using DN = DelaunayGraphNode2D;
    using VN = VoronoiGraphNode2D;
    using UR = UnityEngine.Random;
    using SG = Segment;
    using V2 = Vector2;
    using f2 = float2;

    public class DelaunayGraphNode2D {
        public Triangle triangle;
        public List<DN> neighbor;
        public bool Contains(SG s) => triangle.Contains(s);

        public DelaunayGraphNode2D(SG e, V2 c) : this(e.a, e.b, c) { }
        public DelaunayGraphNode2D(V2 a, V2 b, V2 c) {
            triangle = new Triangle(a, b, c);
            neighbor = new List<DN>();
        }

        public static (SG s1, SG s2, SG s3, DN n1, DN n2, DN n3) Split(DN n, V2 p) {
            var t = n.triangle;
            if (t.Includes(p, false)) {
                var ab = new DN(t.a, t.b, p);
                var bc = new DN(t.b, t.c, p);
                var ca = new DN(t.c, t.a, p);
                ab.neighbor = new List<DN> { bc, ca };
                bc.neighbor = new List<DN> { ca, ab };
                ca.neighbor = new List<DN> { ab, bc };
                n.SetNeighbors(ab, t.a, t.b);
                n.SetNeighbors(bc, t.b, t.c);
                n.SetNeighbors(ca, t.c, t.a);
                return (new SG(t.a, t.b), new SG(t.b, t.c), new SG(t.c, t.a), ab, bc, ca);
            } else {
                throw new Exception("degenerated case like points on edge");
            }
        }

        public static (SG s1, SG s2, SG s3, SG s4, DN n1, DN n2) Flip(DN curr, DN pair, SG prv, V2 p_curr, V2 p_pair) {
            var alt = new SG(p_curr, p_pair);
            var na = new DN(alt, prv.a);
            var nb = new DN(alt, prv.b);

            na.neighbor.Add(nb);
            nb.neighbor.Add(na);
            var s1 = new SG(prv.a, p_curr);
            var s2 = new SG(prv.a, p_pair);
            var s3 = new SG(prv.b, p_curr);
            var s4 = new SG(prv.b, p_pair);
            na.SetNeighborWhenFlip(s1, curr);
            na.SetNeighborWhenFlip(s2, pair);
            nb.SetNeighborWhenFlip(s3, curr);
            nb.SetNeighborWhenFlip(s4, pair);
            return (s1, s2, s3, s4, na, nb);
        }

        void SetNeighborWhenFlip(SG e, DN trgt) {
            var pair = trgt.GetFacingNode(e);
            if (pair != null) {
                this.neighbor.Add(pair);
                pair.ReplaceFacingNode(e, this);
            }
        }

        void SetNeighbors(DN trgt, V2 p1, V2 p2) {
            var edge = new SG(p1, p2);
            var pair = GetFacingNode(edge);
            if (pair != null) {
                trgt.neighbor.Add(pair);
                pair.ReplaceFacingNode(edge, trgt);
            }
        }

        void ReplaceFacingNode(SG e, DN replacer) {
            if (!replacer.Contains(e)) return;
            neighbor = neighbor.Select(n => n.Contains(e) ? replacer : n).ToList();
        }

        DN GetFacingNode(SG e) {
            if (!Contains(e)) return null;
            return neighbor.Find(n => n.Contains(e));
        }
    }

    public class BistellarFlip2D {
        protected Stack<SG> stack;
        protected List<DN> nodes;
        public List<DN> Nodes => nodes;

        public BistellarFlip2D(int num) {
            stack = new Stack<SG>();
            nodes = new List<DN> { new DN(V2.zero, new V2(2, 0), new V2(0, 2)) };
            for (var i = 0; i < num; i++) { Split(new V2(UR.value, UR.value)); Leagalize(); }
        }

        void Split(f2 p) {
            var n = nodes.Find(_t => _t.triangle.Includes(p, true));
            var o = DN.Split(n, p);
            nodes.Remove(n);
            nodes.Add(o.n1);
            nodes.Add(o.n2);
            nodes.Add(o.n3);
            stack.Push(o.s1);
            stack.Push(o.s2);
            stack.Push(o.s3);
        }

        void Leagalize() {
            while (stack.Count > 0) {
                var s = stack.Pop();
                if (FindNodes(s, out DN n1, out DN n2)) {
                    var p1 = n1.triangle.RemainingPoint(s);
                    var p2 = n2.triangle.RemainingPoint(s);
                    if (n1.triangle.GetCircumscribledCircle().Contains(p2)) {
                        var o = DN.Flip(n1, n2, s, p1, p2);
                        stack.Push(o.s1);
                        stack.Push(o.s2);
                        stack.Push(o.s3);
                        stack.Push(o.s4);
                        nodes.Remove(n1);
                        nodes.Remove(n2);
                        nodes.Add(o.n1);
                        nodes.Add(o.n2);
                    }
                }
            }
        }

        bool FindNodes(SG s, out DN n1, out DN n2) {
            var o = nodes.FindAll(n => n.Contains(s));
            if (o.Count == 2) { n1 = o[0]; n2 = o[1]; return true; } 
            else              { n1 = n2 = default;   return false; }
        }
    }

    public class VoronoiGraphNode2D {
        public f2 center;
        public List<SG> segments;
        public Mesh mesh;
        public VoronoiGraphNode2D(f2 c) {
            this.center = c;
            this.segments = new List<SG>();
        }

        public void Meshilify() {
            var l = segments.Count * 3;
            var vtcs = new List<Vector3>();
            var tris = Enumerable.Range(0, l).ToArray();
            segments.ForEach(s => {
                var c = (V2)center;
                var a = (V2)s.a;
                var b = (V2)s.b;
                var f = Vector3.Cross(a - c, b - a).z > 0;
                vtcs.Add(c);
                if (f) { vtcs.Add(b); vtcs.Add(a); }
                else   { vtcs.Add(a); vtcs.Add(b); }
            });
            if (vtcs.Count != l) Debug.Log(vtcs.Count);
            mesh = new Mesh();
            mesh.vertices = vtcs.ToArray();
            mesh.triangles = tris;
        }
    }

    public class VoronoiGraph2D {
        public Dictionary<f2, VN> nodes;

        public VoronoiGraph2D(DN[] dns) {
            nodes = new Dictionary<f2, VN>();

            foreach (var d in dns) {
                var t0 = d.triangle;
                var c0 = t0.GetCircumscribledCircle().center;
                if (!nodes.ContainsKey(t0.a)) nodes.Add(t0.a, new VN(t0.a));
                if (!nodes.ContainsKey(t0.b)) nodes.Add(t0.b, new VN(t0.b));
                if (!nodes.ContainsKey(t0.c)) nodes.Add(t0.c, new VN(t0.c));
                d.neighbor.ForEach(n => {
                    var t1 = n.triangle;
                    var c1 = t1.GetCircumscribledCircle().center;
                    var v1 = c1 - c0;
                    var th = 1e-5d;
                    if (abs(dot(t0.b - t0.a, v1)) < th || abs(dot(t0.c - t0.a, v1)) < th) { nodes.TryGetValue(t0.a, out VN v); v?.segments.Add(new SG(c0, c1)); }
                    if (abs(dot(t0.c - t0.b, v1)) < th || abs(dot(t0.a - t0.b, v1)) < th) { nodes.TryGetValue(t0.b, out VN v); v?.segments.Add(new SG(c0, c1)); }
                    if (abs(dot(t0.a - t0.c, v1)) < th || abs(dot(t0.b - t0.c, v1)) < th) { nodes.TryGetValue(t0.c, out VN v); v?.segments.Add(new SG(c0, c1)); }
                });
            }
        }
    }
}
