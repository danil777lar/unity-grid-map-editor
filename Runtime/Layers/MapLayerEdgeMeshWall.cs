using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class MapLayerEdgeMeshWall : MapLayerEdge
    {
        [SerializeField] private int _subwallCount = 1;
        [SerializeField] private float _wallHeight = 3f;
        [SerializeField] private float _wallWidth = 0.25f;
        [SerializeField] private float _vertexColorsRefHeight = 3f;
        [SerializeField] private List<SubwallConfig> _subwalls = new List<SubwallConfig>();

        public override float Height => _wallHeight;

        public float WallHeight
        {
            get => _wallHeight;
            set => _wallHeight = value;
        }

        public float WallWidth
        {
            get => _wallWidth;
            set => _wallWidth = value;
        }

        public List<SubwallConfig> Subwalls => _subwalls;

        public override void DrawEditorGUI()
        {
#if UNITY_EDITOR
            base.DrawEditorGUI();

            DrawEditorGUIHeader("Wall Settings");
            DrawEditorGUILine(() =>
                _wallHeight = UnityEditor.EditorGUILayout.FloatField("Wall Height", _wallHeight));
            DrawEditorGUILine(() =>
                _wallWidth = UnityEditor.EditorGUILayout.FloatField("Wall Width", _wallWidth));
            DrawEditorGUILine(() =>
                _vertexColorsRefHeight = UnityEditor.EditorGUILayout.FloatField("Vertex Colors Ref Height", _vertexColorsRefHeight));

            DrawEditorGUILine(() => { });

            DrawEditorGUILine(() =>
                _subwallCount = Mathf.Max(1, UnityEditor.EditorGUILayout.IntField("Subwalls Count", _subwallCount)));

            while (_subwalls.Count > _subwallCount)
            {
                _subwalls.RemoveAt(_subwalls.Count - 1);
            }

            for (int i = 0; i < _subwallCount; i++)
            {
                if (_subwalls.Count <= i)
                {
                    _subwalls.Add(null);
                }

                int index = i;
                DrawEditorGUILine(() =>
                    _subwalls[index] = (SubwallConfig)UnityEditor.EditorGUILayout.ObjectField(_subwalls[index], typeof(SubwallConfig), false));
            }
#endif
        }

        public override void Build()
        {
            if (!_subwalls.Exists(x => x != null))
            {
                Debug.LogWarning($"[{Name}] Wall layer has no subwall configs. Layer build skipped.");
                return;
            }

            GameObject layerObject = CreateLayerObject();

            List<Material> materials = new List<Material>();
            MeshFilter meshFilter = layerObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateMesh(materials);

            MeshRenderer meshRenderer = layerObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = materials.ToArray();

            MeshCollider meshCollider = layerObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;

            SaveMesh(meshFilter.sharedMesh);

            layerObject.isStatic = true;
        }

        private Mesh CreateMesh(List<Material> materials)
        {
            Mesh mesh = new Mesh();
            mesh.name = Name;

            MeshData meshData = new MeshData
            {
                Verts = new List<Vector3>(),
                Tris = new List<int>(),
                Uvs = new List<Vector2>(),
                Colors = new List<Color>(),
                SubMeshes = new List<SubMeshDescriptor>(),
                Materials = materials
            };

            List<Tile> tiles = GetOptimizedTiles(GetFixedTiles(FilledTiles));
            foreach (SubwallConfig subwall in _subwalls.Where(x => x != null))
            {
                int trisStart = meshData.Tris.Count;

                foreach (Tile tile in tiles)
                {
                    CreateTileMesh(subwall, tile, tiles, meshData);
                }

                meshData.SubMeshes.Add(new SubMeshDescriptor(trisStart, meshData.Tris.Count - trisStart));
                meshData.Materials.Add(subwall.Material);
            }

            mesh.indexFormat = meshData.Verts.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(meshData.Verts);
            mesh.SetIndices(meshData.Tris, MeshTopology.Triangles, 0);
            mesh.SetUVs(0, meshData.Uvs);
            mesh.SetColors(meshData.Colors);
            if (meshData.SubMeshes.Count > 0)
            {
                mesh.SetSubMeshes(meshData.SubMeshes);
            }

            FinalizeMesh(mesh);
            return mesh;
        }

        private List<Tile> GetFixedTiles(IReadOnlyList<Tile> tiles)
        {
            List<Tile> fixedTiles = new List<Tile>(tiles);
            for (int i = 0; i < fixedTiles.Count; i++)
            {
                Tile tile = fixedTiles[i];
                if (tile.From.x > tile.To.x || tile.From.y > tile.To.y)
                {
                    fixedTiles[i] = tile.Reversed();
                }
            }

            return fixedTiles;
        }

        private List<Tile> GetOptimizedTiles(List<Tile> tiles)
        {
            List<Tile> optimizedTiles = new List<Tile>(tiles);

            bool changed = true;
            while (changed)
            {
                changed = TryMergeAny(optimizedTiles);
            }

            return optimizedTiles;
        }

        private bool TryMergeAny(List<Tile> tiles)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                for (int j = i + 1; j < tiles.Count; j++)
                {
                    Tile other = tiles[j];
                    if (tile.TouchWith(other, out Vector2 selfDirection, out Vector2 otherDirection))
                    {
                        if (selfDirection == tile.Forward && otherDirection == tile.Forward
                            && !TouchesAnother(tiles, tile, other, tile.Forward))
                        {
                            ReplaceWithMerged(tiles, tile, other, new Tile(tile.From, other.To));
                            return true;
                        }

                        if (selfDirection == tile.Backward && otherDirection == tile.Backward
                            && !TouchesAnother(tiles, tile, other, tile.Backward))
                        {
                            ReplaceWithMerged(tiles, tile, other, new Tile(other.From, tile.To));
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool TouchesAnother(List<Tile> tiles, Tile tile, Tile other, Vector2 direction)
        {
            return tiles.Any(x => !x.Equals(tile) && !x.Equals(other)
                && tile.TouchWith(x, out Vector2 selfDirection, out Vector2 _)
                && selfDirection == direction);
        }

        private void ReplaceWithMerged(List<Tile> tiles, Tile tile, Tile other, Tile merged)
        {
            tiles.Remove(tile);
            tiles.Remove(other);
            tiles.Add(merged);
        }

        private bool HasNeighbour(Tile tile, List<Tile> otherTiles, Vector2 selfSide, Vector2 otherSide)
        {
            return otherTiles.Any(x =>
                tile.TouchWith(x, out Vector2 selfDirection, out Vector2 otherDirection)
                && selfDirection == selfSide && otherDirection == otherSide);
        }

        private bool HasNeighbour(Tile tile, List<Tile> otherTiles, Vector2 selfSide)
        {
            return otherTiles.Any(x =>
                tile.TouchWith(x, out Vector2 selfDirection, out Vector2 _)
                && selfDirection == selfSide);
        }

        private void GetToOffsets(Tile tile, List<Tile> otherTiles, out Vector3 right, out Vector3 left)
        {
            right = Vector3.zero;
            left = Vector3.zero;

            bool hasToRight = HasNeighbour(tile, otherTiles, tile.Forward, tile.Right);
            bool hasToLeft = HasNeighbour(tile, otherTiles, tile.Forward, tile.Left);
            bool hasToForward = HasNeighbour(tile, otherTiles, tile.Forward, tile.Forward);

            Vector3 forward = new Vector3(tile.Forward.x, 0f, tile.Forward.y) * (_wallWidth * 0.5f);
            Vector3 backward = -forward;

            if (hasToRight && hasToLeft)
            {
                right = backward;
                left = backward;
            }
            else if (hasToLeft)
            {
                right = backward;
                if (!hasToForward)
                {
                    left = forward;
                }
            }
            else if (hasToRight)
            {
                if (!hasToForward)
                {
                    right = forward;
                }

                left = backward;
            }
        }

        private void GetFromOffsets(Tile tile, List<Tile> otherTiles, out Vector3 right, out Vector3 left)
        {
            right = Vector3.zero;
            left = Vector3.zero;

            bool hasFromRight = HasNeighbour(tile, otherTiles, tile.Backward, tile.Right);
            bool hasFromLeft = HasNeighbour(tile, otherTiles, tile.Backward, tile.Left);
            bool hasFromForward = HasNeighbour(tile, otherTiles, tile.Backward, tile.Backward);

            Vector3 forward = new Vector3(tile.Forward.x, 0f, tile.Forward.y) * (_wallWidth * 0.5f);
            Vector3 backward = -forward;

            if (hasFromRight && hasFromLeft)
            {
                right = forward;
                left = forward;
            }
            else if (hasFromLeft)
            {
                right = forward;
                if (!hasFromForward)
                {
                    left = backward;
                }
            }
            else if (hasFromRight)
            {
                if (!hasFromForward)
                {
                    right = backward;
                }

                left = forward;
            }
        }

        private void CreateTileMesh(SubwallConfig subwall, Tile tile, List<Tile> otherTiles, MeshData meshData)
        {
            Vector3 from = TileToLocal(tile.From);
            Vector3 to = TileToLocal(tile.To);
            Vector3 direction = to - from;
            Vector3 perpendicular = new Vector3(-direction.z, 0f, direction.x).normalized * (_wallWidth * 0.5f);

            GetToOffsets(tile, otherTiles, out Vector3 toRight, out Vector3 toLeft);
            GetFromOffsets(tile, otherTiles, out Vector3 fromRight, out Vector3 fromLeft);

            CreateSideMesh(subwall, from, perpendicular + fromRight, to, perpendicular + toRight, true, meshData);
            CreateSideMesh(subwall, from, -perpendicular + fromLeft, to, -perpendicular + toLeft, false, meshData);

            CreateHorizontalMesh(subwall, from, perpendicular + fromRight, to, perpendicular + toRight, true, meshData);
            CreateHorizontalMesh(subwall, from, perpendicular + fromRight, to, perpendicular + toRight, false, meshData);

            if (!HasNeighbour(tile, otherTiles, tile.Forward))
            {
                CreateSideMesh(subwall, to, perpendicular + toRight, to, -perpendicular + toLeft, true, meshData);
            }

            if (!HasNeighbour(tile, otherTiles, tile.Backward))
            {
                CreateSideMesh(subwall, from, perpendicular + fromRight, from, -perpendicular + fromLeft, false, meshData);
            }
        }

        private void CreateSideMesh(SubwallConfig subwall, Vector3 from, Vector3 fromDir, Vector3 to, Vector3 toDir,
            bool lookForward, MeshData meshData)
        {
            float step = 1f / subwall.Steps;
            for (float percent = 0f; percent <= 1f; percent += step)
            {
                float nextPercent = percent + step;

                GetSubwallPointsAtPercent(from, fromDir, to, toDir, percent, subwall, out Vector3 fromBottom, out Vector3 toBottom);
                GetSubwallPointsAtPercent(from, fromDir, to, toDir, nextPercent, subwall, out Vector3 fromTop, out Vector3 toTop);

                float topColorPercent = Mathf.Clamp01(fromTop.y / _vertexColorsRefHeight);
                float bottomColorPercent = Mathf.Clamp01(fromBottom.y / _vertexColorsRefHeight);

                PlaneData data = new PlaneData
                {
                    A = fromBottom,
                    B = toBottom,
                    C = fromTop,
                    D = toTop,
                    AColor = subwall.VertexColorGradient.Evaluate(bottomColorPercent),
                    BColor = subwall.VertexColorGradient.Evaluate(bottomColorPercent),
                    CColor = subwall.VertexColorGradient.Evaluate(topColorPercent),
                    DColor = subwall.VertexColorGradient.Evaluate(topColorPercent)
                };

                bool smooth = percent > 0f && subwall.SmoothSteps;
                CreatePlaneMesh(data, lookForward, smooth, subwall.UVScale, meshData);
            }
        }

        private void CreateHorizontalMesh(SubwallConfig subwall, Vector3 from, Vector3 fromDir, Vector3 to, Vector3 toDir,
            bool top, MeshData meshData)
        {
            if (top && !subwall.DrawTop || !top && !subwall.DrawBottom)
            {
                return;
            }

            float percent = top ? 1f : 0f;
            Color color = top ? subwall.TopColor : subwall.BottomColor;

            GetSubwallPointsAtPercent(from, fromDir, to, toDir, percent, subwall, out Vector3 fromIn, out Vector3 toIn);
            GetSubwallPointsAtPercent(from, -fromDir, to, -toDir, percent, subwall, out Vector3 fromOut, out Vector3 toOut);

            PlaneData data = new PlaneData
            {
                A = fromIn,
                B = toIn,
                C = fromOut,
                D = toOut,
                AColor = color,
                BColor = color,
                CColor = color,
                DColor = color
            };

            CreatePlaneMesh(data, top, false, subwall.UVScale, meshData);
        }

        private void GetSubwallPointsAtPercent(Vector3 from, Vector3 fromDir, Vector3 to, Vector3 toDir,
            float percent, SubwallConfig subwall, out Vector3 fromRes, out Vector3 toRes)
        {
            float heightBottom = subwall.AnchorBottom * _wallHeight + subwall.OffsetBottom;
            float heightTop = subwall.AnchorTop * _wallHeight + subwall.OffsetTop;

            float yPos = Mathf.Lerp(heightBottom, heightTop, percent);

            Vector3 fromOffset = fromDir + fromDir.normalized * subwall.WidthCurve.Evaluate(percent);
            Vector3 toOffset = toDir + toDir.normalized * subwall.WidthCurve.Evaluate(percent);

            fromRes = from + fromOffset + Vector3.up * yPos;
            toRes = to + toOffset + Vector3.up * yPos;
        }

        private void CreatePlaneMesh(PlaneData data, bool lookForward, bool smooth, float uvScale, MeshData meshData)
        {
            Vector3[] positions;
            Color[] colors;
            if (smooth)
            {
                positions = new[] { data.C, data.D };
                colors = new[] { data.CColor, data.DColor };
            }
            else
            {
                positions = new[] { data.A, data.B, data.C, data.D };
                colors = new[] { data.AColor, data.BColor, data.CColor, data.DColor };
            }

            bool alongZ = Mathf.Approximately(data.A.x, data.B.x);
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 position = positions[i];
                meshData.Verts.Add(position);
                meshData.Colors.Add(colors[i]);
                meshData.Uvs.Add(new Vector2(alongZ ? position.z : position.x, position.y) * uvScale);
            }

            int index = meshData.Verts.Count - 4;
            if (lookForward)
            {
                meshData.Tris.AddRange(new[]
                {
                    index + 0, index + 1, index + 2,
                    index + 2, index + 1, index + 3
                });
            }
            else
            {
                meshData.Tris.AddRange(new[]
                {
                    index + 0, index + 2, index + 1,
                    index + 2, index + 3, index + 1
                });
            }
        }

        private struct MeshData
        {
            public List<Vector3> Verts;
            public List<int> Tris;
            public List<Vector2> Uvs;
            public List<Color> Colors;
            public List<SubMeshDescriptor> SubMeshes;
            public List<Material> Materials;
        }

        private struct PlaneData
        {
            public Vector3 A;
            public Vector3 B;
            public Vector3 C;
            public Vector3 D;
            public Color AColor;
            public Color BColor;
            public Color CColor;
            public Color DColor;
        }
    }
}
