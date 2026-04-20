using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;
using Last_Hope.UI.GumForms;
using MonoGameGum;
using MonoGameGum.GueDeriving;

namespace Last_Hope.UI.Menus;

/// <summary>Title hub: stylized full-screen backdrop + Gum Forms entry list (MonoGame Gum tutorial pattern).</summary>
public sealed class MainMenuScreen : MenuBase
{
    private static readonly string[] Entries =
    {
        "Start Game",
        "Characters",
        "Items Index",
        "Settings",
        "Quit Game",
    };

    private static readonly Color AccentLine = new(120, 175, 255, 90);

    private Panel _rootPanel;
    private bool _hintAdded;

    public void ReleaseGumUi()
    {
        if (_rootPanel == null && !_hintAdded)
            return;

        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _hintAdded = false;
    }

    public void Update(GameTime gameTime)
    {
        EnsureGumMainMenu();
    }

    private void EnsureGumMainMenu()
    {
        if (_rootPanel != null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);

        _rootPanel = new Panel();
        _rootPanel.Dock(Gum.Wireframe.Dock.Fill);
        _rootPanel.AddToRoot();

        // Full-screen wash behind everything (Gum draws after SpriteBatch backdrop in Game.Draw).
        var fill = new ColoredRectangleRuntime();
        fill.Dock(Gum.Wireframe.Dock.Fill);
        fill.Color = new Color(8, 10, 20, 220);
        _rootPanel.AddChild(fill);

        float railW = 52f * ui;
        var leftRail = new ColoredRectangleRuntime();
        leftRail.Dock(Gum.Wireframe.Dock.Left);
        leftRail.Width = railW;
        leftRail.Color = new Color(0, 0, 0, 115);
        _rootPanel.AddChild(leftRail);

        float marginX = 40f * ui;
        float startY = 72f * ui;
        float rowGap = 12f * ui;
        float buttonWidth = 300f * ui;
        float buttonHeight = 48f * ui;

        MenuAnimatedButton first = null;
        for (int i = 0; i < Entries.Length; i++)
        {
            var btn = new MenuAnimatedButton(Game.GraphicsDevice, buttonWidth, buttonHeight);
            btn.Text = Entries[i];
            btn.X = marginX;
            btn.Y = startY + i * (buttonHeight + rowGap);
            btn.Width = buttonWidth;
            btn.Height = buttonHeight;
            int index = i;
            btn.Click += (_, _) => ApplySelection(index);
            _rootPanel.AddChild(btn);
            first ??= btn;
        }

        if (first != null)
            first.IsFocused = true;

        var accent = new ColoredRectangleRuntime();
        accent.X = marginX - 4f;
        accent.Y = startY - 8f * ui;
        accent.Width = 4f;
        accent.Height = Entries.Length * (buttonHeight + rowGap) - rowGap + 16f * ui;
        accent.Color = AccentLine;
        _rootPanel.AddChild(accent);

        var hint = new TextRuntime();
        hint.Text = "Up/Down or W/S  |  Tab  |  Enter / Click";
        hint.X = marginX;
        hint.Y = startY + Entries.Length * (buttonHeight + rowGap) + 22f * ui;
        hint.Color = new Color(200, 210, 230, 200);
        _rootPanel.AddChild(hint);
        _hintAdded = true;
    }

    private void ApplySelection(int index)
    {
        // Gum processes clicks after GameManager.Update; clear UI here so the hub
        // disappears immediately when leaving the main menu.
        ReleaseGumUi();

        switch (index)
        {
            case 0:
                _state = GameState.CharacterSelect;
                break;
            case 1:
                _state = GameState.Characters;
                break;
            case 2:
                _state = GameState.ItemsIndex;
                break;
            case 3:
                _state = GameState.SettingsMenu;
                break;
            case 4:
                Game.Exit();
                break;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        Viewport vp = Game.GraphicsDevice.Viewport;
        Texture2D px = Pixel;

        // Screen-space menu backdrop (do not use gameplay camera transform or the level map).
        float ui = MenuUiScale(vp);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHubMenuBackdrop(spriteBatch, px, vp);
        DrawHubMenuLeftRail(spriteBatch, px, vp, ui);
        spriteBatch.End();
    }
}
