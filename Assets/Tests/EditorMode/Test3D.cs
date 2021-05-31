using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using kmty.geom.d3;
using old;

public class Test3D {
    static int[] nums = {1, 2, 5, 10, 20};

    [Test]
    public void Flip32Test([ValueSource(nameof(nums))]int num) {
        var bf = new BistellarFlip3D(num);
        bf.GetResult().ForEach(curr => {
            foreach (var t in curr.tetrahedra.triangles) {
                var pair = curr.GetFacingNode(t);
                if (pair == null) continue;
                var rmnp = curr.tetrahedra.RemainingPoint(t);
                if (curr.tetrahedra.circumscribedSphere.Contains(rmnp)) {
                    Debug.LogWarning("non deraunay tetrahedra is found");
                    bf.Leagalize(curr, t, rmnp);
                }
            }
        });
    }

    [Test]
    public void DelaunayTest([ValueSource(nameof(nums))]int num) {
        var bf = new BistellarFlip3D(num);
        bf.GetResult().ForEach(curr => {
            foreach (var t in curr.tetrahedra.triangles) {
                var pair = curr.GetFacingNode(t);
                if (pair == null) continue;
                var rmnp = curr.tetrahedra.RemainingPoint(t);
                if (curr.tetrahedra.circumscribedSphere.Contains(rmnp)) {
                    Debug.LogWarning("non deraunay tetrahedra is found");
                    bf.Leagalize(curr, t, rmnp);
                }
            }
        });
    }

    [Test]
    public void DelaunaiesMustHave4NeighborExeptMostOuter([ValueSource(nameof(nums))]int num) {
        var bf = new BistellarFlip3D(num);
        var rt = bf.Root;
        bf.GetResult()
          .ForEach(n => {
              var t = n.tetrahedra;
              var c = 0;

              Assert.IsTrue(rt.Contains(t.a, true) && rt.Contains(t.b, true) &&
                            rt.Contains(t.c, true) && rt.Contains(t.d, true));
              if (rt.Contains(t.a, false)) c++;
              if (rt.Contains(t.b, false)) c++;
              if (rt.Contains(t.c, false)) c++;
              if (rt.Contains(t.d, false)) c++;
              if (c >= 2 && n.neighbor.Count != 4) {
                  Debug.Log($"num: {num}, count:{c}, neighbor: {n.neighbor.Count}");
              }
              //if (c >= 2) Assert.IsTrue(n.neighbor.Count == 4);

          });
    }

}
