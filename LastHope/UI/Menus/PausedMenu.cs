using Gum.Forms.Controls;
using Last_Hope.Engine;
using Last_Hope.UI.GumForms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace Last_Hope.UI.Menus;

public class PausedMenu : MenuBase
{
    private Panel _rootPanel;

    public void ReleaseGumUi()
    {
        if (_rootPanel == null)
            return;

        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
    }

    public void Update(GameTime gameTime)
    {
        EnsurePauseGum();

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            ReleaseGumUi();
            _state = GameState.Running;
        }
    }

    private void EnsurePauseGum()
    {
        if (_rootPanel != null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);

        _rootPanel = new Panel();
        _rootPanel.Dock(Gum.Wireframe.Dock.Fill);
        _rootPanel.AddToRoot();

        var dim = new ColoredRectangleRuntime();
        dim.Dock(Gum.Wireframe.Dock.Fill);
        dim.Color = new Color(0, 0, 0, 140);
        _rootPanel.AddChild(dim);

        float btnW = MathHelper.Min(340f * ui, vp.Width - 80f);
        float btnH = 50f * ui;
        float gap = 14f * ui;
        float stackH = btnH * 4f + gap * 3f;
        float startY = vp.Height / 2f - stackH / 2f;
        float btnX = vp.Width / 2f - btnW / 2f;

        var continueBtn = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        continueBtn.Text = "Continue";
        continueBtn.X = btnX;
        continueBtn.Y = startY;
        continueBtn.Width = btnW;
        continueBtn.Height = btnH;
        continueBtn.Click += (_, _) =>
        {
            ReleaseGumUi();
            _state = GameState.Running;
        };
        _rootPanel.AddChild(continueBtn);

        var restart = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        restart.Text = "Restart game";
        restart.X = btnX;
        restart.Y = startY + btnH + gap;
        restart.Width = btnW;
        restart.Height = btnH;
        restart.Click += (_, _) =>
        {
            ReleaseGumUi();
            gm.ResetGame();
            _state = GameState.Running;
        };
        _rootPanel.AddChild(restart);

        var settings = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        settings.Text = "Settings";
        settings.X = btnX;
        settings.Y = startY + (btnH + gap) * 2f;
        settings.Width = btnW;
        settings.Height = btnH;
        settings.Click += (_, _) =>
        {
            ReleaseGumUi();
            gm.StateAfterClosingSettings = GameState.Paused;
            _state = GameState.SettingsMenu;
        };
        _rootPanel.AddChild(settings);

        var backToMenu = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        backToMenu.Text = "Back To Menu";
        backToMenu.X = btnX;
        backToMenu.Y = startY + (btnH + gap) * 3f;
        backToMenu.Width = btnW;
        backToMenu.Height = btnH;
        backToMenu.Click += (_, _) =>
        {
            ReleaseGumUi();
            _state = GameState.MainMenu;
        };
        _rootPanel.AddChild(backToMenu);

        continueBtn.IsFocused = true;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        float ctrlScale = 0.55f * ui;
        DrawSavedControlsText(spriteBatch, gameTime, new Vector2(48f * ui, 220f * ui), ctrlScale);
        SpriteFont mf = MenuUiFont;
        float itemScale = 0.55f * ui;
        float inner = MeasureItemsContentWidth(mf, itemScale);
        DrawItemsText(spriteBatch, gameTime, new Vector2(vp.Width - inner - 56f * ui, 220f * ui), inner, itemScale);
        spriteBatch.End();
    }
}
