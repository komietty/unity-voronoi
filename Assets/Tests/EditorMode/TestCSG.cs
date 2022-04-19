using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace kmty.geom.csg {
    using V3 = Vector3;
    public class TestCSG {

        [Test]
        public void PlaneDeepCopy() {
            var p1 = new Plane(new V3(0, 0, 0), new V3(1, 0, 0), new V3(0, 1, 0));
            var p2 = new Plane(p1);
            p1.Flip();
            Assert.IsTrue(p1.n == -p2.n);
            Assert.IsTrue(p1.w == -p2.w);
        }

        [Test]
        public void PolygonDeepCopy() {
            var p1 = new Polygon(new Vert[]{
                new Vert(new V3(0, 0, 0), new V3(0, 0, 1)),
                new Vert(new V3(1, 0, 0), new V3(0, 0, 1)),
                new Vert(new V3(0, 1, 0), new V3(0, 0, 1))
            });
            var p2 = new Polygon(p1);
            Assert.AreNotEqual(p1.plane, p2.plane);
            p1.Flip();
            Assert.IsTrue(p1.plane.n == -p2.plane.n);
            Assert.IsTrue(p1.plane.w == -p2.plane.w);
            var l1 = p1.verts.Length;
            var l2 = p2.verts.Length;
            for(var i = 0; i < l1; i++){
                Assert.IsTrue(p1.verts[i].pos == p2.verts[l2 - 1 - i].pos);
                Assert.IsTrue(p1.verts[i].nrm == -p2.verts[l2 - 1 - i].nrm);
            }
        }

        [Test]
        public void NodeDeepCopy() {
            var p1 = new Polygon(new Vert[]{
                new Vert(new V3(0, 0, 0), new V3(0, 0, -1)),
                new Vert(new V3(1, 0, 0), new V3(0, 0, -1)),
                new Vert(new V3(0, 1, 0), new V3(0, 0, -1))
            });
            var p2 = new Polygon(new Vert[]{
                new Vert(new V3(0, 0, 0), new V3(0, -1, 0)),
                new Vert(new V3(1, 0, 0), new V3(0, -1, 0)),
                new Vert(new V3(0, 0, 1), new V3(0, -1, 0))
            });
            var p3 = new Polygon(new Vert[]{
                new Vert(new V3(0, 0, 0), new V3(-1, 0, 0)),
                new Vert(new V3(0, 1, 0), new V3(-1, 0, 0)),
                new Vert(new V3(0, 0, 1), new V3(-1, 0, 0))
            });
            var n1 = new Node(new List<Polygon>() { p1, p2, p3 });
            var n2 = new Node(n1);
            n1.Invert();

            if(n1.plane != null && n2.plane != null){
                Assert.IsTrue(n1.plane.n == -n2.plane.n);
                Assert.IsTrue(n1.plane.w == -n2.plane.w);
            }

            var l1 = n1.polygons.Count;
            var l2 = n2.polygons.Count;
            for(var i = 0; i < l1; i++){
                var v1 = n1.polygons[i].verts;
                var v2 = n2.polygons[i].verts;
                var l3 = v1.Length;
                for (var j = 0; j < l3; j++) {
                    Assert.IsTrue(v1[j].pos ==  v2[l3 - 1 - j].pos);
                    Assert.IsTrue(v1[j].nrm == -v2[l3 - 1 - j].nrm);
                }
            }
        }
    }
}
