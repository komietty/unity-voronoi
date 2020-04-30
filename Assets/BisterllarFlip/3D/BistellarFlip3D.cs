using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using kmty.geom.d3;
using Unity.Mathematics;
using URnd = UnityEngine.Random;

public class BistellarFlip3D 
{
    DelaunayGraphNode3D rootNode;
    public Vector3[] points;
    List<DelaunayGraphNode3D> lastNodes = new List<DelaunayGraphNode3D>();

    public BistellarFlip3D(int numPoints, int seed) {
        rootNode = new DelaunayGraphNode3D((float3)Vector3.zero, (float3)Vector3.right * 5, (float3)Vector3.up * 5, (float3)Vector3.forward * 5);

        // for debug
        URnd.InitState(seed);
        points = Enumerable.Range(0, numPoints).Select(_ => new Vector3(URnd.value, URnd.value, URnd.value)).ToArray();
        for (int i = 0; i < numPoints; i++) Loop(points[i]);
    }



    public void Loop(float3 p) {
        var tgt = GetTgtNode(p);
        var thd = tgt.tetrahedra;
        tgt.Split(p);

        if(thd.Contains(p, false)) {
            tgt.children.ForEach(n => Leagalize(n, new Triangle(thd.a, thd.b, thd.c), p));
            tgt.children.ForEach(n => Leagalize(n, new Triangle(thd.b, thd.c, thd.d), p));
            tgt.children.ForEach(n => Leagalize(n, new Triangle(thd.c, thd.d, thd.a), p));
            tgt.children.ForEach(n => Leagalize(n, new Triangle(thd.d, thd.a, thd.b), p));
        }
    }

    DelaunayGraphNode3D GetTgtNode(Vector3 p) {
        var node = rootNode;
        while (node != null && node.hasChild) node = node.children.Find(c => c.tetrahedra.Contains((float3)p, true));
        return node;
    }

    public List<DelaunayGraphNode3D> GetResult() {
        RefreshLastNodes();
        return lastNodes;
    }

    void GetLastNodes(DelaunayGraphNode3D node) {
        if (node.hasChild == false) lastNodes.Add(node);
        else node.children.ForEach(c => GetLastNodes(c));
    }

    void RefreshLastNodes() {
        lastNodes.Clear();
        GetLastNodes(rootNode);
    }

    void Leagalize(DelaunayGraphNode3D node, Triangle t, double3 p) {
        var pair = node.GetFacingNode(t);
        if (pair == null || node.tetrahedra.HasFace(t) == false) return;
        var q = pair.tetrahedra.RemainingPoint(t);

        if (node.tetrahedra.GetCircumscribedSphere(1e-15d).Contains(q, true)) {
            bool intersects = t.Intersects(new Line(p, q - p), out double3 intersectPos);
            if (DataBaseUtil.CheckDuplication(new double3[] { p, q, t.a, t.b, t.c })) throw new System.Exception();

            // if p - q cross the triangle, flip 23 
            if (intersects) {
                node.Flip23(pair, t, p, q);
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.a, t.b), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.b, t.c), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.c, t.a), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.a, t.b), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.b, t.c), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.c, t.a), q));
            }
            // if not p - q cross the triangle, find third tet, and execute flip 32 
            else {
                double3 far, near1, near2;

                if (Util3D.IsIntersecting(new Segment(intersectPos, t.a), new Segment(t.b, t.c), 0.01f)) {
                    far = t.a;
                    near1 = t.b;
                    near2 = t.c;
                }
                else if (Util3D.IsIntersecting(new Segment(intersectPos, t.b), new Segment(t.c, t.a), 0.01f)) {
                    far = t.b;
                    near1 = t.c;
                    near2 = t.a;
                }
                else if (Util3D.IsIntersecting(new Segment(intersectPos, t.c), new Segment(t.a, t.b), 0.01f)) {
                    far = t.c;
                    near1 = t.a;
                    near2 = t.b;
                }
                else {
                    Debug.Log(intersectPos);
                    Debug.Log(p);
                    Debug.Log(q);
                    Debug.Log(t.a);
                    Debug.Log(t.b);
                    Debug.Log(t.c);
                    throw new System.Exception();
                }

                if (DataBaseUtil.CheckDuplication(new double3[] { p, q, far, near1, near2 })) throw new System.Exception();

                node.Flip32(pair, t, p, q, far);
                node.children.ForEach(n => Leagalize(n, new Triangle(q, far, near1), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(q, far, near2), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(far, p, near1), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(far, p, near2), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, q, near1), far));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, q, near2), far));
            }
        }
    }
}
