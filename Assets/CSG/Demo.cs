using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using remesher;

namespace kmty.geom.csg.demo {
    public class Demo : MonoBehaviour {
        [SerializeField] protected GameObject g1;
        [SerializeField] protected GameObject g2;

        void Start() {
            var csg1 = Csgnize(g1);
            var csg2 = Csgnize(g2);
            var csg3 = csg1.Union(csg2);
            var mf  = GetComponent<MeshFilter>();
            var mf1 = g1.GetComponent<MeshFilter>();
            var mf2 = g2.GetComponent<MeshFilter>();
            mf.mesh = Meshing(csg3);
            g1.SetActive(false);
            g2.SetActive(false);

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
                    trs.TransformPoint(vs[i]),
                    trs.TransformDirection(ns[i])
                    );
            }

            for (var i = 0; i < polys.Length; i++) {
                polys[i] = new Polygon(new Vert[] {
                    verts[i * 3 + 0],
                    verts[i * 3 + 1],
                    verts[i * 3 + 2]
                });
            }
            return new CSG(polys);
        }

        Mesh Meshing(CSG csg){
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            var ts = new List<int>();
            var mesh = new Mesh();
            var count = 0;
            for (var j = 0; j < csg.polygons.Length; j++) {
                var p = csg.polygons[j];
                for (var i = 3; i <= p.verts.Length; i++) {
                    vs.Add(p.verts[0].pos);
                    vs.Add(p.verts[i - 2].pos);
                    vs.Add(p.verts[i - 1].pos);
                    ns.Add(p.verts[0].nrm);
                    ns.Add(p.verts[i - 2].nrm);
                    ns.Add(p.verts[i - 1].nrm);

                    ts.Add(count++);
                    ts.Add(count++);
                    ts.Add(count++);
                }
            }
            mesh.SetVertices(vs);
            mesh.SetNormals(ns);
            mesh.SetTriangles(ts, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
