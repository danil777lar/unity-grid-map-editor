using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor
{
    public sealed class Map : MonoBehaviour
    {
        [SerializeReference] private MapLayer _currentLayer;
        [SerializeReference] private List<MapLayer> _layers = new List<MapLayer>();

        public IReadOnlyList<MapLayer> Layers => _layers;

        public MapLayer CurrentLayer
        {
            get => _currentLayer;
            set => _currentLayer = value != null && _layers.Contains(value) ? value : null;
        }

        public void Build()
        {
            gameObject.isStatic = true;
            BindLayers();

            List<Map> mapsToRebuild = new List<Map>();
            List<GameObject> objectsToDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent(out Map map))
                {
                    mapsToRebuild.Add(map);
                }
                else
                {
                    objectsToDestroy.Add(child.gameObject);
                }
            }

            objectsToDestroy.ForEach(DestroyImmediate);
            mapsToRebuild.ForEach(x => x.Build());

            foreach (MapLayer layer in _layers)
            {
                if (layer.FilledTiles.Count > 0)
                {
                    layer.Build();
                }
            }
        }

        public void BindLayers()
        {
            foreach (MapLayer layer in _layers)
            {
                layer.SetTransform(transform);
            }
        }

        public T AddLayer<T>(string layerName) where T : MapLayer, new()
        {
            T layer = new T();
            layer.Name = layerName;
            layer.Color = MapPalette.GetLayerColor(_layers.Count);
            AddLayer(layer);
            return layer;
        }

        public void AddLayer(MapLayer layer)
        {
            if (!_layers.Contains(layer))
            {
                layer.Name = GetUniqueLayerName(layer.Name, layer);
                layer.SetTransform(transform);
                _layers.Add(layer);
            }
        }

        public bool RemoveLayer(MapLayer layer)
        {
            if (_currentLayer == layer)
            {
                _currentLayer = null;
            }

            return _layers.Remove(layer);
        }

        public MapLayer FindLayer(string layerName)
        {
            return _layers.Find(x => x.Name == layerName);
        }

        public string GetUniqueLayerName(string layerName, MapLayer owner)
        {
            string baseName = string.IsNullOrWhiteSpace(layerName) ? "Layer" : layerName;
            string result = baseName;
            int index = 1;
            while (_layers.Exists(x => x != owner && x.Name == result))
            {
                result = $"{baseName} {index}";
                index++;
            }

            return result;
        }

        public Map DetachLayer(MapLayer layer)
        {
            if (!_layers.Contains(layer))
            {
                return null;
            }

            Map map = new GameObject(layer.Name).AddComponent<Map>();
            map.transform.SetParent(transform);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            map.transform.localScale = Vector3.one;

            RemoveLayer(layer);
            map.AddLayer(layer);
            return map;
        }

        public void ClearLayer(MapLayer layer)
        {
            if (_layers.Contains(layer))
            {
                layer.ClearTiles();
            }
        }
    }
}
