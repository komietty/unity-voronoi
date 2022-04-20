using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine;
using System.Linq;

namespace kmty.geom.csg {
    using V3 = Vector3;

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

        public CSG(Polygon[] polygons) { this.polygons = polygons; }

        public CSG(CSG src) { // copy constructor
            this.polygons = Util.ClonePolygons(src.polygons);
        }

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
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            pair_n.ClipTo(this_n);
            pair_n.Invert();
            this_n.Build(pair_n.GetPolygonsRecursiveBreakData());
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
        public V3 pos { get; private set; }
        public V3 nrm { get; private set; }

        public Vert(V3 p, V3 n) {
            this.pos = p;
            this.nrm = n.normalized;
        }

        public Vert(Vert v) {
            this.pos = v.pos;
            this.nrm = v.nrm.normalized;
        }

        public void Flip() { this.nrm *= -1; }
        public Vert Lerp(Vert pair, float t) {
            return new Vert(
                V3.Lerp(this.pos, pair.pos, t),
                V3.Slerp(this.nrm, pair.nrm, t)
            );
        }
    }

    public class Plane {
        public V3 n { get; private set; }
        public float w { get; private set; }

        enum Split { ONPLANE, FACE, BACK, SPAN }
        static readonly float EPSILON = 1e-5f;

        public Plane(V3 a, V3 b, V3 c) {
            this.n = V3.Cross(b - a, c - a).normalized;
            this.w = V3.Dot(n, a); 
        }
        
        public Plane(Plane src) { // copy constructor
            this.n = src.n;
            this.w = src.w; 
        }

        public void Flip(){
            this.n *= -1;
            this.w *= -1;
        }

        void DiffFromPlane(V3 p, out bool isNearPlane, out bool isFacingSide) {
            var v = V3.Dot(n, p) - this.w;
            isNearPlane  = Mathf.Abs(v) < EPSILON;
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
                    var f = V3.Dot(n, polygon.plane.n) > 0;
                    if (f) return (polygon, null, null, null);
                    else   return (null, polygon, null, null);
                case Split.FACE: return (null, null, polygon, null);
                case Split.BACK: return (null, null, null, polygon);
                case Split.SPAN:
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
                            var t = (w - V3.Dot(n, vi.pos)) / V3.Dot(n, vj.pos - vi.pos);
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

        public Polygon(Vert[] verts) {
            this.verts = verts;
            this.plane = new Plane(
                this.verts[0].pos,
                this.verts[1].pos,
                this.verts[2].pos
            );
        }
            
        public Polygon(Polygon src) { // copy constructor
            this.verts = Util.CloneArray<Vert>(src.verts);
            this.plane = new Plane(
                this.verts[0].pos,
                this.verts[1].pos,
                this.verts[2].pos
            );
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
        
        public Node(Node n) { // copy constructor
            this.polygons = Util.ClonePolygons(n.polygons);
            this.nf = n.nf != null ? new Node(n.nf) : null;
            this.nb = n.nb != null ? new Node(n.nb) : null;
            this.plane = n.plane != null ? new Plane(n.plane) : null;
        }

        public void Invert() {
            for (var i = 0; i < this.polygons.Count; i++) this.polygons[i].Flip();
            if(this.plane != null) plane.Flip();
            if(this.nf != null) nf.Invert();
            if(this.nb != null) nb.Invert();
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
            //Debug.Log(pf.Count);
            //Debug.Log(pb.Count);
            pf.AddRange(pb);
            return pf.ToArray();
        }

        public void ClipTo(Node pair) {
            this.polygons = pair.ClipPolygons(this.polygons.ToArray()).ToList();
            this.nf?.ClipTo(pair);
            this.nb?.ClipTo(pair);
        }

        public List<Polygon> GetPolygonsRecursiveBreakData() {
            if(nf != null) polygons.AddRange(nf.GetPolygonsRecursiveBreakData());
            if(nb != null) polygons.AddRange(nb.GetPolygonsRecursiveBreakData());
            return polygons;
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
            //Debug.Log($"ThisNode: {this.polygons.Count}, NF: {pf.Count}, NB: {pb.Count}");
            if (pf.Count > 0) { if (this.nf == null) this.nf = new Node(); this.nf.Build(pf); }
            if (pb.Count > 0) { if (this.nb == null) this.nb = new Node(); this.nb.Build(pb); }
        }
    }
}