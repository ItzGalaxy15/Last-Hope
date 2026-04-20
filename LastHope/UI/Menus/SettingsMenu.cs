using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

public sealed class SettingsMenu : MenuBase
{
    private enum SettingsTab
    {
        Controls,
        Sound,
        Display,
    }

    private const int KeyFrameSize = 32;
    private const int KeyFrameCount = 3;
    private const double KeyFrameDuration = 0.15;

    private SettingsTab _tab = SettingsTab.Controls;
    private KeybindId? _awaitingRebind;
    private KeybindId? _pendingConflict;
    private GameInputBinding? _pendingNewBind;
    private OverrideConfirmLayout _overrideLayout;

    private static Texture2D _keysTexture;
    private static Texture2D _lmbTexture;

    private SettingsChromeLayout _chrome;
    private List<(Rectangle rect, KeybindId id)> _bindTargets = new();

    public void Update(GameTime gameTime)
    {
        SpriteFont font = MenuUiFont;
        if (font == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        _chrome = BuildChromeLayout(font, vp, ui);
        _bindTargets.Clear();
        if (_tab == SettingsTab.Controls)
            RegisterBindTargets(_chrome.ContentRect, font, 0.56f * ui, ui);

        if (_awaitingRebind.HasValue && _pendingConflict.HasValue && _pendingNewBind.HasValue)
        {
            _overrideLayout = BuildOverrideConfirmLayout(vp, ui);

            if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.N))
            {
                _pendingConflict = null;
                _pendingNewBind = null;
                return;
            }

            if (InputManager.IsKeyPress(Keys.Enter) || InputManager.IsKeyPress(Keys.Y))
            {
                KeybindStore.ApplyRebind(_awaitingRebind.Value, _pendingNewBind.Value);
                _awaitingRebind = null;
                _pendingConflict = null;
                _pendingNewBind = null;
                return;
            }

            if (InputManager.LeftMousePress())
            {
                Point m = InputManager.CurrentMouseState.Position;
                if (_overrideLayout.YesRect.Contains(m))
                {
                    KeybindStore.ApplyRebind(_awaitingRebind.Value, _pendingNewBind.Value);
                    _awaitingRebind = null;
                    _pendingConflict = null;
                    _pendingNewBind = null;
                }
                else if (_overrideLayout.NoRect.Contains(m))
                {
                    _pendingConflict = null;
                    _pendingNewBind = null;
                }
            }

            return;
        }

        if (_awaitingRebind.HasValue)
        {
            if (InputManager.IsKeyPress(Keys.Escape))
            {
                _awaitingRebind = null;
                _pendingConflict = null;
                _pendingNewBind = null;
                return;
            }

            GameInputBinding? nb = InputManager.ConsumeFirstNewBindingPress();
            if (nb.HasValue)
            {
                GameInputBinding bind = nb.Value;
                KeybindId? owner = KeybindStore.FindBindingOwner(bind, _awaitingRebind.Value);
                if (!owner.HasValue)
                    KeybindStore.ApplyRebind(_awaitingRebind.Value, bind);
                else
                {
                    _pendingConflict = owner;
                    _pendingNewBind = bind;
                }

                if (!owner.HasValue)
                {
                    _awaitingRebind = null;
                    _pendingConflict = null;
                    _pendingNewBind = null;
                }
            }

            return;
        }

        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
        {
            _state = GameState.MainMenu;
            return;
        }

        Point mouse = InputManager.CurrentMouseState.Position;

        if (InputManager.LeftMousePress())
        {
            for (int i = 0; i < _chrome.TabRects.Length; i++)
            {
                if (_chrome.TabRects[i].Contains(mouse))
                {
                    _tab = (SettingsTab)i;
                    return;
                }
            }

            if (_tab == SettingsTab.Controls)
            {
                foreach (var (rect, id) in _bindTargets)
                {
                    if (rect.Contains(mouse))
                    {
                        _awaitingRebind = id;
                        return;
                    }
                }
            }
        }

        if (InputManager.IsKeyPress(Keys.D1) || InputManager.IsKeyPress(Keys.NumPad1))
            _tab = SettingsTab.Controls;
        else if (InputManager.IsKeyPress(Keys.D2) || InputManager.IsKeyPress(Keys.NumPad2))
            _tab = SettingsTab.Sound;
        else if (InputManager.IsKeyPress(Keys.D3) || InputManager.IsKeyPress(Keys.NumPad3))
            _tab = SettingsTab.Display;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        SpriteFont font = MenuUiFont;
        if (font == null)
            return;

        if (_keysTexture == null) _keysTexture = _content.Load<Texture2D>("menu/keys");
        if (_lmbTexture == null) _lmbTexture = _content.Load<Texture2D>("menu/LeftMouseClick");

        if (transformMatrix != null)
            DrawWorld(gameTime, spriteBatch, transformMatrix);

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        var chrome = _chrome.PanelRect.Width > 0 ? _chrome : BuildChromeLayout(font, vp, ui);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 100));

        spriteBatch.Draw(Pixel, chrome.PanelRect, new Color(14, 16, 22, 105));
        DrawPanelOutline(spriteBatch, chrome.PanelRect, new Color(220, 225, 235, 70));

        float titleS = 0.68f * ui;
        Vector2 titleSize = font.MeasureString("SETTINGS") * titleS;
        spriteBatch.DrawString(font, "SETTINGS",
            new Vector2(chrome.PanelRect.Center.X - titleSize.X / 2f, chrome.PanelRect.Y + 14f * ui),
            Color.White, 0f, Vector2.Zero, titleS, SpriteEffects.None, 0f);

        string[] tabLabels = { "CONTROLS", "SOUND", "DISPLAY" };
        for (int i = 0; i < chrome.TabRects.Length; i++)
        {
            bool sel = (int)_tab == i;
            Color fg = sel ? Color.Black : Color.White;
            spriteBatch.Draw(Pixel, chrome.TabRects[i], sel ? new Color(255, 255, 255, 240) : new Color(32, 36, 44, 200));
            DrawPanelOutline(spriteBatch, chrome.TabRects[i], new Color(255, 255, 255, 100));
            Vector2 sz = font.MeasureString(tabLabels[i]) * chrome.TabTextScale;
            spriteBatch.DrawString(font, tabLabels[i],
                new Vector2(chrome.TabRects[i].Center.X - sz.X / 2f, chrome.TabRects[i].Center.Y - sz.Y / 2f),
                fg, 0f, Vector2.Zero, chrome.TabTextScale, SpriteEffects.None, 0f);
        }

        float textScale = 0.56f * ui;
        switch (_tab)
        {
            case SettingsTab.Controls:
                DrawControlsRebindable(spriteBatch, gameTime, font, chrome.ContentRect, textScale, ui);
                break;
            case SettingsTab.Sound:
            case SettingsTab.Display:
                DrawPlaceholderInRect(spriteBatch, font, chrome.ContentRect, "Coming soon.", 0.62f * ui);
                break;
        }

        var (footFont, footMul) = BodyFontForMenuText(font);
        float footS = 0.48f * ui * footMul;
        string foot = "Esc / Q - Back  |  1-3 tabs";
        Vector2 fs = footFont.MeasureString(foot) * footS;
        spriteBatch.DrawString(footFont, foot, new Vector2(chrome.PanelRect.Center.X - fs.X / 2f, chrome.PanelRect.Bottom - 36f * ui), Color.Gray, 0f, Vector2.Zero, footS, SpriteEffects.None, 0f);

        if (_awaitingRebind.HasValue && _pendingConflict.HasValue && _pendingNewBind.HasValue)
            DrawOverrideConfirmModal(spriteBatch, font, vp, ui, _overrideLayout, _awaitingRebind.Value, _pendingConflict.Value, _pendingNewBind.Value);
        else if (_awaitingRebind.HasValue)
            DrawRebindModal(spriteBatch, font, vp, ui, _awaitingRebind.Value);

        spriteBatch.End();
    }

    private void DrawRebindModal(SpriteBatch spriteBatch, SpriteFont menuFont, Viewport vp, float ui, KeybindId id)
    {
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 140));

        int mw = (int)MathHelper.Min(520f * ui, vp.Width - 48);
        int mh = (int)(132f * ui);
        var box = new Rectangle(vp.Width / 2 - mw / 2, vp.Height / 2 - mh / 2, mw, mh);
        spriteBatch.Draw(Pixel, box, new Color(18, 20, 28, 220));
        DrawPanelOutline(spriteBatch, box, Color.White * 0.85f);

        var (bf, sm) = BodyFontForMenuText(menuFont);
        float ts = 0.58f * ui * sm;
        GameInputBinding cur = KeybindStore.GetBinding(id);
        string headline = $"Set new key [{KeybindStore.Label(id)}]";
        Vector2 s1 = bf.MeasureString(headline) * ts;
        float y = box.Y + 26f * ui;
        spriteBatch.DrawString(bf, headline, new Vector2(box.Center.X - s1.X / 2f, y), Color.White, 0f, Vector2.Zero, ts, SpriteEffects.None, 0f);
        string curLine = $"Current: {GameInputBinding.Format(cur)}";
        float cs = 0.42f * ui * sm;
        Vector2 sCur = bf.MeasureString(curLine) * cs;
        spriteBatch.DrawString(bf, curLine, new Vector2(box.Center.X - sCur.X / 2f, y + s1.Y + 6f * ui), new Color(200, 205, 215), 0f, Vector2.Zero, cs, SpriteEffects.None, 0f);
        string hint = "Press key / LMB / RMB / MMB  |  Esc cancel";
        float hs = 0.45f * ui * sm;
        Vector2 hsZ = bf.MeasureString(hint) * hs;
        spriteBatch.DrawString(bf, hint, new Vector2(box.Center.X - hsZ.X / 2f, box.Bottom - 36f * ui), Color.Gray, 0f, Vector2.Zero, hs, SpriteEffects.None, 0f);
    }

    private void DrawOverrideConfirmModal(SpriteBatch spriteBatch, SpriteFont menuFont, Viewport vp, float ui, in OverrideConfirmLayout lay,
        KeybindId targetId, KeybindId conflictId, GameInputBinding newBind)
    {
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 150));
        spriteBatch.Draw(Pixel, lay.Box, new Color(28, 22, 18, 235));
        DrawPanelOutline(spriteBatch, lay.Box, new Color(255, 180, 120, 180));

        var (bf, sm) = BodyFontForMenuText(menuFont);
        float ts = 0.5f * ui * sm;
        float bs = 0.46f * ui * sm;
        string line1 = $"{GameInputBinding.Format(newBind)} is already bound to [{KeybindStore.Label(conflictId)}].";
        string line2 = $"Assign it to [{KeybindStore.Label(targetId)}] and unbind the other action?";
        float y = lay.Box.Y + 22f * ui;
        Vector2 s1 = bf.MeasureString(line1) * ts;
        Vector2 s2 = bf.MeasureString(line2) * bs;
        float x1 = lay.Box.Center.X - s1.X / 2f;
        float x2 = lay.Box.Center.X - s2.X / 2f;
        spriteBatch.DrawString(bf, line1, new Vector2(x1, y), new Color(255, 200, 160), 0f, Vector2.Zero, ts, SpriteEffects.None, 0f);
        spriteBatch.DrawString(bf, line2, new Vector2(x2, y + s1.Y + 10f * ui), Color.White, 0f, Vector2.Zero, bs, SpriteEffects.None, 0f);

        spriteBatch.Draw(Pixel, lay.YesRect, new Color(60, 120, 70, 230));
        DrawPanelOutline(spriteBatch, lay.YesRect, Color.LightGreen * 0.7f);
        spriteBatch.Draw(Pixel, lay.NoRect, new Color(70, 70, 80, 230));
        DrawPanelOutline(spriteBatch, lay.NoRect, Color.Gray * 0.8f);

        float btnText = 0.44f * ui * sm;
        Vector2 ys = bf.MeasureString("Yes") * btnText;
        Vector2 ns = bf.MeasureString("No") * btnText;
        spriteBatch.DrawString(bf, "Yes", new Vector2(lay.YesRect.Center.X - ys.X / 2f, lay.YesRect.Center.Y - ys.Y / 2f), Color.White, 0f, Vector2.Zero, btnText, SpriteEffects.None, 0f);
        spriteBatch.DrawString(bf, "No", new Vector2(lay.NoRect.Center.X - ns.X / 2f, lay.NoRect.Center.Y - ns.Y / 2f), Color.White, 0f, Vector2.Zero, btnText, SpriteEffects.None, 0f);

        string hint = "Y / Enter = Yes   |   N / Esc = No";
        float hs = 0.4f * ui * sm;
        Vector2 hz = bf.MeasureString(hint) * hs;
        spriteBatch.DrawString(bf, hint, new Vector2(lay.Box.Center.X - hz.X / 2f, lay.Box.Bottom - 32f * ui), Color.Gray, 0f, Vector2.Zero, hs, SpriteEffects.None, 0f);
    }

    private static OverrideConfirmLayout BuildOverrideConfirmLayout(Viewport vp, float ui)
    {
        int mw = (int)MathHelper.Min(560f * ui, vp.Width - 40);
        int btnW = (int)(108f * ui);
        int btnH = (int)(42f * ui);
        int mh = (int)(210f * ui);
        var box = new Rectangle(vp.Width / 2 - mw / 2, vp.Height / 2 - mh / 2, mw, mh);
        int gap = (int)(18f * ui);
        int totalBtnW = btnW * 2 + gap;
        int bx = box.Center.X - totalBtnW / 2;
        int by = box.Bottom - (int)(58f * ui);
        var yes = new Rectangle(bx, by, btnW, btnH);
        var no = new Rectangle(bx + btnW + gap, by, btnW, btnH);
        return new OverrideConfirmLayout(box, yes, no);
    }

    private readonly struct OverrideConfirmLayout
    {
        public readonly Rectangle Box;
        public readonly Rectangle YesRect;
        public readonly Rectangle NoRect;

        public OverrideConfirmLayout(Rectangle box, Rectangle yes, Rectangle no)
        {
            Box = box;
            YesRect = yes;
            NoRect = no;
        }
    }

    private void DrawControlsRebindable(SpriteBatch spriteBatch, GameTime gameTime, SpriteFont font, Rectangle content, float textScale, float ui)
    {
        var (labelFont, labelMul) = BodyFontForMenuText(font);
        int frame = (int)(gameTime.TotalGameTime.TotalSeconds / KeyFrameDuration) % KeyFrameCount;
        float lineH = font.LineSpacing * textScale;
        float chip = MathHelper.Max(lineH * 1.35f, 28f * ui);
        float x0 = content.X + 12f * ui;
        float y = content.Y + 8f * ui;
        float gap = 8f * ui;

        void Label(string t)
        {
            float ls = textScale * 1.05f * labelMul;
            spriteBatch.DrawString(labelFont, t, new Vector2(x0, y), new Color(200, 210, 230), 0f, Vector2.Zero, ls, SpriteEffects.None, 0f);
            y += lineH * 1.35f;
        }

        void RowChips(params KeybindId[] ids)
        {
            float x = x0;
            foreach (var id in ids)
            {
                GameInputBinding bind = KeybindStore.GetBinding(id);
                var rect = new Rectangle((int)x, (int)(y - 2), (int)chip + 4, (int)chip + 8);
                spriteBatch.Draw(Pixel, rect, new Color(40, 48, 62, 200));
                DrawPanelOutline(spriteBatch, rect, new Color(120, 140, 170, 120));
                DrawBoundKey(spriteBatch, font, frame, rect, bind, chip);
                x += rect.Width + gap;
            }
            y += chip + 14f * ui;
        }

        Label("Movement");
        RowChips(KeybindId.MoveUp, KeybindId.MoveDown, KeybindId.MoveLeft, KeybindId.MoveRight);
        Label("Dash");
        RowChips(KeybindId.Dash);
        Label("Combat");
        {
            float x = x0;
            var lmbRect = new Rectangle((int)x, (int)(y - 2), (int)chip + 4, (int)chip + 8);
            spriteBatch.Draw(Pixel, lmbRect, new Color(40, 48, 62, 180));
            Rectangle src = new Rectangle(frame * KeyFrameSize, 0, KeyFrameSize, KeyFrameSize);
            float sc = chip / KeyFrameSize;
            float yo = -(chip - lineH) / 2f;
            spriteBatch.Draw(_lmbTexture, new Vector2(lmbRect.X + 4f, y + yo), src, Color.White, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
            x += lmbRect.Width + gap;
            spriteBatch.DrawString(labelFont, "-> Attack", new Vector2(x, y), Color.White, 0f, Vector2.Zero, textScale * labelMul, SpriteEffects.None, 0f);
            y += chip + 14f * ui;
        }

        Label("Hotbar");
        RowChips(KeybindId.ItemSlot1, KeybindId.ItemSlot2);
        Label("Items");
        RowChips(KeybindId.PlaceItem, KeybindId.ThrowItem);

        float ty = y;
        spriteBatch.DrawString(labelFont, "Arrows still move if you keep them on a gamepad keyboard layout.", new Vector2(x0, ty), Color.Gray * 0.85f, 0f, Vector2.Zero, textScale * 0.72f * labelMul, SpriteEffects.None, 0f);
    }

    private void DrawBoundKey(SpriteBatch spriteBatch, SpriteFont font, int frame, Rectangle rect, GameInputBinding bind, float chip)
    {
        float lineH = rect.Height * 0.55f;
        float yo = -(chip - lineH) / 2f;
        switch (bind.Kind)
        {
            case BindingKind.Keyboard:
            {
                int? row = KeySpriteRow(bind.Key);
                if (row.HasValue)
                {
                    Rectangle src = new Rectangle(frame * KeyFrameSize, row.Value * KeyFrameSize, KeyFrameSize, KeyFrameSize);
                    float sc = chip / KeyFrameSize;
                    spriteBatch.Draw(_keysTexture, new Vector2(rect.X + 4f, rect.Y + 6f + yo), src, Color.White, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
                }
                else
                {
                    var (bf, sm) = BodyFontForMenuText(font);
                    string s = GameInputBinding.Format(bind);
                    float ts = 0.38f * sm;
                    Vector2 sz = bf.MeasureString(s) * ts;
                    spriteBatch.DrawString(bf, s, new Vector2(rect.Center.X - sz.X / 2f, rect.Center.Y - sz.Y / 2f), Color.White, 0f, Vector2.Zero, ts, SpriteEffects.None, 0f);
                }
                break;
            }
            case BindingKind.Mouse:
            {
                float sc = chip / KeyFrameSize;
                if (bind.Mouse == MouseBindButton.Left)
                {
                    Rectangle src = new Rectangle(frame * KeyFrameSize, 0, KeyFrameSize, KeyFrameSize);
                    spriteBatch.Draw(_lmbTexture, new Vector2(rect.X + 4f, rect.Y + 6f + yo), src, Color.White, 0f, Vector2.Zero, sc, SpriteEffects.None, 0f);
                }
                else
                {
                    var (bf, sm) = BodyFontForMenuText(font);
                    string s = GameInputBinding.Format(bind);
                    float ts = 0.38f * sm;
                    Vector2 sz = bf.MeasureString(s) * ts;
                    spriteBatch.DrawString(bf, s, new Vector2(rect.Center.X - sz.X / 2f, rect.Center.Y - sz.Y / 2f), Color.White, 0f, Vector2.Zero, ts, SpriteEffects.None, 0f);
                }
                break;
            }
            default:
            {
                var (bf, sm) = BodyFontForMenuText(font);
                string s = "(none)";
                float ts = 0.38f * sm;
                Vector2 sz = bf.MeasureString(s) * ts;
                spriteBatch.DrawString(bf, s, new Vector2(rect.Center.X - sz.X / 2f, rect.Center.Y - sz.Y / 2f), Color.Gray, 0f, Vector2.Zero, ts, SpriteEffects.None, 0f);
                break;
            }
        }
    }

    /// <summary>Same geometry as <see cref="DrawControlsRebindable"/> for hit-testing.</summary>
    private void RegisterBindTargets(Rectangle content, SpriteFont font, float textScale, float ui)
    {
        float lineH = font.LineSpacing * textScale;
        float chip = MathHelper.Max(lineH * 1.35f, 28f * ui);
        float x0 = content.X + 12f * ui;
        float y = content.Y + 8f * ui;
        float gap = 8f * ui;

        void AfterLabel() => y += lineH * 1.35f;

        void RowChipsRegister(params KeybindId[] ids)
        {
            float x = x0;
            foreach (var id in ids)
            {
                var rect = new Rectangle((int)x, (int)(y - 2), (int)chip + 4, (int)chip + 8);
                _bindTargets.Add((rect, id));
                x += rect.Width + gap;
            }
            y += chip + 14f * ui;
        }

        AfterLabel();
        RowChipsRegister(KeybindId.MoveUp, KeybindId.MoveDown, KeybindId.MoveLeft, KeybindId.MoveRight);
        AfterLabel();
        RowChipsRegister(KeybindId.Dash);
        AfterLabel();
        y += chip + 14f * ui;
        AfterLabel();
        RowChipsRegister(KeybindId.ItemSlot1, KeybindId.ItemSlot2);
        AfterLabel();
        RowChipsRegister(KeybindId.PlaceItem, KeybindId.ThrowItem);
    }

    private static int? KeySpriteRow(Keys k) => k switch
    {
        Keys.W => 0,
        Keys.A => 1,
        Keys.S => 2,
        Keys.D => 3,
        Keys.T => 4,
        Keys.D1 or Keys.NumPad1 => 5,
        Keys.D2 or Keys.NumPad2 => 6,
        Keys.LeftShift or Keys.RightShift => 7,
        _ => null,
    };

    private void DrawPlaceholderInRect(SpriteBatch spriteBatch, SpriteFont menuFont, Rectangle content, string message, float textScale)
    {
        var (bf, sm) = BodyFontForMenuText(menuFont);
        float drawScale = textScale * sm;
        spriteBatch.Draw(Pixel, content, new Color(24, 28, 36, 120));
        Vector2 sz = bf.MeasureString(message) * drawScale;
        spriteBatch.DrawString(bf, message,
            new Vector2(content.Center.X - sz.X / 2f, content.Center.Y - sz.Y / 2f),
            Color.LightGray, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
    }

    private static void DrawPanelOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle r, Color c)
    {
        const int t = 2;
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Top, r.Width, t), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Bottom - t, r.Width, t), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Top, t, r.Height), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Right - t, r.Top, t, r.Height), c);
    }

    private void DrawPanelOutline(SpriteBatch spriteBatch, Rectangle r, Color c) =>
        DrawPanelOutline(spriteBatch, Pixel, r, c);

    private readonly struct SettingsChromeLayout
    {
        public readonly Rectangle PanelRect;
        public readonly Rectangle[] TabRects;
        public readonly Rectangle ContentRect;
        public readonly float TabTextScale;

        public SettingsChromeLayout(Rectangle panelRect, Rectangle[] tabRects, Rectangle contentRect, float tabTextScale)
        {
            PanelRect = panelRect;
            TabRects = tabRects;
            ContentRect = contentRect;
            TabTextScale = tabTextScale;
        }
    }

    private SettingsChromeLayout BuildChromeLayout(SpriteFont font, Viewport vp, float ui)
    {
        float panelW = MathHelper.Min(720f * ui, vp.Width - 40f);
        float panelH = vp.Height * 0.76f;
        int px = (int)(vp.Width / 2f - panelW / 2f);
        int py = (int)(vp.Height / 2f - panelH / 2f);
        var panel = new Rectangle(px, py, (int)panelW, (int)panelH);

        float tabScale = 0.48f * ui;
        string[] labels = { "CONTROLS", "SOUND", "DISPLAY" };
        float padX = 16f * ui;
        float tabY = panel.Y + (int)(18f * ui);
        float tabH = font.LineSpacing * tabScale + 18f * ui;
        var rects = new Rectangle[3];
        float totalW = 0f;
        for (int i = 0; i < 3; i++)
        {
            float tw = font.MeasureString(labels[i]).X * tabScale + padX * 2f;
            rects[i] = new Rectangle(0, (int)tabY, (int)tw, (int)tabH);
            totalW += tw + (i < 2 ? 10f * ui : 0f);
        }

        float startX = panel.X + panel.Width / 2f - totalW / 2f;
        float x = startX;
        for (int i = 0; i < 3; i++)
        {
            var r = rects[i];
            rects[i] = new Rectangle((int)x, r.Y, r.Width, r.Height);
            x += r.Width + 10f * ui;
        }

        int contentTop = (int)(tabY + tabH + 16f * ui);
        var content = new Rectangle(panel.X + (int)(16f * ui), contentTop, panel.Width - (int)(32f * ui), panel.Bottom - contentTop - (int)(48f * ui));

        return new SettingsChromeLayout(panel, rects, content, tabScale);
    }
}
