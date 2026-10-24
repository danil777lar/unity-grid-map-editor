using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class RectTool : MapTool
    {
        [SerializeField] private bool _filled;

        private bool _pointerDown;
        private Tile _pointerDownTile;

        public override string IconName => "ToolRect";

        public override void DrawEditorGUI()
        {
#if UNITY_EDITOR
            GUILayout.Label("Rect options");
            _filled = UnityEditor.EditorGUILayout.Toggle("Filled", _filled);
#endif
        }

        public override void Process(Map map)
        {
            Tile pointerTile = GetPointerTile(map);
            List<Tile> tiles = map.CurrentLayer.GetTilesInRect(_pointerDownTile.From, pointerTile.From, _filled).ToList();

            OnPointerHover(map, pointerTile, tiles);
            OnPointerDown(pointerTile);
            OnPointerUp(map, tiles);
        }

        private void OnPointerHover(Map map, Tile pointerTile, List<Tile> tiles)
        {
            MapLayer layer = map.CurrentLayer;
            if (tiles.Count > 1 && _pointerDown)
            {
                foreach (Tile tile in tiles)
                {
                    layer.DrawTile(tile, MapPalette.Hover);
                }
            }
            else
            {
                layer.DrawTile(pointerTile, MapPalette.Hover);
            }
        }

        private void OnPointerDown(Tile pointerTile)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                _pointerDown = true;
                _pointerDownTile = pointerTile;
            }
        }

        private void OnPointerUp(Map map, List<Tile> tiles)
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                _pointerDown = false;
                if (tiles.Count > 1)
                {
                    map.CurrentLayer.AddTiles(tiles);
                }
            }
        }

        private Tile GetPointerTile(Map map)
        {
            if (map.CurrentLayer.TryRaycastMousePosition(out Vector3 worldPosition))
            {
                return map.CurrentLayer.WorldPointToTile(worldPosition);
            }

            return new Tile();
        }
    }
}
