using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace kmty.geom.csg {

    public static class CSGUtil {

        static Mesh Create(Mesh original, out List<Vector3> new_vrts_list, out List<Vector3> new_nrms_list) {
            var vrts = original.vertices;
            var tris = original.triangles;
            //var uvs = original.uv;
            var nrms = original.normals;
            var tans = original.tangents;
            var new_vrts = new Vector3[tris.Length];
            //var new_uvs  = new Vector2[tris.Length];
            var new_nrms = new Vector3[tris.Length];
            var new_tans = new Vector3[tris.Length];
            var new_tris = new int[tris.Length];
            var mesh = GameObject.Instantiate(original);
            
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            for (int i = 0; i < tris.Length; i++) {
                var t = tris[i];
                new_vrts[i] = vrts[t];
                //new_uvs[i]  = uvs [t];
                new_nrms[i] = nrms[t];
                new_tans[i] = tans[t];
                new_tris[i] = i;
            }

            mesh.vertices = new_vrts;
            mesh.triangles = new_tris;
            //mesh.uv = new_uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            new_vrts_list = new_vrts.ToList();
            new_nrms_list = new_nrms.ToList();
            return mesh;
        }

        public static CSG Csgnize(GameObject g){
            var mf = g.GetComponent<MeshFilter>();
            var trs = g.transform;
            var vs = new List<Vector3>();
            var ns = new List<Vector3>();
            Create(mf.sharedMesh, out vs, out ns);
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
                if (math.dot(crs, a.nrm) > 0) {
                    polys[i] = new Polygon(new Vert[] { a, b, c });
                } else {
                    polys[i] = new Polygon(new Vert[] { a, c, b });
                    Debug.LogWarning("vertices order is clockwise");
                }
            }
            return new CSG(polys);
        }


        public static Mesh Meshing(CSG csg){
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
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}