using System;
using System.Linq;
using Larje.MapEditor.Editor.Utility;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Larje.MapEditor.Tests
{
    public sealed class MapPaintTests
    {
        private Map _map;

        [SetUp]
        public void SetUp()
        {
            _map = new GameObject("TestMap").AddComponent<Map>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_map.gameObject);
        }

        [Test]
        public void FilledRectOnSurfaceAddsEveryCell()
        {
            MapLayer layer = _map.AddLayer<MapLayerFaceMeshPlane>("Floor");

            int added = layer.AddTiles(layer.GetTilesInRect(new Vector2(0, 0), new Vector2(2, 1), true));

            Assert.AreEqual(6, added);
            Assert.IsTrue(layer.ContainsTile(MapLayerFace.Cell(2, 1)));
        }

        [Test]
        public void OutlineRectOnSurfaceSkipsInnerCells()
        {
            MapLayer layer = _map.AddLayer<MapLayerFaceMeshPlane>("Floor");

            layer.AddTiles(layer.GetTilesInRect(new Vector2(0, 0), new Vector2(2, 2), false));

            Assert.AreEqual(8, layer.FilledTiles.Count);
            Assert.IsFalse(layer.ContainsTile(MapLayerFace.Cell(1, 1)));
        }

        [Test]
        public void RectOnWallBuildsPerimeterEdges()
        {
            MapLayer layer = _map.AddLayer<MapLayerEdgeMeshWall>("Walls");

            layer.AddTiles(layer.GetTilesInRect(new Vector2(0, 0), new Vector2(2, 1), false));

            Assert.AreEqual(10, layer.FilledTiles.Count);
            Assert.IsTrue(layer.ContainsTile(new Tile(new Vector2Int(3, 0), new Vector2Int(3, 1))));
        }

        [Test]
        public void ReversedEdgeIsTreatedAsSameTile()
        {
            MapLayer layer = _map.AddLayer<MapLayerEdgeMeshWall>("Walls");
            Tile edge = new Tile(new Vector2Int(0, 0), new Vector2Int(1, 0));

            Assert.IsTrue(layer.AddTile(edge));
            Assert.IsFalse(layer.AddTile(edge.Reversed()));
            Assert.IsTrue(layer.RemoveTile(edge.Reversed()));
            Assert.AreEqual(0, layer.FilledTiles.Count);
        }

        [Test]
        public void WallLineRejectsDiagonal()
        {
            MapLayerEdge layer = _map.AddLayer<MapLayerEdgeMeshWall>("Walls");

            Assert.AreEqual(3, layer.GetTilesOnLine(new Vector2Int(0, 0), new Vector2Int(0, 3)).Count);
            Assert.Throws<ArgumentException>(() => layer.GetTilesOnLine(new Vector2Int(0, 0), new Vector2Int(2, 3)));
        }

        [Test]
        public void JsonPaintCreatesLayerAndErases()
        {
            MapLayer layer = MapJson.GetOrCreateLayer(_map, "Floor", MapJson.LAYER_SURFACE);
            JArray operations = JArray.Parse(
                "[{\"op\":\"rect\",\"from\":[0,0],\"to\":[3,3]}," +
                "{\"op\":\"cells\",\"cells\":[[1,1],[2,2]],\"erase\":true}]");

            MapJson.PaintResult result = MapJson.Paint(layer, operations);

            Assert.IsInstanceOf<MapLayerFaceMeshPlane>(layer);
            Assert.AreEqual(16, result.Added);
            Assert.AreEqual(2, result.Removed);
            Assert.AreEqual(14, layer.FilledTiles.Count);
        }

        [Test]
        public void JsonPaintRejectsLineOnSurface()
        {
            MapLayer layer = MapJson.GetOrCreateLayer(_map, "Floor", MapJson.LAYER_SURFACE);
            JArray operations = JArray.Parse("[{\"op\":\"line\",\"from\":[0,0],\"to\":[0,3]}]");

            Assert.Throws<ArgumentException>(() => MapJson.Paint(layer, operations));
        }

        [Test]
        public void DescribeReportsLayerTypesAndBounds()
        {
            MapLayer floor = MapJson.GetOrCreateLayer(_map, "Floor", MapJson.LAYER_SURFACE);
            MapJson.GetOrCreateLayer(_map, "Walls", MapJson.LAYER_WALL);
            floor.AddTiles(floor.GetTilesInRect(new Vector2(-1, 0), new Vector2(1, 0), true));

            JObject description = MapJson.Describe(_map, true);
            JArray layers = (JArray)description["layers"];

            Assert.AreEqual(2, layers.Count);
            Assert.AreEqual("surface", layers[0].Value<string>("type"));
            Assert.AreEqual("wall", layers[1].Value<string>("type"));
            Assert.AreEqual(-1, layers[0]["bounds"]["min"][0].Value<int>());
            Assert.AreEqual(2, layers[0]["bounds"]["max"][0].Value<int>());
            Assert.AreEqual(3, ((JArray)layers[0]["tiles"]).Count);
        }

        [Test]
        public void SurfaceMeshMergesCellsIntoSingleQuad()
        {
            MapLayerFaceMeshPlane layer = _map.AddLayer<MapLayerFaceMeshPlane>("Floor");
            layer.AddTiles(layer.GetTilesInRect(new Vector2(0, 0), new Vector2(3, 2), true));

            Assert.AreEqual(1, layer.GetOptimizedTiles().Count);
            Assert.AreEqual(new Vector2Int(4, 3), layer.GetOptimizedTiles().Single().To);
        }

        [Test]
        public void LayerNamesStayUnique()
        {
            _map.AddLayer<MapLayerFaceMeshPlane>("Floor");
            MapLayer second = _map.AddLayer<MapLayerFaceMeshPlane>("Floor");

            Assert.AreEqual("Floor 1", second.Name);
            Assert.AreSame(second, _map.FindLayer("Floor 1"));
        }
    }
}
