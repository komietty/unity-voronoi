using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Drawer3D : MonoBehaviour {

    public Material mat;
    public int numPoint;
    public int seed;
    public KeyCode reset = KeyCode.R;
    public bool showDebugNode;
    public bool showDebugNeighbor;
    public bool showOthers;
    [Range(0, 100)] public int debugNodeId;

    BistellarFlip3D bf;
    DelaunayGraphNode3D[] nodes;

    void Start() {
        bf = new BistellarFlip3D(numPoint, seed);
        nodes = bf.GetResult().ToArray();
    }

    void Update() {
        if (Input.GetKeyDown(reset)) {
            bf = new BistellarFlip3D(numPoint, seed);
            nodes = bf.GetResult().ToArray();
        }
    }

    void OnRenderObject() {
        GL.Clear(true, true, Color.clear);
        DrawDelauney(nodes);
    }

    void DrawDelauney(DelaunayGraphNode3D[] nodes) {
        GL.PushMatrix();
        if(debugNodeId < nodes.Length) {
            var n = nodes[debugNodeId];
            for (int i = 0; i < nodes.Length; i++) {
                if (i != debugNodeId && showOthers) DrawTetrahedra(nodes[i].tetrahedra, 0);
            }

            DrawTetrahedra(n.tetrahedra, 3);
            if (showDebugNode) DrawTetrahedraSpecifid(n.tetrahedra, 1);
            n.neighbor.ForEach(nei => {
                DrawTetrahedra(nei.tetrahedra, 4);
                if (showDebugNeighbor) DrawTetrahedraSpecifid(nei.tetrahedra, 2);
            });
        }
        GL.PopMatrix();
    }

    void DrawCircumscribedSphere(Vector3 p, float r) {

    }

    void DrawTetrahedra(Tetrahedra t, int pass) {
        mat.SetPass(pass);
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex((float3)t.a);
        GL.Vertex((float3)t.b);
        GL.Vertex((float3)t.c);
        GL.Vertex((float3)t.a);
        GL.End();
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex((float3)t.a);
        GL.Vertex((float3)t.d);
        GL.Vertex((float3)t.c);
        GL.End();
        GL.Begin(GL.LINES);
        GL.Vertex((float3)t.d);
        GL.Vertex((float3)t.b);
        GL.End();
    }

    void DrawTetrahedraSpecifid(Tetrahedra t, int pass) {
        mat.SetPass(pass);
        GL.Begin(GL.TRIANGLES);

        GL.Vertex((float3)t.a);
        GL.Vertex((float3)t.b);
        GL.Vertex((float3)t.c);

        GL.Vertex((float3)t.b);
        GL.Vertex((float3)t.c);
        GL.Vertex((float3)t.d);

        GL.Vertex((float3)t.c);
        GL.Vertex((float3)t.d);
        GL.Vertex((float3)t.a);

        GL.Vertex((float3)t.d);
        GL.Vertex((float3)t.a);
        GL.Vertex((float3)t.b);

        GL.End();
    }
}
