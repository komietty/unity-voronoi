using UnityEngine;
using Unity.Mathematics;

namespace kmty.geom.d3.delauney {
    using d3 = double3;
    using f3 = float3;
    using DN = DelaunayGraphNode3D;
    using VG = VoronoiGraph3D;
    public class Drawer3D : MonoBehaviour {

        public Material mat;
        public Material mat2;
        public int numPoint;
        public KeyCode reset = KeyCode.R;
        public bool createMesh;
        public bool showDebugCube;
        public bool showDelaunay;
        public bool showVoronoi;
        [Range(0, 100)] public int debugDNodeId;
        [Range(0, 100)] public int debugVNodeId;

        BistellarFlip3D bf;
        VG voronoi;
        DN[] nodes;


        void Init() {
            bf = new BistellarFlip3D(numPoint, 1);
            nodes = bf.Nodes.ToArray();
            voronoi = new VG(nodes);
            if (createMesh) {
                int count = 0;
                foreach (var n in voronoi.nodes) {
                    if (true || count == debugVNodeId) {
                        n.Value.Meshilify();
                        if (n.Value.mesh != null) {
                            var g = new GameObject(count.ToString());
                            var f = g.AddComponent<MeshFilter>();
                            var r = g.AddComponent<MeshRenderer>();
                            var c = g.AddComponent<MeshCollider>();
                            r.sharedMaterial = mat2;
                            f.mesh = n.Value.mesh;
                            c.sharedMesh = n.Value.mesh;
                            g.transform.position = (float3)n.Value.center * 1.1f;
                            g.transform.SetParent(this.transform);
                        }
                    }
                    count++;
                }
            }
        }

        void Start() { Init(); }
        void Update() {
            debugDNodeId = Mathf.Clamp(debugDNodeId, 0, nodes.Length - 1);
            if (Input.GetKeyDown(reset)) Init();
        }

        void OnDrawGizmos() {
            if(!Application.isPlaying) return;
            int count = 0;
            foreach (var n in voronoi.nodes) {
                if (count == debugVNodeId) {
                    var c = (f3)n.Value.center;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(c, 0.02f);
                    foreach (var f in n.Value.faces) {
                        Gizmos.color = Color.cyan;
                        //Gizmos.DrawWireSphere((float3)f.faceCenter, 0.01f);
                    }
                }
                count++;
            }
        }

        void OnRenderObject() {
            if (showDebugCube) DrawUnitCube();
            if (showDelaunay) DrawDelauney(nodes);
            if (showVoronoi) DrawVoronoi();
        }

        void DrawDelauney(DelaunayGraphNode3D[] nodes) {
            var n = nodes[debugDNodeId];
            var t = n.tetrahedra;
            GL.PushMatrix();
            foreach (var _n in nodes) DrawTetrahedra(_n.tetrahedra, 0);
            //n.neighbor.ForEach(_n => DrawTetrahedra(_n.tetrahedra, 4));
            //DrawTetrahedraSpecifid(t, 1);
            GL.PopMatrix();
        }

        void DrawVoronoi() {
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            int count = 0;
            foreach (var n in voronoi.nodes) {
                if (count == debugVNodeId) {
                    foreach (var s in n.Value.segments) {
                        mat.SetPass(3);
                        GL.Vertex((f3)s.sg.a);
                        GL.Vertex((f3)s.sg.b);
                    }
                }
                count++;
            }
            GL.End();
            GL.PopMatrix();
        }

        void DrawTetrahedra(Tetrahedra t, int pass) {
            mat.SetPass(pass);
            GL.Begin(GL.LINE_STRIP); GL.Vertex((f3)t.a); GL.Vertex((f3)t.b); GL.Vertex((f3)t.c); GL.End();
            GL.Begin(GL.LINE_STRIP); GL.Vertex((f3)t.a); GL.Vertex((f3)t.c); GL.Vertex((f3)t.d); GL.End();
            GL.Begin(GL.LINE_STRIP); GL.Vertex((f3)t.a); GL.Vertex((f3)t.d); GL.Vertex((f3)t.b); GL.End();
        }

        void DrawTetrahedraSpecifid(Tetrahedra t, int pass) {
            mat.SetPass(pass);
            GL.Begin(GL.TRIANGLES);
            GL.Vertex((f3)t.a); GL.Vertex((f3)t.b); GL.Vertex((f3)t.c);
            GL.Vertex((f3)t.b); GL.Vertex((f3)t.c); GL.Vertex((f3)t.d);
            GL.Vertex((f3)t.c); GL.Vertex((f3)t.d); GL.Vertex((f3)t.a);
            GL.Vertex((f3)t.d); GL.Vertex((f3)t.a); GL.Vertex((f3)t.b);
            GL.End();
        }

        void DrawUnitCube() {
            GL.PushMatrix();
            mat.SetPass(0);
            GL.Begin(GL.LINE_STRIP); GL.Vertex(new f3(1, 0, 0)); GL.Vertex(new f3(0, 0, 0)); GL.Vertex(new f3(0, 1, 0)); GL.Vertex(new f3(1, 1, 0)); GL.End();
            GL.Begin(GL.LINE_STRIP); GL.Vertex(new f3(1, 0, 1)); GL.Vertex(new f3(1, 0, 0)); GL.Vertex(new f3(1, 1, 0)); GL.Vertex(new f3(1, 1, 1)); GL.End();
            GL.Begin(GL.LINE_STRIP); GL.Vertex(new f3(0, 0, 1)); GL.Vertex(new f3(1, 0, 1)); GL.Vertex(new f3(1, 1, 1)); GL.Vertex(new f3(0, 1, 1)); GL.End();
            GL.Begin(GL.LINE_STRIP); GL.Vertex(new f3(0, 0, 0)); GL.Vertex(new f3(0, 0, 1)); GL.Vertex(new f3(0, 1, 1)); GL.Vertex(new f3(0, 1, 0)); GL.End();
            GL.PopMatrix();
        }
    }
}
