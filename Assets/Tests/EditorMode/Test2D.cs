using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using kmty.geom.d2;

namespace kmty.geom.d2.delaunay_alt {
    public class Test2D {
        static int[] nums = { 1, 2, 5, 10, 20 };

        [Test]
        public void DelaunayTrianglesTest([ValueSource(nameof(nums))] int num) {
            var bf = new BistellarFlip2D(num);
            var ns = bf.Nodes;
            //var ns = bf.GetResult(); 
            foreach (var n in ns) {
                var t = n.triangle;
                var c = n.triangle.circumscribedCircle;
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
