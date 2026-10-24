using System;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class EraserTool : DrawTool
    {
        public override string IconName => "ToolEraser";

        protected override void OnDraw(MapLayer layer, Tile tile)
        {
            layer.RemoveTile(tile);
        }
    }
}
