using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using DN = DelaunayGraphNode3D;
using UR = UnityEngine.Random;
using static Unity.Mathematics.math;

public class BistellarFlip3D {
    DN root;
    List<DN> tips = new List<DN>();

    public BistellarFlip3D(int num, int seed) {
        root = new DN(new double3(0, 0, 0), new double3(3, 0, 0), new double3(0, 3, 0), new double3(0, 0, 3));
        for (int i = 0; i < num; i++) Loop(new double3(UR.value, UR.value, UR.value));
    }

    public void Loop(double3 p) {
        RefreshTips();
        var n = tips.Find(_t => _t.tetrahedra.Contains(p, true));
        var t = n.tetrahedra;
        n.Split(p);
        if(t.Contains(p, false)) {
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.a, t.b, t.c), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.b, t.c, t.d), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.c, t.d, t.a), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.d, t.a, t.b), p));
        }
    }

    void RefreshTips() {
        tips.Clear();
        GetTips(root);
    }

    public List<DN> GetResult() {
        RefreshTips();
        return tips;
    }

    void GetTips(DN n) {
        if (n.children.Count == 0) tips.Add(n);
        else n.children.ForEach(c => GetTips(c));
    }


    public void Test() {
        RefreshTips();
        tips.ForEach(curr => {
            foreach (var t in curr.tetrahedra.triangles) {
                var pair = curr.GetFacingNode(t);
                if (pair == null) continue;
                var rmnp = pair.tetrahedra.RemainingPoint(t);
                if (curr.tetrahedra.circumscribedSphere.Contains(rmnp)) {
                    Debug.LogWarning("non deraunay tetrahedra is found");
                }
            }
        });
    }

    void Leagalize(DN n, Triangle t, double3 p) {
        DN pair = n.GetFacingNode(t);
        if (pair == null) return;
        if (!n.tetrahedra.HasFace(t)) return;
        var q = pair.tetrahedra.RemainingPoint(t);
        var a = t.a;
        var b = t.b;
        var c = t.c;

        if (n.tetrahedra.circumscribedSphere.Contains(q)) {
            if (DataBaseUtil.CheckDuplication(new double3[] { p, q, a, b, c }))
                throw new System.Exception($"p:{p},\n q:{q},\n ta:{a},\n tb:{b},\n tc:{c}");

            // if p - q intersects the triangle, flip 23 
            // if not, find third tet, and execute flip 32 
            // find intersect point r with plane and p-q, then find far point from distance 
            if (t.Intersects(new Line(p, q - p), out double3 i)) {
                n.Flip23(pair, t, p, q);
                n.children.ForEach(_n => Leagalize(_n, new Triangle(q, a, b), p));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(q, b, c), p));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(q, c, a), p));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(p, a, b), q));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(p, b, c), q));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(p, c, a), q));
            }
            else {
                double3 far, n1, n2;
                if      (Util3D.IsIntersecting(new Segment(i, a), new Segment(b, c), 1e-15d)) { far = a; n1 = b; n2 = c; }
                else if (Util3D.IsIntersecting(new Segment(i, b), new Segment(c, a), 1e-15d)) { far = b; n1 = c; n2 = a; }
                else if (Util3D.IsIntersecting(new Segment(i, c), new Segment(a, b), 1e-15d)) { far = c; n1 = a; n2 = b; }
                else {
                    var da = distancesq(a, i);
                    var db = distancesq(b, i);
                    var dc = distancesq(c, i);
                    if      (da >= db && da >= dc) { far = a; n1 = b; n2 = c; }
                    else if (db >= dc && db >= da) { far = b; n1 = c; n2 = a; }
                    else if (dc >= da && dc >= db) { far = c; n1 = a; n2 = b; }
                    else throw new System.Exception();
                }

                if (DataBaseUtil.CheckDuplication(new double3[] { p, q, far, n1, n2 })) throw new System.Exception();

                n.Flip32(pair, t, p, q, far);
                n.children.ForEach(_n => Leagalize(_n, new Triangle(q, far, n1), p));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(q, far, n2), p));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(far, p, n1), q));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(far, p, n2), q));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(p, q, n1), far));
                n.children.ForEach(_n => Leagalize(_n, new Triangle(p, q, n2), far));
            }
        }
    }
}
