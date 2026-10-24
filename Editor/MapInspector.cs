using UnityEditor;
using UnityEngine;

namespace Larje.MapEditor.Editor
{
    [CustomEditor(typeof(Map))]
    public sealed class MapInspector : UnityEditor.Editor
    {
        private const int TOOL_BUTTON_SIZE = 30;
        private const int TOOL_BUTTON_PADDING = 3;

        private bool _foldoutTools = true;
        private bool _foldoutLayers = true;
        private Map _map;

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(_map, "Edit Map");
            _map.BindLayers();

            EditorGUI.BeginChangeCheck();
            DrawToolsGroup();
            DrawLayersGroup();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_map);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_map);
            }

            DrawBuildGroup();
        }

        private void OnEnable()
        {
            _map = (Map)target;
        }

        private void DrawToolsGroup()
        {
            _foldoutTools = EditorGUILayout.Foldout(_foldoutTools, "TOOLS");
            if (!_foldoutTools)
            {
                return;
            }

            EditorGUI.indentLevel++;
            GUILayout.Space(10);

            MapLayer layer = _map.CurrentLayer;
            if (layer != null)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (MapTool tool in layer.GetTools())
                {
                    GUIStyle style = layer.CurrentTool == tool ? GetToolButtonSelectedStyle() : GetToolButtonStyle();
                    if (GUILayout.Button(MapEditorIcons.Get(tool.IconName), style, GetToolButtonOptions()))
                    {
                        layer.CurrentTool = layer.CurrentTool == tool ? null : tool;
                    }

                    GUILayout.Space(10);
                }

                EditorGUILayout.EndHorizontal();

                if (layer.CurrentTool != null)
                {
                    GUILayout.Space(10);
                    layer.CurrentTool.DrawEditorGUI();
                }
            }
            else
            {
                GUILayout.Label("Select a layer to use tools");
            }

            GUILayout.Space(30);
            EditorGUI.indentLevel--;
        }

        private void DrawLayersGroup()
        {
            _foldoutLayers = EditorGUILayout.Foldout(_foldoutLayers, "LAYERS");
            if (!_foldoutLayers)
            {
                return;
            }

            EditorGUI.indentLevel++;
            GUILayout.Space(10f);

            for (int i = 0; i < _map.Layers.Count; i++)
            {
                if (DrawLayer(_map.Layers[i]))
                {
                    break;
                }
            }

            GUILayout.Space(20f);
            if (GUILayout.Button("Add Layer: Surface"))
            {
                _map.AddLayer<MapLayerFaceMeshPlane>("Surface");
            }

            if (GUILayout.Button("Add Layer: Wall"))
            {
                _map.AddLayer<MapLayerEdgeMeshWall>("Wall");
            }

            if (GUILayout.Button("Add Layer: Tile"))
            {
                _map.AddLayer<MapLayerFacePrefabTile>("Tile");
            }

            EditorGUI.indentLevel--;
        }

        private void DrawBuildGroup()
        {
            GUILayout.Space(20);
            if (GUILayout.Button("Build"))
            {
                _map.Build();
                EditorUtility.SetDirty(_map.gameObject);
            }
        }

        private bool DrawLayer(MapLayer layer)
        {
            bool removed = false;

            EditorGUILayout.BeginHorizontal();
            bool selected = _map.CurrentLayer == layer;
            if (GUILayout.Button(selected ? "=>" : "SELECT", GUILayout.Width(60)))
            {
                _map.CurrentLayer = selected ? null : layer;
            }

            string layerName = EditorGUILayout.TextField(layer.Name);
            if (layerName != layer.Name)
            {
                layer.Name = _map.GetUniqueLayerName(layerName, layer);
            }

            layer.Color = EditorGUILayout.ColorField(layer.Color, GUILayout.Width(75));

            GUILayout.Space(5f);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _map.RemoveLayer(layer);
                removed = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (!removed && GUILayout.Button("DETACH", GUILayout.Width(60)))
            {
                Map detached = _map.DetachLayer(layer);
                Undo.RegisterCreatedObjectUndo(detached.gameObject, "Detach Map Layer");
                removed = true;
            }

            if (!removed && GUILayout.Button("CLEAR", GUILayout.Width(60)))
            {
                _map.ClearLayer(layer);
            }

            EditorGUILayout.EndHorizontal();

            if (!removed)
            {
                GUILayout.Space(10f);
                layer.DrawEditorGUI();
            }

            GUILayout.Space(20f);
            return removed;
        }

        private GUILayoutOption[] GetToolButtonOptions()
        {
            return new[]
            {
                GUILayout.Height(TOOL_BUTTON_SIZE),
                GUILayout.Width(TOOL_BUTTON_SIZE),
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            };
        }

        private GUIStyle GetToolButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING),
                normal =
                {
                    background = Texture2D.blackTexture
                }
            };
        }

        private GUIStyle GetToolButtonSelectedStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING, TOOL_BUTTON_PADDING)
            };
        }
    }
}
