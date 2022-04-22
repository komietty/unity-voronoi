using System.Collections.Generic;
using Unity.Mathematics;

namespace kmty.geom.csg {
    using static Unity.Mathematics.math;
    using d3 = double3;

    public enum OparationType { Union, Subtraction, Intersection }

    static class Util {
        public static T[] CloneArray<T>(T[] src){
            var l = src.Length;
            var d = new T[l];
            System.Array.Copy(src, d, l);
            return d;
        } 

        public static Polygon[] ClonePolygons(Polygon[] src){
            var l = src.Length;
            var d = new Polygon[l];
            for (var i = 0; i < l; i++) d[i] = new Polygon(src[i]);
            return d;
        } 

        public static List<Polygon> ClonePolygons(List<Polygon> src){
            var l = src.Count;
            var d = new List<Polygon>();
            for (var i = 0; i < l; i++) d.Add(new Polygon(src[i]));
            return d;
        } 
    }

    public class CSG {
        public Polygon[] polygons { get; }

        public CSG(Polygon[] src) { polygons = src; }
        public CSG(CSG src) { polygons = Util.ClonePolygons(src.polygons); }

        public CSG Oparation(CSG pair, OparationType op) {
            switch(op){
                case OparationType.Union:        return Union(pair); 
                case OparationType.Subtraction:  return Subtraction(pair); 
                case OparationType.Intersection: return Intersection(pair);
                default: throw new System.Exception();
            }
        }

        public CSG Union(CSG pair){
            var tn = new Node(new List<Polygon>(this.polygons));
            var pn = new Node(new List<Polygon>(pair.polygons));
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            pn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.Build(pn.GetPolygonData());
            return new CSG(tn.GetPolygonData().ToArray());
        }

        public CSG Subtraction(CSG pair){
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
            return new CSG(tn.GetPolygonData().ToArray());
        }

        public CSG Intersection(CSG pair){
            var tn = new Node(new List<Polygon>(this.polygons));
            var pn = new Node(new List<Polygon>(pair.polygons));
            tn.Invert();
            pn.ClipTo(tn);
            pn.Invert();
            tn.ClipTo(pn);
            pn.ClipTo(tn);
            tn.Build(pn.GetPolygonData());
            tn.Invert();
            return new CSG(tn.GetPolygonData().ToArray());
        }
    }

    public struct Vert {
        public d3 pos { get; }
        public d3 nrm { get; }
        public Vert flipped => new Vert(pos, -nrm);

        public Vert(d3 p, d3 n) {
            pos = p;
            nrm = normalize(n);
        }

        public Vert Lerp(Vert pair, double t) {
            return new Vert(
                lerp(this.pos, pair.pos, t),
                normalize(lerp(this.nrm, pair.nrm, t)) //slerp
            );
        }
    }

    public class Plane {
        public d3 n     { get; private set; }
        public double w { get; private set; }

        static readonly double EPSILON = 1e-4d;
        static readonly int ONPLANE = 0;
        static readonly int FACE    = 1;
        static readonly int BACK    = 2;
        static readonly int SPAN    = 3;

        public Plane(Plane src) { n = src.n; w = src.w; }

        public Plane(Vert a, Vert b, Vert c) {
            n = normalize(cross(b.pos - a.pos, c.pos - a.pos));
            w = dot(n, a.pos);
            if (dot(n, a.nrm) < 0 || dot(n, b.nrm) < 0 || dot(n, c.nrm) < 0) throw new System.Exception();
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
                var t = GetType(src.verts[i].pos);
                pType   |= t;
                vType[i] = t;
            }

            switch (pType) {
                default: throw new System.Exception();
                case 0: return (dot(n, src.plane.n) > 0) ? (src, null, null, null) : (null, src, null, null);
                case 1: return (null, null, src, null);
                case 2: return (null, null, null, src);
                case 3:
                    var faces = new List<Vert>();
                    var backs = new List<Vert>();
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
                            var t = (w - dot(n, vi.pos)) / dot(n, vj.pos - vi.pos);
                            var v = vi.Lerp(vj, t);
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
        public Vert[] verts { get; private set; }
        public Plane plane  { get; private set; }

        public Polygon(Vert[] vs) {
            this.verts = vs;
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }
            
        public Polygon(Polygon src) {
            this.verts = Util.CloneArray(src.verts);
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }

        public void Flip(){
            this.plane.Flip();
            System.Array.Reverse(verts);
            for (var i = 0; i < verts.Length; i++) verts[i] = verts[i].flipped;
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
            polygons = Util.ClonePolygons(n.polygons);
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
            var clone = Util.ClonePolygons(polygons);
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
}