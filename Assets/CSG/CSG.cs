using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Mathematics;
using System.Linq;

namespace kmty.geom.csg {
    using d3 = double3;

    static class Util {
        public static T[] CloneArray<T>(T[] src){
            var dst = new T[src.Length];
            System.Array.Copy(src, dst, src.Length);
            return dst;
        } 

        public static Polygon[] ClonePolygons(Polygon[] src){
            var dst = new Polygon[src.Length];
            for (var i = 0; i < src.Length; i++) { dst[i] = new Polygon(src[i]); }
            return dst;
        } 

        public static List<Polygon> ClonePolygons(List<Polygon> src){
            var dst = new List<Polygon>();
            for (var i = 0; i < src.Count; i++) { dst.Add(new Polygon(src[i])); }
            return dst;
        } 
    }

    class CSG {
        public Polygon[] polygons { get; protected set; }

        public CSG(Polygon[] src) { this.polygons = src; }
        public CSG(CSG src) { this.polygons = Util.ClonePolygons(src.polygons); }

        public CSG Inverse(){
            var clone = new CSG(this);
            for (var i = 0; i < clone.polygons.Length; i++) {
                clone.polygons[i].Flip();
            }
            return clone;
        }

        public CSG Union(CSG pair){
            var this_n = new Node(this.polygons.ToList());
            var pair_n = new Node(pair.polygons.ToList());
            this_n.ClipTo(pair_n);
            //pair_n.ClipTo(this_n);
            //pair_n.Invert();
            //pair_n.ClipTo(this_n);
            //pair_n.Invert();
            //this_n.Build(pair_n.GetPolygonsRecursiveBreakData());
            var polygons = this_n.GetPolygonsRecursiveBreakData().ToArray();
            return new CSG(polygons);
        }

        public CSG Subtraction(CSG pair){
            var this_n = new Node(this.polygons.ToList());
            var pair_n = new Node(pair.polygons.ToList());
            this_n.Invert();
            this_n.ClipTo(pair_n);
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.Build(pair_n.GetPolygonsRecursiveBreakData());
            this_n.Invert();
            var polygons = this_n.GetPolygonsRecursiveBreakData().ToArray();
            return new CSG(polygons);
        }

        public CSG Intersection(CSG pair){
            var this_n = new Node(this.polygons.ToList());
            var pair_n = new Node(pair.polygons.ToList());
            this_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.ClipTo(pair_n);
            pair_n.ClipTo(this_n);
            this_n.Build(pair_n.GetPolygonsRecursiveBreakData());
            this_n.Invert();
            var polygons = this_n.GetPolygonsRecursiveBreakData().ToArray();
            return new CSG(polygons);
        }
    }

    public struct Vert {
        public d3 pos { get; private set; }
        public d3 nrm { get; private set; }

        public Vert(d3 p, d3 n) {
            this.pos = p;
            this.nrm = math.normalize(n);
        }

        public Vert(Vert v) {
            this.pos = v.pos;
            this.nrm = math.normalize(v.nrm);
        }

        public void Flip() { this.nrm *= -1; }
        public Vert Lerp(Vert pair, double t) {
            return new Vert(
                math.lerp(this.pos, pair.pos, t),
                math.lerp(this.nrm, pair.nrm, t) //slerp
            );
        }
    }

    public class Plane {
        public d3 n { get; private set; }
        public double w { get; private set; }

        enum Split { ONPLANE, FACE, BACK, SPAN }
        static readonly double EPSILON = 1e-5d;

        //public Plane(V3 a, V3 b, V3 c, V3 nrm) {
        //    var n1 = V3.Cross(b - a, c - a).normalized;
        //    var n2 = V3.Cross(c - a, b - a).normalized;
        //    if (V3.Dot(n1, nrm) == 0) Debug.LogError(); 
        //    if (V3.Dot(n1, nrm) > 0) this.n = n1;
        //    else { this.n = n2;}
        //    this.w = V3.Dot(n, a); 
        //}

        public Plane(Vert a, Vert b, Vert c) {
            var n1 = math.normalize(math.cross(b.pos - a.pos, c.pos - a.pos));
            var n2 = math.normalize(math.cross(c.pos - a.pos, b.pos - a.pos));
            if (math.dot(n1, n2) > 0) { Debug.LogError(math.dot(n1, n2)); }
            Assert.IsTrue(math.dot(n1, a.nrm) != 0);

            if (math.dot(n1, a.nrm) > 0) { this.n = n1; }
            else                         { this.n = n2; Debug.Log("rot vert order"); }

            this.w = math.dot(n, a.pos);
        }
        
        public Plane(Plane src) { // copy constructor
            this.n = src.n;
            this.w = src.w; 
        }

        public void Flip(){
            this.n *= -1;
            this.w *= -1;
        }

        void DiffFromPlane(d3 p, out bool isNearPlane, out bool isFacingSide) {
            var v = math.dot(n, p) - this.w;
            isNearPlane  = math.abs(v) < EPSILON;
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
                    //Debug.LogWarning("aaa");
                    var faces = new List<Vert>();
                    var backs = new List<Vert>();
                    for (var i = 0; i < polygon.verts.Length; i++) {
                        var j = (i + 1) % polygon.verts.Length;
                        var si = vertSplit[i];
                        var sj = vertSplit[j];
                        var vi = polygon.verts[i];
                        var vj = polygon.verts[j];
                        if (si != (int)Split.BACK) faces.Add(vi);
                        if (si != (int)Split.FACE) backs.Add(si != (int)Split.BACK ? new Vert(vi) : vi); // need to be another clone??
                        if ((si | sj) == (int)Split.SPAN) {
                            var t = (w - math.dot(n, vi.pos)) / math.dot(n, vj.pos - vi.pos);
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
            plane.Flip();
            System.Array.Reverse(verts);
            for (var i = 0; i < verts.Length; i++) verts[i].Flip();
        }
    }

    public class Node {
        public Node nf { get; private set; }
        public Node nb { get; private set; }
        public Plane plane { get; private set; }
        public List<Polygon> polygons { get; private set; }

        public Node() {
            this.nf = null;
            this.nb = null;
            this.plane = null;
            this.polygons = new List<Polygon>();
        }

        public Node(List<Polygon> polygons) {
            this.nf = null;
            this.nb = null;
            this.plane = null;
            this.polygons = new List<Polygon>();
            Build(polygons);
        }
        
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

        Polygon[] ClipPolygons(Polygon[] src) {
            if(this.plane == null) return Util.CloneArray(src);
            var pf = new List<Polygon>();
            var pb = new List<Polygon>();
            for (var i = 0; i < src.Length; i++){
                var p = src[i];
                var o = this.plane.SplitPolygon(p);
                if (o.onPF != null) pf.Add(o.onPF);
                if (o.onPB != null) pb.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (this.nf != null) pf = this.nf.ClipPolygons(pf.ToArray()).ToList();
            if (this.nb != null) pb = this.nb.ClipPolygons(pb.ToArray()).ToList(); else pb.Clear();
            pf.AddRange(pb);
            return pf.ToArray();
        }

        public void ClipTo(Node pair) {
            this.polygons = pair.ClipPolygons(this.polygons.ToArray()).ToList();
            this.nf?.ClipTo(pair);
            this.nb?.ClipTo(pair);
        }

        public List<Polygon> GetPolygonsRecursiveBreakData() {
            var clone = Util.ClonePolygons(this.polygons);
            if(nf != null) clone.AddRange(nf.GetPolygonsRecursiveBreakData());
            if(nb != null) clone.AddRange(nb.GetPolygonsRecursiveBreakData());
            return clone;
        }

        public void Build(List<Polygon> src) {
            if (src.Count == 0) return;
            if (this.plane == null) this.plane = new Plane(src[0].plane);
            var pf = new List<Polygon>();
            var pb = new List<Polygon>();
            for (var i = 0; i < src.Count; i++) {
                var o = plane.SplitPolygon(src[i]);
                if (o.onPF != null) this.polygons.Add(o.onPF);
                if (o.onPB != null) this.polygons.Add(o.onPB);
                if (o.face != null) pf.Add(o.face);
                if (o.back != null) pb.Add(o.back);
            }
            if (pf.Count > 0) { if (this.nf == null) this.nf = new Node(); this.nf.Build(pf); }
            if (pb.Count > 0) { if (this.nb == null) this.nb = new Node(); this.nb.Build(pb); }
        }
    }
}