using UnityEngine;

namespace kmty.geom.d2.delaunay {
    public class Drawer2D : MonoBehaviour {
        public Material mat;
        public int numPoint;

        void Start() {
            var bf = new BistellarFlip2D(numPoint);
            var nodes = bf.Nodes.ToArray();
            var voronoi = new VoronoiGraph2D(nodes);
            int count = 0;
            foreach (var n in voronoi.nodes) {
                var o = n.Value.Meshilify();
                var g = new GameObject(count.ToString());
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                var m = new Material(mat);
                m.SetColor("_Color", Color.HSVToRGB(UnityEngine.Random.value, 1, 1));
                r.sharedMaterial = m;
                f.mesh = o;
                count++;
            }
        }
    }
}
