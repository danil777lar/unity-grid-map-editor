using System;
using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public abstract class MapLayerFace : MapLayer
    {
        public static Tile Cell(int x, int y)
        {
            Vector2Int from = new Vector2Int(x, y);
            return new Tile(from, from + Vector2Int.one);
        }

        public override Tile WorldPointToTile(Vector3 worldPoint)
        {
            Vector2 tilePoint = WorldToTile(worldPoint);
            return Cell(Mathf.FloorToInt(tilePoint.x), Mathf.FloorToInt(tilePoint.y));
        }

        public override Tile WorldPointToTile(Vector3 worldPoint, Tile pointerDownTile)
        {
            return WorldPointToTile(worldPoint);
        }

        public override IReadOnlyCollection<Tile> GetTilesInRadius(Vector2 center, int radius)
        {
            int extent = radius - 1;
            Vector2Int flooredCenter = new Vector2Int(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y));

            List<Tile> tiles = new List<Tile>();
            for (int x = -extent; x <= extent; x++)
            {
                for (int y = -extent; y <= extent; y++)
                {
                    tiles.Add(Cell(flooredCenter.x + x, flooredCenter.y + y));
                }
            }

            return tiles;
        }

        public override IReadOnlyCollection<Tile> GetTilesInRect(Vector2 from, Vector2 to, bool filled)
        {
            Vector2Int min = new Vector2Int(
                Mathf.FloorToInt(Mathf.Min(from.x, to.x)),
                Mathf.FloorToInt(Mathf.Min(from.y, to.y)));

            Vector2Int max = new Vector2Int(
                Mathf.FloorToInt(Mathf.Max(from.x, to.x)),
                Mathf.FloorToInt(Mathf.Max(from.y, to.y)));

            List<Tile> tiles = new List<Tile>();
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    if (filled || x == min.x || x == max.x || y == min.y || y == max.y)
                    {
                        tiles.Add(Cell(x, y));
                    }
                }
            }

            return tiles;
        }
    }
}
