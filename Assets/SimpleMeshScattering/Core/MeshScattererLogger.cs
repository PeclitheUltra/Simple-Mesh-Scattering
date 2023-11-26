using UnityEngine;

namespace SimpleMeshScattering.Core
{
    public static class MeshScattererLogger
    {
        public static void LogError(string text)
        {
            Debug.LogError($"<color=yellow> MESH SCATTERER </color> {text}");
        }
    }
}
