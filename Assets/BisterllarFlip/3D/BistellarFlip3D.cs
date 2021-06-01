using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using kmty.geom.d3;

namespace old {
    using DN = DelaunayGraphNode3D;
    using UR = UnityEngine.Random;

    public class BistellarFlip3D : BistellarFlip<DN> {

        public BistellarFlip3D(int num, int seed = 0) {
            root = new DN(new double3(0, 0, 0), new double3(3, 0, 0), new double3(0, 3, 0), new double3(0, 0, 3));
            UR.InitState(seed);
            for (int i = 0; i < num; i++) Loop(new double3(UR.value, UR.value, UR.value));
        }

        public void RefreshTips() { tips.Clear(); GetTips(root); }
        public List<DN> GetResult() { RefreshTips(); return tips; }
        void GetTips(DN n) { if (n.children.Count == 0) tips.Add(n); else n.children.ForEach(c => GetTips(c)); }

        public void Loop(double3 p) {
            RefreshTips();
            var n = tips.Find(_t => _t.tetrahedra.Contains(p, true));
            var t = n.tetrahedra;
            if (!t.Contains(p, false)) Debug.LogWarning("on the surface");
            n.Split(p);
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.a, t.b, t.c), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.b, t.c, t.d), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.c, t.d, t.a), p));
            n.children.ForEach(_n => Leagalize(_n, new Triangle(t.d, t.a, t.b), p));
        }

        public void Leagalize(DN n, Triangle t, double3 p) {
            DN pair = n.GetFacingNode(t);
            if (n.children.Count > 0) Debug.Log("having children");
            if (pair == null) return;
            if (!n.tetrahedra.HasFace(t)) return;
            var q = pair.tetrahedra.RemainingPoint(t);
            var a = t.a;
            var b = t.b;
            var c = t.c;

            if (n.tetrahedra.circumscribedSphere.Contains(q)) {
                if (CheckDuplication(new double3[] { p, q, a, b, c })) throw new System.Exception();

                // if p - q intersects the triangle, flip 23 
                // if not, find third tet, and execute flip 32 
                // find intersect point r with plane and p-q, then find far point from distance 
                //if (t.Intersects(new Segment(p, q - p), out double3 i, out bool isOnEdge)) {
                if (t.Intersects(new Segment(p, q), out double3 i, out bool isOnEdge)) {
                    n.Flip23(pair, t, p, q);
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(q, a, b), p));
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(q, b, c), p));
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(q, c, a), p));
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(p, a, b), q));
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(p, b, c), q));
                    n.children.ForEach(_n => Leagalize(_n, new Triangle(p, c, a), q));
                } else if (isOnEdge) {
                    Debug.LogWarning("point is on edge");
                } else {
                    double3 far, n1, n2;
                    if (Util3D.IsIntersecting(new Segment(i, a), new Segment(b, c), 1e-15d)) { far = a; n1 = b; n2 = c; } else if (Util3D.IsIntersecting(new Segment(i, b), new Segment(c, a), 1e-15d)) { far = b; n1 = c; n2 = a; } else if (Util3D.IsIntersecting(new Segment(i, c), new Segment(a, b), 1e-15d)) { far = c; n1 = a; n2 = b; } else throw new System.Exception();

                    #region not nessasary maybe
                    /*
                    else {
                        throw new System.Exception();
                        var da = distancesq(a, i);
                        var db = distancesq(b, i);
                        var dc = distancesq(c, i);
                        if      (da >= db && da >= dc) { far = a; n1 = b; n2 = c; }
                        else if (db >= dc && db >= da) { far = b; n1 = c; n2 = a; }
                        else if (dc >= da && dc >= db) { far = c; n1 = a; n2 = b; }
                        else throw new System.Exception();
                    }
                    */
                    #endregion

                    if (CheckDuplication(new double3[] { p, q, far, n1, n2 })) throw new System.Exception();

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
}
