using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using System.Linq;
using Unity.Mathematics;

public class DelaunayGraphNode3D {
    public Tetrahedra tetrahedra;
    public List<DelaunayGraphNode3D> children;
    public List<DelaunayGraphNode3D> neighbor;

    public DelaunayGraphNode3D(double3 a, double3 b, double3 c, double3 d) {
        tetrahedra = new Tetrahedra(a, b, c, d);
        children = new List<DelaunayGraphNode3D>();
        neighbor = new List<DelaunayGraphNode3D>();
    }

    public DelaunayGraphNode3D(Triangle t, double3 d) : this(t.a, t.b, t.c, d) { }

    public bool hasChild => children.Count > 0;

    public void Split(double3 p) {
        var thd = tetrahedra;
        if (thd.Contains(p, false)) {
            var p_abc = new DelaunayGraphNode3D(p, thd.a, thd.b, thd.c);
            var p_bcd = new DelaunayGraphNode3D(p, thd.b, thd.c, thd.d);
            var p_cda = new DelaunayGraphNode3D(p, thd.c, thd.d, thd.a);
            var p_dab = new DelaunayGraphNode3D(p, thd.d, thd.a, thd.b);
            p_abc.neighbor = new List<DelaunayGraphNode3D> { p_bcd, p_cda, p_dab };
            p_bcd.neighbor = new List<DelaunayGraphNode3D> { p_cda, p_dab, p_abc };
            p_cda.neighbor = new List<DelaunayGraphNode3D> { p_dab, p_abc, p_bcd };
            p_dab.neighbor = new List<DelaunayGraphNode3D> { p_abc, p_bcd, p_cda };
            SetNeighbors(p_abc, new Triangle(thd.a, thd.b, thd.c));
            SetNeighbors(p_bcd, new Triangle(thd.b, thd.c, thd.d));
            SetNeighbors(p_cda, new Triangle(thd.c, thd.d, thd.a));
            SetNeighbors(p_dab, new Triangle(thd.d, thd.a, thd.b));
            children = new List<DelaunayGraphNode3D> { p_abc, p_bcd, p_cda, p_dab };
        }
    }

    void SetNeighbors(DelaunayGraphNode3D tgt, Triangle t) {
        var pair = GetFacingNode(t);
        if (pair != null) {
            tgt.neighbor.Add(pair);
            neighbor.ForEach(n => { if (n.tetrahedra.HasFace(t)) n.SetFacingNode(t, tgt); });
        }
    }

    public void Flip23(DelaunayGraphNode3D pair, Triangle t, double3 pointThis, double3 pointPair) {
        if (DataBaseUtil.CheckDuplication(new Vector3[] { (float3)t.a, (float3)t.b, (float3)t.c, (float3)pointThis, (float3)pointPair })) throw new System.Exception();

        var nodeA = new DelaunayGraphNode3D(pointThis, pointPair, t.a, t.b);
        var nodeB = new DelaunayGraphNode3D(pointThis, pointPair, t.b, t.c);
        var nodeC = new DelaunayGraphNode3D(pointThis, pointPair, t.c, t.a);

        nodeA.neighbor = new List<DelaunayGraphNode3D> { nodeB, nodeC };
        nodeB.neighbor = new List<DelaunayGraphNode3D> { nodeC, nodeA };
        nodeC.neighbor = new List<DelaunayGraphNode3D> { nodeA, nodeB };

        nodeA.SetNeighborWhenFlip(new Triangle(t.a, t.b, pointThis), this);
        nodeA.SetNeighborWhenFlip(new Triangle(t.a, t.b, pointPair), pair);
        nodeB.SetNeighborWhenFlip(new Triangle(t.b, t.c, pointThis), this);
        nodeB.SetNeighborWhenFlip(new Triangle(t.b, t.c, pointPair), pair);
        nodeC.SetNeighborWhenFlip(new Triangle(t.c, t.a, pointThis), this);
        nodeC.SetNeighborWhenFlip(new Triangle(t.c, t.a, pointPair), pair);

        this.children = new List<DelaunayGraphNode3D> { nodeA, nodeB, nodeC };
        pair.children = new List<DelaunayGraphNode3D> { nodeA, nodeB, nodeC };
    }


    public void Flip32(DelaunayGraphNode3D pair0, Triangle t, double3 pointThis, double3 pointPair, double3 pointAway) {
        var edge = t.Remaining(pointAway);
        var pair1 = GetFacingNode(new Triangle(edge.a, edge.b, pointThis));

        if (pair1 == null) {
            Debug.LogWarning("pair1 is not exist");
            return;
        }
        if (Equals(pair1.tetrahedra.RemainingPoint(new Triangle(edge.a, edge.b, pointThis)), pointPair) == false) {
            Debug.LogWarning("not 3 to 2 case");
            return;
        }

        var nodeA = new DelaunayGraphNode3D(pointThis, pointPair, pointAway, edge.a);
        var nodeB = new DelaunayGraphNode3D(pointThis, pointPair, pointAway, edge.b);

        nodeA.neighbor = new List<DelaunayGraphNode3D> { nodeB };
        nodeB.neighbor = new List<DelaunayGraphNode3D> { nodeA };

        nodeA.SetFacingNode(new Triangle(edge.a, pointThis, pointAway), this);  // except pointPair => search from this
        nodeA.SetFacingNode(new Triangle(edge.a, pointPair, pointAway), pair0); // except pointThis => search from pair0
        nodeA.SetFacingNode(new Triangle(edge.a, pointThis, pointPair), pair1); // except pointAway => search from pair1
        nodeB.SetFacingNode(new Triangle(edge.b, pointThis, pointAway), this);  // except pointPair => search from this
        nodeB.SetFacingNode(new Triangle(edge.b, pointPair, pointAway), pair0); // except pointThis => search from pair0
        nodeB.SetFacingNode(new Triangle(edge.b, pointThis, pointPair), pair1); // except pointAway => search from pair1

        this.children  = new List<DelaunayGraphNode3D> { nodeA, nodeB };
        pair0.children = new List<DelaunayGraphNode3D> { nodeA, nodeB };
        pair1.children = new List<DelaunayGraphNode3D> { nodeA, nodeB };

    }

    void SetNeighborWhenFlip(Triangle t, DelaunayGraphNode3D _this) {
        var _pair = _this.GetFacingNode(t);
        if (_pair != null) {
            this.neighbor.Add(_pair);
            _pair.SetFacingNode(t, this);
        }
    }

    public void SetFacingNode(Triangle t, DelaunayGraphNode3D node) {
        if (node.tetrahedra.HasFace(t) == false) return;
        neighbor = neighbor.Select(n => n.tetrahedra.HasFace(t) ? node : n).ToList();
    }

    public DelaunayGraphNode3D GetFacingNode(Triangle t) {
        if (tetrahedra.HasFace(t) == false) return null;
        return neighbor.Find(n => n.tetrahedra.HasFace(t));
    }
}
