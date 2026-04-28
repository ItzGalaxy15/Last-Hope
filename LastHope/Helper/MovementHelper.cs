using Microsoft.Xna.Framework;
using Last_Hope.Engine;

namespace Last_Hope.Helpers;

public static class MovementHelper
{
    public static Vector2 ClampToMapBounds(Vector2 position, float bodyWidth)
    {
        var grid = GameManager.GetGameManager().NavigationGrid;

        if (grid == null)
            return position;

        float mapW = grid.WidthInTiles * grid.TileSize;
        float mapH = grid.HeightInTiles * grid.TileSize;

        return new Vector2(
            MathHelper.Clamp(position.X, 0f, mapW - bodyWidth),
            MathHelper.Clamp(position.Y, 0f, mapH - bodyWidth)
        );
    }
}