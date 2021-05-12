using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using kmty.geom.d2;
using Unity.Mathematics;
using DN = DelaunayGraphNode2D;
using VN = VoronoiGraphNode2D;
using static Unity.Mathematics.math;

public class DelaunayGraphNode2D {
    public Triangle triangle;
    public List<DN> children;
    public List<DN> neighbor;
    public bool hasChild => children.Count > 0;

    public DelaunayGraphNode2D(Segment e, Vector2 c) : this(e.a, e.b, c) { }
    public DelaunayGraphNode2D(Vector2 a, Vector2 b, Vector2 c) {
        triangle = new Triangle(a, b, c);
        children = new List<DN>();
        neighbor = new List<DN>();
    }

    public void Split(Vector2 p) {
        if (triangle.OnEdge(p)) throw new ArgumentOutOfRangeException();
        if (triangle.Includes(p, false)) {
            var ab = new DN(triangle.a, triangle.b, p);
            var bc = new DN(triangle.b, triangle.c, p);
            var ca = new DN(triangle.c, triangle.a, p);
            ab.neighbor = new List<DN> { bc, ca };
            bc.neighbor = new List<DN> { ca, ab };
            ca.neighbor = new List<DN> { ab, bc };
            SetNeighbors(ab, triangle.a, triangle.b);
            SetNeighbors(bc, triangle.b, triangle.c);
            SetNeighbors(ca, triangle.c, triangle.a);
            children = new List<DN> { ab, bc, ca };
        }
    }

    void SetNeighbors(DN tgt, Vector2 p1, Vector2 p2) {
        var edge = new Segment(p1, p2);
        var pair = GetFacingNode(edge);
        if (pair != null) {
            tgt.neighbor.Add(pair);
            neighbor.ForEach(n => { if (n.triangle.ContainsSegment(edge)) n.SetFacingNode(edge, tgt); });
        }
    }

    public void Flip(DN pair, Segment prvEdge, Vector2 pointThis, Vector2 pointPair) {
        var newEdge = new Segment(pointThis, pointPair);
        var na = new DN(newEdge, prvEdge.a);
        var nb = new DN(newEdge, prvEdge.b);

        na.neighbor = new List<DN> { nb };
        nb.neighbor = new List<DN> { na };
        na.SetNeighborWhenFlip(new Segment(prvEdge.a, pointThis), this);
        na.SetNeighborWhenFlip(new Segment(prvEdge.a, pointPair), pair);
        nb.SetNeighborWhenFlip(new Segment(prvEdge.b, pointThis), this);
        nb.SetNeighborWhenFlip(new Segment(prvEdge.b, pointPair), pair);

        this.children = new List<DN> { na, nb };
        pair.children = new List<DN> { na, nb };
    }

    void SetNeighborWhenFlip(Segment e, DN _this) {
        var _pair = _this.GetFacingNode(e);
        if (_pair != null) {
            this.neighbor.Add(_pair);
            _pair.SetFacingNode(e, this);
        }
    }

    public void SetFacingNode(Segment e, DN node) {
        if (!node.triangle.ContainsSegment(e)) return;
        neighbor = neighbor.Select(n => n.triangle.ContainsSegment(e) ? node : n).ToList();
    }

    public DN GetFacingNode(Vector2 a, Vector2 b) => GetFacingNode(new Segment(a, b));
    public DN GetFacingNode(Segment e) {
        if (!triangle.ContainsSegment(e)) return null;
        return neighbor.Find(n => n.triangle.ContainsSegment(e));
    }
}

public class VoronoiGraphNode2D {
    public float2 center;
    public List<Segment> segments;
    public Mesh mesh;
    public VoronoiGraphNode2D(float2 c) {
        this.center = c;
        this.segments = new List<Segment>();
    }

    public void Meshilify() {
        var l = segments.Count * 3;
        var vtcs = new List<Vector3>();
        var tris = Enumerable.Range(0, l).ToArray();
        segments.ForEach(s => {
            var c = (Vector2)center;
            var a = (Vector2)s.a;
            var b = (Vector2)s.b;
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
    public Dictionary<float2, VN> nodes;

    public VoronoiGraph2D(DN[] dns) {
        nodes = new Dictionary<float2, VN>();

        foreach (var d in dns) {
            var t0 = d.triangle;
            var c0 = t0.circumscribedCenter;
            if (!nodes.ContainsKey(t0.a)) nodes.Add(t0.a, new VN(t0.a));
            if (!nodes.ContainsKey(t0.b)) nodes.Add(t0.b, new VN(t0.b));
            if (!nodes.ContainsKey(t0.c)) nodes.Add(t0.c, new VN(t0.c));
            d.neighbor.ForEach(n => {
                var t1 = n.triangle;
                var c1 = t1.circumscribedCenter;
                var v1 = c1 - c0;
                var th = 1e-5d;
                if (abs(dot(t0.b - t0.a, v1)) < th || abs(dot(t0.c - t0.a, v1)) < th) { nodes.TryGetValue(t0.a, out VN v); v?.segments.Add(new Segment(c0, c1)); }
                if (abs(dot(t0.c - t0.b, v1)) < th || abs(dot(t0.a - t0.b, v1)) < th) { nodes.TryGetValue(t0.b, out VN v); v?.segments.Add(new Segment(c0, c1)); }
                if (abs(dot(t0.a - t0.c, v1)) < th || abs(dot(t0.b - t0.c, v1)) < th) { nodes.TryGetValue(t0.c, out VN v); v?.segments.Add(new Segment(c0, c1)); }
            });
        }

        //foreach (var n in nodes.Values) {
        //    foreach (var s in n.segments) {
        //        var a = s.a;
        //        var b = s.b;
        //        if (cross(float3(a - n.center, 0), float3(b - a, 0)).z < 0) s.Set(b, a);
        //    }
        //}
        //var removes = new List<float2>();
        //foreach (var v in nodes) {
        //    var n = v.Value;
        //    foreach (var s0 in n.segments) {
        //        bool flag = false;
        //        foreach (var s1 in n.segments) {
        //            if (s1.a.x == s0.a.x && s1.a.y == s0.a.y &&
        //                s1.b.x == s0.b.x && s1.b.y == s0.b.y
        //                ) { Debug.Log("same"); continue; }
        //            Debug.Log("not same");
        //            if (s0.b.x == s1.a.x && s0.b.y == s1.a.y ||
        //                s0.a.x == s1.b.x && s0.a.y == s1.b.y 
        //                ) { flag = true; break; }
        //        }
        //        if (!flag) { removes.Add(n.center); Debug.Log(n.center); break; }
        //    }
        //}
        //foreach (var r in removes) { nodes.Remove(r); }
    }
}

