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
        [SerializeField, Range(0.1f, 2f)] float scale = 1f;

        void Start() {
            var bf = new BistellarFlip3D(numPoint, scale);
            var nodes = bf.Nodes.ToArray();
            var voronoi = new VG(nodes);
            var meshes = new List<Transform>();

            foreach (var n in voronoi.nodes) {
                if (math.abs(n.Value.center.x - scale * 0.5f) < scale * 0.45f &&
                    math.abs(n.Value.center.y - scale * 0.5f) < scale * 0.45f &&
                    math.abs(n.Value.center.z - scale * 0.5f) < scale * 0.45f
                    ) {
                    n.Value.Meshilify();
                    var g = new GameObject();
                    var f = g.AddComponent<MeshFilter>();
                    var r = g.AddComponent<MeshRenderer>();
                    r.sharedMaterial = mat;
                    f.mesh = n.Value.mesh;
                    g.transform.position = (float3)n.Value.center;
                    g.transform.SetParent(this.transform);
                    meshes.Add(g.transform);
                }
            }


            var t1 = CSG.GenCsgTree(tgt.transform);
            //foreach (var m in meshes) {
            for (var i = 0; i < 2; i++) {
                var m = meshes[i];
                var t2 = CSG.GenCsgTree(m);
                var o = CSG.Meshing(t1.Oparation(t2, op1));
                var g = new GameObject();
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                f.mesh = o;
                r.sharedMaterial = new Material(mat);
                mat.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
            }

            tgt.SetActive(false);
            foreach (Transform t in transform) { t.gameObject.SetActive(false); }
        }

        void OnDrawGizmos(){
            Gizmos.DrawWireCube(Vector3.one * scale / 2, Vector3.one * scale);
        }

    }
}
