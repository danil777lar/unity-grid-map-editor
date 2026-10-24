using System.Collections.Generic;
using UnityEngine;

namespace Larje.MapEditor.Editor
{
    public static class MapEditorIcons
    {
        private const string ICONS_PATH = "LarjeMapEditor/Icons/";

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string iconName)
        {
            if (!Cache.TryGetValue(iconName, out Texture2D icon) || icon == null)
            {
                icon = Resources.Load<Texture2D>(ICONS_PATH + iconName);
                Cache[iconName] = icon;
            }

            return icon;
        }
    }
}
