using Microsoft.Xna.Framework;

namespace Last_Hope.Classes.Camera;

public class Camera
{
    public Vector2 Position { get; private set; }
    public Matrix ViewMatrix { get; private set; }

    private readonly Point _viewportSize;
    private readonly Point _worldSize;

    public Camera(Point viewportSize, Point worldSize)
    {
        _viewportSize = viewportSize;
        _worldSize = worldSize;
        Update(Vector2.Zero);
    }

    public void Update(Vector2 targetPosition)
    {
        Vector2 halfViewport = _viewportSize.ToVector2() / 2f;
        Vector2 cameraPosition = targetPosition - halfViewport;

        Position = cameraPosition;
        ViewMatrix = Matrix.CreateTranslation(-Position.X, -Position.Y, 0f);
    }
}