using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI;

public class SkillNode
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int MaxLevel { get; set; }
    public int CurrentLevel { get; set; }
    public Vector2 Position { get; set; }
    
    public bool IsActiveSkill { get; set; }
    public bool IsSubStat { get; set; }
    
    public List<SkillNode> Prerequisites { get; set; } = new List<SkillNode>();
    public List<SkillNode> Children { get; set; } = new List<SkillNode>();

    // Node is unlocked if all prerequisites have reached their MaxLevel
    public bool IsUnlocked()
    {
        if (Prerequisites.Count == 0) return true;
        foreach (var req in Prerequisites)
        {
            if (req.CurrentLevel < req.MaxLevel) return false;
        }
        return true;
    }
    
    public bool IsMaxed() => CurrentLevel >= MaxLevel;

    public void AddChild(SkillNode node)
    {
        Children.Add(node);
        if (!node.Prerequisites.Contains(this))
            node.Prerequisites.Add(this);
    }

    public void AddPrerequisite(SkillNode node)
    {
        Prerequisites.Add(node);
        if (!node.Children.Contains(this))
            node.Children.Add(this);
    }
}

public class SkillTreeUI : UIElement
{
    private readonly List<SkillNode> _nodes = new List<SkillNode>();
    private readonly Texture2D _pixel;
    private SkillNode _hoveredNode;

    public SkillTreeUI(Texture2D pixel, int screenWidth)
    {
        _pixel = pixel;
        InitializeNodes(screenWidth);
    }

    private void InitializeNodes(int screenWidth)
    {
        int centerX = screenWidth / 2;
        int startY = 180; // Pushed down to give breathing room
        int trackSpacingX = 450; // Dramatically widened branches
        int rowSpacingY = 200; // Deepened vertical progression

        // 1. Root
        var swordsman = new SkillNode { Name = "SWORDSMAN", Description = "The starting point. Unlock to access following abilities.", MaxLevel = 1, IsActiveSkill = true, Position = new Vector2(centerX, startY) };

        // 2. Middle Track (Single Target Focus)
        var swordSlash = new SkillNode { Name = "sword slash", Description = "Passive: Enables basic sword attacks.", MaxLevel = 1, IsActiveSkill = false, Position = new Vector2(centerX, startY + rowSpacingY) };
        swordsman.AddChild(swordSlash);

        var midDmg = new SkillNode { Name = "dmg+", Description = "Increases melee damage.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX - 120, startY + (int)(rowSpacingY * 1.8)) };
        var midHp = new SkillNode { Name = "hp+", Description = "Increases maximum health.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX, startY + (int)(rowSpacingY * 1.8)) };
        var midRange = new SkillNode { Name = "range+", Description = "Increases attack range.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX + 120, startY + (int)(rowSpacingY * 1.8)) };
        
        swordSlash.AddChild(midDmg);
        swordSlash.AddChild(midHp);
        swordSlash.AddChild(midRange);

        var burstSlash = new SkillNode { Name = "burst slash", Description = "Active: A powerful forward cleave.", MaxLevel = 1, IsActiveSkill = true, Position = new Vector2(centerX, startY + (int)(rowSpacingY * 2.8)) };
        burstSlash.AddPrerequisite(midDmg);
        burstSlash.AddPrerequisite(midHp);
        burstSlash.AddPrerequisite(midRange);

        // 3. Left Track (Agility & Dual Wielding)
        var dualWield = new SkillNode { Name = "dual wield", Description = "Passive: Equips two axes. Grants +5% base damage.", MaxLevel = 1, IsActiveSkill = false, Position = new Vector2(centerX - trackSpacingX, startY + rowSpacingY) };
        swordsman.AddChild(dualWield);

        var leftHaste = new SkillNode { Name = "haste+", Description = "Increases attack speed.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX - trackSpacingX - 120, startY + (int)(rowSpacingY * 1.8)) };
        var leftCrit = new SkillNode { Name = "crit+", Description = "Increases critical hit chance.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX - trackSpacingX, startY + (int)(rowSpacingY * 1.8)) };
        var leftDmg = new SkillNode { Name = "dmg+", Description = "Increases off-hand damage.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX - trackSpacingX + 120, startY + (int)(rowSpacingY * 1.8)) };

        dualWield.AddChild(leftHaste);
        dualWield.AddChild(leftCrit);
        dualWield.AddChild(leftDmg);

        var whirlwind = new SkillNode { Name = "whirlwind", Description = "Active: Spin and fire radial slashes. Press [H] to cast.\nCooldown: 5 seconds.", MaxLevel = 1, IsActiveSkill = true, Position = new Vector2(centerX - trackSpacingX, startY + (int)(rowSpacingY * 2.8)) };
        whirlwind.AddPrerequisite(leftHaste);
        whirlwind.AddPrerequisite(leftCrit);
        whirlwind.AddPrerequisite(leftDmg);

        // 4. Right Track (Defense & Crowd Control)
        var shield = new SkillNode { Name = "shield", Description = "Passive: Enables shield usage.", MaxLevel = 1, IsActiveSkill = false, Position = new Vector2(centerX + trackSpacingX, startY + rowSpacingY) };
        swordsman.AddChild(shield);

        var rightHp = new SkillNode { Name = "hp+", Description = "Increases maximum health.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX + trackSpacingX - 120, startY + (int)(rowSpacingY * 1.8)) };
        var rightDmgReduc = new SkillNode { Name = "dmg reduc+", Description = "Increases damage mitigation/armor.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX + trackSpacingX, startY + (int)(rowSpacingY * 1.8)) };
        var rightDmg = new SkillNode { Name = "dmg+", Description = "Increases shield-based damage.", MaxLevel = 3, IsSubStat = true, Position = new Vector2(centerX + trackSpacingX + 120, startY + (int)(rowSpacingY * 1.8)) };

        shield.AddChild(rightHp);
        shield.AddChild(rightDmgReduc);
        shield.AddChild(rightDmg);

        var shieldSlam = new SkillNode { Name = "shield slam", Description = "Active: Bash enemies with your shield.", MaxLevel = 1, IsActiveSkill = true, Position = new Vector2(centerX + trackSpacingX, startY + (int)(rowSpacingY * 2.8)) };
        shieldSlam.AddPrerequisite(rightHp);
        shieldSlam.AddPrerequisite(rightDmgReduc);
        shieldSlam.AddPrerequisite(rightDmg);

        // Register all nodes for drawing
        _nodes.AddRange(new[] {
            swordsman,
            swordSlash, midDmg, midHp, midRange, burstSlash,
            dualWield, leftHaste, leftCrit, leftDmg, whirlwind,
            shield, rightHp, rightDmgReduc, rightDmg, shieldSlam
        });
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        var input = GameManager.GetGameManager().InputManager;
        Vector2 mousePos = input.CurrentMouseState.Position.ToVector2();
        
        _hoveredNode = null;
        
        foreach (var node in _nodes)
        {
            float radius = node.IsSubStat ? 15f : (node.IsActiveSkill ? 30f : 25f);
            
            // Check tooltip hovering
            if (Vector2.Distance(mousePos, node.Position) <= radius * 1.5f)
            {
                _hoveredNode = node;
                
                // Handle Upgrade / Unlocking
                if (input.LeftMousePress() && node.IsUnlocked() && !node.IsMaxed())
                {
                    node.CurrentLevel++;
                    ApplySkillUpgrade(node.Name);
                }
                break;
            }
        }
    }

    private void ApplySkillUpgrade(string skillName)
    {
        var player = GameManager.GetGameManager()._player as Warrior;
        if (player == null) return;

        switch (skillName.ToLower())
        {
            case "dual wield":
                player.DualWieldUnlocked = true;
                break;
            case "haste+":
                player.HasteLevel++;
                break;
            case "crit+":
                player.CritLevel++;
                break;
            case "dmg+":
                player.DmgLevel++;
                break;
            case "whirlwind":
                player.WhirlwindUnlocked = true;
                break;
        }
        player.UpdateStats();
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Texture2D pixel = _pixel ?? GameManager.GetGameManager().Pixel;
        SpriteFont font = GameManager.GetGameManager()._font;

        // Draw Connections
        foreach (var node in _nodes)
        {
            foreach (var child in node.Children)
            {
                // Glowing radiant line for unlocked, dark muted line for locked
                Color lineColor = child.IsUnlocked() ? Color.DeepSkyBlue : new Color(40, 40, 40, 255);
                DrawLine(spriteBatch, pixel, node.Position, child.Position, lineColor, child.IsUnlocked() ? 5 : 2);
                
                if (child.IsUnlocked()) // Add a soft outer glow for active channels
                    DrawLine(spriteBatch, pixel, node.Position, child.Position, Color.LightCyan * 0.4f, 10);
            }
        }

        // Draw Nodes
        foreach (var node in _nodes)
        {
            // Desaturated Bronze for locked, Polished Steel for active, Radiant Light for maxed
            Color bgColor = node.IsMaxed() ? Color.LightSkyBlue : (node.IsUnlocked() ? new Color(150, 160, 170, 255) : new Color(85, 75, 65, 255));
            Color borderColor = node.IsMaxed() ? Color.White : (node.IsUnlocked() ? new Color(200, 210, 220) : new Color(50, 45, 40));
            Color innerGlowColor = node.IsUnlocked() ? Color.White * 0.2f : Color.Transparent;
            
            // Shapes based on ability types
            if (node.IsActiveSkill)
            {
                DrawFilledCircle(spriteBatch, pixel, node.Position.ToPoint(), 28, bgColor);
                DrawFilledCircle(spriteBatch, pixel, node.Position.ToPoint(), 22, innerGlowColor);
                DrawCircleOutline(spriteBatch, pixel, node.Position.ToPoint(), 28, 2, borderColor);
                DrawCircleOutline(spriteBatch, pixel, node.Position.ToPoint(), 34, 1, borderColor * 0.5f); // Outer emblem ring
            }
            else if (node.IsSubStat)
            {
                Rectangle rect = new Rectangle((int)node.Position.X - 15, (int)node.Position.Y - 15, 30, 30);
                spriteBatch.Draw(pixel, rect, bgColor);
                spriteBatch.Draw(pixel, new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8), innerGlowColor);
                DrawRectangleOutline(spriteBatch, pixel, rect, 2, borderColor);
            }
            else // Passives
            {
                Rectangle rect = new Rectangle((int)node.Position.X - 25, (int)node.Position.Y - 25, 50, 50);
                spriteBatch.Draw(pixel, rect, bgColor);
                spriteBatch.Draw(pixel, new Rectangle(rect.X + 6, rect.Y + 6, rect.Width - 12, rect.Height - 12), innerGlowColor);
                DrawRectangleOutline(spriteBatch, pixel, rect, 2, borderColor);
            }

            // Text Information
            if (font != null)
            {
                string label = node.Name.ToUpper();
                Vector2 size = font.MeasureString(label);
                float scale = node.IsSubStat ? 0.35f : 0.45f;
                Vector2 pos = new Vector2(node.Position.X - (size.X * scale / 2), node.Position.Y + (node.IsActiveSkill ? 38 : 28));
                
                spriteBatch.DrawString(font, label, pos, node.IsUnlocked() ? Color.White : new Color(180, 180, 180), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                string levelStr = $"[{node.CurrentLevel}/{node.MaxLevel}]";
                Vector2 lvlSize = font.MeasureString(levelStr);
                Vector2 lvlPos = new Vector2(node.Position.X - (lvlSize.X * scale / 2), node.Position.Y - (node.IsSubStat ? 8 : 12));
                spriteBatch.DrawString(font, levelStr, lvlPos, node.IsUnlocked() ? Color.Gold : Color.Gray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                if (!node.IsUnlocked())
                {
                    // Draw subtle padlock icon instead of massive red text
                    int lockX = (int)(lvlPos.X + (lvlSize.X * scale) + 6);
                    int lockY = (int)lvlPos.Y + 2;
                    spriteBatch.Draw(pixel, new Rectangle(lockX, lockY + 3, 6, 5), Color.Gray); // lock body
                    DrawRectangleOutline(spriteBatch, pixel, new Rectangle(lockX + 1, lockY, 4, 4), 1, Color.Gray); // lock shackle
                }
            }
        }

        // Compact, Elegant Tooltip Panel
        if (_hoveredNode != null && font != null)
        {
            float tipScale = 0.35f;
            string tipTitle = $"{_hoveredNode.Name.ToUpper()} ({_hoveredNode.CurrentLevel}/{_hoveredNode.MaxLevel})";
            string tipDesc = _hoveredNode.Description;
            string tipLock = _hoveredNode.IsUnlocked() ? "" : "[LOCKED: Requires previous node to unlock.]";

            Vector2 titleSize = font.MeasureString(tipTitle) * tipScale;
            Vector2 descSize = font.MeasureString(tipDesc) * tipScale;
            Vector2 lockSize = string.IsNullOrEmpty(tipLock) ? Vector2.Zero : font.MeasureString(tipLock) * tipScale;

            float tipWidth = Math.Max(titleSize.X, Math.Max(descSize.X, lockSize.X)) + 20;
            float tipHeight = titleSize.Y + descSize.Y + (lockSize.Y > 0 ? lockSize.Y + 6 : 0) + 20;

            Vector2 mousePos = GameManager.GetGameManager().InputManager.CurrentMouseState.Position.ToVector2();
            Rectangle bgTip = new Rectangle((int)mousePos.X + 20, (int)mousePos.Y + 20, (int)tipWidth, (int)tipHeight);
            
            // Dark paneled background with steel border
            spriteBatch.Draw(pixel, bgTip, new Color(20, 22, 25, 245));
            DrawRectangleOutline(spriteBatch, pixel, bgTip, 2, new Color(130, 140, 150));
            
            Vector2 textTipPos = new Vector2(bgTip.X + 10, bgTip.Y + 10);
            spriteBatch.DrawString(font, tipTitle, textTipPos, Color.Gold, 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, tipDesc, textTipPos + new Vector2(0, titleSize.Y + 2), Color.LightGray, 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
            
            if (!string.IsNullOrEmpty(tipLock))
            {
                spriteBatch.DrawString(font, tipLock, textTipPos + new Vector2(0, titleSize.Y + descSize.Y + 6), new Color(200, 80, 80), 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
            }
        }
    }

    // --- Drawing Helpers ---
    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);
        spriteBatch.Draw(pixel, new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), thickness), null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
    }

    private void DrawFilledCircle(SpriteBatch spriteBatch, Texture2D pixel, Point center, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            int x = (int)MathF.Sqrt((radius * radius) - (y * y));
            spriteBatch.Draw(pixel, new Rectangle(center.X - x, center.Y + y, (x * 2) + 1, 1), color);
        }
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Texture2D pixel, Point center, int radius, int thickness, Color color)
    {
        int innerRadius = Math.Max(0, radius - thickness);
        for (int y = -radius; y <= radius; y++)
        {
            int outerX = (int)MathF.Sqrt((radius * radius) - (y * y));
            int innerX = (int)MathF.Sqrt(Math.Max(0, (innerRadius * innerRadius) - (y * y)));

            int leftWidth = Math.Max(0, outerX - innerX);
            if (leftWidth > 0)
            {
                spriteBatch.Draw(pixel, new Rectangle(center.X - outerX, center.Y + y, leftWidth, 1), color);
                spriteBatch.Draw(pixel, new Rectangle(center.X + innerX + 1, center.Y + y, leftWidth, 1), color);
            }
        }
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }
}