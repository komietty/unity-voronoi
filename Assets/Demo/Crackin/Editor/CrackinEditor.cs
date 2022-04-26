using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace kmty.geom.crackin {
    [CustomEditor(typeof(Crackin))]
    public class SurfaceHandlerEditor : Editor {
        protected int selectedId = -1;
        protected Crackin handler => (Crackin)target;

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            EditorGUILayout.Space(1);
            if (GUILayout.Button("Bake Mesh")) {
                var path = $"{handler.BakePath}/{handler.BakeName}.asset";
                CreateOrUpdate(handler.mesh, path);
            }
        }

        void CreateOrUpdate(Object altAsset, string assetPath) {
            var oldAsset = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (oldAsset == null) {
                AssetDatabase.CreateAsset(altAsset, assetPath);
            } else {
                EditorUtility.CopySerializedIfDifferent(altAsset, oldAsset);
                AssetDatabase.SaveAssets();
            }
        }
    }
}