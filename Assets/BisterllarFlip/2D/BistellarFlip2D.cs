using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace kmty.geom.d2.delaunay {
    using DN = DelaunayGraphNode2D;
    using UR = UnityEngine.Random;

    public class BistellarFlip2D : BistellarFlip<DN> {
        public BistellarFlip2D(int num) {
            root = new DN(Vector2.zero, new Vector2(2, 0), new Vector2(0, 2));
            tips = new List<DN>();
            for (var i = 0; i < num; i++) Loop(new Vector2(UR.value, UR.value));
        }

        public void RefreshTips() { tips.Clear(); GetTips(root); }
        public List<DN> GetResult() { RefreshTips(); return tips; }
        void GetTips(DN n) { if (n.children.Count == 0) tips.Add(n); else n.children.ForEach(c => GetTips(c)); }

        public void Loop(float2 p) {
            RefreshTips();
            var n = tips.Find(_t => _t.triangle.Includes(p, true));
            var t = n.triangle;
            n.Split(p);

            if (t.OnEdge(p)) {
                if (cross(float3(t.b - t.a, 0), float3(p - t.b, 0)).z == 0) SplitOnEdge(n, p, new Segment(t.a, t.b), t.c);
                if (cross(float3(t.c - t.b, 0), float3(p - t.c, 0)).z == 0) SplitOnEdge(n, p, new Segment(t.b, t.c), t.a);
                if (cross(float3(t.a - t.c, 0), float3(p - t.a, 0)).z == 0) SplitOnEdge(n, p, new Segment(t.c, t.a), t.b);
            } else {
                n.children.ForEach(_n => Legalize(_n, new Segment(t.a, t.b), p));
                n.children.ForEach(_n => Legalize(_n, new Segment(t.b, t.c), p));
                n.children.ForEach(_n => Legalize(_n, new Segment(t.c, t.a), p));
            }
        }

        void Legalize(DN n, Segment s, Vector2 p) {
            var pair = n.GetFacingNode(s);
            if (pair == null || !n.triangle.ContainsSegment(s)) return;

            var apex = pair.triangle.RemainingPoint(s);
            if (n.triangle.circumscribedCircle.Contains(apex)) {
                n.Flip(pair, s, p, apex);
                n.children.ForEach(_n => Legalize(_n, new Segment(s.a, apex), p));
                n.children.ForEach(_n => Legalize(_n, new Segment(s.b, apex), p));
                n.children.ForEach(_n => Legalize(_n, new Segment(s.a, p), apex));
                n.children.ForEach(_n => Legalize(_n, new Segment(s.b, p), apex));
            }
        }

        void SplitOnEdge(DN tgt, Vector2 p, Segment s, Vector2 other) {
            var pair = tgt.GetFacingNode(s);
            var na = new DN(s.a, p, other);
            var nb = new DN(s.b, p, other);
            na.neighbor = new List<DN> { nb };
            nb.neighbor = new List<DN> { na };
            var na_pair = tgt.GetFacingNode(s.a, other);
            var nb_pair = tgt.GetFacingNode(s.b, other);
            if (na_pair != null) na.neighbor.Add(na_pair);
            if (nb_pair != null) nb.neighbor.Add(nb_pair);

            if (pair != null) {
                var pairPoint = pair.triangle.RemainingPoint(s);
                var nc = new DN(s.a, p, pairPoint);
                var nd = new DN(s.b, p, pairPoint);
                na.neighbor.Add(nc);
                nb.neighbor.Add(nd);
                nc.neighbor = new List<DN> { nd, na };
                nd.neighbor = new List<DN> { nc, nb };
                var nc_pair = pair.GetFacingNode(s.a, pairPoint);
                var nd_pair = pair.GetFacingNode(s.b, pairPoint);
                if (nc_pair != null) nc.neighbor.Add(nc_pair);
                if (nd_pair != null) nd.neighbor.Add(nd_pair);
                pair.children = new List<DN> { nc, nd };
                Legalize(nc, new Segment(s.a, pairPoint), p);
                Legalize(nd, new Segment(s.b, pairPoint), p);
            }

            tgt.children = new List<DN> { na, nb };
            Legalize(na, new Segment(s.a, other), p);
            Legalize(nb, new Segment(s.b, other), p);
        }
    }
}
