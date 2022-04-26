using System.Collections.Generic;
using Unity.Mathematics;

namespace kmty.geom.csg {
    using static Unity.Mathematics.math;
    using d3 = double3;
    using PG = Polygon;

    public enum OpType { Union, Subtraction, Intersection, Clip }

    public class CsgTree {
        public PG[] polygons { get; }

        public CsgTree(PG[] src) { polygons = src; }
        public CsgTree(CsgTree src) { polygons = Util.Clone(src.polygons); }

        public CsgTree Oparation(CsgTree pair, OpType op) {
            switch(op){
                case OpType.Union:        return Union(pair); 
                case OpType.Subtraction:  return Subtraction(pair); 
                case OpType.Intersection: return Intersection(pair);
                case OpType.Clip:         return Clip(pair);
                default: throw new System.Exception();
            }
        }

        public CsgTree Union(CsgTree pair){
            var tn = new Node(new List<PG>(this.polygons));
            var pn = new Node(new List<PG>(pair.polygons));
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            pn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.Build(pn.GetPolygonData());
            return new CsgTree(tn.GetPolygonData().ToArray());
        }

        public CsgTree Subtraction(CsgTree pair){
            var tn = new Node(new List<PG>(this.polygons));
            var pn = new Node(new List<PG>(pair.polygons));
            tn.Invert();
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            pn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.Build(pn.GetPolygonData());
            tn.Invert();
            return new CsgTree(tn.GetPolygonData().ToArray());
        }

        public CsgTree Intersection(CsgTree pair){
            var tn = new Node(new List<PG>(this.polygons));
            var pn = new Node(new List<PG>(pair.polygons));
            tn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            tn.Build(pn.GetPolygonData());
            tn.Invert();
            return new CsgTree(tn.GetPolygonData().ToArray());
        }

        public CsgTree Clip(CsgTree pair){
            var tn = new Node(new List<PG>(this.polygons));
            var pn = new Node(new List<PG>(pair.polygons));
            tn.ClipTo(pn);
            return new CsgTree(tn.GetPolygonData().ToArray());
        }
    }

    public class Plane {
        public d3 n     { get; private set; }
        public double w { get; private set; }

        static readonly double EPSILON = 1e-5d;
        static readonly int ONPLANE = 0;
        static readonly int FACE    = 1;
        static readonly int BACK    = 2;
        static readonly int SPAN    = 3;

        public Plane(Plane src) { n = src.n; w = src.w; }

        public Plane(d3 a, d3 b, d3 c) {
            n = normalize(cross(b - a, c - a));
            w = dot(n, a);
        }
        
        public void Flip() {
            n *= -1;
            w *= -1;
        }

        int GetType(d3 p){
            var v = dot(n, p) - w;
            var isNearPlane  = abs(v) < EPSILON;
            var isFacingSide = v > 0;
            if (isNearPlane) return ONPLANE;
            else             return isFacingSide ? FACE : BACK;
        }

        public (PG onPF, PG onPB, PG face, PG back) SplitPolygon(PG p) {
            var l = p.verts.Length;
            var pType = 0;
            var vType = new int[l];

            for(var i = 0; i < l; i++) {
                var t = GetType(p.verts[i]);
                pType   |= t;
                vType[i] = t;
            }

            switch (pType) {
                default: throw new System.Exception();
                case 0: return (dot(n, p.plane.n) > 0) ? (p, null, null, null) : (null, p, null, null);
                case 1: return (null, null, p, null);
                case 2: return (null, null, null, p);
                case 3:
                    var faces = new List<d3>();
                    var backs = new List<d3>();
                    for (var i = 0; i < l; i++) {
                        var j = (i + 1) % l;
                        var si = vType[i];
                        var sj = vType[j];
                        var vi = p.verts[i];
                        var vj = p.verts[j];

                        if      (si == FACE) faces.Add(vi);
                        else if (si == BACK) backs.Add(vi);
                        else    { faces.Add(vi); backs.Add(vi); }

                        if ((si | sj) == SPAN) {
                            var t = (w - dot(n, vi)) / dot(n, vj - vi);
                            var v = lerp(vi, vj, t);
                            faces.Add(v);
                            backs.Add(v);
                        }
                    }
                    return (null, null, new PG(faces.ToArray()), new PG(backs.ToArray()));
            }
        }
    }

    public class Polygon {
        public d3[] verts   { get; private set; }
        public Plane plane  { get; private set; }

        public Polygon(d3[] vs) {
            this.verts = vs;
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }

        public Polygon(PG src) {
            this.verts = Util.Clone(src.verts);
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }

        public void Flip(){
            this.plane.Flip();
            System.Array.Reverse(verts);
        }
    }

    public class Node {
        Node nf;
        Node nb;
        Plane pl;
        List<PG> polygons;

        public Node() {
            polygons = new List<PG>();
        }

        public Node(List<PG> src) {
            polygons = new List<PG>();
            Build(src);
        }
        
        public Node(Node n) {
            polygons = Util.Clone(n.polygons);
            nf = n.nf != null ? new Node(n.nf)  : null;
            nb = n.nb != null ? new Node(n.nb)  : null;
            pl = n.pl != null ? new Plane(n.pl) : null;
        }

        public void Invert() {
            for (var i = 0; i < polygons.Count; i++) polygons[i].Flip();
            pl?.Flip();
            nf?.Invert();
            nb?.Invert();
            var tmp = nf;
            nf = nb;
            nb = tmp;
        }

        List<PG> ClipPolygons(List<PG> src) {
            if (pl == null) return new List<PG>(src);
            var pf = new List<PG>();
            var pb = new List<PG>();
            for (var i = 0; i < src.Count; i++){
                var p = src[i];
                var o = pl.SplitPolygon(p);
                if (o.onPF != null) pf.Add(o.onPF);
                if (o.onPB != null) pb.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (nf != null) pf = nf.ClipPolygons(pf);
            if (nb != null) pb = nb.ClipPolygons(pb); else pb.Clear();
            pf.AddRange(pb);
            return pf;
        }

        public void ClipTo(Node pair) {
            polygons = pair.ClipPolygons(polygons);
            nf?.ClipTo(pair);
            nb?.ClipTo(pair);
        }

        public List<PG> GetPolygonData() {
            var clone = Util.Clone(polygons);
            if(nf != null) clone.AddRange(nf.GetPolygonData());
            if(nb != null) clone.AddRange(nb.GetPolygonData());
            return clone;
        }

        public void Build(List<PG> src) {
            if (src.Count == 0) return;
            if (pl == null) pl = new Plane(src[0].plane);
            var pf = new List<PG>();
            var pb = new List<PG>();
            for (var i = 0; i < src.Count; i++) {
                var o = pl.SplitPolygon(src[i]);
                if (o.onPF != null) polygons.Add(o.onPF);
                if (o.onPB != null) polygons.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (pf.Count > 0) { if (nf == null) { nf = new Node(); } nf.Build(pf); }
            if (pb.Count > 0) { if (nb == null) { nb = new Node(); } nb.Build(pb); }
            pf.Clear();
            pb.Clear();
        }
    }

   static class Util {
        public static T[] Clone<T>(T[] src){
            var l = src.Length;
            var d = new T[l];
            System.Array.Copy(src, d, l);
            return d;
        } 

        public static PG[] Clone(PG[] src){
            var l = src.Length;
            var d = new PG[l];
            for (var i = 0; i < l; i++) d[i] = new PG(src[i]);
            return d;
        } 

        public static List<PG> Clone(List<PG> src){
            var l = src.Count;
            var d = new List<PG>();
            for (var i = 0; i < l; i++) d.Add(new PG(src[i]));
            return d;
        } 
    }
}