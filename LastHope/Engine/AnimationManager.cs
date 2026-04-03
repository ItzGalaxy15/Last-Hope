using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;
internal class AnimationManager
{
    int numFrames;
    int numColumns;
    Vector2 size;

    int counter;
    int activeFrame;
    int interval; // speed

    int rowPos;
    int colPos;

    bool loop;
    public bool isFinished;
    public int ActiveFrame => activeFrame;
    public float FrameProgress => (float)counter / interval;

    int offsetX;
    int offsetY;


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

    private void NextFrame()
    {
        activeFrame++;
        colPos++;

        // Check if the animation has reached the end of the frames
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

    public Rectangle GetSourceRect()
    {
        return new Rectangle(
            (colPos * (int)size.X) + offsetX, (rowPos * (int)size.Y) + offsetY, (int)size.X, (int)size.Y);
    }

    private void ResetAnimation()
    {
        activeFrame = 0;
        colPos = 0;
        rowPos = 0;
    }
}