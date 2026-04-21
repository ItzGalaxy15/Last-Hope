using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

/// <summary>
/// AngelCode BMFont (.fnt) bitmap font (used with <c>Content/Font.fnt</c> + <c>Content/Font.png</c>).
/// </summary>
public sealed class BmFont
{
    private readonly Texture2D _page;
    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly int _lineHeight;
    private readonly int _spacingX;

    private BmFont(Texture2D page, Dictionary<char, Glyph> glyphs, int lineHeight, int spacingX)
    {
        _page = page;
        _glyphs = glyphs;
        _lineHeight = lineHeight;
        _spacingX = spacingX;
    }

    public int LineHeight => _lineHeight;

    public static BmFont? TryLoad(GraphicsDevice device, string fntPath)
    {
        if (device == null || string.IsNullOrWhiteSpace(fntPath) || !File.Exists(fntPath))
            return null;

        string? dir = Path.GetDirectoryName(Path.GetFullPath(fntPath));
        if (string.IsNullOrEmpty(dir))
            dir = ".";

        string[] lines;
        try
        {
            lines = File.ReadAllLines(fntPath);
        }
        catch
        {
            return null;
        }

        int lineHeight = 16;
        int spacingX = 0;
        string pageFile = "Font.png";

        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.StartsWith("common ", StringComparison.Ordinal))
            {
                int? lh = ReadInt(line, "lineHeight=");
                if (lh.HasValue)
                    lineHeight = lh.Value;
                int? sp = ReadSpacingX(line);
                if (sp.HasValue)
                    spacingX = sp.Value;
            }
            else if (line.StartsWith("page ", StringComparison.Ordinal))
            {
                Match m = Regex.Match(line, @"file=""([^""]+)""");
                if (m.Success)
                    pageFile = m.Groups[1].Value;
            }
        }

        string pngPath = Path.Combine(dir, pageFile);
        if (!File.Exists(pngPath))
            return null;

        Texture2D? texture;
        try
        {
            using FileStream fs = File.OpenRead(pngPath);
            texture = Texture2D.FromStream(device, fs);
        }
        catch
        {
            return null;
        }

        var glyphs = new Dictionary<char, Glyph>();
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (!line.StartsWith("char ", StringComparison.Ordinal))
                continue;

            int? id = ReadInt(line, "id=");
            if (!id.HasValue)
                continue;

            int? x = ReadInt(line, "x=");
            int? y = ReadInt(line, "y=");
            int? w = ReadInt(line, "width=");
            int? h = ReadInt(line, "height=");
            int? xo = ReadInt(line, "xoffset=");
            int? yo = ReadInt(line, "yoffset=");
            int? adv = ReadInt(line, "xadvance=");
            if (x == null || y == null || w == null || h == null || xo == null || yo == null || adv == null)
                continue;

            char ch = id.Value > char.MaxValue ? '?' : (char)id.Value;
            glyphs[ch] = new Glyph(x.Value, y.Value, w.Value, h.Value, xo.Value, yo.Value, adv.Value);
        }

        if (glyphs.Count == 0)
        {
            texture.Dispose();
            return null;
        }

        return new BmFont(texture, glyphs, lineHeight, spacingX);
    }

    private bool TryGetGlyph(char c, out Glyph g)
    {
        if (_glyphs.TryGetValue(c, out g))
            return true;
        if (_glyphs.TryGetValue(' ', out g))
            return true;
        if (_glyphs.TryGetValue('?', out g))
            return true;
        g = default;
        return false;
    }

    private static int? ReadSpacingX(string commonLine)
    {
        Match m = Regex.Match(commonLine, @"spacing=(-?\d+),");
        if (!m.Success)
            return null;
        return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static int? ReadInt(string line, string key)
    {
        int i = line.IndexOf(key, StringComparison.Ordinal);
        if (i < 0)
            return null;
        i += key.Length;
        int j = i;
        while (j < line.Length && (char.IsDigit(line[j]) || line[j] == '-'))
            j++;
        if (j == i)
            return null;
        return int.Parse(line.AsSpan(i, j - i), CultureInfo.InvariantCulture);
    }

    public Vector2 MeasureString(string text, float scale)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;

        string[] rows = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        float maxW = 0f;
        foreach (string row in rows)
        {
            float w = 0f;
            foreach (char c in row)
            {
                if (!TryGetGlyph(c, out Glyph g))
                    w += 8f * scale;
                else
                    w += g.XAdvance * scale + _spacingX * scale;
            }

            if (w > maxW)
                maxW = w;
        }

        float lineH = _lineHeight * scale;
        return new Vector2(maxW, lineH * rows.Length);
    }

    public void Draw(SpriteBatch spriteBatch, string text, Vector2 position, Color color, float scale, float layerDepth)
    {
        if (string.IsNullOrEmpty(text))
            return;

        string[] rows = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        float yPen = position.Y;
        foreach (string row in rows)
        {
            float xPen = position.X;
            foreach (char c in row)
            {
                if (!TryGetGlyph(c, out Glyph g))
                {
                    xPen += 8f * scale;
                    continue;
                }

                if (g.Width > 0 && g.Height > 0)
                {
                    var src = new Rectangle(g.X, g.Y, g.Width, g.Height);
                    var dest = new Vector2(
                        xPen + g.XOffset * scale,
                        yPen + g.YOffset * scale);
                    spriteBatch.Draw(_page, dest, src, color, 0f, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
                }

                xPen += g.XAdvance * scale + _spacingX * scale;
            }

            yPen += _lineHeight * scale;
        }
    }

    private readonly struct Glyph
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly int XOffset;
        public readonly int YOffset;
        public readonly int XAdvance;

        public Glyph(int x, int y, int w, int h, int xo, int yo, int adv)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            XOffset = xo;
            YOffset = yo;
            XAdvance = adv;
        }
    }
}
