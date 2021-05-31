using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace kmty.geom.d3.delauney_alt {
    using DN = DelaunayGraphNode3D;
    using TR = Triangle;
    using d3 = double3;

    public class DelaunayGraphNode3D {
        public Tetrahedra tetrahedra;
        public List<DN> neighbor;
        public bool Contains(d3 p, bool inclusive) => tetrahedra.Contains(p, inclusive);
        public bool HasFacet(TR t) => tetrahedra.HasFace(t);

        public DelaunayGraphNode3D(TR t, d3 d) : this(t.a, t.b, t.c, d) { }
        public DelaunayGraphNode3D(d3 a, d3 b, d3 c, d3 d) {
            tetrahedra = new Tetrahedra(a, b, c, d);
            neighbor = new List<DN>();
        }

        public static (TR t1, TR t2, TR t3, TR t4, DN n1, DN n2, DN n3, DN n4) Split(DN node, d3 p) {
            throw new Exception();
        }

        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2, DN n3) Flip23(DN curr, DN pair, TR tri, d3 p_this, d3 p_pair) {
            throw new Exception();
        }
        public static (TR t1, TR t2, TR t3, TR t4, TR t5, TR t6, DN n1, DN n2) Flip32(DN curr, DN pair, TR tri, d3 p_this, d3 p_pair, d3 p_away) {
            throw new Exception();
        }
    }
    public class BistellarFlip3D {
    }
}
