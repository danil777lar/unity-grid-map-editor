using System;

namespace Larje.MapEditor
{
    [Serializable]
    public abstract class MapTool
    {
        public abstract string IconName { get; }

        public abstract void Process(Map map);

        public abstract void DrawEditorGUI();
    }
}
