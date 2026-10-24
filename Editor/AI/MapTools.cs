using System;
using System.Linq;
using Larje.Core.Extensions;
using Larje.MapEditor.Editor.Utility;
using Newtonsoft.Json.Linq;
using Unity.AI.MCP.Editor.Helpers;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Larje.MapEditor.Editor.AI
{
    public static class MapTools
    {
        private const string DESCRIBE_DESCRIPTION = "Describes a Larje Map in the open scenes as JSON: layers {name, type: surface|wall|tile, selected, color, tileSize, tileOffset, verticalOffset, height, tileCount, bounds{min,max}, tiles[[fromX,fromY,toX,toY]]} and nested childMaps. Grid coordinates are integers; surface/tile layers store unit cells (x,y)->(x+1,y+1), wall layers store unit edges between grid points.";
        private const string PAINT_DESCRIPTION = "Paints tiles on a Larje Map layer (undo-able). Operations JSON array, each item may set erase:true to remove instead of add: {op:'rect', from:[x,y], to:[x,y], filled:true} fills cells (surface/tile) or outlines the cell rect with walls (wall); {op:'cells', cells:[[x,y],...]} (surface/tile); {op:'line', from:[x,y], to:[x,y]} axis-aligned wall between grid points (wall); {op:'tiles', tiles:[[fx,fy,tx,ty],...]} raw tiles; {op:'clear'}. Missing layer is created when CreateAs is surface|wall|tile. Set Build=true to rebuild meshes afterwards.";

        [McpTool("Larje.DescribeMap", DESCRIBE_DESCRIPTION, "Describe Larje Map", EnabledByDefault = true, Groups = new[] { "larje", "larje-scene" })]
        public static object Describe(DescribeParams parameters)
        {
            Map map = FindMap(parameters.Target, out string error);
            if (map == null)
            {
                return Response.Error(error);
            }

            JObject description = MapJson.Describe(map, !parameters.SkipTiles);
            return Response.Success(map.transform.GetPath(), description.ToString());
        }

        [McpTool("Larje.PaintMap", PAINT_DESCRIPTION, "Paint Larje Map", EnabledByDefault = true, Groups = new[] { "larje", "larje-scene" })]
        public static object Paint(PaintParams parameters)
        {
            Map map = FindMap(parameters.Target, out string error);
            if (map == null)
            {
                return Response.Error(error);
            }

            JArray operations;
            try
            {
                operations = string.IsNullOrWhiteSpace(parameters.Operations) ? new JArray() : JArray.Parse(parameters.Operations);
            }
            catch (Exception exception)
            {
                return Response.Error("Invalid operations JSON: " + exception.Message);
            }

            Undo.RecordObject(map, "Paint Map");
            MapJson.PaintResult result;
            MapLayer layer;
            try
            {
                layer = MapJson.GetOrCreateLayer(map, parameters.Layer, parameters.CreateAs);
                result = MapJson.Paint(layer, operations);
            }
            catch (ArgumentException exception)
            {
                return Response.Error(exception.Message);
            }

            EditorUtility.SetDirty(map);
            PrefabUtility.RecordPrefabInstancePropertyModifications(map);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);

            if (parameters.Build)
            {
                map.Build();
            }

            JObject data = MapJson.DescribeLayer(layer, map.CurrentLayer == layer, false);
            data["added"] = result.Added;
            data["removed"] = result.Removed;
            data["built"] = parameters.Build;
            return Response.Success($"Painted layer '{layer.Name}'", data.ToString());
        }

        private static Map FindMap(string target, out string error)
        {
            Map[] maps = UnityEngine.Object.FindObjectsByType<Map>(FindObjectsInactive.Include);
            error = null;

            if (string.IsNullOrWhiteSpace(target))
            {
                Map[] roots = maps.Where(x => x.transform.parent == null || x.transform.parent.GetComponentInParent<Map>() == null).ToArray();
                if (roots.Length == 1)
                {
                    return roots[0];
                }

                error = roots.Length == 0
                    ? "No Map found in the open scenes"
                    : "Several maps found, pass Target: " + string.Join(", ", roots.Select(x => x.transform.GetPath()));
                return null;
            }

            Map map = maps.FirstOrDefault(x => x.transform.GetPath() == target)
                ?? maps.FirstOrDefault(x => x.name == target);
            if (map == null)
            {
                error = $"Map '{target}' not found. Available: " + string.Join(", ", maps.Select(x => x.transform.GetPath()));
            }

            return map;
        }

        public sealed class DescribeParams
        {
            [McpDescription("Map GameObject hierarchy path or name; optional when the scene has a single root map")]
            public string Target { get; set; }

            [McpDescription("Omit tile lists and return only layer summaries")]
            public bool SkipTiles { get; set; }
        }

        public sealed class PaintParams
        {
            [McpDescription("Map GameObject hierarchy path or name; optional when the scene has a single root map")]
            public string Target { get; set; }

            [McpDescription("Layer name", Required = true)]
            public string Layer { get; set; }

            [McpDescription("Layer type used to create the layer when it is missing: surface|wall|tile")]
            public string CreateAs { get; set; }

            [McpDescription("JSON array of paint operations", Required = true)]
            public string Operations { get; set; }

            [McpDescription("Rebuild map meshes after painting (default false)")]
            public bool Build { get; set; }
        }
    }
}
