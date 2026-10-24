using System;

namespace Larje.MapEditor
{
    [Serializable]
    public sealed class BrushTool : DrawTool
    {
        public override string IconName => "ToolPen";

        protected override void OnDraw(MapLayer layer, Tile tile)
        {
            layer.AddTile(tile);
        }
    }
}
