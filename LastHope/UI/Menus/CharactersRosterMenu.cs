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
        SpriteFont layoutFont = MenuUiFont ?? _font;
        if (layoutFont == null && gm.FontBitmap == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        var anchor = new Rectangle(0, 0, vp.Width, vp.Height);
        MenuHubBackChrome back = LayoutMenuHubBackChrome(anchor, ui, layoutFont);
        if (InputManager.LeftMousePress() && back.BackHitRect.Contains(InputManager.CurrentMouseState.Position))
        {
            _state = GameState.MainMenu;
            return;
        }

        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
        {
            _state = GameState.MainMenu;
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
        SpriteFont layoutFont = MenuUiFont ?? _font;
        if (layoutFont == null && gm.FontBitmap == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        var anchor = new Rectangle(0, 0, vp.Width, vp.Height);
        MenuHubBackChrome back = LayoutMenuHubBackChrome(anchor, ui, layoutFont);
        bool backHover = back.BackHitRect.Contains(InputManager.CurrentMouseState.Position);

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
            "A/D or click portrait to browse");

        DrawMenuHubBackChrome(spriteBatch, in back, backHover, ui);

        spriteBatch.End();
    }
}
