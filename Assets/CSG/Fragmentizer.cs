using UnityEngine;
using Unity.Collections;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace remesher {

    public class MeshFragmentizer : System.IDisposable {
        public Mesh mesh  { get; private set; }
        public Mesh cache { get; private set; }

        // with animation controller vertex munipulation does not work correctly
        public NativeArray<Vector3> vertices_handler;
        public NativeArray<Vector3> vertices_cached;

        //public MeshFragmentizer(GameObject root) {
        //    var filter = root.GetComponent<MeshFilter>();
        //    var skin = root.GetComponent<SkinnedMeshRenderer>();
        //    var mtxTRS = Matrix4x4.TRS(root.transform.position, root.transform.rotation, root.transform.localScale).inverse;
        //    cache = skin == null ? filter.sharedMesh : skin.sharedMesh;
        //    var new_vrts = new List<Vector3>();
        //    mesh = Create(cache, out new_vrts);
        //    vertices_handler = new NativeArray<Vector3>(new_vrts.ToArray(), Allocator.Persistent);
        //    vertices_cached  = new NativeArray<Vector3>(new_vrts.ToArray(), Allocator.Persistent);
        //}

        public static Mesh Create(Mesh original, out List<Vector3> new_vrts_list, out List<Vector3> new_nrms_list) {
            var vrts = original.vertices;
            var tris = original.triangles;
            var uvs = original.uv;
            var nrms = original.normals;
            var tans = original.tangents;
            var bnws = original.boneWeights;
            var bdps = original.bindposes;
            var new_vrts = new Vector3[tris.Length];
            var new_uvs  = new Vector2[tris.Length];
            var new_nrms = new Vector3[tris.Length];
            var new_tans = new Vector3[tris.Length];
            var new_tris = new int[tris.Length];
            var new_bnws = new BoneWeight[tris.Length];
            var mesh = GameObject.Instantiate(original);
            
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            for (int i = 0; i < tris.Length; i++) {
                var t = tris[i];
                new_vrts[i] = vrts[t];
                new_uvs[i]  = uvs [t];
                new_nrms[i] = nrms[t];
                new_tans[i] = tans[t];
                if (bnws.Length > 0) new_bnws[i] = bnws[t];
                new_tris[i] = i;
            }

            mesh.vertices = new_vrts;
            mesh.triangles = new_tris;
            mesh.uv = new_uvs;
            mesh.boneWeights = new_bnws;
            mesh.bindposes = bdps;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            new_vrts_list = new_vrts.ToList();
            new_nrms_list = new_nrms.ToList();
            return mesh;
        }

        public void UpdateVertices() {
            mesh.SetVertices(vertices_handler);
        }

        public void Dispose() {
            vertices_handler.Dispose();
            vertices_cached.Dispose();
            GameObject.Destroy(mesh);
        }
    }
}