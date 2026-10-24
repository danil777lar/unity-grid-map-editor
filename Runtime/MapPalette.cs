using UnityEngine;

namespace Larje.MapEditor
{
    public static class MapPalette
    {
        public static readonly Color Hover = new Color(1f, 0.92f, 0.016f, 0.5f);
        public static readonly Color Grid = new Color(1f, 1f, 1f, 0.5f);

        private static readonly Color[] LayerColors =
        {
            new Color(1f, 0.4f, 0.4f, 0.75f),
            new Color(0.76f, 0.4f, 1f, 0.75f),
            new Color(0.41f, 0.4f, 1f, 0.75f),
            new Color(0.4f, 0.85f, 1f, 0.75f),
            new Color(0.4f, 1f, 0.57f, 0.75f),
            new Color(1f, 0.99f, 0.4f, 0.75f)
        };

        public static Color GetLayerColor(int index)
        {
            return LayerColors[Mathf.Abs(index) % LayerColors.Length];
        }
    }
}
