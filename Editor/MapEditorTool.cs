using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Larje.MapEditor.Editor
{
    [EditorTool("Map Editor Tool", typeof(Map))]
    public sealed class MapEditorTool : EditorTool
    {
        public override GUIContent toolbarIcon => new GUIContent(MapEditorIcons.Get("MapEditorIcon"), "Map Editor Tool");

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView))
            {
                return;
            }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            foreach (Object targetObject in targets)
            {
                if (targetObject is Map map)
                {
                    UpdateMap(map);
                }
            }
        }

        private void UpdateMap(Map map)
        {
            map.BindLayers();

            foreach (MapLayer layer in map.Layers)
            {
                if (map.CurrentLayer == layer)
                {
                    layer.DrawGrid();
                }

                layer.DrawFilledTiles(map.CurrentLayer == layer);
            }

            if (map.CurrentLayer is { CurrentTool: not null })
            {
                ProcessTool(map);
            }
        }

        private void ProcessTool(Map map)
        {
            bool editing = Event.current.button == 0 && (Event.current.type == EventType.MouseDown
                || Event.current.type == EventType.MouseDrag
                || Event.current.type == EventType.MouseUp);

            if (editing)
            {
                Undo.RecordObject(map, "Paint Map");
            }

            map.CurrentLayer.CurrentTool.Process(map);

            if (editing)
            {
                EditorUtility.SetDirty(map);
                PrefabUtility.RecordPrefabInstancePropertyModifications(map);
            }
        }
    }
}
