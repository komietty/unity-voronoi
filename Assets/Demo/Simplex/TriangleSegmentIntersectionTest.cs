using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using UR = UnityEngine.Random;

public class TriangleSegmentIntersectionTest : MonoBehaviour {
    Triangle t;
    Segment  s;
    Vector3 intersection;

    void Start() { }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            t = new Triangle(UR.insideUnitSphere, UR.insideUnitSphere, UR.insideUnitSphere);
            s = new Segment(new Vector3(-1, 0, 0), new Vector3(1, 0, 0));
        }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.white;
        Gizmos.DrawLine((float3)t.a, (float3)t.b);
        Gizmos.DrawLine((float3)t.b, (float3)t.c);
        Gizmos.DrawLine((float3)t.c, (float3)t.a);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine((float3)s.a, (float3)s.b);
        var size = Vector3.one * 0.01f;
        var f1 = t.IntersectsUsingMtx(s, out double3 p1, out bool o1);
        var f2 = t.Intersects(s, out double3 p2, out bool o2);
        if(f1) {
            Gizmos.color = Color.green;
            Gizmos.DrawCube((float3)p1, size);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere((float3)p2, 0.02f);
            Debug.Log(math.all(p1 == p2));
            if(!math.all(p1 == p2)) {
                Debug.Log(p1 - p2);
            }
        }
    }
}
