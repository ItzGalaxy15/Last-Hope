using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.Engine;
    
    internal class AnimationManager
    {
        int numFrames;
        int numColumns;
        Vector2 size;

        float counter;
        int activeFrame;
        int interval;

        int rowPos;
        int colPos;

        public float speed {get; set; } = 5f;

        public bool IsDone { get; private set; } = false;

        public AnimationManager(int numFrames, int numColumns, Vector2 size)
        {
            this.numFrames = numFrames;
            this.numColumns = numColumns;
            this.size = size;

            counter = 0;
            activeFrame = 0;
            interval = 30;

            rowPos = 0;
            colPos = 0;

        }

        public void Update()
        {
            counter += speed;
            if (counter > interval) {counter = 0; NextFrame();}

        }

        public void NextFrame()
        {
            activeFrame++;
            colPos++;

            if (activeFrame >= numFrames) { IsDone = true; activeFrame = 0; colPos = 0; rowPos = 0; }

            if (colPos >= numColumns)
            {
                colPos = 0;
                rowPos++;
            }
        }

        public Rectangle GetFrame()
        {
            return new Rectangle(
                colPos * (int)size.X, 
                rowPos * (int)size.Y, 
                (int)size.X, (int)size.Y);
        }
    }