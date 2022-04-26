using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using kmty.geom.d3.delauney;
using kmty.geom.csg;

namespace kmty.geom.crackin {
    using VG = VoronoiGraph3D;

public class Crackin : MonoBehaviour {
        [SerializeField] protected GameObject tgt;
        [SerializeField] protected PhysicMaterial phy;
        [SerializeField] protected OpType op1;
        [SerializeField] protected Material mat;
        [SerializeField] protected int numPoint;
        [SerializeField, Range(-1, 100)] protected int targetID;
        [SerializeField, Range(0.1f, 10f)] float scale = 1f;

        public Mesh mesh;
        public string BakePath;
        public string BakeName;

        void Start() {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
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
                    g.transform.position = (float3)n.Value.center * 1.01f;
                    g.transform.SetParent(this.transform);
                    meshes.Add(g.transform);
                }
                count++;
            }
            sw.Stop();
            Debug.Log("build voronoi: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();

            mesh = meshes[0].GetComponent<MeshFilter>().sharedMesh;

            var tree = CSG.GenCsgTree(tgt.transform);
            for (var i = 0; i < meshes.Count; i++) {
                var t1 = new CsgTree(tree);
                var t2 = CSG.GenCsgTree(meshes[i], true);
                var o = CSG.Meshing(t1.Oparation(t2, op1));
                var g = new GameObject(i.ToString());
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                //var c = g.AddComponent<MeshCollider>();
                //var b = g.AddComponent<Rigidbody>();
                //c.convex = true;
                //c.sharedMaterial = phy;
                //c.sharedMesh = mesh;
                //b.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                //b.mass = 0.1f;
                f.mesh = o;
                r.sharedMaterial = new Material(mat);
                mat.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
            }

            sw.Stop();
            Debug.Log("generate csg: " + sw.ElapsedMilliseconds + "ms");

            tgt.SetActive(false);
            foreach (Transform t in transform) {
                if (int.Parse(t.gameObject.name) != targetID)
                    t.gameObject.SetActive(false);
            }
        }

        void OnDrawGizmosSelected(){
            Gizmos.DrawWireCube(Vector3.one * scale / 2, Vector3.one * scale);
        }
    }
}
