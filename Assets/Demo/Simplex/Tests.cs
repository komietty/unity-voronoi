using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using kmty.geom.d3;
using Unity.Mathematics;
using UR = UnityEngine.Random;

public class Tests : MonoBehaviour {
    Triangle t1;
    Triangle t2;
    int num;
    Vector3 p1;
    Vector3 p2;

    void Start() {
            t1 = new Triangle(Vector3.zero, Vector3.right, Vector3.up);
     }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            t2 = new Triangle(UR.insideUnitSphere, UR.insideUnitSphere, UR.insideUnitSphere);
            var o = Triangle.Intersects(t1, t2);
            num = o.num;
            p1 = (float3)o.p1;
            p2 = (float3)o.p2;
        }
    }

    void OnDrawGizmos() {
        if(num > 0) Gizmos.DrawWireCube(p1, Vector3.one * 0.03f);
        if(num > 1) Gizmos.DrawWireCube(p2, Vector3.one * 0.03f);
        
        Gizmos.color = num > 0 ? Color.cyan : Color.white;
        Gizmos.DrawLine((float3)t1.a, (float3)t1.b);
        Gizmos.DrawLine((float3)t1.b, (float3)t1.c);
        Gizmos.DrawLine((float3)t1.c, (float3)t1.a);

        Gizmos.DrawLine((float3)t2.a, (float3)t2.b);
        Gizmos.DrawLine((float3)t2.b, (float3)t2.c);
        Gizmos.DrawLine((float3)t2.c, (float3)t2.a);
    }
}
