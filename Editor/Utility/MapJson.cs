using System;
using System.Collections.Generic;
using System.Linq;
using Larje.Core.Extensions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Larje.MapEditor.Editor.Utility
{
    public static class MapJson
    {
        public const string LAYER_SURFACE = "surface";
        public const string LAYER_WALL = "wall";
        public const string LAYER_TILE = "tile";

        public static JObject Describe(Map map, bool includeTiles)
        {
            JArray layers = new JArray();
            foreach (MapLayer layer in map.Layers)
            {
                layers.Add(DescribeLayer(layer, map.CurrentLayer == layer, includeTiles));
            }

            JArray children = new JArray();
            foreach (Transform child in map.transform)
            {
                if (child.TryGetComponent(out Map childMap))
                {
                    children.Add(Describe(childMap, includeTiles));
                }
            }

            return new JObject
            {
                ["path"] = map.transform.GetPath(),
                ["layers"] = layers,
                ["childMaps"] = children
            };
        }

        public static JObject DescribeLayer(MapLayer layer, bool selected, bool includeTiles)
        {
            JObject result = new JObject
            {
                ["name"] = layer.Name,
                ["type"] = GetLayerType(layer),
                ["selected"] = selected,
                ["color"] = "#" + ColorUtility.ToHtmlStringRGBA(layer.Color),
                ["tileSize"] = ToArray(layer.TileSize),
                ["tileOffset"] = ToArray(layer.TileOffset),
                ["verticalOffset"] = layer.VerticalOffset,
                ["height"] = layer.Height,
                ["tileCount"] = layer.FilledTiles.Count
            };

            if (layer.TryGetBounds(out Vector2Int min, out Vector2Int max))
            {
                result["bounds"] = new JObject { ["min"] = ToArray(min), ["max"] = ToArray(max) };
            }

            if (includeTiles)
            {
                result["tiles"] = new JArray(layer.FilledTiles.Select(tile =>
                    new JArray(tile.From.x, tile.From.y, tile.To.x, tile.To.y)));
            }

            return result;
        }

        public static string GetLayerType(MapLayer layer)
        {
            switch (layer)
            {
                case MapLayerFaceMeshPlane _:
                    return LAYER_SURFACE;
                case MapLayerEdgeMeshWall _:
                    return LAYER_WALL;
                case MapLayerFacePrefabTile _:
                    return LAYER_TILE;
                default:
                    return layer.GetType().Name;
            }
        }

        public static MapLayer GetOrCreateLayer(Map map, string layerName, string createAs)
        {
            MapLayer layer = map.FindLayer(layerName);
            if (layer != null)
            {
                return layer;
            }

            if (string.IsNullOrEmpty(createAs))
            {
                throw new ArgumentException($"Layer '{layerName}' not found. Pass a layer type ({LAYER_SURFACE}|{LAYER_WALL}|{LAYER_TILE}) to create it");
            }

            switch (createAs.ToLowerInvariant())
            {
                case LAYER_SURFACE:
                    return map.AddLayer<MapLayerFaceMeshPlane>(layerName);
                case LAYER_WALL:
                    return map.AddLayer<MapLayerEdgeMeshWall>(layerName);
                case LAYER_TILE:
                    return map.AddLayer<MapLayerFacePrefabTile>(layerName);
                default:
                    throw new ArgumentException($"Unknown layer type '{createAs}'. Use {LAYER_SURFACE}|{LAYER_WALL}|{LAYER_TILE}");
            }
        }

        public static PaintResult Paint(MapLayer layer, JArray operations)
        {
            PaintResult result = new PaintResult();
            for (int i = 0; i < operations.Count; i++)
            {
                if (!(operations[i] is JObject operation))
                {
                    throw new ArgumentException($"Operation #{i} must be an object");
                }

                ApplyOperation(layer, operation, i, ref result);
            }

            return result;
        }

        private static void ApplyOperation(MapLayer layer, JObject operation, int index, ref PaintResult result)
        {
            string op = operation.Value<string>("op");
            bool erase = operation.Value<bool?>("erase") ?? false;

            if (op == "clear")
            {
                result.Removed += layer.FilledTiles.Count;
                layer.ClearTiles();
                return;
            }

            List<Tile> tiles = GetOperationTiles(layer, op, operation, index);
            if (erase)
            {
                result.Removed += layer.RemoveTiles(tiles);
            }
            else
            {
                result.Added += layer.AddTiles(tiles);
            }
        }

        private static List<Tile> GetOperationTiles(MapLayer layer, string op, JObject operation, int index)
        {
            switch (op)
            {
                case "rect":
                    bool filled = operation.Value<bool?>("filled") ?? true;
                    return layer.GetTilesInRect(ReadPoint(operation, "from", index), ReadPoint(operation, "to", index), filled).ToList();
                case "cells":
                    if (!(layer is MapLayerFace))
                    {
                        throw new ArgumentException($"Operation #{index}: 'cells' works only on surface and tile layers");
                    }

                    return ReadPoints(operation, "cells", index).Select(cell => MapLayerFace.Cell(cell.x, cell.y)).ToList();
                case "line":
                    if (!(layer is MapLayerEdge edgeLayer))
                    {
                        throw new ArgumentException($"Operation #{index}: 'line' works only on wall layers");
                    }

                    return edgeLayer.GetTilesOnLine(ReadPoint(operation, "from", index), ReadPoint(operation, "to", index)).ToList();
                case "tiles":
                    return ReadTiles(operation, index);
                default:
                    throw new ArgumentException($"Operation #{index}: unknown op '{op}'. Use rect|cells|line|tiles|clear");
            }
        }

        private static Vector2Int ReadPoint(JObject operation, string key, int index)
        {
            return ParsePoint(operation[key], $"Operation #{index}: '{key}'");
        }

        private static List<Vector2Int> ReadPoints(JObject operation, string key, int index)
        {
            if (!(operation[key] is JArray points))
            {
                throw new ArgumentException($"Operation #{index}: '{key}' must be an array of [x, y]");
            }

            return points.Select(point => ParsePoint(point, $"Operation #{index}: '{key}' item")).ToList();
        }

        private static List<Tile> ReadTiles(JObject operation, int index)
        {
            if (!(operation["tiles"] is JArray items))
            {
                throw new ArgumentException($"Operation #{index}: 'tiles' must be an array of [fromX, fromY, toX, toY]");
            }

            List<Tile> tiles = new List<Tile>();
            foreach (JToken item in items)
            {
                if (!(item is JArray coords) || coords.Count != 4)
                {
                    throw new ArgumentException($"Operation #{index}: tile must be [fromX, fromY, toX, toY]");
                }

                tiles.Add(new Tile(
                    new Vector2Int(coords[0].Value<int>(), coords[1].Value<int>()),
                    new Vector2Int(coords[2].Value<int>(), coords[3].Value<int>())));
            }

            return tiles;
        }

        private static Vector2Int ParsePoint(JToken token, string context)
        {
            if (!(token is JArray coords) || coords.Count != 2)
            {
                throw new ArgumentException($"{context} must be [x, y]");
            }

            return new Vector2Int(coords[0].Value<int>(), coords[1].Value<int>());
        }

        private static JArray ToArray(Vector2 vector)
        {
            return new JArray(vector.x, vector.y);
        }

        private static JArray ToArray(Vector2Int vector)
        {
            return new JArray(vector.x, vector.y);
        }

        public struct PaintResult
        {
            public int Added;
            public int Removed;
        }
    }
}
