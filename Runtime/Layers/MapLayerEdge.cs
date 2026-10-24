using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larje.MapEditor
{
    [Serializable]
    public abstract class MapLayerEdge : MapLayer
    {
        public override Tile WorldPointToTile(Vector3 worldPoint, Tile pointerDownTile)
        {
            Vector2 direction = ((Vector2)pointerDownTile.To - pointerDownTile.From).normalized;
            return WorldRayToTile(worldPoint, direction);
        }

        public override Tile WorldPointToTile(Vector3 worldPoint)
        {
            return WorldRayToTile(worldPoint, Vector2.zero);
        }

        public override IReadOnlyCollection<Tile> GetTilesInRadius(Vector2 center, int radius)
        {
            int extent = radius - 1;
            Vector2Int flooredCenter = new Vector2Int(Mathf.FloorToInt(center.x), Mathf.FloorToInt(center.y));

            Tile closest = GetCellEdges(flooredCenter)
                .OrderBy(tile => Vector2.Distance(center, tile.GetCenter()))
                .First();
            Vector2Int from = closest.From;
            Vector2Int delta = closest.From - closest.To;

            List<Tile> tiles = new List<Tile>();
            for (int i = -extent - 1; i < extent; i++)
            {
                tiles.Add(new Tile(from + delta * i, from + delta + delta * i));
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
                    Vector2Int cell = new Vector2Int(x, y);
                    if (x == min.x)
                    {
                        tiles.Add(new Tile(cell, cell + Vector2Int.up));
                    }

                    if (x == max.x)
                    {
                        tiles.Add(new Tile(cell + Vector2Int.right, cell + Vector2Int.one));
                    }

                    if (y == min.y)
                    {
                        tiles.Add(new Tile(cell, cell + Vector2Int.right));
                    }

                    if (y == max.y)
                    {
                        tiles.Add(new Tile(cell + Vector2Int.up, cell + Vector2Int.one));
                    }
                }
            }

            return tiles;
        }

        public IReadOnlyCollection<Tile> GetTilesOnLine(Vector2Int from, Vector2Int to)
        {
            if (from.x != to.x && from.y != to.y)
            {
                throw new ArgumentException($"Edge line {from} -> {to} must be horizontal or vertical");
            }

            Vector2Int step = new Vector2Int(Math.Sign(to.x - from.x), Math.Sign(to.y - from.y));
            List<Tile> tiles = new List<Tile>();
            Vector2Int current = from;
            while (current != to)
            {
                tiles.Add(new Tile(current, current + step));
                current += step;
            }

            return tiles;
        }

        public Tile WorldRayToTile(Vector3 worldFrom, Vector2 direction)
        {
            Vector2 tilePoint = WorldToTile(worldFrom);
            Vector2Int cell = new Vector2Int(Mathf.FloorToInt(tilePoint.x), Mathf.FloorToInt(tilePoint.y));

            List<Tile> tiles = GetCellEdges(cell);
            if (direction != Vector2.zero)
            {
                tiles = tiles.FindAll(x =>
                {
                    Vector2 xDirection = ((Vector2)x.To - x.From).normalized;
                    return xDirection == direction || xDirection == -direction;
                });
            }

            return tiles.OrderBy(tile => Vector3.Distance(worldFrom, TileToWorld(tile.GetCenter()))).First();
        }

        private List<Tile> GetCellEdges(Vector2Int cell)
        {
            return new List<Tile>
            {
                new Tile(cell, cell + Vector2Int.up),
                new Tile(cell + Vector2Int.up, cell + Vector2Int.one),
                new Tile(cell + Vector2Int.one, cell + Vector2Int.right),
                new Tile(cell + Vector2Int.right, cell)
            };
        }
    }
}
