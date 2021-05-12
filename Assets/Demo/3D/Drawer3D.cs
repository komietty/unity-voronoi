using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using URnd = UnityEngine.Random;

public class Drawer3D : MonoBehaviour {

    public Material mat;
    public int numPoint;
    public int seed;
    public KeyCode reset  = KeyCode.R;
    public KeyCode add    = KeyCode.A;
    public KeyCode toggle = KeyCode.T;
    public bool showDebugNode;
    public bool showDebugNeighbor;
    public bool showOthers;
    public bool showVoronoi;
    [Range(0, 100)] public int debugNodeId;

    BistellarFlip3D bf;
    Voronoi3D voronoi;
    DelaunayGraphNode3D[] nodes;

    void Start() {
        bf = new BistellarFlip3D(numPoint, seed);
        nodes = bf.GetResult().ToArray();
        voronoi = new Voronoi3D(nodes);
    }

    void Update() {
        if (Input.GetKeyDown(reset)) {
            bf = new BistellarFlip3D(numPoint, seed);
            nodes = bf.GetResult().ToArray();
            voronoi = new Voronoi3D(nodes);
        }
        if (Input.GetKeyDown(add)) {
            bf.Loop(new Vector3(URnd.value, URnd.value, URnd.value));
            nodes = bf.GetResult().ToArray();
            voronoi = new Voronoi3D(nodes);
        }
        if (Input.GetKeyDown(toggle)) showVoronoi = !showVoronoi;
    }

    void OnRenderObject() {
        GL.Clear(true, true, Color.clear);
        if (showVoronoi) DrawVoronoi();
        else DrawDelauney(nodes);
    }

    void DrawDelauney(DelaunayGraphNode3D[] nodes) {
        GL.PushMatrix();
        mat.SetPass(0);
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex(new float3(0, 0, 0));
        GL.Vertex(new float3(1, 0, 0));
        GL.Vertex(new float3(1, 1, 0));
        GL.Vertex(new float3(0, 1, 0));
        GL.Vertex(new float3(0, 0, 0));
        GL.End();
        GL.Begin(GL.LINE_STRIP);
        GL.Vertex(new float3(0, 0, 1));
        GL.Vertex(new float3(1, 0, 1));
        GL.Vertex(new float3(1, 1, 1));
        GL.Vertex(new float3(0, 1, 1));
        GL.Vertex(new float3(0, 0, 1));
        GL.End();
        GL.Begin(GL.LINES);
        GL.Vertex(new float3(0, 0, 0));
        GL.Vertex(new float3(0, 0, 1));
        GL.Vertex(new float3(1, 0, 0));
        GL.Vertex(new float3(1, 0, 1));
        GL.Vertex(new float3(0, 1, 0));
        GL.Vertex(new float3(0, 1, 1));
        GL.Vertex(new float3(1, 1, 0));
        GL.Vertex(new float3(1, 1, 1));
        GL.End();
        GL.PopMatrix();

        GL.PushMatrix();
        if (debugNodeId < nodes.Length) {
            var n = nodes[debugNodeId];
            for (int i = 0; i < nodes.Length; i++) {
                if (i != debugNodeId && showOthers)
                    DrawTetrahedra(nodes[i].tetrahedra, 0);
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


    void DrawVoronoi() {
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        foreach (var s in voronoi.segments) {
            mat.SetPass(0);
            GL.Vertex((float3)s.a);
            GL.Vertex((float3)s.b);

        }
        GL.End();
        GL.PopMatrix();
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
