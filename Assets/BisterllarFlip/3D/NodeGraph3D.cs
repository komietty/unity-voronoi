using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using System.Linq;
using Unity.Mathematics;

namespace old {
    using DN = DelaunayGraphNode3D;
    public class DelaunayGraphNode3D {
        public Tetrahedra tetrahedra;
        public List<DN> children;
        public List<DN> neighbor;
        public bool Contains(double3 p, bool includeOnFacet) => tetrahedra.Contains(p, includeOnFacet);
        public bool HasFacet(Triangle t) => tetrahedra.HasFace(t);

        public DelaunayGraphNode3D(Triangle t, double3 d) : this(t.a, t.b, t.c, d) { }
        public DelaunayGraphNode3D(double3 a, double3 b, double3 c, double3 d) {
            tetrahedra = new Tetrahedra(a, b, c, d);
            children = new List<DN>();
            neighbor = new List<DN>();
        }

        public void Split(double3 p) {
            var a = tetrahedra.a;
            var b = tetrahedra.b;
            var c = tetrahedra.c;
            var d = tetrahedra.d;
            var p_abc = new DN(p, a, b, c);
            var p_bcd = new DN(p, b, c, d);
            var p_cda = new DN(p, c, d, a);
            var p_dab = new DN(p, d, a, b);
            p_abc.neighbor = new List<DN> { p_bcd, p_cda, p_dab };
            p_bcd.neighbor = new List<DN> { p_cda, p_dab, p_abc };
            p_cda.neighbor = new List<DN> { p_dab, p_abc, p_bcd };
            p_dab.neighbor = new List<DN> { p_abc, p_bcd, p_cda };
            SetNeighbors(p_abc, new Triangle(a, b, c));
            SetNeighbors(p_bcd, new Triangle(b, c, d));
            SetNeighbors(p_cda, new Triangle(c, d, a));
            SetNeighbors(p_dab, new Triangle(d, a, b));
            children = new List<DN> { p_abc, p_bcd, p_cda, p_dab };
        }

        void SetNeighbors(DN tgt, Triangle t) {
            var pair = GetFacingNode(t);
            if (pair != null) {
                tgt.neighbor.Add(pair);
                neighbor.ForEach(n => { if (n.HasFacet(t)) n.SetFacingNode(t, tgt); });
            }
        }

        public void Flip23(DN pair, Triangle t, double3 p_this, double3 p_pair) {
            var na = new DN(p_this, p_pair, t.a, t.b);
            var nb = new DN(p_this, p_pair, t.b, t.c);
            var nc = new DN(p_this, p_pair, t.c, t.a);

            na.neighbor = new List<DN> { nb, nc };
            nb.neighbor = new List<DN> { nc, na };
            nc.neighbor = new List<DN> { na, nb };

            na.SetNeighborWhenFlip(new Triangle(t.a, t.b, p_this), this);
            na.SetNeighborWhenFlip(new Triangle(t.a, t.b, p_pair), pair);
            nb.SetNeighborWhenFlip(new Triangle(t.b, t.c, p_this), this);
            nb.SetNeighborWhenFlip(new Triangle(t.b, t.c, p_pair), pair);
            nc.SetNeighborWhenFlip(new Triangle(t.c, t.a, p_this), this);
            nc.SetNeighborWhenFlip(new Triangle(t.c, t.a, p_pair), pair);

            this.children = new List<DN> { na, nb, nc };
            pair.children = new List<DN> { na, nb, nc };
        }


        public void Flip32(DN pair0, Triangle t, double3 p_this, double3 p_pair, double3 p_away) {
            var edge = t.Remaining(p_away);
            // pair1 and pair2 must be existed!
            var pair1 = GetFacingNode(new Triangle(edge.a, edge.b, p_this));
            var pair2 = GetFacingNode(new Triangle(edge.a, edge.b, p_pair));

            if (pair1 == null) {
                this.neighbor.ForEach(n => n.tetrahedra.Log());
                Debug.Log(pair2);
                throw new System.Exception();
            }

            if (!Equals(pair1.tetrahedra.RemainingPoint(new Triangle(edge.a, edge.b, p_this)), p_pair)) {
                Debug.Log("flip32 passed!");
                return;
            }

            var na = new DN(p_this, p_pair, p_away, edge.a);
            var nb = new DN(p_this, p_pair, p_away, edge.b);
            na.neighbor = new List<DN> { nb };
            nb.neighbor = new List<DN> { na };

            na.SetFacingNode(new Triangle(edge.a, p_this, p_away), this);  // except pointPair => search from this
            na.SetFacingNode(new Triangle(edge.a, p_pair, p_away), pair0); // except pointThis => search from pair0
            na.SetFacingNode(new Triangle(edge.a, p_this, p_pair), pair1); // except pointAway => search from pair1
            nb.SetFacingNode(new Triangle(edge.b, p_this, p_away), this);  // except pointPair => search from this
            nb.SetFacingNode(new Triangle(edge.b, p_pair, p_away), pair0); // except pointThis => search from pair0
            nb.SetFacingNode(new Triangle(edge.b, p_this, p_pair), pair1); // except pointAway => search from pair1

            this.children = new List<DN> { na, nb };
            pair0.children = new List<DN> { na, nb };
            pair1.children = new List<DN> { na, nb };
        }

        void SetNeighborWhenFlip(Triangle t, DN _this) {
            var _pair = _this.GetFacingNode(t);
            if (_pair != null) {
                this.neighbor.Add(_pair);
                _pair.SetFacingNode(t, this);
            }
        }

        // is this really correct??? look up 2d version
        public void SetFacingNode(Triangle t, DN node) {
            if (!node.HasFacet(t)) return;
            neighbor = node.neighbor.Select(n => n.HasFacet(t) ? node : n).ToList();
        }

        public DN GetFacingNode(Triangle t) {
            if (!HasFacet(t)) return null;
            return neighbor.Find(n => n.HasFacet(t));
        }
    }

    public class Voronoi3D {
        public DN[] delaunaies;
        public List<Segment> segments;

        public Voronoi3D(DN[] delaunaies) {
            this.delaunaies = delaunaies;
            segments = new List<Segment>();
            foreach (var d in delaunaies) {
                var c0 = d.tetrahedra.circumscribedSphere.center;
                d.neighbor.ForEach(n => segments.Add(new Segment(c0, n.tetrahedra.circumscribedSphere.center)));
            }
        }
    }
}
