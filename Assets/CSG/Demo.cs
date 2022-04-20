using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;
using remesher;

namespace kmty.geom.csg.demo {
    public class Demo : MonoBehaviour {
        [SerializeField] protected GameObject g1;
        [SerializeField] protected GameObject g2;
        [SerializeField] protected GameObject g3;

        void Start() {
            var mf  = GetComponent<MeshFilter>();
            var csg1 = Csgnize(g1);
            var csg2 = Csgnize(g2);
            var csg3 = Csgnize(g3);
            //var n2 = new Node(csg2.polygons.ToList());
            //mf.mesh = Meshing(new CSG(n2.GetPolygonsRecursiveBreakData().ToArray()));
            //var csgA = csg1.Union(csg2);
            var csgA = csg1.Subtraction(csg2);
            var csgB = csg3.Subtraction(csgA);
            mf.mesh = Meshing(csgB);
            foreach(Transform t in transform){
                t.gameObject.SetActive(false);
            }
        }

        CSG Csgnize(GameObject g){
            var mf = g.GetComponent<MeshFilter>();
            var trs = g.transform;
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var mesh = MeshFragmentizer.Create(mf.mesh, out vs, out ns);
            mf.mesh = mesh;
            Assert.IsTrue(vs.Count % 3 == 0);

            var verts = new Vert[vs.Count];
            var polys = new Polygon[vs.Count / 3];
            for (var i = 0; i < verts.Length; i++) {
                verts[i] = new Vert(
                    (float3)trs.TransformPoint(vs[i]),
                    (float3)trs.TransformDirection(ns[i])
                );
            }

            for (var i = 0; i < polys.Length; i++) {
                var a = verts[i * 3 + 0];
                var b = verts[i * 3 + 1];
                var c = verts[i * 3 + 2];
                var crs = math.cross(b.pos - a.pos, c.pos - a.pos);
                if (math.dot(crs, a.nrm) > 0)
                    polys[i] = new Polygon(new Vert[] { a, b, c });
                else
                    polys[i] = new Polygon(new Vert[] { a, c, b });
            }
            return new CSG(polys);
        }

        Mesh Meshing(CSG csg){
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var ts = new List<int>();
            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var count = 0;
            for (var j = 0; j < csg.polygons.Length; j++) {
                var p = csg.polygons[j];
                for (var i = 3; i <= p.verts.Length; i++) {
                    vs.Add((float3)p.verts[0].pos);
                    vs.Add((float3)p.verts[i - 2].pos);
                    vs.Add((float3)p.verts[i - 1].pos);
                    ns.Add((float3)p.verts[0].nrm);
                    ns.Add((float3)p.verts[i - 2].nrm);
                    ns.Add((float3)p.verts[i - 1].nrm);

                    ts.Add(count++);
                    ts.Add(count++);
                    ts.Add(count++);
                }
            }
            mesh.SetVertices(vs);
            mesh.SetNormals(ns);
            mesh.SetTriangles(ts, 0);
            //mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            //mesh.RecalculateTangents();
            return mesh;
        }
    }
}
