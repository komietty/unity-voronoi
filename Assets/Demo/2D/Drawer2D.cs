using UnityEngine;
using kmty.geom.d2;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Drawer2D : MonoBehaviour {
    public Material mat;
    public int numPoint;
    public KeyCode reset = KeyCode.R;
    public bool drawCirlcle;
    [Range(0, 100)] public int debugNodeId;
    BistellarFlip2D bf;
    DelaunayGraphNode2D[] nodes;

    void Start() {
        bf = new BistellarFlip2D(numPoint);
        nodes = bf.GetResult().ToArray();
    }

    void Update() {
        if (Input.GetKeyDown(reset)) {
            bf = new BistellarFlip2D(numPoint);
            nodes = bf.GetResult().ToArray();
        }
    }

    void OnRenderObject() {
        GL.Clear(true, true, Color.clear);
        if (nodes != null) DrawDelaunay(nodes);
    }

    void DrawDelaunay(DelaunayGraphNode2D[] nodes) {
        GL.PushMatrix();
        GL.LoadOrtho();
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
                var c = t.GetCircumscribedCircle();
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
}