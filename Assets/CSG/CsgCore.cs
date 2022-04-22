using System.Collections.Generic;
using Unity.Mathematics;

namespace kmty.geom.csg {
    using static Unity.Mathematics.math;
    using d3 = double3;

    public enum OpType { Union, Subtraction, Intersection }

    public class CsgTree {
        public Polygon[] polygons { get; }

        public CsgTree(Polygon[] src) { polygons = src; }
        public CsgTree(CsgTree src) { polygons = Util.Clone(src.polygons); }

        public CsgTree Oparation(CsgTree pair, OpType op) {
            switch(op){
                case OpType.Union:        return Union(pair); 
                case OpType.Subtraction:  return Subtraction(pair); 
                case OpType.Intersection: return Intersection(pair);
                default: throw new System.Exception();
            }
        }

        public CsgTree Union(CsgTree pair){
            var tn = new Node(new List<Polygon>(this.polygons));
            var pn = new Node(new List<Polygon>(pair.polygons));
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            pn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.Build(pn.GetPolygonData());
            return new CsgTree(tn.GetPolygonData().ToArray());
        }

        public CsgTree Subtraction(CsgTree pair){
            var tn = new Node(new List<Polygon>(this.polygons));
            var pn = new Node(new List<Polygon>(pair.polygons));
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
            var tn = new Node(new List<Polygon>(this.polygons));
            var pn = new Node(new List<Polygon>(pair.polygons));
            tn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            tn.Build(pn.GetPolygonData());
            tn.Invert();
            return new CsgTree(tn.GetPolygonData().ToArray());
        }
    }

    public class Plane {
        public d3 n     { get; private set; }
        public double w { get; private set; }

        static readonly double EPSILON = 1e-3d;
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

        public (Polygon onPF, Polygon onPB, Polygon face, Polygon back) SplitPolygon(Polygon src) {
            var l = src.verts.Length;
            var pType = 0;
            var vType = new int[l];

            for(var i = 0; i < l; i++) {
                var t = GetType(src.verts[i]);
                pType   |= t;
                vType[i] = t;
            }

            switch (pType) {
                default: throw new System.Exception();
                case 0: return (dot(n, src.plane.n) > 0) ? (src, null, null, null) : (null, src, null, null);
                case 1: return (null, null, src, null);
                case 2: return (null, null, null, src);
                case 3:
                    var faces = new List<d3>();
                    var backs = new List<d3>();
                    for (var i = 0; i < l; i++) {
                        var j = (i + 1) % l;
                        var si = vType[i];
                        var sj = vType[j];
                        var vi = src.verts[i];
                        var vj = src.verts[j];

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
                    if(faces.Count < 3 || backs.Count < 3) throw new System.Exception();
                    return (
                        null,
                        null,
                        new Polygon(faces.ToArray()),
                        new Polygon(backs.ToArray())
                    );
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
            
        public Polygon(Polygon src) {
            this.verts = Util.Clone(src.verts);
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }

        public void Flip(){
            this.plane.Flip();
            System.Array.Reverse(verts);
        }
    }

    public class Node {
        public Node nf { get; private set; }
        public Node nb { get; private set; }
        public Plane plane { get; private set; }
        public List<Polygon> polygons { get; private set; }

        public Node() {
            polygons = new List<Polygon>();
        }

        public Node(List<Polygon> src) {
            polygons = new List<Polygon>();
            Build(src);
        }
        
        public Node(Node n) {
            polygons = Util.Clone(n.polygons);
            nf = n.nf != null ? new Node(n.nf) : null;
            nb = n.nb != null ? new Node(n.nb) : null;
            plane = n.plane != null ? new Plane(n.plane) : null;
        }

        public void Invert() {
            for (var i = 0; i < polygons.Count; i++) polygons[i].Flip();
            plane?.Flip();
            nf?.Invert();
            nb?.Invert();
            var tmp = nf;
            nf = nb;
            nb = tmp;
        }

        List<Polygon> ClipPolygons(List<Polygon> src) {
            if (plane == null) return new List<Polygon>(src);
            var pf = new List<Polygon>();
            var pb = new List<Polygon>();
            for (var i = 0; i < src.Count; i++){
                var p = src[i];
                var o = plane.SplitPolygon(p);
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

        public List<Polygon> GetPolygonData() {
            var clone = Util.Clone(polygons);
            if(nf != null) clone.AddRange(nf.GetPolygonData());
            if(nb != null) clone.AddRange(nb.GetPolygonData());
            return clone;
        }

        public void Build(List<Polygon> src) {
            if (src.Count == 0) return;
            if (plane == null) plane = new Plane(src[0].plane);
            var pf = new List<Polygon>();
            var pb = new List<Polygon>();
            for (var i = 0; i < src.Count; i++) {
                var o = plane.SplitPolygon(src[i]);
                if (o.onPF != null) polygons.Add(o.onPF);
                if (o.onPB != null) polygons.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (pf.Count > 0) { if (nf == null) { nf = new Node(); } nf.Build(pf); }
            if (pb.Count > 0) { if (nb == null) { nb = new Node(); } nb.Build(pb); }
        }
    }

   static class Util {
        public static T[] Clone<T>(T[] src){
            var l = src.Length;
            var d = new T[l];
            System.Array.Copy(src, d, l);
            return d;
        } 

        public static Polygon[] Clone(Polygon[] src){
            var l = src.Length;
            var d = new Polygon[l];
            for (var i = 0; i < l; i++) d[i] = new Polygon(src[i]);
            return d;
        } 

        public static List<Polygon> Clone(List<Polygon> src){
            var l = src.Count;
            var d = new List<Polygon>();
            for (var i = 0; i < l; i++) d.Add(new Polygon(src[i]));
            return d;
        } 
    }
}