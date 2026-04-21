using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Graphics.Animation;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;

namespace Last_Hope.UI.GumForms;

/// <summary>
/// Gum Forms <see cref="Button"/> with custom <see cref="ButtonVisual"/> styling and
/// nine-slice background animation chains for default vs focused states (MonoGame Gum tutorial ch.21).
/// </summary>
public sealed class MenuAnimatedButton : Button
{
    private const string UnfocusedChainName = "MenuBtnUnfocused";
    private const string FocusedChainName = "MenuBtnFocused";

    private static Texture2D _stripTexture;

    /// <param name="graphics">Used once to build a small shared strip atlas (unfocused + focused frames).</param>
    /// <param name="width">Same as <see cref="FrameworkElement.Width"/> for this menu row (absolute Gum units).</param>
    /// <param name="height">Same as <see cref="FrameworkElement.Height"/>.</param>
    /// <remarks>
    /// Do not use <see cref="DimensionUnitType.PercentageOfParent"/> on the visual: the visual's parent can be the
    /// full-screen panel, which stretches chrome across the window. Absolute size keeps rows aligned and sized.
    /// </remarks>
    public MenuAnimatedButton(GraphicsDevice graphics, float width, float height)
    {
        Texture2D atlas = _stripTexture ??= BuildButtonStripTexture(graphics);

        var buttonVisual = (ButtonVisual)Visual;
        buttonVisual.WidthUnits = DimensionUnitType.Absolute;
        buttonVisual.Width = width;
        buttonVisual.HeightUnits = DimensionUnitType.Absolute;
        buttonVisual.Height = height;

        NineSliceRuntime background = buttonVisual.Background;
        background.Texture = atlas;
        background.TextureAddress = TextureAddress.Custom;
        background.Color = Color.White;

        // Leave TextInstance at Gum V3 defaults (SpriteFont). Scaling/custom width caused blurry, uneven glyphs;
        // Font.fnt is used via BmFont on SpriteBatch menus elsewhere, not through Gum's ContentManager path.

        int tw = atlas.Width;
        int th = atlas.Height;
        int frameW = tw / 3;

        AnimationChain unfocused = new AnimationChain { Name = UnfocusedChainName };
        unfocused.Add(MakeFrame(atlas, 0, 0, frameW, th, 0.3f, tw, th));

        AnimationChain focused = new AnimationChain { Name = FocusedChainName };
        focused.Add(MakeFrame(atlas, frameW, 0, frameW, th, 0.18f, tw, th));
        focused.Add(MakeFrame(atlas, frameW * 2, 0, frameW, th, 0.18f, tw, th));

        background.AnimationChains = new AnimationChainList { unfocused, focused };

        buttonVisual.ButtonCategory.ResetAllStates();

        StateSave enabledState = buttonVisual.States.Enabled;
        enabledState.Apply = () =>
        {
            background.CurrentChainName = UnfocusedChainName;
            background.Animate = false;
        };

        StateSave focusedState = buttonVisual.States.Focused;
        focusedState.Apply = () =>
        {
            background.CurrentChainName = FocusedChainName;
            background.Animate = true;
        };

        StateSave highlightedFocused = buttonVisual.States.HighlightedFocused;
        highlightedFocused.Apply = focusedState.Apply;

        StateSave highlighted = buttonVisual.States.Highlighted;
        highlighted.Apply = enabledState.Apply;

        buttonVisual.RollOn += (_, _) => IsFocused = true;
    }

    private static AnimationFrame MakeFrame(
        Texture2D texture,
        int srcX,
        int srcY,
        int srcW,
        int srcH,
        float frameLength,
        int textureWidth,
        int textureHeight)
    {
        float l = (float)srcX / textureWidth;
        float r = (float)(srcX + srcW) / textureWidth;
        float t = (float)srcY / textureHeight;
        float b = (float)(srcY + srcH) / textureHeight;

        return new AnimationFrame
        {
            LeftCoordinate = l,
            RightCoordinate = r,
            TopCoordinate = t,
            BottomCoordinate = b,
            FrameLength = frameLength,
            Texture = texture
        };
    }

    private static Texture2D BuildButtonStripTexture(GraphicsDevice graphics)
    {
        const int frameW = 64;
        const int frameH = 28;
        int w = frameW * 3;
        int h = frameH;
        var tex = new Texture2D(graphics, w, h, false, SurfaceFormat.Color);
        var data = new Color[w * h];

        void FillFrame(int frameIndex, Color fill, Color edge, Color glow)
        {
            int ox = frameIndex * frameW;
            for (int y = 0; y < frameH; y++)
            {
                for (int x = 0; x < frameW; x++)
                {
                    bool edgePx = x == 0 || y == 0 || x == frameW - 1 || y == frameH - 1;
                    bool innerEdge = x == 1 || y == 1 || x == frameW - 2 || y == frameH - 2;
                    Color c = edgePx ? edge : innerEdge ? glow : fill;
                    data[(y * w) + ox + x] = c;
                }
            }
        }

        var baseFill = new Color(18, 22, 38, 255);
        var baseEdge = new Color(55, 70, 110, 255);
        FillFrame(0, baseFill, baseEdge, new Color(40, 52, 88, 255));

        var hiFill = new Color(22, 30, 52, 255);
        var hiEdge = new Color(100, 150, 220, 255);
        FillFrame(1, hiFill, hiEdge, new Color(70, 110, 180, 255));
        FillFrame(2, hiFill, new Color(120, 175, 255, 255), new Color(90, 130, 210, 255));

        tex.SetData(data);
        return tex;
    }
}
