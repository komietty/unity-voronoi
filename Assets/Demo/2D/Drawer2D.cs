using UnityEngine;
using kmty.geom.d2;
using static Unity.Mathematics.math;
using DN = DelaunayGraphNode2D;
using VG = VoronoiGraph2D;

public class Drawer2D : MonoBehaviour {
    public Material mat;
    public Material mat2;
    public int numPoint;
    public KeyCode reset = KeyCode.R;
    public bool createMesh;
    public bool drawCirlcle;
    public bool showVoronoi;
    [Range(0, 100)] public int debugNodeId;
    BistellarFlip2D bf;
    VG voronoi;
    DN[] nodes;
    GameObject[] gos;

    void Init() {
        bf = new BistellarFlip2D(numPoint);
        nodes = bf.GetResult().ToArray();
        voronoi = new VG(nodes);
        if (createMesh) {
            foreach (var n in voronoi.nodes) {
                n.Value.Meshilify();
                var g = new GameObject();
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat2;
                f.mesh = n.Value.mesh;
                g.transform.position = Vector3.forward * Random.value;
                g.transform.SetParent(this.transform);
            }
        }
    }

    void Start() { Init(); }
    void Update() {
        if (Input.GetKeyDown(reset)) Init();
    }
    void OnRenderObject() {
        if (showVoronoi) DrawVoronoi();
        else DrawDelaunay();
    }

    void DrawVoronoi() {
        mat.SetPass(0);
        GL.PushMatrix();
        //GL.LoadOrtho();
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

    #region delaunay gizmo
    void DrawDelaunay() {
        GL.PushMatrix();
        //GL.LoadOrtho();
        for (int i = 0; i < nodes.Length; i++) {
            var t = nodes[i].triangle;
            mat.SetPass(0);
            GL.Begin(GL.LINE_STRIP);
            GL.Vertex(float3(t.a, 0));
            GL.Vertex(float3(t.b, 0));
            GL.Vertex(float3(t.c, 0));
            GL.Vertex(float3(t.a, 0));
            GL.End();
            if (i == debugNodeId) {
                var neighbors = nodes[i].neighbor;
                DrawTriangle(t, 0);
                neighbors.ForEach(n => DrawTriangle(n.triangle, 1));
            }
            if (drawCirlcle) {
                mat.SetPass(1);
                GL.Begin(GL.LINE_STRIP);
                var c = t.circumscribedCircle;
                for (float j = 0; j < Mathf.PI * 2.1f; j += Mathf.PI * 0.03f)
                    GL.Vertex(new Vector2(cos(j), sin(j)) * c.radius + (Vector2)c.center);
                GL.End();
            }
        }
        GL.PopMatrix();
    }

    void DrawTriangle(Triangle t, int pass) {
        mat.SetPass(pass);
        GL.Begin(GL.TRIANGLES);
        GL.Vertex(float3(t.a, 0));
        GL.Vertex(float3(t.b, 0));
        GL.Vertex(float3(t.c, 0));
        GL.Vertex(float3(t.a, 0));
        GL.End();
    }
    #endregion
}