using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleMeshScattering.Core
{
    public class MeshScatterer : MonoBehaviour
    {
        [SerializeField] private MeshFilter _targetMeshFilter;
        [SerializeField] private MeshFilter _sourceMeshFilter;

        [SerializeField] private ESpawnType _spawnType;

        [SerializeField] private float _minimalDistance, _scale, _scaleRandomness;
        [SerializeField] private int _seed;

        private List<Vector3> _previewPositions = new List<Vector3>();
        private List<Vector3> _previewNormals = new List<Vector3>();
        private List<float> _previewScales = new List<float>();
        private List<GameObject> _previewObjects = new List<GameObject>();
        
        
        public void Preview()
        {
            ClearPreviewData();
            Random.InitState(_seed);
            switch (_spawnType)
            {
                case ESpawnType.Vertex:
                    GeneratePreviewDataVertexType();
                    break;
                case ESpawnType.Face:
                    GeneratePreviewDataFaceType();
                    break;
            }
            DrawPreviewUsingData();
        }

        private void DrawPreviewUsingData()
        {
            for (int i = 0; i < _previewPositions.Count; i++)
            {
                var previewObject = Instantiate(_sourceMeshFilter.gameObject, Vector3.zero, Quaternion.identity,
                    _targetMeshFilter.transform);
                previewObject.transform.localPosition = _previewPositions[i];
                previewObject.transform.up = _previewNormals[i];
                previewObject.transform.localScale = Vector3.one * _previewScales[i];
                _previewObjects.Add(previewObject);
            }
        }
        private void GeneratePreviewDataVertexType()
        {
            if (_minimalDistance <= 0)
            {
                MeshScattererLogger.LogError("Minimal Distance should be greater than zero.");
                return;
            }
            var targetMesh = _targetMeshFilter.sharedMesh;
            var vertices = targetMesh.vertices;
            var normals = targetMesh.normals;
            int maxTries = 15;
            int currentTry = 0;
            var minimalDistanceSquared = _minimalDistance * _minimalDistance;
            while (currentTry < maxTries)
            {
                var tooClose = false;
                var randomId = Random.Range(0, vertices.Length);
                var randomPosition = vertices[randomId];
                foreach (var position in _previewPositions)
                {
                    if ((position - randomPosition).sqrMagnitude < minimalDistanceSquared)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    currentTry++;
                }
                else
                {
                    _previewPositions.Add(randomPosition);
                    _previewNormals.Add(normals[randomId]);
                    _previewScales.Add(_scale + Random.value * _scaleRandomness);
                    currentTry = 0;
                }
            }
        }

        private void GeneratePreviewDataFaceType()
        {
            if (_minimalDistance <= 0)
            {
                MeshScattererLogger.LogError("Minimal Distance should be greater than zero.");
                return;
            }
            var targetMesh = _targetMeshFilter.sharedMesh;
            var vertices = targetMesh.vertices;
            var normals = targetMesh.normals;
            var triangles = targetMesh.triangles;
            int maxTries = 15;
            int currentTry = 0;
            var minimalDistanceSquared = _minimalDistance * _minimalDistance;
            while (currentTry < maxTries)
            {
                var tooClose = false;
                var triangleStartPointer = Mathf.FloorToInt(Random.Range(0, triangles.Length) / 3f) * 3;
                var vertId1 = triangles[triangleStartPointer];
                var vertId2 = triangles[triangleStartPointer + 1];
                var vertId3 = triangles[triangleStartPointer + 2];
                var pos1 = vertices[vertId1];
                var pos2 = vertices[vertId2];
                var pos3 = vertices[vertId3];
                var lerp1 = Random.value;
                var lerp2 = Random.value;
                var randomPosition = Vector3.Lerp(Vector3.Lerp(pos1, pos2, lerp1), pos3, lerp2);
                foreach (var position in _previewPositions)
                {
                    if ((position - randomPosition).sqrMagnitude < minimalDistanceSquared)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    currentTry++;
                }
                else
                {
                    _previewPositions.Add(randomPosition);
                    _previewNormals.Add(Vector3.Lerp(Vector3.Lerp(normals[vertId1], normals[vertId2], lerp1), normals[vertId3], lerp2));
                    _previewScales.Add(_scale + Random.value * _scaleRandomness);
                    currentTry = 0;
                }
            }
        }
        
        public void ClearPreviewData()
        {
            foreach (var previewObject in _previewObjects)
            {
                if(previewObject != null)
                    DestroyImmediate(previewObject);
            }
            _previewPositions.Clear();
            _previewNormals.Clear();
            _previewObjects.Clear();
            _previewScales.Clear();
        }
        public void Bake()
        {
            if (_previewObjects.Count == 0)
            {
                MeshScattererLogger.LogError("You should generate data using Preview button first.");
                return;
            }
            
            var bakedMesh = new Mesh();
            bakedMesh.indexFormat = IndexFormat.UInt32;
            var verticesList = new List<Vector3>();
            var normalsList = new List<Vector3>();
            var uvsList = new List<Vector2>();
            var trianglesList = new List<int>();
            foreach (var previewObject in _previewObjects)
            {
                trianglesList.AddRange(previewObject.GetComponent<MeshFilter>().sharedMesh.triangles.Select(x => x + verticesList.Count));
                verticesList.AddRange(previewObject.GetComponent<MeshFilter>().sharedMesh.vertices.Select(x => previewObject.transform.parent.InverseTransformPoint(previewObject.transform.TransformPoint(x))));
                normalsList.AddRange(previewObject.GetComponent<MeshFilter>().sharedMesh.normals);
                uvsList.AddRange(previewObject.GetComponent<MeshFilter>().sharedMesh.uv);
            }
            
            bakedMesh.vertices = verticesList.ToArray();
            bakedMesh.normals = normalsList.ToArray();
            bakedMesh.uv = uvsList.ToArray();
            bakedMesh.triangles = trianglesList.ToArray();

            var bakedMeshGameObject = new GameObject();
            bakedMeshGameObject.name = "Baked Scattered Mesh";
            bakedMeshGameObject.transform.position = _targetMeshFilter.transform.position;
            bakedMeshGameObject.transform.parent = _targetMeshFilter.transform;
            var bakedMeshFilter = bakedMeshGameObject.AddComponent<MeshFilter>();
            var bakedMeshRenderer = bakedMeshGameObject.AddComponent<MeshRenderer>();
            bakedMeshFilter.sharedMesh = bakedMesh;
            if (_sourceMeshFilter.TryGetComponent<MeshRenderer>(out var sourceMeshrenderer))
            {
                bakedMeshRenderer.sharedMaterial = sourceMeshrenderer.sharedMaterial;
            }
            ClearPreviewData();
        }
    }

    public enum ESpawnType
    {
        Vertex,
        Face
    }
}
