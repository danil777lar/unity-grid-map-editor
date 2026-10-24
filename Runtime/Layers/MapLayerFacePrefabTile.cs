using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class MapLayerFacePrefabTile : MapLayerFace
    {
        [Header("Tile")]
        [SerializeField] private bool _lookBackward;
        [SerializeField] private float _verticalScale = 1f;
        [SerializeField] private TilePrefab _tilePrefab;

        [Header("Bake")]
        [SerializeField] private bool _bake;
        [SerializeField] private bool _optimizeMesh;
        [SerializeField] private bool _convexCollider;
        [SerializeField] private float _uvScale = 1f;

        [Header("Vertex Color")]
        [SerializeField] private bool _useVertexColor;
        [SerializeField] private VerticalGradientVertexColorer _vertexColorer = new VerticalGradientVertexColorer();

        public TilePrefab TilePrefab
        {
            get => _tilePrefab;
            set => _tilePrefab = value;
        }

        public bool Bake
        {
            get => _bake;
            set => _bake = value;
        }

        public override void DrawEditorGUI()
        {
#if UNITY_EDITOR
            base.DrawEditorGUI();

            DrawEditorGUIHeader("Tile Base");
            DrawEditorGUILine(() =>
                _verticalScale = UnityEditor.EditorGUILayout.FloatField("Vertical Scale", _verticalScale));
            DrawEditorGUILine(() =>
                _lookBackward = UnityEditor.EditorGUILayout.Toggle("Look Backward", _lookBackward));
            DrawEditorGUILine(() =>
                _tilePrefab = (TilePrefab)UnityEditor.EditorGUILayout.ObjectField("Tile Prefab", _tilePrefab, typeof(TilePrefab), false));

            UnityEditor.EditorGUILayout.Space();
            DrawEditorGUIHeader("Tile Bake");
            DrawEditorGUILine(() => _bake = UnityEditor.EditorGUILayout.Toggle("Bake", _bake));
            if (_bake)
            {
                DrawEditorGUILine(() => _optimizeMesh = UnityEditor.EditorGUILayout.Toggle("Optimize Mesh", _optimizeMesh));
                DrawEditorGUILine(() => _convexCollider = UnityEditor.EditorGUILayout.Toggle("Convex Collider", _convexCollider));
                DrawEditorGUILine(() => _uvScale = UnityEditor.EditorGUILayout.FloatField("UV Scale", _uvScale));

                UnityEditor.EditorGUILayout.Space();
                DrawEditorGUIHeader("Tile Vertex Color");
                DrawEditorGUILine(() =>
                    _useVertexColor = UnityEditor.EditorGUILayout.Toggle("Use Vertex Color", _useVertexColor));
                if (_useVertexColor)
                {
                    foreach (Action line in _vertexColorer.GetEditorGUIProperties())
                    {
                        DrawEditorGUILine(line);
                    }
                }
            }

            UnityEditor.EditorGUILayout.Space(20f);
#endif
        }

        public override void Build()
        {
            if (_tilePrefab == null)
            {
                Debug.LogWarning($"[{Name}] Tile prefab is not assigned. Layer build skipped.");
                return;
            }

            GameObject root = CreateLayerObject();
            List<MeshRenderer> renderers = SpawnTiles(root.transform);
            if (_bake)
            {
                BakeTiles(root, renderers);
            }

            root.isStatic = true;
        }

        private List<MeshRenderer> SpawnTiles(Transform root)
        {
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

            foreach (Tile tile in FilledTiles)
            {
                TilePrefab tileObject = Object.Instantiate(_tilePrefab, root);
                tileObject.Initialize();

                Vector3 tileFrom = TileToLocal(tile.From);
                Vector3 tileTo = TileToLocal(tile.To);

                Vector3 scale = tileObject.transform.localScale;
                scale *= Vector3.Distance(tileFrom, tileTo) / Vector3.Distance(tileObject.From.position, tileObject.To.position);
                scale = new Vector3(scale.x, scale.y * _verticalScale, scale.z);
                if (_lookBackward)
                {
                    scale.y *= -1f;
                }

                tileObject.transform.localScale = scale;

                Vector3 tileObjectFrom = root.InverseTransformPoint(tileObject.From.position);
                Vector3 tileObjectTo = root.InverseTransformPoint(tileObject.To.position);

                Vector3 tileObjectCenter = (tileObjectFrom + tileObjectTo) / 2f;
                Vector3 tileCenter = (tileFrom + tileTo) / 2f;
                tileObject.transform.localPosition = tileCenter - tileObjectCenter;

                meshRenderers.AddRange(tileObject.GetComponentsInChildren<MeshRenderer>());
            }

            return meshRenderers;
        }

        private void BakeTiles(GameObject root, List<MeshRenderer> renderers)
        {
            List<Material> materials = new List<Material>();
            Mesh mesh = CombineRenderers(root.transform, renderers, materials);

            Vector2[] uv = mesh.uv;
            for (int i = 0; i < uv.Length; i++)
            {
                uv[i] *= _uvScale;
            }

            mesh.uv = uv;
            if (_optimizeMesh)
            {
                mesh.Optimize();
            }

            FinalizeMesh(mesh);
            if (_useVertexColor)
            {
                _vertexColorer.ColorMesh(mesh);
            }

            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            root.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials.ToArray();
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;

            MeshCollider meshCollider = root.AddComponent<MeshCollider>();
            meshCollider.convex = _convexCollider;
            meshCollider.sharedMesh = mesh;

            SaveMesh(mesh);
        }

        private Mesh CombineRenderers(Transform root, List<MeshRenderer> renderers, List<Material> materials)
        {
            Matrix4x4 rootMatrix = root.worldToLocalMatrix;
            Dictionary<Material, List<CombineInstance>> groups = new Dictionary<Material, List<CombineInstance>>();

            foreach (MeshRenderer meshRenderer in renderers)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                bool visible = meshRenderer.enabled && meshRenderer.gameObject.activeInHierarchy;
                if (visible && meshFilter != null && meshFilter.sharedMesh != null)
                {
                    AddCombineInstances(meshRenderer, meshFilter.sharedMesh, rootMatrix, groups);
                }
            }

            List<CombineInstance> parts = new List<CombineInstance>();
            foreach (KeyValuePair<Material, List<CombineInstance>> group in groups)
            {
                Mesh part = new Mesh { indexFormat = IndexFormat.UInt32 };
                part.CombineMeshes(group.Value.ToArray(), true, true);
                parts.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
                materials.Add(group.Key);
            }

            Mesh mesh = new Mesh { name = Name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), false, true);
            foreach (CombineInstance part in parts)
            {
                Object.DestroyImmediate(part.mesh);
            }

            return mesh;
        }

        private void AddCombineInstances(MeshRenderer meshRenderer, Mesh mesh, Matrix4x4 rootMatrix,
            Dictionary<Material, List<CombineInstance>> groups)
        {
            Material[] rendererMaterials = meshRenderer.sharedMaterials;
            Matrix4x4 matrix = rootMatrix * meshRenderer.transform.localToWorldMatrix;

            for (int i = 0; i < mesh.subMeshCount && rendererMaterials.Length > 0; i++)
            {
                Material material = rendererMaterials[Mathf.Min(i, rendererMaterials.Length - 1)];
                if (material != null)
                {
                    if (!groups.TryGetValue(material, out List<CombineInstance> instances))
                    {
                        instances = new List<CombineInstance>();
                        groups.Add(material, instances);
                    }

                    instances.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = matrix });
                }
            }
        }
    }
}
