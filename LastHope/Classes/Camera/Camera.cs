using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Last_Hope.Classes.Camera
{
    public class Camera
    {
        public Vector2 Position { get; private set; }
        public Matrix ViewMatrix { get; private set; }
        public float Zoom { get; }
        public float MinX { get; set; } = 0f;

    private readonly Point _viewportSize;
    private readonly Point _worldSize;

        public Camera(Point viewportSize, Point worldSize, float zoom = 1f)
        {
            _viewportSize = viewportSize;
            _worldSize = worldSize;
            Zoom = MathF.Max(0.1f, zoom);
            Update(Vector2.Zero);
        }

        public void Update(Vector2 targetPosition)
        {
            Vector2 halfViewport = _viewportSize.ToVector2() / (2f * Zoom);
            Vector2 cameraPosition = targetPosition - halfViewport;

            float maxX = MathF.Max(0f, _worldSize.X - (_viewportSize.X / Zoom));
            float maxY = MathF.Max(0f, _worldSize.Y - (_viewportSize.Y / Zoom));
            // https://gamedev.net/forums/topic/631952-2d-camera-limiting-bounds-and-zoom/
            Position = Vector2.Clamp(cameraPosition, new Vector2(MinX, 0f), new Vector2(maxX, maxY));
            ViewMatrix = Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) * Matrix.CreateScale(Zoom, Zoom, 1f);
        }
    }
}
