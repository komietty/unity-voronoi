using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace kmty.geom.d2.delaunay {
    public class Test2D {
        //static int[] nums = { 1, 2, 3, 5, 10, 20, 30 };
        static int[] nums = { 1000 };

        [Test]
        public void DelaunayTrianglesTest([ValueSource(nameof(nums))] int num) {
            var sw = Stopwatch.StartNew();
            var bf = new BistellarFlip2D(num);
            sw.Stop();
            UnityEngine.Debug.Log(sw.Elapsed);
            foreach (var n in bf.Nodes) {
                var t = n.triangle;
                var c = n.triangle.GetCircumscribledCircle();
                var ab = new Segment(t.a, t.b);
                var bc = new Segment(t.b, t.c);
                var ca = new Segment(t.c, t.a);
                var pair_ab = n.neighbor.Find(_n => _n.Contains(ab));
                var pair_bc = n.neighbor.Find(_n => _n.Contains(bc));
                var pair_ca = n.neighbor.Find(_n => _n.Contains(ca));
                if (pair_ab != null) Assert.IsFalse(c.Contains(pair_ab.triangle.RemainingPoint(ab)));
                if (pair_bc != null) Assert.IsFalse(c.Contains(pair_bc.triangle.RemainingPoint(bc)));
                if (pair_ca != null) Assert.IsFalse(c.Contains(pair_ca.triangle.RemainingPoint(ca)));
            }
        }

    }
}
