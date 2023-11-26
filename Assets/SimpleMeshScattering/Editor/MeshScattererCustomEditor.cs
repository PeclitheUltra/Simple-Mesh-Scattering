using SimpleMeshScattering.Core;
using UnityEditor;
using UnityEngine;

namespace SimpleMeshScattering.Editor
{
    [CustomEditor(typeof(MeshScatterer))]
    public class MeshScattererCustomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            MeshScatterer scatterer = (MeshScatterer)target;
            if (GUILayout.Button("Preview"))
            {
                scatterer.Preview();
            }
            if (GUILayout.Button("Clear Preview"))
            {
                scatterer.ClearPreviewData();
            }
            if (GUILayout.Button("Bake"))
            {
                scatterer.Bake();
            }
        }
    }
}
