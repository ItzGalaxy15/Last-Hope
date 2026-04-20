using Microsoft.Xna.Framework.Input;

namespace Last_Hope.Engine;

    public class InputManager
    {
        public KeyboardState LastKeyboardState { get; private set; }
        public KeyboardState CurrentKeyboardState { get; private set; }
        public MouseState LastMouseState { get; private set; }
        public MouseState CurrentMouseState { get; private set; }



        /// <summary>
        /// Keeps track of input states and contains methods to work with them.
        /// </summary>
        public InputManager()
        {
            LastKeyboardState = Keyboard.GetState();
            CurrentKeyboardState = Keyboard.GetState();
            CurrentMouseState = Mouse.GetState();
            LastMouseState = Mouse.GetState();

        }
        
        /// <summary>
        /// Updates the current and previous keyboard and mouse states
        /// </summary>
        public void Update()
        {
            LastKeyboardState = CurrentKeyboardState;
            CurrentKeyboardState = Keyboard.GetState();
            LastMouseState = CurrentMouseState;
            CurrentMouseState = Mouse.GetState();
        }

        /// <summary>
        /// Gets whether the <paramref name="key"/> is currently down.
        /// </summary>
        /// <param name="key">The key for which you wish to know the state</param>
        /// <returns>true if the key is currently down, otherwise false</returns>
        public bool IsKeyDown(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key);
        }


        /// <summary>
        /// Gets whether the <paramref name="key"/> is currently up.
        /// </summary>
        /// <param name="key">The key for which you wish to know the state</param>
        /// <returns>true if the key is currently up, otherwise false</returns>
        public bool IsKeyUp(Keys key)
        {
            return CurrentKeyboardState.IsKeyUp(key);
        }



        /// <summary>
        /// Gets whether the <paramref name="key"/> was pressed in this frame.
        /// </summary>
        /// <param name="key">The key for which you wish to know the state</param>
        /// <returns>true if the key is currently down and was up in the previous step, otherwise false</returns>
        public bool IsKeyPress(Keys key) 
        {
            return CurrentKeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key);
        }

        public bool IsGameplayKeyDown(KeybindId id)
        {
            GameInputBinding b = KeybindStore.GetBinding(id);
            if (b.IsUnbound)
                return false;
            return b.Kind switch
            {
                BindingKind.Keyboard => CurrentKeyboardState.IsKeyDown(b.Key),
                BindingKind.Mouse => IsMouseButtonDown(b.Mouse),
                _ => false,
            };
        }

        public bool IsGameplayKeyPress(KeybindId id)
        {
            GameInputBinding b = KeybindStore.GetBinding(id);
            if (b.IsUnbound)
                return false;
            return b.Kind switch
            {
                BindingKind.Keyboard =>
                    CurrentKeyboardState.IsKeyDown(b.Key) && LastKeyboardState.IsKeyUp(b.Key),
                BindingKind.Mouse =>
                    IsMouseButtonDown(b.Mouse) && WasMouseButtonUp(b.Mouse),
                _ => false,
            };
        }

        private bool IsMouseButtonDown(MouseBindButton mb) => mb switch
        {
            MouseBindButton.Left => CurrentMouseState.LeftButton == ButtonState.Pressed,
            MouseBindButton.Right => CurrentMouseState.RightButton == ButtonState.Pressed,
            MouseBindButton.Middle => CurrentMouseState.MiddleButton == ButtonState.Pressed,
            _ => false,
        };

        private bool WasMouseButtonUp(MouseBindButton mb) => mb switch
        {
            MouseBindButton.Left => LastMouseState.LeftButton == ButtonState.Released,
            MouseBindButton.Right => LastMouseState.RightButton == ButtonState.Released,
            MouseBindButton.Middle => LastMouseState.MiddleButton == ButtonState.Released,
            _ => false,
        };

        /// <summary>First keyboard key or mouse button that transitioned to down this frame (for rebinding).</summary>
        public GameInputBinding? ConsumeFirstNewBindingPress()
        {
            foreach (Keys k in CurrentKeyboardState.GetPressedKeys())
            {
                if (k == Keys.None || k == Keys.Escape)
                    continue;
                if (LastKeyboardState.IsKeyUp(k) && CurrentKeyboardState.IsKeyDown(k))
                    return GameInputBinding.Keyboard(k);
            }

            if (LeftMousePress())
                return GameInputBinding.FromMouse(MouseBindButton.Left);
            if (RightMousePress())
                return GameInputBinding.FromMouse(MouseBindButton.Right);
            if (MiddleMousePress())
                return GameInputBinding.FromMouse(MouseBindButton.Middle);
            return null;
        }


        /// <summary>
        /// Gets whether the left mouse button was pressed in this frame.
        /// </summary>
        /// <returns>true if the button is currently down and was up in the previous step, otherwise false</returns>
        public bool LeftMousePress()
        {
            return CurrentMouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released;
        }


        /// <summary>
        /// Gets whether the right mouse button was pressed in this frame.
        /// </summary>
        /// <returns>true if the button is currently down and was up in the previous step, otherwise false</returns>
        public bool RightMousePress()
        {
            return CurrentMouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released;
        }

        public bool MiddleMousePress()
        {
            return CurrentMouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released;
        }
    }