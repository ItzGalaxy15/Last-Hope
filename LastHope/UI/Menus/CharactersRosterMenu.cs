using System.Collections.Generic;
using Last_Hope;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Browse playable characters with the same layout as character select, without starting a run.
/// </summary>
public class CharactersRosterMenu : MenuBase
{
    private int _selectedIndex;
    private readonly Dictionary<string, Texture2D> _portraitTextures = new();

    public void Update(GameTime gameTime)
    {
        if (_font == null)
            return;

        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
        {
            _state = GameState.StartMenu;
            return;
        }

        int n = PlayableCharacterRegistry.Count;
        if (n == 0)
            return;

        _selectedIndex = System.Math.Clamp(_selectedIndex, 0, n - 1);

        if (InputManager.IsKeyPress(Keys.A) || InputManager.IsKeyPress(Keys.Left))
            _selectedIndex = (_selectedIndex - 1 + n) % n;
        if (InputManager.IsKeyPress(Keys.D) || InputManager.IsKeyPress(Keys.Right))
            _selectedIndex = (_selectedIndex + 1) % n;

        Viewport vp = Game.GraphicsDevice.Viewport;
        for (int i = 0; i < n; i++)
        {
            if (!PlayableCharacterOverviewDraw.GetPortraitRect(vp, i).Contains(InputManager.CurrentMouseState.Position))
                continue;
            if (InputManager.LeftMousePress())
                _selectedIndex = i;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_font == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        PlayableCharacterOverviewDraw.Draw(
            spriteBatch,
            _font,
            Pixel,
            _content,
            _portraitTextures,
            vp,
            _selectedIndex,
            "CHARACTERS",
            0.85f,
            showStartButton: false,
            "Back: Esc / Q  |  A/D or click portrait to browse");

        spriteBatch.End();
    }
}
