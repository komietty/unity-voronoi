using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace kmty.geom.d3.delauney_alt {
    using DN = DelaunayGraphNode3D;
    using UR = UnityEngine.Random;
    using TR = Triangle;
    using d3 = double3;

    public class DelaunayGraphNode3D {
        public Tetrahedra tetrahedra;
        public List<DN> neighbor;
        public bool Contains(d3 p, bool inclusive) => tetrahedra.Contains(p, inclusive);
        public bool HasFacet(TR t) => tetrahedra.HasFace(t);

        public DelaunayGraphNode3D(TR t, d3 d) : this(t.a, t.b, t.c, d) { }
        public DelaunayGraphNode3D(d3 a, d3 b, d3 c, d3 d) {
            tetrahedra = new Tetrahedra(a, b, c, d);
            neighbor = new List<DN>();
        }

        public (TR t1, TR t2, TR t3, TR t4, DN n1, DN n2, DN n3, DN n4) Split(d3 p) {
            var th = tetrahedra;
            (DN n, TR t) abc = (new DN(p, th.a, th.b, th.c), new TR(th.a, th.b, th.c));
            (DN n, TR t) bcd = (new DN(p, th.b, th.c, th.d), new TR(th.b, th.c, th.d));
            (DN n, TR t) cda = (new DN(p, th.c, th.d, th.a), new TR(th.c, th.d, th.a));
            (DN n, TR t) dab = (new DN(p, th.d, th.a, th.b), new TR(th.d, th.a, th.b));
            abc.n.neighbor = new List<DN> { bcd.n, cda.n, dab.n };
            bcd.n.neighbor = new List<DN> { cda.n, dab.n, abc.n };
            cda.n.neighbor = new List<DN> { dab.n, abc.n, bcd.n };
            dab.n.neighbor = new List<DN> { abc.n, bcd.n, cda.n };
            this.SetNeighbor(abc.n, abc.t);
            this.SetNeighbor(bcd.n, bcd.t);
            this.SetNeighbor(cda.n, cda.t);
            this.SetNeighbor(dab.n, dab.t);
            return (abc.t, bcd.t, cda.t, dab.t, abc.n, bcd.n, cda.n, dab.n);
        }

        public (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2, DN n3) Flip23(DN pair, TR tri, d3 p_this, d3 p_pair) {
            var nab = new DN(p_this, p_pair, tri.a, tri.b);
            var nbc = new DN(p_this, p_pair, tri.b, tri.c);
            var nca = new DN(p_this, p_pair, tri.c, tri.a);

            nab.neighbor = new List<DN> { nbc, nca };
            nbc.neighbor = new List<DN> { nca, nab };
            nca.neighbor = new List<DN> { nab, nbc };

            var t_ab_this = new TR(tri.a, tri.b, p_this);
            var t_ab_pair = new TR(tri.a, tri.b, p_pair);
            var t_bc_this = new TR(tri.b, tri.c, p_this);
            var t_bc_pair = new TR(tri.b, tri.c, p_pair);
            var t_ca_this = new TR(tri.c, tri.a, p_this);
            var t_ca_pair = new TR(tri.c, tri.a, p_pair);

            this.SetNeighbor(nab, t_ab_this);
            pair.SetNeighbor(nab, t_ab_pair);
            this.SetNeighbor(nbc, t_bc_this);
            pair.SetNeighbor(nbc, t_bc_pair);
            this.SetNeighbor(nca, t_ca_this);
            pair.SetNeighbor(nca, t_ca_pair);

            return (
                t_ab_this, t_ab_pair,
                t_bc_this, t_bc_pair,
                t_ca_this, t_ca_pair,
                nab, nbc, nca );
        }

        public (bool f, TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2) Flip32(DN pair0, TR tri, d3 p_this, d3 p_pair, d3 p_away) {
            var edge = tri.Remaining(p_away);
            var pair1 = GetFacingNode(new TR(edge.a, edge.b, p_this));
            var pair2 = GetFacingNode(new TR(edge.a, edge.b, p_pair));
            if (pair1 == null || pair2 == null) { throw new Exception(); }

            if (!Equals(pair1.tetrahedra.RemainingPoint(new TR(edge.a, edge.b, p_this)), p_pair)) {
                Debug.Log("flip32 passed!");
                return (false, default, default, default, default, default, default, default, default);
            }

            var na = new DN(p_this, p_pair, p_away, edge.a);
            var nb = new DN(p_this, p_pair, p_away, edge.b);
            na.neighbor = new List<DN> { nb };
            nb.neighbor = new List<DN> { na };

            // double check!
            var a_this_pair = new TR(edge.a, p_this, p_pair);
            var a_pair_away = new TR(edge.a, p_pair, p_away);
            var a_away_this = new TR(edge.a, p_away, p_this);
            var b_this_pair = new TR(edge.b, p_this, p_pair);
            var b_pair_away = new TR(edge.b, p_pair, p_away);
            var b_away_this = new TR(edge.b, p_away, p_this);

            this .SetNeighbor(na, a_away_this); // except pointPair => search from this
            pair0.SetNeighbor(na, a_pair_away); // except pointThis => search from pair0
            pair1.SetNeighbor(na, a_this_pair); // except pointAway => search from pair1
            this .SetNeighbor(nb, b_away_this); // except pointPair => search from this
            pair0.SetNeighbor(nb, b_pair_away); // except pointThis => search from pair0
            pair1.SetNeighbor(nb, b_this_pair); // except pointAway => search from pair1

            return (
                true,
                a_this_pair, a_pair_away, a_away_this,
                b_this_pair, b_pair_away, b_away_this,
                na, nb);
        }

        void SetNeighbor(DN n, TR t) {
            var pair = GetFacingNode(t);
            if (pair != null) {
                n.neighbor.Add(pair);
                pair.ReplaceFacingNode(t, n);
            }
        }

        void ReplaceFacingNode(TR t, DN replacer) {
            if (!replacer.HasFacet(t)) return;
            neighbor = neighbor.Select(n => n.HasFacet(t) ? replacer : n).ToList();
        }

        DN GetFacingNode(TR t) {
            if (!HasFacet(t)) return null;
            return neighbor.Find(n => n.HasFacet(t));
        }
    }
    public class BistellarFlip3D {
        protected Stack<TR> stack;
        protected List<DN> nodes;
        public List<DN> Nodes => nodes;

        public BistellarFlip3D(int num) {
            stack = new Stack<TR>();
            nodes = new List<DN> { new DN(new d3(0, 0, 0), new d3(3, 0, 0), new d3(0, 3, 0), new d3(0, 0, 3)) };
            for (var i = 0; i < num; i++) { Split(new d3(UR.value, UR.value, UR.value)); Leagalize(); }
        }

        void Split(d3 p) { 
            var n = nodes.Find(_t => _t.tetrahedra.Contains(p, true));
            var o = n.Split(p);
            nodes.Remove(n);
            nodes.Add(o.n1);
            nodes.Add(o.n2);
            nodes.Add(o.n3);
            nodes.Add(o.n4);
            stack.Push(o.t1);
            stack.Push(o.t2);
            stack.Push(o.t3);
            stack.Push(o.t4);
        }

        void Leagalize() { 
            while (stack.Count > 0) {
                var s = stack.Pop();
                if (FindNodes(s, out DN n1, out DN n2)) {
                }
            }
        }


        bool FindNodes(TR t, out DN n1, out DN n2) {
            var o = nodes.FindAll(n => n.tetrahedra.HasFace(t));
            if (o.Count == 2) { n1 = o[0]; n2 = o[1]; return true; } 
            else              { n1 = n2 = default;   return false; }
        }

    }
}
