using UnityEngine;
using Unity.Mathematics;

namespace kmty.geom.d3.delauney {
    using d3 = double3;
    using f3 = float3;
    public class Drawer3D : MonoBehaviour {

        public Material mat;
        public int numPoint;
        public KeyCode reset = KeyCode.R;
        public bool showDebugCube;
        public bool showVoronoi;
        [Range(0, 100)] public int debugNodeId;

        BistellarFlip3D bf;
        VoronoiTestViewer voronoi;
        DelaunayGraphNode3D[] nodes;


        void Init() {
            bf = new BistellarFlip3D(numPoint);
            nodes = bf.Nodes.ToArray();
            voronoi = new VoronoiTestViewer(nodes);
        }

        void Start() { Init(); }
        void Update() {
            debugNodeId = Mathf.Clamp(debugNodeId, 0, nodes.Length - 1);
            if (Input.GetKeyDown(reset)) Init();
        }

        void OnRenderObject() {
            if (showDebugCube) DrawUnitCube();
            if (showVoronoi) DrawVoronoi();
            else DrawDelauney(nodes);
        }

        void DrawDelauney(DelaunayGraphNode3D[] nodes) {
            var n = nodes[debugNodeId];
            var t = n.tetrahedra;
            GL.PushMatrix();
            foreach (var _n in nodes) DrawTetrahedra(_n.tetrahedra, 0);
            n.neighbor.ForEach(_n => DrawTetrahedra(_n.tetrahedra, 4));
            DrawTetrahedraSpecifid(t, 1);
            GL.PopMatrix();
        }

        void DrawVoronoi() {
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            foreach (var s in voronoi.segments) {
                mat.SetPass(3);
                GL.Vertex((f3)s.a);
                GL.Vertex((f3)s.b);
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
