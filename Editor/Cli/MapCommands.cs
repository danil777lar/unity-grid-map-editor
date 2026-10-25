using System;
using System.Linq;
using Larje.Core.Editor.Cli;
using Larje.Core.Extensions;
using Larje.MapEditor.Editor.Utility;
using Newtonsoft.Json.Linq;
using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Larje.MapEditor.Editor.Cli
{
    public static class MapCommands
    {
        private const string GROUP = "larje.map";
        private const string DESCRIBE_DESCRIPTION = "Describes a Larje Map in the open scenes as JSON: layers {name, type: surface|wall|tile, selected, color, tileSize, tileOffset, verticalOffset, height, tileCount, bounds{min,max}, tiles[[fromX,fromY,toX,toY]]} and nested childMaps. Grid coordinates are integers; surface/tile layers store unit cells (x,y)->(x+1,y+1), wall layers store unit edges between grid points.";
        private const string PAINT_DESCRIPTION = "Paints tiles on a Larje Map layer (undo-able). Operations are a JSON array (inline --ops or a file path --file); each item may set erase:true to remove instead of add: {op:'rect', from:[x,y], to:[x,y], filled:true} fills cells (surface/tile) or outlines the cell rect with walls (wall); {op:'cells', cells:[[x,y],...]} (surface/tile); {op:'line', from:[x,y], to:[x,y]} axis-aligned wall between grid points (wall); {op:'tiles', tiles:[[fx,fy,tx,ty],...]} raw tiles; {op:'clear'}. A missing layer is created when --create_as is surface|wall|tile. --build rebuilds meshes afterwards. Save the scene with 'scene save' when done.";

        [CliGroupCommand(GROUP, DESCRIBE_DESCRIPTION, Tags = new[] { "larje", "scenes" })]
        public static object Describe(
            [CliArg("target", "Map GameObject hierarchy path or name; optional when the scene has a single root map")] string target = null,
            [CliArg("skip_tiles", "Omit tile lists and return only layer summaries")] bool skipTiles = false)
        {
            Map map = FindMap(target);
            return new { map = map.transform.GetPath(), description = MapJson.Describe(map, !skipTiles) };
        }

        [CliGroupCommand(GROUP, PAINT_DESCRIPTION, Tags = new[] { "larje", "scenes" })]
        public static object Paint(
            [CliArg("layer", "Layer name", Required = true)] string layer,
            [CliArg("ops", "Paint operations as a JSON array")] string ops = null,
            [CliArg("file", "Path to a file with the operations JSON array, relative to the project root")] string file = null,
            [CliArg("target", "Map GameObject hierarchy path or name; optional when the scene has a single root map")] string target = null,
            [CliArg("create_as", "Layer type used to create the layer when it is missing: surface|wall|tile")] string createAs = null,
            [CliArg("build", "Rebuild map meshes after painting")] bool build = false)
        {
            Map map = FindMap(target);
            JArray operations;
            try
            {
                operations = JArray.Parse(CliText.Resolve(ops, file, "ops"));
            }
            catch (Newtonsoft.Json.JsonException exception)
            {
                throw new ArgumentException("Invalid operations JSON: " + exception.Message);
            }

            Undo.RecordObject(map, "Paint Map");
            MapLayer mapLayer = MapJson.GetOrCreateLayer(map, layer, createAs);
            MapJson.PaintResult result = MapJson.Paint(mapLayer, operations);

            EditorUtility.SetDirty(map);
            PrefabUtility.RecordPrefabInstancePropertyModifications(map);
            EditorSceneManager.MarkSceneDirty(map.gameObject.scene);

            if (build)
            {
                map.Build();
            }

            JObject data = MapJson.DescribeLayer(mapLayer, map.CurrentLayer == mapLayer, false);
            data["added"] = result.Added;
            data["removed"] = result.Removed;
            data["built"] = build;
            return data;
        }

        private static Map FindMap(string target)
        {
            Map[] maps = UnityEngine.Object.FindObjectsByType<Map>(FindObjectsInactive.Include);

            if (string.IsNullOrWhiteSpace(target))
            {
                Map[] roots = maps.Where(x => x.transform.parent == null || x.transform.parent.GetComponentInParent<Map>() == null).ToArray();
                if (roots.Length == 1)
                {
                    return roots[0];
                }

                throw new ArgumentException(roots.Length == 0
                    ? "No Map found in the open scenes"
                    : "Several maps found, pass --target: " + string.Join(", ", roots.Select(x => x.transform.GetPath())));
            }

            Map map = maps.FirstOrDefault(x => x.transform.GetPath() == target)
                ?? maps.FirstOrDefault(x => x.name == target);
            if (map == null)
            {
                throw new ArgumentException($"Map '{target}' not found. Available: " + string.Join(", ", maps.Select(x => x.transform.GetPath())));
            }

            return map;
        }
    }
}
