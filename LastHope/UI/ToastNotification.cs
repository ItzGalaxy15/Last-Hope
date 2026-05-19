using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;
// https://community.monogame.net/t/sharing-an-easy-way-to-fade-in-fade-out-screen/2677
public class ToastNotification : UIElement
{
    private readonly Texture2D _pixel;
    private Texture2D _fallbackPixel;

    private string _message = "";
    private float _timer;
    private const float DisplayDuration = 2.0f;
    private const float FadeDuration    = 0.5f;
    private const float TotalDuration   = DisplayDuration + FadeDuration;

    public ToastNotification(Texture2D pixel)
    {
        _pixel = pixel;
    }
    // fallback to avoid null checks in Draw, since this is called every frame and we don't want to create a new texture every time.
    private Texture2D GetPixel(SpriteBatch spriteBatch)
    {
        if (_pixel != null) return _pixel;
        if (_fallbackPixel == null)
        {
            _fallbackPixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _fallbackPixel.SetData(new[] { Color.White });
        }
        return _fallbackPixel;
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        string pending = GameManager.GetGameManager().ConsumeToast();
        if (pending != null)
        {
            _message = pending;
            _timer   = TotalDuration;
        }

        if (_timer > 0f)
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_timer <= 0f) return;

        float alpha = _timer < FadeDuration ? _timer / FadeDuration : 1f;

        var gm       = GameManager.GetGameManager();
        var viewport = spriteBatch.GraphicsDevice.Viewport;

        const float TextScale  = 2.0f;
        const int   PadX       = 24;
        const int   PadY       = 12;

        Vector2 textSize = gm.MeasureUiString(gm._font, _message, TextScale);
        int panelW = (int)textSize.X + PadX * 2;
        int panelH = (int)textSize.Y + PadY * 2;
        int panelX = (viewport.Width - panelW) / 2;
        int panelY = viewport.Height / 4;

        Color bg   = new Color(20, 20, 20, (int)(200 * alpha));
        Color text = new Color(255, 215, 0, (int)(255 * alpha)); // gold

        spriteBatch.Draw(GetPixel(spriteBatch), new Rectangle(panelX, panelY, panelW, panelH), bg);
        gm.DrawUiString(spriteBatch, gm._font, _message,
            new Vector2(panelX + PadX, panelY + PadY), text, TextScale);
    }
}
