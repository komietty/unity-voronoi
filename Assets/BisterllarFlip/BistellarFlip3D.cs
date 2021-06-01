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
    using SG = Segment;
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

        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2, DN n3) Flip23(DN n1, DN n2, d3 p1, d3 p2, TR t) {
            DN nab = new DN(p1, p2, t.a, t.b);
            DN nbc = new DN(p1, p2, t.b, t.c);
            DN nca = new DN(p1, p2, t.c, t.a);
            nab.neighbor = new List<DN> { nbc, nca };
            nbc.neighbor = new List<DN> { nca, nab };
            nca.neighbor = new List<DN> { nab, nbc };
            TR t_ab_p1 = new TR(t.a, t.b, p1); n1.SetNeighbor(nab, t_ab_p1);
            TR t_ab_p2 = new TR(t.a, t.b, p2); n2.SetNeighbor(nab, t_ab_p2);
            TR t_bc_p1 = new TR(t.b, t.c, p1); n1.SetNeighbor(nbc, t_bc_p1);
            TR t_bc_p2 = new TR(t.b, t.c, p2); n2.SetNeighbor(nbc, t_bc_p2);
            TR t_ca_p1 = new TR(t.c, t.a, p1); n1.SetNeighbor(nca, t_ca_p1);
            TR t_ca_p2 = new TR(t.c, t.a, p2); n2.SetNeighbor(nca, t_ca_p2);
            return (t_ab_p1, t_ab_p2, t_bc_p1, t_bc_p2, t_ca_p1, t_ca_p2, nab, nbc, nca);
        }

        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2) Flip32(DN n1, DN n2, DN n3, TR t, d3 apex_x, d3 apex_y) {
            DN nx = new DN(t.a, t.b, t.c, apex_x);
            DN ny = new DN(t.a, t.b, t.c, apex_y);
            nx.neighbor = new List<DN> { ny };
            ny.neighbor = new List<DN> { nx };

            TR xab = new TR(apex_x, t.a, t.b);
            TR yab = new TR(apex_y, t.a, t.b);
            TR xbc = new TR(apex_x, t.b, t.c);
            TR ybc = new TR(apex_y, t.b, t.c);
            TR xca = new TR(apex_x, t.c, t.a);
            TR yca = new TR(apex_y, t.c, t.a);

            n1.SetNeighbor(nx, xab); n1.SetNeighbor(nx, xbc); n1.SetNeighbor(nx, xca);
            n2.SetNeighbor(nx, xab); n2.SetNeighbor(nx, xbc); n2.SetNeighbor(nx, xca);
            n3.SetNeighbor(nx, xab); n3.SetNeighbor(nx, xbc); n3.SetNeighbor(nx, xca);

            n1.SetNeighbor(ny, yab); n1.SetNeighbor(ny, ybc); n1.SetNeighbor(ny, yca);
            n2.SetNeighbor(ny, yab); n2.SetNeighbor(ny, ybc); n2.SetNeighbor(ny, yca);
            n3.SetNeighbor(ny, yab); n3.SetNeighbor(ny, ybc); n3.SetNeighbor(ny, yca);

            return (xab, xbc, xca, yab, ybc, yca, nx, ny);
        }

        void SetNeighbor(DN n, TR t) {
            var pair = GetFacingNode(t);
            if (pair != null) {
                if (n.neighbor.Count > 4) Debug.LogWarning("over 4!");
                n.neighbor.Add(pair);
                pair.ReplaceFacingNode(t, n);
            }
        }

        void ReplaceFacingNode(TR t, DN replacer) {
            if (!replacer.HasFacet(t)) return;
            neighbor = neighbor.Select(n => n.HasFacet(t) ? replacer : n).ToList();
        }

        public DN GetFacingNode(TR t) {
            if (!HasFacet(t)) return null;
            return neighbor.Find(n => n.HasFacet(t));
        }
    }


    public class Voronoi3D {
        public DN[] delaunaies;
        public List<SG> segments;

        public Voronoi3D(DN[] delaunaies) {
            this.delaunaies = delaunaies;
            segments = new List<SG>();
            foreach (var d in delaunaies) {
                var c0 = d.tetrahedra.circumscribedSphere.center;
                d.neighbor.ForEach(n => segments.Add(new SG(c0, n.tetrahedra.circumscribedSphere.center)));
            }
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
                var t = stack.Pop();
                if (FindNodes(t, out DN n1, out DN n2)) {
                    var p1 = n1.tetrahedra.RemainingPoint(t);
                    var p2 = n2.tetrahedra.RemainingPoint(t);
                    if (n1.tetrahedra.circumscribedSphere.Contains(p2) ||
                        n2.tetrahedra.circumscribedSphere.Contains(p1)) {
                        if (t.Intersects(new SG(p1, p2), out d3 i, out bool isOnEdge)) {
                            var o = DN.Flip23(n1, n2, p1, p2, t);
                            stack.Push(o.t1);
                            stack.Push(o.t2);
                            stack.Push(o.t3);
                            stack.Push(o.t4);
                            stack.Push(o.t5);
                            stack.Push(o.t6);
                            nodes.Remove(n1);
                            nodes.Remove(n2);
                            nodes.Add(o.n1);
                            nodes.Add(o.n2);
                            nodes.Add(o.n3);
                        } else if (isOnEdge) { Debug.LogWarning("point is on edge");
                        } else {
                            d3 far;
                            if      (Util3D.IsIntersecting(new SG(i, t.a), new SG(t.b, t.c), 1e-15d)) far = t.a;
                            else if (Util3D.IsIntersecting(new SG(i, t.b), new SG(t.c, t.a), 1e-15d)) far = t.b;
                            else if (Util3D.IsIntersecting(new SG(i, t.c), new SG(t.a, t.b), 1e-15d)) far = t.c;
                            else    throw new Exception();

                            var cm = t.Remaining(far);
                            var t3 = new TR(cm, p1); 
                            var n3 = n1.GetFacingNode(t3);

                            if (!Equals(n3.tetrahedra.RemainingPoint(t3), p2)) continue;

                            var o = DN.Flip32(n1, n2, n3, new TR(p1, p2, far), cm.a, cm.b);
                            stack.Push(o.t1);
                            stack.Push(o.t2);
                            stack.Push(o.t3);
                            stack.Push(o.t4);
                            stack.Push(o.t5);
                            stack.Push(o.t6);
                            nodes.Remove(n1);
                            nodes.Remove(n2);
                            nodes.Remove(n3);
                            nodes.Add(o.n1);
                            nodes.Add(o.n2);
                        }
                    }
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