using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using kmty.geom.d3.delauney;
using kmty.geom.csg;

namespace kmty.geom.crackin {
    using VG = VoronoiGraph3D;

public class Crackin : MonoBehaviour {
        [SerializeField] protected GameObject tgt;
        [SerializeField] protected OpType op1;
        [SerializeField] protected Material mat;
        [SerializeField] protected int numPoint;
        [SerializeField, Range(-1, 100)] protected int targetID;
        [SerializeField, Range(0.1f, 2f)] float scale = 1f;

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
                if (math.abs(n.Value.center.x - scale * 0.5f) < scale * 0.45f &&
                    math.abs(n.Value.center.y - scale * 0.5f) < scale * 0.45f &&
                    math.abs(n.Value.center.z - scale * 0.5f) < scale * 0.45f
                    ) {
                    if (targetID == -1 || count == targetID) {
                        n.Value.Meshilify();
                        var g = new GameObject(count.ToString());
                        var f = g.AddComponent<MeshFilter>();
                        var r = g.AddComponent<MeshRenderer>();
                        r.sharedMaterial = mat;
                        f.mesh = n.Value.mesh;
                        g.transform.position = (float3)n.Value.center;
                        g.transform.SetParent(this.transform);
                        meshes.Add(g.transform);
                    }
                    count++;
                }
            }

            mesh = meshes[0].GetComponent<MeshFilter>().sharedMesh;

            var t1 = CSG.GenCsgTree(tgt.transform);
            for (var i = 0; i < meshes.Count; i++) {
                var t2 = CSG.GenCsgTree(meshes[i], true);
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

    }
}
