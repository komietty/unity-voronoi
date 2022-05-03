using UnityEngine;
using System.Linq;

namespace kmty.geom.d2.delaunay {
    public class Drawer2D : MonoBehaviour {
        public Material mat;
        public int numPoint;
        float min = 0f;
        float max = 1f;
        VoronoiGraph2D voronoi;

        void Start() {
            var bf = new BistellarFlip2D(numPoint);
            var nodes = bf.Nodes.ToArray();
            int count = 0;
            voronoi = new VoronoiGraph2D(nodes);
            foreach (var n in voronoi.nodes) {
                var o = n.Value.Meshilify();
                if (o.vertices.Any(v => v.x < min || v.x > max || v.y < min || v.y > max)) continue;
                var g = new GameObject(count.ToString());
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                var m = new Material(mat);
                r.sharedMaterial = m;
                f.mesh = o;
                count++;
            }
        }

        void OnDrawGizmosSelected() {
            Gizmos.color = Color.black;
            if(voronoi == null) return;
            foreach (var n in voronoi.nodes) {
                foreach (var s in n.Value.segments) {
                    var a = (Vector2)s.a;
                    var b = (Vector2)s.b;
                    if (a.x < min || a.x > max || a.y < min || a.y > max) continue;
                    if (b.x < min || b.x > max || b.y < min || b.y > max) continue;
                    Gizmos.DrawLine(a, b);
                } 
            }
        }
    }
}
