using UnityEngine;

namespace kmty.geom.d2.delaunay {
    using DN = DelaunayGraphNode2D;
    using VG = VoronoiGraph2D;

    public class Drawer2D : MonoBehaviour {
        public Material mat;
        public int numPoint;

        BistellarFlip2D bf;
        VG voronoi;
        DN[] nodes;

        void Start() {
            bf = new BistellarFlip2D(numPoint);
            nodes = bf.Nodes.ToArray();
            voronoi = new VG(nodes);
            foreach (var n in voronoi.nodes) {
                n.Value.Meshilify();
                var g = new GameObject();
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                var m = new Material(mat);
                m.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
                r.sharedMaterial = m;
                f.mesh = n.Value.mesh;
                g.transform.SetParent(this.transform);
            }
        }

/*
        void OnRenderObject() {
            if (showVoronoi) DrawVoronoi();
            else DrawDelaunay();
        }

        void DrawVoronoi() {
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            foreach (var n in voronoi.nodes) {
                foreach (var s in n.Value.segments) {
                    GL.Vertex((Vector2)s.a);
                    GL.Vertex((Vector2)s.b);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        void DrawDelaunay() {
            GL.PushMatrix();
            GL.LoadOrtho();
            for (int i = 0; i < nodes.Length; i++) {
                var t = nodes[i].triangle;
                GL.Begin(GL.LINE_STRIP);
                GL.Vertex(float3(t.a, 0));
                GL.Vertex(float3(t.b, 0));
                GL.Vertex(float3(t.c, 0));
                GL.Vertex(float3(t.a, 0));
                GL.End();
                if (drawCirlcle) {
                    GL.Begin(GL.LINE_STRIP);
                    var c = t.GetCircumscribledCircle();
                    for (float j = 0; j < Mathf.PI * 2.1f; j += Mathf.PI * 0.03f)
                        GL.Vertex(new Vector2(cos(j), sin(j)) * c.radius + (Vector2)c.center);
                    GL.End();
                }
            }
            GL.PopMatrix();
        }

        void DrawTriangle(Triangle t, int pass) {
            GL.Begin(GL.TRIANGLES);
            GL.Vertex(float3(t.a, 0));
            GL.Vertex(float3(t.b, 0));
            GL.Vertex(float3(t.c, 0));
            GL.Vertex(float3(t.a, 0));
            GL.End();
        }
*/
    }
}
