using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace kmty.geom.d3.delauney {
    using static Unity.Mathematics.math;
    using DN = DelaunayGraphNode3D;
    using VN = VoronoiGraphNode3D;
    using VF = VoronoiGraphFace3D;
    using UR = UnityEngine.Random;
    using TR = Triangle;
    using SG = Segment;
    using d3 = double3;

    public class DelaunayGraphNode3D {
        public Tetrahedra tetrahedra;
        public List<DN> neighbor;
        public bool Contains(d3 p, bool inclusive) => tetrahedra.Contains(p, inclusive);
        public bool HasFacet(TR t) => tetrahedra.HasFace(t);

        public DelaunayGraphNode3D(Tetrahedra t) : this(t.a, t.b, t.c, t.d) { }
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

        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2, DN n3)
            Flip23(DN n1, DN n2, d3 p1, d3 p2, TR t) {
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

        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2)
            Flip32(DN n1, DN n2, DN n3, d3 p31, d3 p12, d3 p23, d3 apex_x, d3 apex_y) {
            d3 a = p31;
            d3 b = p12;
            d3 c = p23;
            DN nx = new DN(a, b, c, apex_x);
            DN ny = new DN(a, b, c, apex_y);
            nx.neighbor.Add(ny);
            ny.neighbor.Add(nx);
            TR xab = new TR(apex_x, a, b);
            TR yab = new TR(apex_y, a, b);
            TR xbc = new TR(apex_x, b, c);
            TR ybc = new TR(apex_y, b, c);
            TR xca = new TR(apex_x, c, a);
            TR yca = new TR(apex_y, c, a);
            n1.SetNeighbor(nx, xab);
            n2.SetNeighbor(nx, xbc);
            n3.SetNeighbor(nx, xca);
            n1.SetNeighbor(ny, yab);
            n2.SetNeighbor(ny, ybc);
            n3.SetNeighbor(ny, yca);
            return (xab, xbc, xca, yab, ybc, yca, nx, ny);
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

        public DN GetFacingNode(TR t) {
            if (!HasFacet(t)) return null;
            return neighbor.Find(n => n.HasFacet(t));
        }
    }

    public class BistellarFlip3D {
        protected Stack<TR> stack;
        protected List<DN> nodes;
        protected Tetrahedra root;
        public List<DN> Nodes => nodes;

        public BistellarFlip3D(int num, float scl) {
            stack = new Stack<TR>();
            var s = scl * 3;
            root = new Tetrahedra(
                new d3(0, 0, 0),
                new d3(s, 0, 0),
                new d3(0, s, 0),
                new d3(0, 0, s));
            nodes = new List<DN> { new DN(root) };

            var state = UR.state;
            UR.InitState(123);
            for (var i = 0; i < num; i++) {
                Split(UR.value * scl, UR.value * scl, UR.value * scl);
                Leagalize();
            }
            UR.state = state;
        }

        void Split(float x, float y, float z) { Split(new d3(x, y, z)); }

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
                    if (n1.tetrahedra.GetCircumscribedSphere().Contains(p2)) {
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
                            else throw new System.Exception();

                            var cm = t.Remaining(far);
                            var t3 = new TR(cm, p1);
                            var n3 = n1.GetFacingNode(t3);

                            if (!Equals(n3.tetrahedra.RemainingPoint(t3), p2)) continue;

                            var p12 = far;
                            var p23 = p2;
                            var p31 = p1;
                            var o = DN.Flip32(n1, n2, n3, p31, p12, p23, cm.a, cm.b);
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
            var o = nodes.FindAll(n => n.HasFacet(t));
            if (o.Count == 2) { n1 = o[0]; n2 = o[1]; return true; }
            else              { n1 = n2 = default; return false; }
        }
    }

    public class VoronoiGraphFace3D {
        public Vector3 key;
        public List<SG> segments = new List<SG>();
        public List<d3> vrts = new List<d3>();
        public d3 nodeCenter;

        public VoronoiGraphFace3D(Vector3 key, d3 nodeCenter) {
            this.key = key;
            this.nodeCenter = nodeCenter;
        }

        public void TryAddVrts(SG s, d3 center){
            bool f1 = false;
            bool f2 = false;
            foreach (var v in vrts) {
                if(all(v == s.a - center)) f1 = true;
                if(all(v == s.b - center)) f2 = true;
            }
            if(!f1) vrts.Add(s.a - center);
            if(!f2) vrts.Add(s.b - center);
        }


        public d3[] Meshilify() {
            var v0 = vrts[0];
            var v1 = vrts[1];
            var alts = new List<d3>();

            vrts = vrts.Skip(2)
                       .OrderByDescending(v => dot(v1 - v0, normalize(v - v1)))
                       .Prepend(v1)
                       .Prepend(v0)
                       .ToList();

            for (var i = 1; i < vrts.Count; i++) {
                var va = vrts[i];
                var vb = vrts[(i + 1) % vrts.Count];
                if (dot(cross(vb - va, v0 - va), v0) > 0) {
                    alts.Add(v0);
                    alts.Add(va);
                    alts.Add(vb);
                } else {
                    alts.Add(v0);
                    alts.Add(vb);
                    alts.Add(va);
                }
            }
            return alts.ToArray();
        }

        public bool TryAddSegs(SG s) {
            if (segments.Any(i => i.EqualsIgnoreDirection(s))) return false;
            segments.Add(s);
            return true;
        }

        public d3[] Meshilify_F() {
            var bgn = segments[0];
            var end = new SG(d3.zero, new d3(1, 1, 1));
            var flg = false;
            var c = bgn.a;

            for (var j = 1; j < segments.Count; j++) {
                var s = segments[j];
                if (all(c == s.a) || all(c == s.b)) {
                    end = s;
                    flg = true;
                }
            }

            if (!flg) return new d3[0]; 

            var vts = new d3[(segments.Count - 2) * 3];
            int itr = 0;

            foreach (var s in segments) {
                if (s.Equals(end) || s.Equals(bgn)) continue;
                var va = s.a - nodeCenter;
                var vb = s.b - nodeCenter;
                var vc = c   - nodeCenter;
                var f = dot(cross(vb - va, vc - va), vc) > 0;
                vts[itr * 3 + 0] = vc;
                vts[itr * 3 + 1] = f ? va : vb;
                vts[itr * 3 + 2] = f ? vb : va;
                itr++;
            }

            return vts;
        }
/*
*/
    }

    public class VoronoiGraphNode3D {
        public d3 center;
        public Mesh mesh;
        public List<(SG segment, Vector3 pair)> segments;
        public List<VF> faces;
        public VoronoiGraphNode3D(d3 c) {
            this.center = c;
            this.segments = new List<(SG, Vector3)>();
        }
        public void Meshilify() {
            faces = segments.Select(s => s.pair)
                            .Distinct()
                            .Select(p => new VF(p, center))
                            .ToList();

            foreach (var s in segments) {
                foreach (var f in faces) {
                    //if (s.pair == f.key) f.TryAddVrts(s.segment, center);
                    if (s.pair == f.key) f.TryAddSegs(s.segment);
                }
            }
           
            var nums = 0;
            var vrts = new List<Vector3>();
            var closed = true;

            foreach (var f in faces) {
                //var vs = f.Meshilify();
                var vs = f.Meshilify_F();
                closed &= vs.Length > 0;
                nums += vs.Length;
                vrts.AddRange(vs.Select(v => (Vector3)(float3)v));
            }

            if (closed) {
                mesh = new Mesh();
                mesh.SetVertices(vrts);
                mesh.SetTriangles(Enumerable.Range(0, nums).ToArray(), 0);
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
            }
        }
    }

    public class VoronoiGraph3D {
        public Dictionary<d3, VN> nodes;

        public VoronoiGraph3D(DN[] dns) {
            nodes = new Dictionary<d3, VN>();
            foreach (var d in dns) {
                var t = d.tetrahedra;
                var c = t.GetCircumscribedSphere().center;
                if (!nodes.ContainsKey(t.a)) nodes.Add(t.a, new VN(t.a));
                if (!nodes.ContainsKey(t.b)) nodes.Add(t.b, new VN(t.b));
                if (!nodes.ContainsKey(t.c)) nodes.Add(t.c, new VN(t.c));
                if (!nodes.ContainsKey(t.d)) nodes.Add(t.d, new VN(t.d));
                d.neighbor.ForEach(n => {
                    var c1 = n.tetrahedra.GetCircumscribedSphere().center;
                    var v  = c1 - c;
                    var s  = new SG(c, c1);
                    AssignSegment(t.b, t.a, v, s);
                    AssignSegment(t.c, t.a, v, s);
                    AssignSegment(t.d, t.a, v, s);
                    AssignSegment(t.c, t.b, v, s);
                    AssignSegment(t.d, t.b, v, s);
                    AssignSegment(t.a, t.b, v, s);
                    AssignSegment(t.d, t.c, v, s);
                    AssignSegment(t.a, t.c, v, s);
                    AssignSegment(t.b, t.c, v, s);
                    AssignSegment(t.a, t.d, v, s);
                    AssignSegment(t.b, t.d, v, s);
                    AssignSegment(t.c, t.d, v, s);
                });
            }

            const double th = 1e-9d;
            void AssignSegment(d3 pair, d3 cntr, d3 v1, SG sg) {
                if (math.abs(math.dot(pair - cntr, v1)) < th) {
                    nodes.TryGetValue(cntr, out VN v);
                    v?.segments.Add((sg, (float3)pair));
                }
            }

        }
    }

}
