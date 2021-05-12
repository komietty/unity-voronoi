using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using DN = DelaunayGraphNode3D;
using UR = UnityEngine.Random;

public class BistellarFlip3D {
    DN root;
    List<DN> tips = new List<DN>();

    public BistellarFlip3D(int num, int seed) {
        root = new DN(
            (float3)Vector3.zero,
            (float3)Vector3.right * 2.5f,
            (float3)Vector3.up * 2.5f,
            (float3)Vector3.forward * 2.5f);

        UR.InitState(seed);
        for (int i = 0; i < num; i++)
            Loop(new Vector3(UR.value, UR.value, UR.value));
    }



    public void Loop(float3 p) {
        var n = GetNode(p);
        var t = n.tetrahedra;
        n.Split(p);
        if(t.Contains(p, false)) {
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.a, t.b, t.c), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.b, t.c, t.d), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.c, t.d, t.a), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.d, t.a, t.b), p));
        }
    }

    DN GetNode(Vector3 p) {
        var n = root;
        while (n != null && n.hasChild) n = n.children.Find(c => c.tetrahedra.Contains((float3)p, true));
        return n;
    }

    public List<DN> GetResult() {
        RefreshLastNodes();
        return tips;
    }

    void GetLastNodes(DN n) {
        if (!n.hasChild) tips.Add(n);
        else n.children.ForEach(c => GetLastNodes(c));
    }

    void RefreshLastNodes() {
        tips.Clear();
        GetLastNodes(root);
    }

    void Leagalize(DN node, Triangle t, double3 p) {
        var pair = node.GetFacingNode(t);
        if (pair == null || node.tetrahedra.HasFace(t) == false) return;
        var q = pair.tetrahedra.RemainingPoint(t);

        if (node.tetrahedra.GetCircumscribedSphere(1e-15d).Contains(q, true)) {
            bool intersects = t.Intersects(new Line(p, q - p), out double3 intersectPos);
            if (DataBaseUtil.CheckDuplication(new double3[] { p, q, t.a, t.b, t.c }))
                throw new System.Exception($"p:{p},\n q:{q},\n ta:{t.a},\n tb:{t.b},\n tc:{t.c}");

            // if p - q intersects the triangle, flip 23 
            if (intersects) {
                node.Flip23(pair, t, p, q);
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.a, t.b), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.b, t.c), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(q, t.c, t.a), p));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.a, t.b), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.b, t.c), q));
                node.children.ForEach(n => Leagalize(n, new Triangle(p, t.c, t.a), q));
            }
            // if not p - q intersects the triangle, find third tet, and execute flip 32 
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
                    // find intersect point r with plane and p-q, then find far point from distance 
                    var da = math.distancesq(t.a, intersectPos);
                    var db = math.distancesq(t.b, intersectPos);
                    var dc = math.distancesq(t.c, intersectPos);
                    if      (da >= db && da >= dc) { far = t.a; near1 = t.b; near2 = t.c; }
                    else if (db >= dc && db >= da) { far = t.b; near1 = t.c; near2 = t.a; }
                    else if (dc >= da && dc >= db) { far = t.c; near1 = t.a; near2 = t.b; }
                    else throw new System.Exception();
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
