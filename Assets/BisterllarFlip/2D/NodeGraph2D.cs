using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using kmty.geom.d2;

public class DelaunayGraphNode2D {
    public Triangle triangle;
    public List<DelaunayGraphNode2D> children;
    public List<DelaunayGraphNode2D> neighbor;

    public DelaunayGraphNode2D(Vector2 a, Vector2 b, Vector2 c) {
        triangle = new Triangle(a, b, c);
        children = new List<DelaunayGraphNode2D>();
        neighbor = new List<DelaunayGraphNode2D>();
    }

    public DelaunayGraphNode2D(Segment e, Vector2 c) : this(e.a, e.b, c) { }

    public bool hasChild => children.Count > 0;

    public void Split(Vector2 p) {
        if (triangle.OnEdge(p)) throw new ArgumentOutOfRangeException();
        if (triangle.Includes(p, false)) {
            var ab = new DelaunayGraphNode2D(triangle.a, triangle.b, p);
            var bc = new DelaunayGraphNode2D(triangle.b, triangle.c, p);
            var ca = new DelaunayGraphNode2D(triangle.c, triangle.a, p);
            ab.neighbor = new List<DelaunayGraphNode2D> { bc, ca };
            bc.neighbor = new List<DelaunayGraphNode2D> { ca, ab };
            ca.neighbor = new List<DelaunayGraphNode2D> { ab, bc };
            SetNeighbors(ab, triangle.a, triangle.b);
            SetNeighbors(bc, triangle.b, triangle.c);
            SetNeighbors(ca, triangle.c, triangle.a);
            children = new List<DelaunayGraphNode2D> { ab, bc, ca };
        }
    }

    void SetNeighbors(DelaunayGraphNode2D tgt, Vector2 p1, Vector2 p2) {
        var edge = new Segment(p1, p2);
        var pair = GetFacingNode(edge);
        if (pair != null) {
            tgt.neighbor.Add(pair);
            neighbor.ForEach(n => { if (n.triangle.ContainsSegment(edge)) n.SetFacingNode(edge, tgt); });
        }
    }

    public void Flip(DelaunayGraphNode2D pair, Segment oldEdge, Vector2 pointThis, Vector2 pointPair) {
        var newEdge = new Segment(pointThis, pointPair);
        var nodeA = new DelaunayGraphNode2D(newEdge, oldEdge.a);
        var nodeB = new DelaunayGraphNode2D(newEdge, oldEdge.b);

        nodeA.neighbor = new List<DelaunayGraphNode2D> { nodeB };
        nodeB.neighbor = new List<DelaunayGraphNode2D> { nodeA };
        nodeA.SetNeighborWhenFlip(new Segment(oldEdge.a, pointThis), this);
        nodeA.SetNeighborWhenFlip(new Segment(oldEdge.a, pointPair), pair);
        nodeB.SetNeighborWhenFlip(new Segment(oldEdge.b, pointThis), this);
        nodeB.SetNeighborWhenFlip(new Segment(oldEdge.b, pointPair), pair);

        this.children = new List<DelaunayGraphNode2D> { nodeA, nodeB };
        pair.children = new List<DelaunayGraphNode2D> { nodeA, nodeB };
    }

    void SetNeighborWhenFlip(Segment e, DelaunayGraphNode2D _this) {
        var _pair = _this.GetFacingNode(e);
        if (_pair != null) {
            this.neighbor.Add(_pair);
            _pair.SetFacingNode(e, this);
        }
    }

    public void SetFacingNode(Segment e, DelaunayGraphNode2D node) {
        if (node.triangle.ContainsSegment(e) == false) return;
        neighbor = neighbor.Select(n => n.triangle.ContainsSegment(e) ? node : n).ToList();
    }

    public DelaunayGraphNode2D GetFacingNode(Vector2 a, Vector2 b) {
        return GetFacingNode(new Segment(a, b));
    }

    public DelaunayGraphNode2D GetFacingNode(Segment e) {
        if (triangle.ContainsSegment(e) == false) return null;
        return neighbor.Find(n => n.triangle.ContainsSegment(e));
    }
}
