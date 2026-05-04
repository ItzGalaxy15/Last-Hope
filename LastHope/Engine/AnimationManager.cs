using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;

/// <summary>
/// Manages 2D sprite sheet animations by tracking frame progression and calculating source rectangles.
/// </summary>
/// <remarks>
/// Based on a standard 2D sprite sheet animation approach where frames are arranged in a grid.
/// The manager increments a counter each update tick and advances the frame when the defined interval is reached.
/// </remarks>
internal class AnimationManager
{
    private readonly int numFrames;
    private readonly int numColumns;
    private readonly Vector2 size;

    private int counter;
    private int activeFrame;
    private readonly int interval; // speed

    private int rowPos;
    private int colPos;

    private readonly bool loop;
    
    /// <summary>
    /// Indicates whether the animation has finished playing. Always false if the animation is set to loop.
    /// </summary>
    public bool isFinished;
    
    /// <summary>
    /// Gets the zero-based index of the currently active frame.
    /// </summary>
    public int ActiveFrame => activeFrame;
    
    /// <summary>
    /// Gets the progress toward the next frame as a normalized value between 0.0 and 1.0.
    /// </summary>
    public float FrameProgress => (float)counter / interval;

    private readonly int offsetX;
    private readonly int offsetY;

    /// <summary>
    /// Initializes a new instance of the AnimationManager.
    /// </summary>
    /// <param name="numFrames">The total number of frames in the animation.</param>
    /// <param name="numColumns">The number of columns in the sprite sheet grid for this animation.</param>
    /// <param name="size">The width and height of a single frame in pixels.</param>
    /// <param name="interval">The number of update ticks to wait before advancing to the next frame (controls animation speed).</param>
    /// <param name="loop">Whether the animation should automatically restart from the beginning when it finishes.</param>
    /// <param name="offsetX">The starting X coordinate offset on the sprite sheet.</param>
    /// <param name="offsetY">The starting Y coordinate offset on the sprite sheet.</param>
    public AnimationManager(int numFrames, int numColumns, Vector2 size, int interval = 30, bool loop = false, int offsetX = 0, int offsetY = 0)
    {
        this.numFrames = numFrames;
        this.numColumns = numColumns;
        this.size = size;

        this.counter = 0;
        this.activeFrame = 0;
        this.interval = interval;

        this.rowPos = 0;
        this.colPos = 0;

        this.loop = loop;
        this.isFinished = false;

        this.offsetX = offsetX;
        this.offsetY = offsetY;
    }

    /// <summary>
    /// Advances the animation progress. Should be called once per game update tick.
    /// </summary>
    public void Update()
    {
        if (isFinished) return;
        counter++;
        if (counter >= interval)
        {
            counter = 0;
            NextFrame();
        }
    }

    /// <summary>
    /// Advances to the next frame in the sequence, handling grid wrapping and looping logic.
    /// </summary>
    private void NextFrame()
    {
        activeFrame++;
        colPos++;

        if (activeFrame >= numFrames)
        {
            if (loop)
            {
                ResetAnimation();
            }
            else
            {
                activeFrame = numFrames - 1;
                colPos = activeFrame % numColumns;
                rowPos = activeFrame / numColumns;
                isFinished = true;
                return;
            }
        }

        if (colPos >= numColumns)
        {
            colPos = 0;
            rowPos++;
        }
    }

    /// <summary>
    /// Calculates the source rectangle for the current frame to be used in rendering.
    /// </summary>
    /// <returns>A rectangle representing the region of the sprite sheet to draw for the active frame.</returns>
    public Rectangle GetSourceRect()
    {
        return new Rectangle(
            (colPos * (int)size.X) + offsetX, (rowPos * (int)size.Y) + offsetY, (int)size.X, (int)size.Y);
    }

    /// <summary>
    /// Resets the animation back to the first frame.
    /// </summary>
    private void ResetAnimation()
    {
        activeFrame = 0;
        colPos = 0;
        rowPos = 0;
    }
}