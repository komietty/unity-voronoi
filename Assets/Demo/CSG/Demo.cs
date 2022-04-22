using UnityEngine;

namespace kmty.geom.csg.demo {
    public class Demo : MonoBehaviour {
        [SerializeField] protected GameObject g1;
        [SerializeField] protected GameObject g2;
        [SerializeField] protected GameObject g3;
        [SerializeField] protected OpType op1;
        [SerializeField] protected OpType op2;

        void Start() {
            var mf  = GetComponent<MeshFilter>();
            var t1 = CSG.GenCsgTree(g1.transform);
            var t2 = CSG.GenCsgTree(g2.transform);
            if (g3 != null) {
                var t3 = CSG.GenCsgTree(g3.transform);
                var t4 = t1.Oparation(t2, op1);
                mf.mesh = CSG.Meshing(t4.Oparation(t3, op2));
            } else {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                mf.mesh = CSG.Meshing(t1.Oparation(t2, op1));
                sw.Stop();
                Debug.Log(sw.ElapsedMilliseconds + "ms");
            }
            foreach (Transform t in transform) { t.gameObject.SetActive(false); }
        }
    }
}
