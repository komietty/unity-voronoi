using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;

namespace kmty.geom.d3.delauney {
    using DN = DelaunayGraphNode3D;
    using TR = Triangle;
    using d3 = double3;

    public class Test3D {
        static int[] nums = { 1, 2, 3, 4, 5, 10, 20, 30 };

        [Test]
        public void Performance([Values(100)] int num) {
            // takes 15.3 sec before refactor for 100 nums
            var sw = Stopwatch.StartNew();
            var bf = new BistellarFlip3D(num);
            sw.Stop();
            UnityEngine.Debug.Log(sw.Elapsed);
        }

        [Test]
        public void DelaunayTrianglesTest([ValueSource(nameof(nums))] int num) {
            var bf = new BistellarFlip3D(num);
            foreach (var n in bf.Nodes) {
                var t = n.tetrahedra;
                var c = n.tetrahedra.GetCircumscribedSphere();
                var abc = new TR(t.a, t.b, t.c);
                var bcd = new TR(t.b, t.c, t.d);
                var cda = new TR(t.c, t.d, t.a);
                var dab = new TR(t.d, t.a, t.b);
                var pair_abc = n.neighbor.Find(_n => _n.HasFacet(abc));
                var pair_bcd = n.neighbor.Find(_n => _n.HasFacet(bcd));
                var pair_cda = n.neighbor.Find(_n => _n.HasFacet(cda));
                var pair_dab = n.neighbor.Find(_n => _n.HasFacet(dab));
                var d1 = n.neighbor.Count(_n => _n.HasFacet(abc));
                var d2 = n.neighbor.Count(_n => _n.HasFacet(bcd));
                var d3 = n.neighbor.Count(_n => _n.HasFacet(cda));
                var d4 = n.neighbor.Count(_n => _n.HasFacet(dab));
                Assert.IsTrue(d1 < 2 && d2 < 2 && d3 < 2 && d4 < 2);
                Assert.IsTrue(n.neighbor.Count >= 3);
                if (pair_abc != null) Assert.IsFalse(c.Contains(pair_abc.tetrahedra.RemainingPoint(abc)));
                if (pair_bcd != null) Assert.IsFalse(c.Contains(pair_bcd.tetrahedra.RemainingPoint(bcd)));
                if (pair_cda != null) Assert.IsFalse(c.Contains(pair_cda.tetrahedra.RemainingPoint(cda)));
                if (pair_dab != null) Assert.IsFalse(c.Contains(pair_dab.tetrahedra.RemainingPoint(dab)));
            }
        }

        [Test]
        public void MustHave4NeighborExceptMostOuter([ValueSource(nameof(nums))]int num) {
            var bf = new BistellarFlip3D(num);
            var rt = new DN(new d3(0, 0, 0), new d3(3, 0, 0), new d3(0, 3, 0), new d3(0, 0, 3));
            bf.Nodes.ForEach(n => {
                var t = n.tetrahedra;
                var c = 0;

                Assert.IsTrue(rt.Contains(t.a, true) && rt.Contains(t.b, true) &&
                              rt.Contains(t.c, true) && rt.Contains(t.d, true));
                if (rt.Contains(t.a, false)) c++;
                if (rt.Contains(t.b, false)) c++;
                if (rt.Contains(t.c, false)) c++;
                if (rt.Contains(t.d, false)) c++;
                if (c >= 2) Assert.IsTrue(n.neighbor.Count == 4);
            });
        }

    }
}
