using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using kmty.geom.d2;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class BistellarFlip2D
{
    DelaunayGraphNode2D rootNode;
    List<DelaunayGraphNode2D> lastNodes = new List<DelaunayGraphNode2D>();

    public BistellarFlip2D(int numPoints) {
        rootNode = new DelaunayGraphNode2D(Vector2.one, new Vector2(2, -9), new Vector2(-9, 2));
        var nodeA = new DelaunayGraphNode2D(Vector2.zero, Vector2.right, Vector2.up);
        var nodeB = new DelaunayGraphNode2D(Vector2.one, Vector2.right, Vector2.up);
        nodeA.neighbor = new List<DelaunayGraphNode2D> { nodeB };
        nodeB.neighbor = new List<DelaunayGraphNode2D> { nodeA };
        rootNode.children = new List<DelaunayGraphNode2D> { nodeA, nodeB };

        var points = Enumerable.Range(0, numPoints).Select(_ => new Vector2(UnityEngine.Random.value, UnityEngine.Random.value)).ToArray();
        for (int i = 0; i < numPoints; i++) Loop(points[i]);
    }

    public void Loop(float2 p) {
        var tgt = GetTgtNode(p);
        var tri = tgt.triangle;
        tgt.Split(p);

        if (tri.Includes(p, false)) {
            tgt.children.ForEach(n => LegalizeEdge(n, new Segment(tri.a, tri.b), p));
            tgt.children.ForEach(n => LegalizeEdge(n, new Segment(tri.b, tri.c), p));
            tgt.children.ForEach(n => LegalizeEdge(n, new Segment(tri.c, tri.a), p));
        }
        else if (tri.OnEdge(p)) {
            if (cross(float3(tri.b - tri.a, 0), float3(p - tri.b, 0)).z == 0) SplitOnEdge(tgt, p, tri.a, tri.b, tri.c);
            if (cross(float3(tri.c - tri.b, 0), float3(p - tri.c, 0)).z == 0) SplitOnEdge(tgt, p, tri.b, tri.c, tri.a);
            if (cross(float3(tri.a - tri.c, 0), float3(p - tri.a, 0)).z == 0) SplitOnEdge(tgt, p, tri.c, tri.a, tri.b);
            throw new System.ArgumentException("degenerated case");
        }
    }

    DelaunayGraphNode2D GetTgtNode(Vector2 p) {
        var n = rootNode;
        while (n != null && n.hasChild) n = n.children.Find(c => c.triangle.Includes(p, true));
        return n;
    }

    public List<DelaunayGraphNode2D> GetResult() {
        RefreshLastNodes();
        return lastNodes;
    }

    void GetLastNodes(DelaunayGraphNode2D node) {
        if (node.hasChild == false) lastNodes.Add(node);
        else node.children.ForEach(c => GetLastNodes(c));
    }

    void RefreshLastNodes() {
        lastNodes.Clear();
        GetLastNodes(rootNode);
    }

    DelaunayGraphNode2D GetPairNode(Segment e, Vector2 excludePoint) {
        RefreshLastNodes();
        return lastNodes.Find(n => n.triangle.ContainsSegment(e) && n.triangle.Excludes(excludePoint));
    }

    void LegalizeEdge(DelaunayGraphNode2D node, Segment checkEdge, Vector2 p) {
        var pairNode = node.GetFacingNode(checkEdge);
        if (pairNode == null || node.triangle.ContainsSegment(checkEdge) == false) return;

        var pairPoint = pairNode.triangle.RemainingPoint(checkEdge);
        if (node.triangle.GetCircumscribedCircle().Contains(pairPoint)) {
            node.Flip(pairNode, checkEdge, p, pairPoint);
            node.children.ForEach(n => LegalizeEdge(n, new Segment(checkEdge.a, pairPoint), p));
            node.children.ForEach(n => LegalizeEdge(n, new Segment(checkEdge.b, pairPoint), p));
            node.children.ForEach(n => LegalizeEdge(n, new Segment(checkEdge.a, p), pairPoint));
            node.children.ForEach(n => LegalizeEdge(n, new Segment(checkEdge.b, p), pairPoint));
        }
    }

    // TODO: refactor
    void SplitOnEdge(DelaunayGraphNode2D tgt, Vector2 p,  Vector2 edgePoint1, Vector2 edgePoint2, Vector2 otherPoint) {
        var edge = new Segment(edgePoint1, edgePoint2);
        var pair = tgt.GetFacingNode(edge);
        var nodeA = new DelaunayGraphNode2D(edgePoint1, p, otherPoint);
        var nodeB = new DelaunayGraphNode2D(edgePoint2, p, otherPoint);
        nodeA.neighbor = new List<DelaunayGraphNode2D> { nodeB };
        nodeB.neighbor = new List<DelaunayGraphNode2D> { nodeA };
        var nodeA_pair = tgt.GetFacingNode(edgePoint1, otherPoint);
        var nodeB_pair = tgt.GetFacingNode(edgePoint2, otherPoint);
        if (nodeA_pair != null) nodeA.neighbor.Add(nodeA_pair);
        if (nodeB_pair != null) nodeB.neighbor.Add(nodeB_pair);

        if (pair != null) {
            var pairPoint = pair.triangle.RemainingPoint(edge);
            var nodeC = new DelaunayGraphNode2D(edgePoint1, p, pairPoint);
            var nodeD = new DelaunayGraphNode2D(edgePoint2, p, pairPoint);
            nodeA.neighbor.Add(nodeC);
            nodeB.neighbor.Add(nodeD);
            nodeC.neighbor = new List<DelaunayGraphNode2D> { nodeD, nodeA };
            nodeD.neighbor = new List<DelaunayGraphNode2D> { nodeC, nodeB };
            var nodeC_pair = pair.GetFacingNode(edgePoint1, pairPoint);
            var nodeD_pair = pair.GetFacingNode(edgePoint2, pairPoint);
            if (nodeC_pair != null) nodeC.neighbor.Add(nodeC_pair);
            if (nodeD_pair != null) nodeD.neighbor.Add(nodeD_pair);
            pair.children = new List<DelaunayGraphNode2D> { nodeC, nodeD };
            LegalizeEdge(nodeC, new Segment(edgePoint1, pairPoint), p);
            LegalizeEdge(nodeD, new Segment(edgePoint2, pairPoint), p);
        }

        tgt.children = new List<DelaunayGraphNode2D> { nodeA, nodeB };
        LegalizeEdge(nodeA, new Segment(edgePoint1, otherPoint), p);
        LegalizeEdge(nodeB, new Segment(edgePoint2, otherPoint), p);
    }
}
