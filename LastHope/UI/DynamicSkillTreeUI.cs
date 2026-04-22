using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.SkillTree;

namespace Last_Hope.UI
{
    // ==========================================
    // 1. GLOBAL THEMING SYSTEM
    // ==========================================
    
    /// <summary>
    /// Data structure defining the visual aesthetics of a class skill tree.
    /// Can be swapped dynamically to completely change the look and feel.
    /// Example: Warrior = Dark Steel/Red, Archer = Leather/Green, Mage = Parchment/Purple
    /// </summary>
    public class UIThemeData
    {
        public string ThemeId { get; set; }
        
        // Colors
        public Color BackgroundTint { get; set; }
        public Color BorderColor { get; set; }
        public Color AccentGlowColor { get; set; }
        public Color LockedDesaturation { get; set; }
        
        // Assets (Loaded via Content Pipeline)
        public string BackgroundTexturePath { get; set; }
        public string NodeBorderTexturePath { get; set; }
        public string NodeMaskTexturePath { get; set; }
        
        // VFX / Juiciness
        public string ParticleEffectPrefab { get; set; }
        public NodeShape DefaultShape { get; set; }
    }

    // ==========================================
    // 2. VISUAL NODE PREFAB & STATE MACHINE
    // ==========================================
    
    public class SkillNodeUI
    {
        public SkillNodeData Data { get; private set; }
        public NodeState CurrentState { get; private set; }
        public bool HasPendingPoints { get; set; }
        
        // Transform & Layout
        public Vector2 Position { get; set; }
        
        // Tweening Targets (For Polish & Juice)
        private float _currentScale = 1f;
        private float _targetScale = 1f;
        
        private float _currentFill = 0f;
        private float _targetFill = 0f;
        
        private Color _currentColor;
        private Color _targetColor;
        private float _pulseTimer;

        // References
        private UIThemeData _theme;

        public SkillNodeUI(SkillNodeData data, UIThemeData theme)
        {
            Data = data;
            _theme = theme;
            _currentColor = theme.LockedDesaturation;
        }

        public void UpdateState(NodeState newState, int allocatedPoints, int maxPoints, bool hasPending)
        {
            // [AUDIO HOOK]: Detect state changes
            if (CurrentState != newState && newState == NodeState.Maxed)
            {
                // AudioManager.PlaySound("Node_Maxed_Aura");
                // ParticleManager.Emit(_theme.ParticleEffectPrefab, Position);
            }
            else if (_targetFill < (float)allocatedPoints / maxPoints)
            {
                // AudioManager.PlaySound("Point_Allocated_Tick");
            }
            else if (CurrentState == NodeState.Locked && newState == NodeState.Available)
            {
                // AudioManager.PlaySound("Node_Unlocked_Thump");
            }

            CurrentState = newState;
            HasPendingPoints = hasPending;
            _targetFill = maxPoints > 0 ? (float)allocatedPoints / maxPoints : 0f;

            // Set Target Visuals based on state
            switch (CurrentState)
            {
                case NodeState.Locked:
                    _targetColor = _theme.LockedDesaturation;
                    break;
                case NodeState.Available:
                    _targetColor = new Color(120, 130, 140); // Metallic idle
                    break;
                case NodeState.Partial:
                    _targetColor = Color.Lerp(new Color(200, 50, 20), _theme.AccentGlowColor, _targetFill);
                    break;
                case NodeState.Maxed:
                    _targetColor = _theme.AccentGlowColor;
                    break;
            }
            
            if (HasPendingPoints)
            {
                _targetColor = new Color(255, 210, 50); // Radiant Gold for planned nodes
            }
        }

        public void UpdateVisuals(GameTime gameTime, bool isHovered)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // 1. Hover Logic & Tooltip Activation
            if (isHovered)
            {
                if (_targetScale < 1.15f) 
                {
                    // [AUDIO HOOK]: AudioManager.PlaySound("UI_Hover_Soft");
                }
                _targetScale = 1.15f;
                
                // Request Controller/Tree to draw tooltip panel safely away from cursor
            }
            else
            {
                _targetScale = 1.0f;
            }

            // 2. State-Based Animations
            if (CurrentState == NodeState.Available)
            {
                // Subtle pulsing border glow to attract attention
                _pulseTimer += dt * 4f;
                float pulseAlpha = (float)(Math.Sin(_pulseTimer) + 1f) * 0.5f;
                _targetColor = Color.Lerp(new Color(100, 110, 120), new Color(160, 80, 50), pulseAlpha);
            }

            // 3. Tweening Execution (Fades, Scales, Color Lerps)
            _currentScale = MathHelper.Lerp(_currentScale, _targetScale, dt * 12f);
            _currentFill = MathHelper.Lerp(_currentFill, _targetFill, dt * 6f);
            
            // Safe Color Lerp (MonoGame doesn't natively lerp alpha perfectly via standard lerp without premultiplication)
            _currentColor = new Color(
                (int)MathHelper.Lerp(_currentColor.R, _targetColor.R, dt * 8f),
                (int)MathHelper.Lerp(_currentColor.G, _targetColor.G, dt * 8f),
                (int)MathHelper.Lerp(_currentColor.B, _targetColor.B, dt * 8f),
                (int)MathHelper.Lerp(_currentColor.A, _targetColor.A, dt * 8f)
            );
        }
        
        public float GetCurrentFill() => _currentFill;
        public float GetScale() => _currentScale;
        public Color GetColor() => _currentColor;
    }

    // ==========================================
    // 3. DYNAMIC CONNECTION LINES
    // ==========================================
    public class SkillConnectionUI
    {
        public SkillNodeUI ParentNode;
        public SkillNodeUI ChildNode;
        
        private float _fillProgress = 0f;

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // If parent has allocated points and child is available/unlocked, animate the connection line filling up
            bool isRouteActive = (ParentNode.CurrentState == NodeState.Maxed || ParentNode.CurrentState == NodeState.Partial) && ChildNode.CurrentState != NodeState.Locked;
            
            float targetFill = isRouteActive ? 1f : 0f;
            _fillProgress = MathHelper.Lerp(_fillProgress, targetFill, dt * 4f);
        }

        public float GetFillProgress() => _fillProgress;
    }

    // ==========================================
    // 4. DYNAMIC LAYOUT ENGINE (The Grid/Canvas)
    // ==========================================
    public class DynamicSkillTreeLayout
    {
        public NodeShape GlobalShapeOverride { get; private set; }

        public void ToggleShapeDebug()
        {
            GlobalShapeOverride = GlobalShapeOverride == NodeShape.Circle ? NodeShape.Square : NodeShape.Circle;
        }

        /// <summary>
        /// Dynamically calculates vertical and horizontal spacing to symmetrically center 
        /// layers regardless of how many nodes are in them.
        /// </summary>
        public void GenerateLayout(List<SkillNodeUI> uiNodes, Rectangle availableScreenArea)
        {
            if (uiNodes.Count == 0) return;

            // 1. Determine Tree Depth
            int maxLayer = uiNodes.Max(n => n.Data.Layer);
            int layerCount = maxLayer + 1;

            // 2. Fixed spacing for a tighter, centered UI
            float verticalSpacing = 160f;
            float totalHeight = (layerCount - 1) * verticalSpacing;
            float startY = availableScreenArea.Center.Y - (totalHeight / 2f);

            // 3. Process Layers for Horizontal Centering
            for (int l = 0; l <= maxLayer; l++)
            {
                // Grab all nodes in this layer, sort them conceptually by their Data.GridX value
                var nodesInLayer = uiNodes.Where(n => n.Data.Layer == l)
                                          .OrderBy(n => n.Data.GridX)
                                          .ToList();
                                          
                int nodeCount = nodesInLayer.Count;
                if (nodeCount == 0) continue;

                // Tighter horizontal spacing
                float horizontalSpacing = 200f;
                float totalWidth = (nodeCount - 1) * horizontalSpacing;
                float startX = availableScreenArea.Center.X - (totalWidth / 2f);
                
                for (int i = 0; i < nodeCount; i++)
                {
                    float xPos = startX + (i * horizontalSpacing);
                    float yPos = startY + (l * verticalSpacing);
                    
                    nodesInLayer[i].Position = new Vector2(xPos, yPos);
                }
            }
        }
    }

    // ==========================================
    // 5. MASTER CANVAS & INPUT HANDLING
    // ==========================================
    public class SkillTreeMenuCanvas : UIElement
    {
        private readonly BaseSkillTree _tree;
        private readonly UIThemeData _theme;
        private readonly Texture2D _pixel;
        
        private readonly List<SkillNodeUI> _uiNodes = new List<SkillNodeUI>();
        private readonly List<SkillConnectionUI> _uiConnections = new List<SkillConnectionUI>();
        private readonly DynamicSkillTreeLayout _layout = new DynamicSkillTreeLayout();

        // UI Panel & Controls
        private Rectangle _mainPanel;
        private Rectangle _topBarRect;
        private Rectangle _bottomBarRect;
        
        // Buttons
        private Rectangle _btnConfirm;
        private Rectangle _btnReset;
        private Rectangle _btnCancel;
        public bool IsClosed { get; private set; }

        // Animations
        private float _entranceProgress = 0f;
        private float _btnConfirmHover = 0f;
        private float _btnCancelHover = 0f;
        private float _btnResetHover = 0f;

        private SkillNodeUI _hoveredNode;

        public SkillTreeMenuCanvas(BaseSkillTree tree, UIThemeData theme, Texture2D pixel, Viewport viewport)
        {
            _tree = tree;
            _theme = theme;
            _pixel = pixel;

            // 1. Instantiate UI Nodes
            foreach (var nodeData in tree.GetData().Nodes)
            {
                _uiNodes.Add(new SkillNodeUI(nodeData, theme));
            }

            // 2. Instantiate UI Connections
            foreach (var connData in tree.GetData().Connections)
            {
                var parent = _uiNodes.First(n => n.Data.Id == connData.FromNodeId);
                var child = _uiNodes.First(n => n.Data.Id == connData.ToNodeId);
                _uiConnections.Add(new SkillConnectionUI { ParentNode = parent, ChildNode = child });
            }

            // 3. Layout Generation
            int panelWidth = (int)(viewport.Width * 0.8f);
            int panelHeight = (int)(viewport.Height * 0.8f);
            _mainPanel = new Rectangle(
                (viewport.Width - panelWidth) / 2,
                (viewport.Height - panelHeight) / 2,
                panelWidth,
                panelHeight
            );
            
            _topBarRect = new Rectangle(_mainPanel.X, _mainPanel.Y, _mainPanel.Width, 75);
            _bottomBarRect = new Rectangle(_mainPanel.X, _mainPanel.Bottom - 85, _mainPanel.Width, 85);

            int btnWidth = 130;
            int btnHeight = 44;
            int btnY = _bottomBarRect.Center.Y - (btnHeight / 2);
            
            _btnCancel = new Rectangle(_bottomBarRect.Right - btnWidth - 30, btnY, btnWidth, btnHeight);
            _btnConfirm = new Rectangle(_btnCancel.Left - btnWidth - 15, btnY, btnWidth, btnHeight);
            _btnReset = new Rectangle(_bottomBarRect.Left + 30, btnY, btnWidth, btnHeight);

            Rectangle treeArea = new Rectangle(_mainPanel.X, _topBarRect.Bottom, _mainPanel.Width, _bottomBarRect.Top - _topBarRect.Bottom);
            _layout.GenerateLayout(_uiNodes, treeArea);

            // [DEBUG STEP 3]: Coordinate System Check
            if (_uiNodes.Count > 0)
            {
                System.Console.WriteLine($"[SkillTree UI] Panel Center: {_mainPanel.Center.X}, {_mainPanel.Center.Y}");
                System.Console.WriteLine($"[SkillTree UI] First Node Generated at Absolute Position: {_uiNodes[0].Position.X}, {_uiNodes[0].Position.Y}");
            }
        }

        public override void Update(GameTime gameTime, Viewport viewport)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = GameManager.GetGameManager().InputManager;
            Vector2 mousePos = input.CurrentMouseState.Position.ToVector2();
            bool isLeftClickPressed = input.LeftMousePress();
            bool isRightClickPressed = input.RightMousePress();

            IsClosed = false;

            // Transitions
            _entranceProgress = MathHelper.Clamp(_entranceProgress + dt * 6f, 0f, 1f);
            _btnConfirmHover = MathHelper.Lerp(_btnConfirmHover, _btnConfirm.Contains(mousePos) ? 1f : 0f, dt * 12f);
            _btnCancelHover = MathHelper.Lerp(_btnCancelHover, _btnCancel.Contains(mousePos) ? 1f : 0f, dt * 12f);
            _btnResetHover = MathHelper.Lerp(_btnResetHover, _btnReset.Contains(mousePos) ? 1f : 0f, dt * 12f);

            // Keyboard Shortcuts
            if (input.IsKeyPress(Keys.Escape))
            {
                IsClosed = true;
                _tree.CancelPendingPoints();
                return;
            }
            if (input.IsKeyPress(Keys.R))
            {
                _tree.Respec();
            }
            if (input.IsKeyPress(Keys.Enter))
            {
                _tree.ConfirmPendingPoints();
            }

            // Bottom Bar Button Clicks
            if (isLeftClickPressed)
            {
                if (_btnCancel.Contains(mousePos)) { _tree.CancelPendingPoints(); IsClosed = true; return; }
                if (_btnReset.Contains(mousePos)) { _tree.Respec(); }
                if (_btnConfirm.Contains(mousePos)) { _tree.ConfirmPendingPoints(); }
            }

            _hoveredNode = null;
            foreach (var node in _uiNodes)
            {
                // Sync backend state into UI presentation
                NodeState state = _tree.GetNodeState(node.Data.Id, true);
                int pts = _tree.GetAllocatedPoints(node.Data.Id, true);
                bool hasPending = _tree.GetAllocatedPoints(node.Data.Id, true) > _tree.GetAllocatedPoints(node.Data.Id, false);
                
                node.UpdateState(state, pts, node.Data.MaxPoints, hasPending);
                
                // Broad hover check (Radius 25)
                bool isHovered = Vector2.Distance(mousePos, node.Position) <= 25f;
                if (isHovered) 
                {
                    _hoveredNode = node;

                    if (isLeftClickPressed && state != NodeState.Locked && pts < node.Data.MaxPoints)
                    {
                        _tree.AddPendingPoint(node.Data.Id);
                    }
                    else if (isRightClickPressed && hasPending)
                    {
                        _tree.RemovePendingPoint(node.Data.Id);
                    }
                }

                node.UpdateVisuals(gameTime, isHovered);
            }

            foreach (var conn in _uiConnections)
                conn.Update(gameTime);
        }

        private Color Fade(Color c, float a) => c * a;

        private void DrawPremiumPanel(SpriteBatch spriteBatch, Rectangle bounds, Color bg, Color borderOuter, Color borderInner, float alpha, int outerThick = 2)
        {
            // Drop shadow
            spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width, bounds.Height), Fade(Color.Black * 0.5f, alpha));
            
            // Main Fill
            spriteBatch.Draw(_pixel, bounds, Fade(bg, alpha));
            
            // Outer Border
            DrawRectangleOutline(spriteBatch, bounds, outerThick, Fade(borderOuter, alpha));
            
            // Inner Highlight
            DrawRectangleOutline(spriteBatch, new Rectangle(bounds.X + outerThick, bounds.Y + outerThick, bounds.Width - (outerThick*2), bounds.Height - (outerThick*2)), 1, Fade(borderInner, alpha));
        }

        private void DrawRectangleOutline(SpriteBatch sb, Rectangle rect, int t, Color c)
        {
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, t), c);
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom - t, rect.Width, t), c);
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Top, t, rect.Height), c);
            sb.Draw(_pixel, new Rectangle(rect.Right - t, rect.Top, t, rect.Height), c);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            SpriteFont font = GameManager.GetGameManager()._font;
            Vector2 mousePos = GameManager.GetGameManager().InputManager.CurrentMouseState.Position.ToVector2();
            Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
            
            float globalAlpha = _entranceProgress;

            // 1. Draw Main Panel
            Color panelBg = new Color(20, 22, 26, 245);
            Color borderOuter = new Color(25, 28, 32);
            Color borderInner = new Color(70, 75, 85);
            DrawPremiumPanel(spriteBatch, _mainPanel, panelBg, borderOuter, borderInner, globalAlpha, 3);

            // Subtle background blueprint grid
            for (int x = _mainPanel.X; x < _mainPanel.Right; x += 40)
                spriteBatch.Draw(_pixel, new Rectangle(x, _mainPanel.Y, 1, _mainPanel.Height), Fade(Color.White * 0.02f, globalAlpha));
            for (int y = _mainPanel.Y; y < _mainPanel.Bottom; y += 40)
                spriteBatch.Draw(_pixel, new Rectangle(_mainPanel.X, y, _mainPanel.Width, 1), Fade(Color.White * 0.02f, globalAlpha));

            // 2. Top & Bottom Headers
            DrawPremiumPanel(spriteBatch, _topBarRect, new Color(15, 17, 20, 255), borderOuter, new Color(90, 95, 105), globalAlpha, 2);
            DrawPremiumPanel(spriteBatch, _bottomBarRect, new Color(15, 17, 20, 255), borderOuter, new Color(90, 95, 105), globalAlpha, 2);

            if (font != null)
            {
                // Title
                string title = $"{_tree.GetData().ClassId.ToUpper()} TALENT TREE";
                Vector2 titleSize = font.MeasureString(title) * 0.65f;
                spriteBatch.DrawString(font, title, new Vector2(_topBarRect.Center.X - titleSize.X/2, _topBarRect.Center.Y - titleSize.Y/2), Fade(new Color(255, 215, 100), globalAlpha), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

                // Re-designed Available / Pending Block
                string pointsLabel = "AVAILABLE:";
                string pointsVal = $" {_tree.UnspentPoints}";
                float lblScale = 0.4f;
                float valScale = 0.45f;
                
                Vector2 pointsTextPos = new Vector2(_topBarRect.Left + 30, _topBarRect.Center.Y - 15);
                if (_tree.PendingPoints == 0) pointsTextPos.Y = _topBarRect.Center.Y - (font.MeasureString(pointsLabel).Y * lblScale) / 2;
                
                Vector2 lblSize = font.MeasureString(pointsLabel) * lblScale;
                spriteBatch.DrawString(font, pointsLabel, pointsTextPos, Fade(new Color(180, 185, 190), globalAlpha), 0f, Vector2.Zero, lblScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, pointsVal, pointsTextPos + new Vector2(lblSize.X, -2), Fade(Color.White, globalAlpha), 0f, Vector2.Zero, valScale, SpriteEffects.None, 0f);

                if (_tree.PendingPoints > 0)
                {
                    string pendLabel = "PENDING:";
                    string pendVal = $" {_tree.PendingPoints}";
                    float pScale = 0.35f;
                    Vector2 pPos = pointsTextPos + new Vector2(0, 20);
                    
                    spriteBatch.DrawString(font, pendLabel, pPos, Fade(new Color(160, 150, 120), globalAlpha), 0f, Vector2.Zero, pScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, pendVal, pPos + new Vector2(font.MeasureString(pendLabel).X * pScale, 0), Fade(new Color(255, 215, 80), globalAlpha), 0f, Vector2.Zero, pScale, SpriteEffects.None, 0f);
                }

                // Draw Bottom Buttons
                DrawPremiumButton(spriteBatch, font, _btnConfirm, "CONFIRM", new Color(30, 80, 40), new Color(60, 150, 70), _btnConfirmHover, globalAlpha);
                DrawPremiumButton(spriteBatch, font, _btnCancel, "CLOSE", new Color(40, 45, 50), new Color(80, 85, 95), _btnCancelHover, globalAlpha);
                DrawPremiumButton(spriteBatch, font, _btnReset, "RESET", new Color(90, 30, 25), new Color(160, 50, 40), _btnResetHover, globalAlpha);
                
                // Instructions
                string hint = "LMB: Assign   |   RMB: Remove   |   ENTER: Confirm   |   ESC: Close";
                Vector2 hintSize = font.MeasureString(hint) * 0.3f;
                spriteBatch.DrawString(font, hint, new Vector2(_bottomBarRect.Center.X - hintSize.X/2, _bottomBarRect.Center.Y - hintSize.Y/2), Fade(new Color(120, 125, 130), globalAlpha), 0f, Vector2.Zero, 0.3f, SpriteEffects.None, 0f);
            }

            // 3. Draw Connections
            foreach (var conn in _uiConnections)
            {
                Vector2 startPos = conn.ParentNode.Position;
                Vector2 endPos = conn.ChildNode.Position;
                Vector2 dir = endPos - startPos;
                if (dir == Vector2.Zero) continue;
                dir.Normalize();

                float parentRadius = (40 * conn.ParentNode.GetScale()) / 2f;
                float childRadius = (40 * conn.ChildNode.GetScale()) / 2f;

                startPos += dir * parentRadius;
                endPos -= dir * childRadius;

                Vector2 diff = endPos - startPos;
                if (diff.LengthSquared() < 1) continue;

                float angle = (float)Math.Atan2(diff.Y, diff.X);
                float length = diff.Length();
                
                // Brighter engraved line casing
                spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, (int)length, 6), null, Fade(new Color(55, 60, 65), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                // Brighter inner groove
                spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, (int)length, 2), null, Fade(new Color(85, 95, 105), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                
                if (conn.GetFillProgress() > 0)
                {
                    int fillLength = (int)(length * conn.GetFillProgress());
                    
                    // Energy Glow
                    spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, fillLength, 8), null, Fade(_theme.AccentGlowColor * 0.35f, globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                    
                    // Energy Core
                    spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, fillLength, 3), null, Fade(Color.Lerp(_theme.AccentGlowColor, Color.Yellow, 0.5f), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                }
            }

            // 4. Draw Nodes
            foreach (var node in _uiNodes)
            {
                float currentScale = node.GetScale();

                Color nodeBg = new Color(35, 40, 45); // Brighter dark iron background
                Color nodeBorder = node.GetColor(); // Derived from tweened state
                if (node.CurrentState == NodeState.Locked) nodeBorder = new Color(80, 85, 95); // Better contrast for locked nodes
                if (node.HasPendingPoints) nodeBorder = new Color(255, 215, 0); // Gold

                Point pos = node.Position.ToPoint();

                if (node.Data.Type == SkillNodeType.Major)
                {
                    int s = (int)(44 * currentScale);
                    Rectangle borderRect = new Rectangle(pos.X - s/2 - 2, pos.Y - s/2 - 2, s + 4, s + 4);
                    Rectangle bgRect = new Rectangle(pos.X - s/2, pos.Y - s/2, s, s);
                    Rectangle fillRect = new Rectangle(pos.X - s/2 + 2, pos.Y - s/2 + 2, s - 4, s - 4);
                    
                    spriteBatch.Draw(_pixel, borderRect, Fade(nodeBorder, globalAlpha));
                    spriteBatch.Draw(_pixel, bgRect, Fade(nodeBg, globalAlpha));
                    spriteBatch.Draw(_pixel, fillRect, Fade(node.GetColor(), globalAlpha));
                }
                else if (node.Data.Type == SkillNodeType.Minor)
                {
                    int r = (int)(26 * currentScale);
                    DrawFilledDiamond(spriteBatch, _pixel, pos, r + 2, Fade(nodeBorder, globalAlpha));
                    DrawFilledDiamond(spriteBatch, _pixel, pos, r, Fade(nodeBg, globalAlpha));
                    DrawFilledDiamond(spriteBatch, _pixel, pos, r - 2, Fade(node.GetColor(), globalAlpha));
                }
                else // Standard
                {
                    int r = (int)(18 * currentScale);
                    DrawFilledCircle(spriteBatch, _pixel, pos, r + 2, Fade(nodeBorder, globalAlpha));
                    DrawFilledCircle(spriteBatch, _pixel, pos, r, Fade(nodeBg, globalAlpha));
                    DrawFilledCircle(spriteBatch, _pixel, pos, r - 2, Fade(node.GetColor(), globalAlpha));
                }
                
                // Rank Text inside Major Nodes
                if (node.Data.Type == SkillNodeType.Major && font != null)
                {
                    string rankTxt = $"{_tree.GetAllocatedPoints(node.Data.Id, true)}/{node.Data.MaxPoints}";
                    Vector2 rSize = font.MeasureString(rankTxt) * 0.35f;
                    spriteBatch.DrawString(font, rankTxt, new Vector2(pos.X - rSize.X/2, pos.Y - rSize.Y/2 + 1), Fade(Color.White, globalAlpha), 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
                }
            }

            // 5. Draw Hover Tooltip on Top
            if (_hoveredNode != null && font != null)
            {
                int pts = _tree.GetAllocatedPoints(_hoveredNode.Data.Id, true);
                string typeLabel = _hoveredNode.Data.Type.ToString().ToUpper();
                string title = $"{_hoveredNode.Data.Name} ({pts}/{_hoveredNode.Data.MaxPoints})";
                
                List<string> tooltips = new List<string> { title, $"[{typeLabel}]", "---", _hoveredNode.Data.Description };
                
                if (_hoveredNode.Data.Effects.Count > 0)
                {
                    tooltips.Add("---");
                    tooltips.Add("Effects per Rank:");
                    foreach (var eff in _hoveredNode.Data.Effects)
                    {
                        tooltips.Add($"+{eff.ValuePerPoint} {eff.EffectId.Replace("_", " ")}");
                    }
                }
                
                if (_hoveredNode.CurrentState == NodeState.Locked)
                {
                    tooltips.Add("---");
                    tooltips.Add("[LOCKED] Requires active path or sufficient points.");
                }

                float tipScale = 0.4f;
                float smallScale = 0.35f;
                
                float tipWidth = 0;
                float tipHeight = 10;
                
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i] == "---") 
                    {
                        tipHeight += 8;
                        continue;
                    }
                    float s = i == 0 ? tipScale : smallScale;
                    Vector2 sz = font.MeasureString(tooltips[i]) * s;
                    tipWidth = Math.Max(tipWidth, sz.X);
                    tipHeight += sz.Y + 2;
                }
                
                tipWidth += 30;
                tipHeight += 20;

                Rectangle bgTip = new Rectangle((int)mousePos.X + 20, (int)mousePos.Y + 20, (int)tipWidth, (int)tipHeight);
                
                // Clamp to screen bounds
                if (bgTip.Right > viewport.Width) bgTip.X -= (bgTip.Width + 40);
                if (bgTip.Bottom > viewport.Height) bgTip.Y -= (bgTip.Height + 40);
                
                DrawPremiumPanel(spriteBatch, bgTip, new Color(18, 20, 24, 250), new Color(130, 120, 100), new Color(200, 180, 120), globalAlpha, 2);
                
                float currentY = bgTip.Y + 10;
                for (int i = 0; i < tooltips.Count; i++)
                {
                    if (tooltips[i] == "---") 
                    {
                        spriteBatch.Draw(_pixel, new Rectangle(bgTip.X + 15, (int)currentY + 2, bgTip.Width - 30, 1), Fade(new Color(100, 100, 100) * 0.5f, globalAlpha));
                        currentY += 8;
                        continue;
                    }
                    float s = i == 0 ? tipScale : smallScale;
                    Color c = Color.LightGray;
                    if (i == 0) c = new Color(255, 220, 120);
                    else if (i == 1) c = new Color(150, 160, 170);
                    else if (tooltips[i].StartsWith("+")) c = Color.LimeGreen;
                    else if (tooltips[i].StartsWith("[LOCKED]")) c = new Color(255, 80, 80);
                    
                    spriteBatch.DrawString(font, tooltips[i], new Vector2(bgTip.X + 15, currentY), Fade(c, globalAlpha), 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
                    currentY += font.MeasureString(tooltips[i]).Y * s + 2;
                }
            }
        }
        
        private void DrawPremiumButton(SpriteBatch sb, SpriteFont font, Rectangle bounds, string text, Color baseColor, Color highlightColor, float hoverAlpha, float globalAlpha)
        {
            Color currentBg = Color.Lerp(baseColor, highlightColor, hoverAlpha);
            Color currentBorderInner = Color.Lerp(new Color(60, 65, 70), new Color(200, 200, 200), hoverAlpha);
            
            DrawPremiumPanel(sb, bounds, currentBg, new Color(20, 22, 25), currentBorderInner, globalAlpha, 2);
            
            float txtScale = 0.45f;
            Vector2 size = font.MeasureString(text) * txtScale;
            Vector2 pos = new Vector2(bounds.Center.X - size.X/2, bounds.Center.Y - size.Y/2);
            
            sb.DrawString(font, text, pos + new Vector2(1, 1), Fade(Color.Black * 0.8f, globalAlpha), 0f, Vector2.Zero, txtScale, SpriteEffects.None, 0f);
            sb.DrawString(font, text, pos, Fade(Color.White, globalAlpha), 0f, Vector2.Zero, txtScale, SpriteEffects.None, 0f);
        }

        private void DrawFilledCircle(SpriteBatch sb, Texture2D pixel, Point center, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int x = (int)Math.Sqrt((radius * radius) - (y * y));
                sb.Draw(pixel, new Rectangle(center.X - x, center.Y + y, (x * 2) + 1, 1), color);
            }
        }
        
        private void DrawFilledDiamond(SpriteBatch sb, Texture2D pixel, Point center, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int width = radius - Math.Abs(y);
                sb.Draw(pixel, new Rectangle(center.X - width, center.Y + y, (width * 2) + 1, 1), color);
            }
        }
    }
}