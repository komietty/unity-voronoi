using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using kmty.geom.d3.delauney;
using kmty.geom.csg;
using System.Linq;

namespace kmty.geom.crackin {
    using VG = VoronoiGraph3D;

public class Crackin : MonoBehaviour {
        [SerializeField] protected GameObject tgt;
        [SerializeField] protected OpType op1;
        [SerializeField] protected Material mat;
        [SerializeField] protected int numPoint;
        [SerializeField, Range(-1, 100)] protected int targetID;
        [SerializeField, Range(0.1f, 10f)] float scale = 1f;

        public Mesh mesh;
        public string BakePath;
        public string BakeName;

        void Start() {
            var bf = new BistellarFlip3D(numPoint, scale);
            var nodes = bf.Nodes.ToArray();
            var voronoi = new VG(nodes);
            var meshes = new List<Transform>();
            var count = 0;
            foreach (var n in voronoi.nodes) {
                if ((targetID == -1 || count == targetID)) {
                    n.Value.Meshilify();
                    if(n.Value.mesh == null) continue;
                    var g = new GameObject(count.ToString());
                    var f = g.AddComponent<MeshFilter>();
                    var r = g.AddComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    f.mesh = n.Value.mesh;
                    //f.mesh = Weld(n.Value.mesh);
                    g.transform.position = (float3)n.Value.center;
                    g.transform.SetParent(this.transform);
                    meshes.Add(g.transform);
                }
                count++;
            }

            mesh = meshes[0].GetComponent<MeshFilter>().sharedMesh;

            var tree = CSG.GenCsgTree(tgt.transform);
            for (var i = 0; i < meshes.Count; i++) {
                var t1 = new CsgTree(tree);
                var t2 = CSG.GenCsgTree(meshes[i], true);
                //var t2 = CSG.GenCsgTree(meshes[i]);
                var o = CSG.Meshing(t1.Oparation(t2, op1));
                var g = new GameObject(i.ToString());
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                f.mesh = o;
                r.sharedMaterial = new Material(mat);
                mat.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
            }
            /*
            var _o = CSG.Meshing(t1);
            var _g = new GameObject();
            var _f = _g.AddComponent<MeshFilter>();
            var _r = _g.AddComponent<MeshRenderer>();
            _f.mesh = _o;
            _r.sharedMaterial = new Material(mat);
            */

            tgt.SetActive(false);
            foreach (Transform t in transform) {
                if (int.Parse(t.gameObject.name) != targetID)
                    t.gameObject.SetActive(false);
            }
        }

        void OnDrawGizmos(){
            Gizmos.DrawWireCube(Vector3.one * scale / 2, Vector3.one * scale);
        }

        Mesh Weld(Mesh original) {
            var ogl_vrts = original.vertices;
            var ogl_idcs = original.triangles;
            var alt_mesh = new Mesh();
            var alt_vrts = ogl_vrts.Distinct().ToArray();
            var alt_idcs = new int[ogl_idcs.Length];
            var vrt_rplc = new int[ogl_vrts.Length];
            for (var i = 0; i < ogl_vrts.Length; i++) {
                var o = -1;
                for (var j = 0; j < alt_vrts.Length; j++) {
                    //if (alt_vrts[j] == ogl_vrts[i]) { o = j; break; }
                    if (Vector3.Distance(alt_vrts[j], ogl_vrts[i]) < 0.01f) { o = j; break; }
                }
                vrt_rplc[i] = o;
            }

            for (var i = 0; i < alt_idcs.Length; i++) {
                alt_idcs[i] = vrt_rplc[ogl_idcs[i]];
            }
            alt_mesh.SetVertices(alt_vrts);
            alt_mesh.SetTriangles(alt_idcs, 0);
            alt_mesh.RecalculateBounds();
            alt_mesh.RecalculateNormals();
            alt_mesh.RecalculateTangents();
            return alt_mesh;
        }

    }
}
