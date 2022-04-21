using System.Collections.Generic;
using Unity.Mathematics;

namespace kmty.geom.csg {
    using static Unity.Mathematics.math;
    using d3 = double3;

    public enum OparationType { Union, Subtraction, Intersection }

    static class Util {
        public static T[] CloneArray<T>(T[] src){
            var dst = new T[src.Length];
            System.Array.Copy(src, dst, src.Length);
            return dst;
        } 

        public static List<T> CloneList<T>(List<T> src){
            var dst = new List<T>();
            for (var i = 0; i < src.Count; i++) dst.Add(src[i]);
            return dst;
        } 

        public static Polygon[] ClonePolygons(Polygon[] src){
            var dst = new Polygon[src.Length];
            for (var i = 0; i < src.Length; i++) dst[i] = new Polygon(src[i]);
            return dst;
        } 

        public static List<Polygon> ClonePolygons(List<Polygon> src){
            var dst = new List<Polygon>();
            for (var i = 0; i < src.Count; i++) { dst.Add(new Polygon(src[i])); }
            return dst;
        } 
    }

    public class CSG {
        public Polygon[] polygons { get; protected set; }

        public CSG(Polygon[] src) { this.polygons = src; }
        public CSG(CSG src) { this.polygons = Util.ClonePolygons(src.polygons); }

        public CSG Oparation(CSG pair, OparationType op) {
            switch(op){
                case OparationType.Union:        return this.Union(pair); 
                case OparationType.Subtraction:  return this.Subtraction(pair); 
                case OparationType.Intersection: return this.Intersection(pair);
                default: throw new System.Exception();
            }
        }

        public CSG Union(CSG pair){
            var this_n = new Node(new List<Polygon>(this.polygons));
            var pair_n = new Node(new List<Polygon>(pair.polygons));
            this_n.ClipTo(pair_n);
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.Build(pair_n.GetPolygonData());
            return new CSG(this_n.GetPolygonData().ToArray());
        }

        public CSG Subtraction(CSG pair){
            var this_n = new Node(new List<Polygon>(this.polygons));
            var pair_n = new Node(new List<Polygon>(pair.polygons));
            this_n.Invert();
            this_n.ClipTo(pair_n);
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.Build(pair_n.GetPolygonData());
            this_n.Invert();
            return new CSG(this_n.GetPolygonData().ToArray());
        }

        public CSG Intersection(CSG pair){
            var this_n = new Node(new List<Polygon>(this.polygons));
            var pair_n = new Node(new List<Polygon>(pair.polygons));
            this_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.ClipTo(pair_n);
            pair_n.ClipTo(this_n);
            this_n.Build(pair_n.GetPolygonData());
            this_n.Invert();
            return new CSG(this_n.GetPolygonData().ToArray());
        }
    }

    public struct Vert {
        public readonly d3 pos;
        public d3 nrm { get; private set; }

        public Vert(d3 p, d3 n) {
            this.pos = p;
            this.nrm = normalize(n);
        }

        public Vert(Vert v) {
            this.pos = v.pos;
            this.nrm = v.nrm;
        }

        public void Flip() { this.nrm *= -1; }

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

        enum Split { ONPLANE, FACE, BACK, SPAN }
        static readonly double EPSILON = 1e-3d;

        public Plane(Vert a, Vert b, Vert c) {
            n = normalize(cross(b.pos - a.pos, c.pos - a.pos));
            w = dot(n, a.pos);
            if (dot(n, a.nrm) < 0 || dot(n, b.nrm) < 0 || dot(n, c.nrm) < 0) throw new System.Exception();
        }
        
        public Plane(Plane src) { this.n = src.n; this.w = src.w; }

        public void Flip() { this.n *= -1; this.w *= -1; }

        void DiffFromPlane(d3 p, out bool isNearPlane, out bool isFacingSide) {
            var v = dot(n, p) - this.w;
            isNearPlane  = abs(v) < EPSILON;
            isFacingSide = v > 0; 
        }

        public (Polygon onPF, Polygon onPB, Polygon face, Polygon back) SplitPolygon(Polygon polygon) {
            var l = polygon.verts.Length;
            var polySplit = 0;
            var vertSplit = new int[l];

            for(var i = 0; i < l; i++) {
                var v = polygon.verts[i]; 
                int t;
                DiffFromPlane(v.pos, out bool isNear, out bool isFace);
                if (isNear) t = (int)Split.ONPLANE;
                else t = isFace ? (int)Split.FACE : (int)Split.BACK;
                polySplit |= t;
                vertSplit[i] = t;
            }

            switch ((Split)polySplit) {
                default: throw new System.Exception();
                case Split.ONPLANE:
                    var f = math.dot(n, polygon.plane.n) > 0;
                    if (f) return (polygon, null, null, null);
                    else   return (null, polygon, null, null);
                case Split.FACE: return (null, null, polygon, null);
                case Split.BACK: return (null, null, null, polygon);
                case Split.SPAN:
                    var faces = new List<Vert>();
                    var backs = new List<Vert>();
                    for (var i = 0; i < l; i++) {
                        var j = (i + 1) % l;
                        var si = vertSplit[i];
                        var sj = vertSplit[j];
                        var vi = polygon.verts[i];
                        var vj = polygon.verts[j];
                        if (si != (int)Split.BACK) faces.Add(vi);
                        if (si != (int)Split.FACE) backs.Add(si != (int)Split.BACK ? new Vert(vi) : vi); // need to be another clone??  //if (si != (int)Split.FACE) backs.Add(vi);
                        if ((si | sj) == (int)Split.SPAN) {
                            var t = (w - dot(n, vi.pos)) / dot(n, vj.pos - vi.pos);
                            var v = vi.Lerp(vj, t);
                            faces.Add(new Vert(v));
                            backs.Add(new Vert(v));
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
            this.verts = Util.CloneArray<Vert>(src.verts);
            this.plane = new Plane(verts[0], verts[1], verts[2]);
        }

        public void Flip(){
            this.plane.Flip();
            System.Array.Reverse(verts);
            for (var i = 0; i < verts.Length; i++) verts[i].Flip();
        }
    }

    public class Node {
        public Node nf { get; private set; }
        public Node nb { get; private set; }
        public Plane plane { get; private set; }
        public List<Polygon> polygons { get; private set; }

        public Node() { this.polygons = new List<Polygon>(); }
        public Node(List<Polygon> src) { this.polygons = new List<Polygon>(); Build(src); }
        
        public Node(Node n) {
            this.polygons = Util.ClonePolygons(n.polygons);
            this.nf = n.nf != null ? new Node(n.nf) : null;
            this.nb = n.nb != null ? new Node(n.nb) : null;
            this.plane = n.plane != null ? new Plane(n.plane) : null;
        }

        public void Invert() {
            for (var i = 0; i < this.polygons.Count; i++) this.polygons[i].Flip();
            plane?.Flip();
            nf?.Invert();
            nb?.Invert();
            var tmp = this.nf;
            this.nf = this.nb;
            this.nb = tmp;
        }

        List<Polygon> ClipPolygons(List<Polygon> src) {
            if(this.plane == null) return Util.CloneList(src);
            var pf = new List<Polygon>();
            var pb = new List<Polygon>();
            for (var i = 0; i < src.Count; i++){
                var p = src[i];
                var o = this.plane.SplitPolygon(p);
                if (o.onPF != null) pf.Add(o.onPF);
                if (o.onPB != null) pb.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (this.nf != null) pf = this.nf.ClipPolygons(pf);
            if (this.nb != null) pb = this.nb.ClipPolygons(pb); else pb.Clear();
            pf.AddRange(pb);
            return pf;
        }

        public void ClipTo(Node pair) {
            this.polygons = pair.ClipPolygons(this.polygons);
            this.nf?.ClipTo(pair);
            this.nb?.ClipTo(pair);
        }

        public List<Polygon> GetPolygonData() {
            var clone = Util.ClonePolygons(this.polygons);
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