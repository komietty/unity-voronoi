using UnityEngine;
using Unity.Mathematics;

namespace kmty.geom.d3.delauney {
    public class Drawer3D : MonoBehaviour {
        public Material mat;
        public int numPoint;

        void Start () {
            var bf = new BistellarFlip3D(numPoint, 1);
            var nodes = bf.Nodes.ToArray();
            var voronoi = new VoronoiGraph3D(nodes);
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
                g.transform.position = (float3)n.Value.center * 1.01f;
                count++;
            }
        }
    }
}
