using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using System.Linq;
using Unity.Mathematics;
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
        if (tetrahedra.Contains(p, false)) {
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
    }

    void SetNeighbors(DN tgt, Triangle t) {
        var pair = GetFacingNode(t);
        if (pair != null) {
            tgt.neighbor.Add(pair);
            neighbor.ForEach(n => { if (n.HasFacet(t)) n.SetFacingNode(t, tgt); });
        }
    }

    public void Flip23(DN pair, Triangle t, double3 pointThis, double3 pointPair) {
        if (DataBaseUtil.CheckDuplication(new double3[] { t.a, t.b, t.c, pointThis, pointPair })) throw new System.Exception();

        var nodeA = new DN(pointThis, pointPair, t.a, t.b);
        var nodeB = new DN(pointThis, pointPair, t.b, t.c);
        var nodeC = new DN(pointThis, pointPair, t.c, t.a);

        nodeA.neighbor = new List<DN> { nodeB, nodeC };
        nodeB.neighbor = new List<DN> { nodeC, nodeA };
        nodeC.neighbor = new List<DN> { nodeA, nodeB };

        nodeA.SetNeighborWhenFlip(new Triangle(t.a, t.b, pointThis), this);
        nodeA.SetNeighborWhenFlip(new Triangle(t.a, t.b, pointPair), pair);
        nodeB.SetNeighborWhenFlip(new Triangle(t.b, t.c, pointThis), this);
        nodeB.SetNeighborWhenFlip(new Triangle(t.b, t.c, pointPair), pair);
        nodeC.SetNeighborWhenFlip(new Triangle(t.c, t.a, pointThis), this);
        nodeC.SetNeighborWhenFlip(new Triangle(t.c, t.a, pointPair), pair);

        this.children = new List<DN> { nodeA, nodeB, nodeC };
        pair.children = new List<DN> { nodeA, nodeB, nodeC };
    }


    public void Flip32(DN pair0, Triangle t, double3 pointThis, double3 pointPair, double3 pointAway) {
        var edge = t.Remaining(pointAway);
        var pair1 = GetFacingNode(new Triangle(edge.a, edge.b, pointThis));

        if (pair1 == null) {
            Debug.LogWarning("pair not find");
            return;
        } 
        if (!Equals(pair1.tetrahedra.RemainingPoint(new Triangle(edge.a, edge.b, pointThis)), pointPair)) {
            Debug.LogWarning("not 3 to 2 case");
            return;
        }

        var nodeA = new DN(pointThis, pointPair, pointAway, edge.a);
        var nodeB = new DN(pointThis, pointPair, pointAway, edge.b);

        nodeA.neighbor = new List<DN> { nodeB };
        nodeB.neighbor = new List<DN> { nodeA };

        nodeA.SetFacingNode(new Triangle(edge.a, pointThis, pointAway), this);  // except pointPair => search from this
        nodeA.SetFacingNode(new Triangle(edge.a, pointPair, pointAway), pair0); // except pointThis => search from pair0
        nodeA.SetFacingNode(new Triangle(edge.a, pointThis, pointPair), pair1); // except pointAway => search from pair1
        nodeB.SetFacingNode(new Triangle(edge.b, pointThis, pointAway), this);  // except pointPair => search from this
        nodeB.SetFacingNode(new Triangle(edge.b, pointPair, pointAway), pair0); // except pointThis => search from pair0
        nodeB.SetFacingNode(new Triangle(edge.b, pointThis, pointPair), pair1); // except pointAway => search from pair1

        this.children  = new List<DN> { nodeA, nodeB };
        pair0.children = new List<DN> { nodeA, nodeB };
        pair1.children = new List<DN> { nodeA, nodeB };

    }

    void SetNeighborWhenFlip(Triangle t, DN _this) {
        var _pair = _this.GetFacingNode(t);
        if (_pair != null) {
            this.neighbor.Add(_pair);
            _pair.SetFacingNode(t, this);
        }
    }

    public void SetFacingNode(Triangle t, DN node) {
        if (!node.HasFacet(t)) return;
        neighbor = neighbor.Select(n => n.HasFacet(t) ? node : n).ToList();
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
