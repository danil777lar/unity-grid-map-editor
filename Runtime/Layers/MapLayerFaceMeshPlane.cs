using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class MapLayerFaceMeshPlane : MapLayerFace
    {
        [SerializeField] private bool _lookBackwards;
        [SerializeField] private Vector2 _uvScale = new Vector2(1f, 1f);
        [SerializeField] private Color _vertexColor = Color.white;
        [SerializeField] private Material _material;

        public Material Material
        {
            get => _material;
            set => _material = value;
        }

        public override void DrawEditorGUI()
        {
#if UNITY_EDITOR
            base.DrawEditorGUI();

            DrawEditorGUIHeader("Plane Settings");
            DrawEditorGUILine(() =>
                _lookBackwards = UnityEditor.EditorGUILayout.Toggle("Look Backwards", _lookBackwards));
            DrawEditorGUILine(() =>
                _uvScale = UnityEditor.EditorGUILayout.Vector2Field("UV Scale", _uvScale));
            DrawEditorGUILine(() =>
                _material = UnityEditor.EditorGUILayout.ObjectField("Material", _material, typeof(Material), false) as Material);
            DrawEditorGUILine(() =>
                _vertexColor = UnityEditor.EditorGUILayout.ColorField("Vertex Color", _vertexColor));
#endif
        }

        public override void Build()
        {
            GameObject layerObject = CreateLayerObject();

            MeshFilter meshFilter = layerObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateMesh();

            MeshRenderer meshRenderer = layerObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;

            MeshCollider meshCollider = layerObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;

            SaveMesh(meshFilter.sharedMesh);

            layerObject.isStatic = true;
        }

        public List<Tile> GetOptimizedTiles()
        {
            List<Tile> tiles = OptimizeTiles(FilledTiles.ToList(), true);
            return OptimizeTiles(tiles, false);
        }

        private List<Tile> OptimizeTiles(List<Tile> input, bool directionHorizontal)
        {
            List<Tile> tiles = new List<Tile>(input);

            bool changed = true;
            while (changed)
            {
                changed = TryMergeAny(tiles, directionHorizontal);
            }

            return tiles;
        }

        private bool TryMergeAny(List<Tile> tiles, bool directionHorizontal)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                for (int j = 0; j < tiles.Count; j++)
                {
                    Tile other = tiles[j];
                    if (i != j && CanMerge(tile, other, directionHorizontal))
                    {
                        tile.To = other.To;
                        tiles[i] = tile;
                        tiles.RemoveAt(j);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanMerge(Tile tile, Tile other, bool directionHorizontal)
        {
            if (directionHorizontal)
            {
                return tile.To.x == other.From.x && tile.From.y == other.From.y && tile.To.y == other.To.y;
            }

            return tile.To.y == other.From.y && tile.From.x == other.From.x && tile.To.x == other.To.x;
        }

        private Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = Name;

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            foreach (Tile tile in GetOptimizedTiles())
            {
                CreateTileMesh(tile, vertices, triangles, uvs);
            }

            mesh.indexFormat = vertices.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(Enumerable.Repeat(_vertexColor, vertices.Count).ToList());

            FinalizeMesh(mesh);
            return mesh;
        }

        private void CreateTileMesh(Tile tile, List<Vector3> verts, List<int> tris, List<Vector2> uvs)
        {
            Vector3[] positions =
            {
                TileToLocal(tile.From),
                TileToLocal(new Vector2(tile.To.x, tile.From.y)),
                TileToLocal(tile.To),
                TileToLocal(new Vector2(tile.From.x, tile.To.y))
            };

            foreach (Vector3 position in positions)
            {
                verts.Add(position);
                uvs.Add(new Vector2(position.x, position.z) * _uvScale);
            }

            int index = verts.Count - 4;
            if (_lookBackwards)
            {
                tris.AddRange(new[] { index, index + 1, index + 2, index, index + 2, index + 3 });
            }
            else
            {
                tris.AddRange(new[] { index, index + 2, index + 1, index, index + 3, index + 2 });
            }
        }
    }
}
