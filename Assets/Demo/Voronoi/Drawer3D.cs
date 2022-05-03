using UnityEngine;
using Unity.Mathematics;
using System.Linq;

namespace kmty.geom.d3.delauney {
    public class Drawer3D : MonoBehaviour {
        public Material mat;
        public int numPoint;
        float min = 0f;
        float max = 1f;

        void Start () {
            var bf = new BistellarFlip3D(numPoint, 1);
            var nodes = bf.Nodes.ToArray();
            var voronoi = new VoronoiGraph3D(nodes);
            int count = 0;
            foreach (var n in voronoi.nodes) {
                var o = n.Value.Meshilify();
                var c = (float3)n.Value.center;
                if (o.vertices.Any(v => 
                        v.x + c.x < min ||
                        v.x + c.x > max ||
                        v.y + c.y < min ||
                        v.y + c.y > max ||
                        v.z + c.z < min ||
                        v.z + c.z > max)) continue;
                var g = new GameObject(count.ToString());
                var f = g.AddComponent<MeshFilter>();
                var r = g.AddComponent<MeshRenderer>();
                var m = new Material(mat);
                r.sharedMaterial = m;
                f.mesh = o;
                g.transform.position = c;
                count++;
            }
        }

        void OnDrawGizmosSelected() {
            var c = (min + max) / 2;
            var l = (max - min);
            Gizmos.DrawWireCube(Vector3.one * c, Vector3.one * l);
        }
    }
}
