using UnityEngine;

namespace kmty.geom.csg.demo {
    public class Demo : MonoBehaviour {
        [SerializeField] protected GameObject g1;
        [SerializeField] protected GameObject g2;
        [SerializeField] protected GameObject g3;
        [SerializeField] protected OparationType op1;
        [SerializeField] protected OparationType op2;

        void Start() {
            var mf  = GetComponent<MeshFilter>();
            var csg1 = CSGUtil.Csgnize(g1);
            var csg2 = CSGUtil.Csgnize(g2);
            if (g3 != null) {
                var csg3 = CSGUtil.Csgnize(g3);
                var csg4 = csg1.Oparation(csg2, op1);
                mf.mesh = CSGUtil.Meshing(csg3.Oparation(csg4, op2));
            } else {
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                mf.mesh = CSGUtil.Meshing(csg1.Oparation(csg2, op1));
                sw.Stop();
                Debug.Log(sw.ElapsedMilliseconds + "ms");
            }
            foreach (Transform t in transform) { t.gameObject.SetActive(false); }
        }
    }
}
