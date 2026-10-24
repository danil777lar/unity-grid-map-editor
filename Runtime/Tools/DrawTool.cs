using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public abstract class DrawTool : MapTool
    {
        [SerializeField] private int _radius = 1;

        private bool _pointerDown;
        private Tile _pointerDownTile;

        public override void DrawEditorGUI()
        {
#if UNITY_EDITOR
            GUILayout.Label("Options:");
            _radius = Mathf.Max(UnityEditor.EditorGUILayout.IntField("Radius", _radius), 1);
#endif
        }

        public override void Process(Map map)
        {
            List<Tile> tiles = GetCurrentTiles(map);

            UpdatePointerState();
            DrawTiles(map, tiles);
            TryFillTiles(map, tiles);
        }

        protected abstract void OnDraw(MapLayer layer, Tile tile);

        private void UpdatePointerState()
        {
            if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                _pointerDown = true;
            }
            else if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                _pointerDown = false;
            }
        }

        private List<Tile> GetCurrentTiles(Map map)
        {
            List<Tile> tiles = new List<Tile>();

            if (map.CurrentLayer != null && map.CurrentLayer.TryRaycastMousePosition(out Vector3 position))
            {
                Tile centerTile = _pointerDown
                    ? map.CurrentLayer.WorldPointToTile(position, _pointerDownTile)
                    : map.CurrentLayer.WorldPointToTile(position);

                tiles = map.CurrentLayer.GetTilesInRadius(centerTile.GetCenter(), _radius).ToList();

                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                {
                    _pointerDownTile = centerTile;
                }
            }

            return tiles;
        }

        private void DrawTiles(Map map, List<Tile> tiles)
        {
            foreach (Tile tile in tiles)
            {
                map.CurrentLayer.DrawTile(tile, MapPalette.Hover);
            }
        }

        private void TryFillTiles(Map map, List<Tile> tiles)
        {
            bool pressed = Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag;
            pressed &= Event.current.button == 0;

            if (pressed)
            {
                foreach (Tile tile in tiles)
                {
                    OnDraw(map.CurrentLayer, tile);
                }
            }
        }
    }
}
