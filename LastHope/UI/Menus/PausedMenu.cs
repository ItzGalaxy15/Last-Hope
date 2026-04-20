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
        float stackH = btnH * 3f + gap * 2f;
        float startY = vp.Height / 2f - stackH / 2f;
        float btnX = vp.Width / 2f - btnW / 2f;

        var back = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        back.Text = "Back to Menu";
        back.X = btnX;
        back.Y = startY;
        back.Width = btnW;
        back.Height = btnH;
        back.Click += (_, _) =>
        {
            ReleaseGumUi();
            _state = GameState.MainMenu;
        };
        _rootPanel.AddChild(back);

        var restart = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        restart.Text = "Restart Game";
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

        var quit = new MenuAnimatedButton(Game.GraphicsDevice, btnW, btnH);
        quit.Text = "Quit Game";
        quit.X = btnX;
        quit.Y = startY + (btnH + gap) * 2f;
        quit.Width = btnW;
        quit.Height = btnH;
        quit.Click += (_, _) =>
        {
            ReleaseGumUi();
            Game.Exit();
        };
        _rootPanel.AddChild(quit);

        back.IsFocused = true;
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
