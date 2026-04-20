using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace Last_Hope.UI;

/// <summary>
/// One-time Gum setup aligned with the MonoGame + Gum tutorial:
/// https://docs.monogame.net/articles/tutorials/building_2d_games/20_implementing_ui_with_gum/index.html
/// </summary>
public static class GumBootstrap
{
    public static bool IsInitialized { get; private set; }

    public static void Initialize(Game game, Microsoft.Xna.Framework.Content.ContentManager content)
    {
        if (IsInitialized)
            return;

        GumService.Default.Initialize(game, DefaultVisualsVersion.V3);
        GumService.Default.ContentLoader.XnaContentManager = content;

        FrameworkElement.KeyboardsForUiControl.Add(GumService.Default.Keyboard);
        FrameworkElement.GamePadsForUiControl.AddRange(GumService.Default.Gamepads);

        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.Up });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo { PushedKey = Keys.W });
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.Down });
        FrameworkElement.TabKeyCombos.Add(new KeyCombo { PushedKey = Keys.S });

        int w = game.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int h = game.GraphicsDevice.PresentationParameters.BackBufferHeight;
        GumService.Default.CanvasWidth = w;
        GumService.Default.CanvasHeight = h;
        GumService.Default.Renderer.Camera.Zoom = 1f;

        IsInitialized = true;
    }
}
