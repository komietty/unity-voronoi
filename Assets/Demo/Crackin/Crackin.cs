using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using kmty.geom.d3.delauney;
using kmty.geom.csg;

namespace kmty.geom.crackin {
    using VG = VoronoiGraph3D;
    using UR = UnityEngine.Random;

public class Crackin : MonoBehaviour {
        [SerializeField] protected GameObject tgt;
        [SerializeField] protected PhysicMaterial phy;
        [SerializeField] protected OpType op1;
        [SerializeField] protected Material mat;
        [SerializeField] protected int numPoint;
        [SerializeField, Range(-1, 100)] protected int targetID;
        [SerializeField, Range(0.1f, 10f)] float scale = 1f;

        public List<Rigidbody> rbs = new List<Rigidbody>();
        public string BakePath;
        public string BakeName;

        void Start() {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var state = UR.state;
            UR.InitState(1);
            var bf = new BistellarFlip3D(numPoint, scale);
            UR.state = state;
            var vg = new VG(bf.Nodes.ToArray());
            var ms = new List<(Matrix4x4 t, Mesh m)>(vg.nodes.Count);
            var itr = 0;
            foreach (var n in vg.nodes) {
                if ((targetID == -1 || itr == targetID)) {
                    n.Value.Meshilify();
                    if(n.Value.mesh == null) continue;
                    var t = Matrix4x4.TRS((float3)n.Value.center * 1.01f, Quaternion.identity, Vector3.one);
                    var m = n.Value.mesh;
                    ms.Add((t, m));
                }
                itr++;
            }

            sw.Stop();
            Debug.Log("build voronoi: " + sw.ElapsedMilliseconds + "ms");
            sw.Restart();

            var tree = CSG.GenCsgTree(tgt.transform);
            for (var i = 0; i < ms.Count; i++) {
                var m  = ms[i];
                var t1 = new CsgTree(tree);
                var t2 = CSG.GenCsgTree(m.t, m.m, true);
                var o  = CSG.Meshing(t1.Oparation(t2, op1));
                var g  = new GameObject(i.ToString());
                var f  = g.AddComponent<MeshFilter>();
                var r  = g.AddComponent<MeshRenderer>();
                var c  = g.AddComponent<MeshCollider>();
                var b  = g.AddComponent<Rigidbody>();
                c.convex = true;
                c.sharedMaterial = phy;
                c.sharedMesh = o;
                b.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                b.useGravity = false;
                f.mesh = o;
                r.sharedMaterial = new Material(mat);
                mat.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
                rbs.Add(b);
            }

            sw.Stop();
            Debug.Log("generate csg: " + sw.ElapsedMilliseconds + "ms");
            tgt.SetActive(false);
        }

        void Update() {
            if(Input.GetKeyDown(KeyCode.F))
                foreach (var b in rbs) b.useGravity = true;
        }

        void OnDrawGizmosSelected(){
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(Vector3.one * scale / 2, Vector3.one * scale);
        }
    }
}
