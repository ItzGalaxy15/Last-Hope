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
    /// <summary>
    /// Data structure defining the visual aesthetics of a class skill tree.
    /// Source: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes
    /// </summary>
    public class UIThemeData
    {
        public string ThemeId { get; set; }
        
        // Colors
        public Color BackgroundTint { get; set; }
        public Color BorderColor { get; set; }
        public Color AccentGlowColor { get; set; }
        public Color LockedDesaturation { get; set; }
        
        // Assets loaded via content pipeline
        public string BackgroundTexturePath { get; set; }
        public string NodeBorderTexturePath { get; set; }
        public string NodeMaskTexturePath { get; set; }
        
        // Visual effects
        public string ParticleEffectPrefab { get; set; }
        public NodeShape DefaultShape { get; set; }
    }

    /// <summary>
    /// Visual representation and state machine for a single skill node on the canvas.
    /// </summary>
    public class SkillNodeUI
    {
        public SkillNodeData Data { get; private set; }
        public NodeState CurrentState { get; private set; }
        public bool HasPendingPoints { get; set; }
        
        // Transform and layout
        public Vector2 Position { get; set; }
        
        // Tweening targets
        private float _currentScale = 1f;
        private float _targetScale = 1f;
        
        private float _currentFill = 0f;
        private float _targetFill = 0f;
        
        private Color _currentColor;
        private Color _targetColor;
        private float _pulseTimer;

        // References
        private UIThemeData _theme;

        /// <summary>
        /// Initializes a new instance of a visual skill node.
        /// Source: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/constructors
        /// </summary>
        public SkillNodeUI(SkillNodeData data, UIThemeData theme)
        {
            Data = data;
            _theme = theme;
            _currentColor = theme.LockedDesaturation;
        }

        /// <summary>
        /// Updates the internal state and sets up visual tween targets.
        /// Source: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/selection-statements
        /// </summary>
        public void UpdateState(NodeState newState, int allocatedPoints, int maxPoints, bool hasPending)
        {
            // Audio hook: detect state changes
            if (CurrentState != newState && newState == NodeState.Maxed)
            {
                
            }
            else if (_targetFill < (float)allocatedPoints / maxPoints)
            {
                
            }
            else if (CurrentState == NodeState.Locked && newState == NodeState.Available)
            {
                
            }

            CurrentState = newState;
            HasPendingPoints = hasPending;
            _targetFill = maxPoints > 0 ? (float)allocatedPoints / maxPoints : 0f;

            // Set target visuals based on state
            switch (CurrentState)
            {
                case NodeState.Locked:
                    _targetColor = _theme.LockedDesaturation;
                    break;
                case NodeState.Available:
                    // Metallic idle
                    _targetColor = new Color(120, 130, 140); 
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
                // Radiant gold for planned nodes
                _targetColor = new Color(255, 210, 50); 
            }
        }

        /// <summary>
        /// Executes tween animations and hover effects over time.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html
        /// </summary>
        public void UpdateVisuals(GameTime gameTime, bool isHovered)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Hover logic and tooltip activation
            if (isHovered)
            {
                if (_targetScale < 1.15f) 
                {
                    
                }
                _targetScale = 1.15f;
            }
            else
            {
                _targetScale = 1.0f;
            }

            // State based animations
            if (CurrentState == NodeState.Available)
            {
                // Subtle pulsing border glow to attract attention
                _pulseTimer += dt * 4f;
                float pulseAlpha = (float)(Math.Sin(_pulseTimer) + 1f) * 0.5f;
                _targetColor = Color.Lerp(new Color(100, 110, 120), new Color(160, 80, 50), pulseAlpha);
            }

            // Tweening execution
            _currentScale = MathHelper.Lerp(_currentScale, _targetScale, dt * 12f);
            _currentFill = MathHelper.Lerp(_currentFill, _targetFill, dt * 6f);
            
            // Safe color lerp
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

    /// <summary>
    /// Visual representation of the line connecting two skill nodes.
    /// </summary>
    public class SkillConnectionUI
    {
        public SkillNodeUI ParentNode;
        public SkillNodeUI ChildNode;
        
        private float _fillProgress = 0f;

        /// <summary>
        /// Updates the connection fill progress based on parent allocation.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // If parent has allocated points and child is unlocked, animate the connection line filling up
            bool isRouteActive = (ParentNode.CurrentState == NodeState.Maxed || ParentNode.CurrentState == NodeState.Partial) && ChildNode.CurrentState != NodeState.Locked;
            
            float targetFill = isRouteActive ? 1f : 0f;
            _fillProgress = MathHelper.Lerp(_fillProgress, targetFill, dt * 4f);
        }

        public float GetFillProgress() => _fillProgress;
    }

    /// <summary>
    /// Dynamic layout engine responsible for generating the skill tree grid.
    /// </summary>
    public class DynamicSkillTreeLayout
    {
        public NodeShape GlobalShapeOverride { get; private set; }

        /// <summary>
        /// Swaps the visual shape archetype used by nodes for layout debugging.
        /// </summary>
        public void ToggleShapeDebug()
        {
            GlobalShapeOverride = GlobalShapeOverride == NodeShape.Circle ? NodeShape.Square : NodeShape.Circle;
        }

        /// <summary>
        /// Dynamically calculates vertical and horizontal spacing to symmetrically center layers.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable
        /// </summary>
        public void GenerateLayout(List<SkillNodeUI> uiNodes, Rectangle availableScreenArea)
        {
            if (uiNodes.Count == 0) return;

            // Determine tree depth
            int maxLayer = uiNodes.Max(n => n.Data.Layer);
            int layerCount = maxLayer + 1;

            // Fixed spacing for a tighter centered UI
            float verticalSpacing = 160f;
            float totalHeight = (layerCount - 1) * verticalSpacing;
            float startY = availableScreenArea.Center.Y - (totalHeight / 2f);

            // Process layers for horizontal centering
            for (int l = 0; l <= maxLayer; l++)
            {
                // Grab all nodes in this layer and sort them conceptually by grid x value
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

    /// <summary>
    /// Master UI element holding the full skill tree canvas, managing navigation and rendering.
    /// </summary>
    public class SkillTreeMenuCanvas : UIElement
    {
        private readonly BaseSkillTree _tree;
        private readonly UIThemeData _theme;
        private readonly Texture2D _pixel;
        
        private readonly List<SkillNodeUI> _uiNodes = new List<SkillNodeUI>();
        private readonly List<SkillConnectionUI> _uiConnections = new List<SkillConnectionUI>();
        private readonly DynamicSkillTreeLayout _layout = new DynamicSkillTreeLayout();

        // UI panel and controls
        private Rectangle _mainPanel;
        private Rectangle _topBarRect;
        private Rectangle _bottomBarRect;
        
        // Buttons
        private Rectangle _btnConfirm;
        private Rectangle _btnReset;
        private Rectangle _btnCancel;
        public bool IsClosed { get; private set; }

        /// <summary>
        /// Directly awards a sandbox progression talent point to the model.
        /// </summary>
        public void AddTalentPoint() => _tree.AddUnspentPoint();

        // Keyboard navigation state
        private bool _isKeyboardMode = false;
        private SkillNodeUI _selectedNode;
        private Vector2 _lastMousePosition;
        private float _kbInputCooldown = 0f;

        // Animations
        private float _entranceProgress = 0f;
        private float _btnConfirmHover = 0f;
        private float _btnCancelHover = 0f;
        private float _btnResetHover = 0f;

        private SkillNodeUI _hoveredNode;
        private int _layoutViewportW;
        private int _layoutViewportH;

        /// <summary>
        /// Constructs the menu canvas and initializes node layout templates.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.first
        /// </summary>
        public SkillTreeMenuCanvas(BaseSkillTree tree, UIThemeData theme, Texture2D pixel, Viewport viewport)
        {
            _tree = tree;
            _theme = theme;
            _pixel = pixel;
            _lastMousePosition = Mouse.GetState().Position.ToVector2();

            // Instantiate UI nodes
            foreach (var nodeData in tree.GetData().Nodes)
            {
                _uiNodes.Add(new SkillNodeUI(nodeData, theme));
            }

            // Instantiate UI connections
            foreach (var connData in tree.GetData().Connections)
            {
                var parent = _uiNodes.First(n => n.Data.Id == connData.FromNodeId);
                var child = _uiNodes.First(n => n.Data.Id == connData.ToNodeId);
                _uiConnections.Add(new SkillConnectionUI { ParentNode = parent, ChildNode = child });
            }

            ApplyViewportLayout(viewport);
        }

        /// <summary>
        /// Adjusts the internal bounding boxes when the screen resolution changes.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Viewport.html
        /// </summary>
        private void ApplyViewportLayout(Viewport viewport)
        {
            _layoutViewportW = viewport.Width;
            _layoutViewportH = viewport.Height;

            int panelWidth = (int)(viewport.Width * 0.8f);
            int panelHeight = (int)(viewport.Height * 0.8f);
            _mainPanel = new Rectangle(
                (viewport.Width - panelWidth) / 2,
                (viewport.Height - panelHeight) / 2,
                panelWidth,
                panelHeight
            );

            _topBarRect = new Rectangle(_mainPanel.X, _mainPanel.Y, _mainPanel.Width, 75);
            _bottomBarRect = new Rectangle(_mainPanel.X, _mainPanel.Bottom - 92, _mainPanel.Width, 92);

            int btnWidth = 160;
            int btnHeight = 34;
            int btnY = _bottomBarRect.Center.Y - (btnHeight / 2);

            _btnCancel = new Rectangle(_bottomBarRect.Right - btnWidth - 30, btnY, btnWidth, btnHeight);
            _btnConfirm = new Rectangle(_btnCancel.Left - btnWidth - 15, btnY, btnWidth, btnHeight);
            _btnReset = new Rectangle(_bottomBarRect.Left + 30, btnY, btnWidth, btnHeight);

            Rectangle treeArea = new Rectangle(_mainPanel.X, _topBarRect.Bottom, _mainPanel.Width, _bottomBarRect.Top - _topBarRect.Bottom);
            _layout.GenerateLayout(_uiNodes, treeArea);
        }

        /// <summary>
        /// Updates canvas logic, input tracking systems, and triggers UI tween updates.
        /// </summary>
        public override void Update(GameTime gameTime, Viewport viewport)
        {
            if (viewport.Width != _layoutViewportW || viewport.Height != _layoutViewportH)
                ApplyViewportLayout(viewport);

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = GameManager.GetGameManager().InputManager;
            Vector2 mousePos = input.CurrentMouseState.Position.ToVector2();
            bool isLeftClickPressed = input.LeftMousePress();
            bool isRightClickPressed = input.RightMousePress();

            // Detect mouse movement
            if (Vector2.DistanceSquared(mousePos, _lastMousePosition) > 1f)
            {
                _isKeyboardMode = false;
                _lastMousePosition = mousePos;
            }

            IsClosed = false;

            // Transitions
            _entranceProgress = MathHelper.Clamp(_entranceProgress + dt * 6f, 0f, 1f);
            _btnConfirmHover = MathHelper.Lerp(_btnConfirmHover, _btnConfirm.Contains(mousePos) ? 1f : 0f, dt * 12f);
            _btnCancelHover = MathHelper.Lerp(_btnCancelHover, _btnCancel.Contains(mousePos) ? 1f : 0f, dt * 12f);
            _btnResetHover = MathHelper.Lerp(_btnResetHover, _btnReset.Contains(mousePos) ? 1f : 0f, dt * 12f);

            // Keyboard shortcuts
            if (input.IsKeyPress(Keys.Escape))
            {
                IsClosed = true;
                if (_tree.PendingPoints > 0)
                {
                    // Auto save for ease of use
                    _tree.ConfirmPendingPoints(); 
                }
                return;
            }
            if (input.IsKeyPress(Keys.R))
            {
                _tree.Respec();
            }
            if (input.IsKeyPress(Keys.Enter) || input.IsKeyPress(Keys.Space))
            {
                _tree.ConfirmPendingPoints();
            }

            // Keyboard navigation
            Vector2 navDir = Vector2.Zero;
            if (input.IsKeyPress(Keys.W) || input.IsKeyPress(Keys.Up)) navDir = new Vector2(0, -1);
            if (input.IsKeyPress(Keys.S) || input.IsKeyPress(Keys.Down)) navDir = new Vector2(0, 1);
            if (input.IsKeyPress(Keys.A) || input.IsKeyPress(Keys.Left)) navDir = new Vector2(-1, 0);
            if (input.IsKeyPress(Keys.D) || input.IsKeyPress(Keys.Right)) navDir = new Vector2(1, 0);

            if (navDir != Vector2.Zero && _uiNodes.Count > 0)
            {
                _isKeyboardMode = true;
                if (_selectedNode == null)
                    _selectedNode = _uiNodes.FirstOrDefault(n => n.Data.Layer == 0) ?? _uiNodes[0];
                else
                {
                    SkillNodeUI bestNode = null;
                    float bestDist = float.MaxValue;
                    foreach(var node in _uiNodes)
                    {
                        if (node == _selectedNode) continue;
                        Vector2 diff = node.Position - _selectedNode.Position;
                        if (diff.LengthSquared() < 1) continue;
                        diff.Normalize();
                        
                        // Check if node is in the general direction
                        if (Vector2.Dot(diff, navDir) > 0.4f)
                        {
                            float dist = Vector2.Distance(node.Position, _selectedNode.Position);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestNode = node;
                            }
                        }
                    }
                    if (bestNode != null) _selectedNode = bestNode;
                }
            }

            // Bottom bar button clicks
            if (isLeftClickPressed)
            {
                if (_btnCancel.Contains(mousePos)) 
                { 
                    if (_tree.PendingPoints > 0)
                    {
                        // Auto save for ease of use
                        _tree.ConfirmPendingPoints(); 
                    }
                    IsClosed = true; 
                    return; 
                }
                if (_btnReset.Contains(mousePos)) { _tree.Respec(); return; }
                if (_btnConfirm.Contains(mousePos)) { _tree.ConfirmPendingPoints(); return; }
            }

            _hoveredNode = null;
            foreach (var node in _uiNodes)
            {
                // Sync backend state into UI presentation
                NodeState state = _tree.GetNodeState(node.Data.Id, true);
                int pts = _tree.GetAllocatedPoints(node.Data.Id, true);
                bool hasPending = _tree.GetAllocatedPoints(node.Data.Id, true) > _tree.GetAllocatedPoints(node.Data.Id, false);
                
                node.UpdateState(state, pts, node.Data.MaxPoints, hasPending);
                
                // Set hover check
                bool isHovered = false;
                if (_isKeyboardMode)
                {
                    isHovered = (_selectedNode == node);
                }
                else
                {
                    isHovered = Vector2.Distance(mousePos, node.Position) <= 25f;
                }

                if (isHovered) 
                {
                    _hoveredNode = node;

                    bool assignInput = (!_isKeyboardMode && isLeftClickPressed) || (_isKeyboardMode && input.IsKeyPress(Keys.E));
                    bool removeInput = (!_isKeyboardMode && isRightClickPressed) || (_isKeyboardMode && input.IsKeyPress(Keys.Q));

                    if (assignInput && state != NodeState.Locked && pts < node.Data.MaxPoints)
                    {
                        _tree.AddPendingPoint(node.Data.Id);
                    }
                    else if (removeInput && hasPending)
                    {
                        _tree.RemovePendingPoint(node.Data.Id);
                    }
                }

                node.UpdateVisuals(gameTime, isHovered);
            }

            foreach (var conn in _uiConnections)
                conn.Update(gameTime);
        }

        /// <summary>
        /// Multiplies a drawing color parameter structure with a transparency parameter.
        /// </summary>
        private Color Fade(Color c, float a) => c * a;

        /// <summary>
        /// Handles complex canvas layout container styling and draw passes.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
        /// </summary>
        private void DrawPremiumPanel(SpriteBatch spriteBatch, Rectangle bounds, Color bg, Color borderOuter, Color borderInner, float alpha, int outerThick = 2)
        {
            // Drop shadow
            spriteBatch.Draw(_pixel, new Rectangle(bounds.X + 8, bounds.Y + 8, bounds.Width, bounds.Height), Fade(Color.Black * 0.5f, alpha));
            
            // Main fill
            spriteBatch.Draw(_pixel, bounds, Fade(bg, alpha));
            
            // Outer border
            DrawRectangleOutline(spriteBatch, bounds, outerThick, Fade(borderOuter, alpha));
            
            // Inner highlight
            DrawRectangleOutline(spriteBatch, new Rectangle(bounds.X + outerThick, bounds.Y + outerThick, bounds.Width - (outerThick*2), bounds.Height - (outerThick*2)), 1, Fade(borderInner, alpha));
        }

        /// <summary>
        /// Loops out primitive vector paths representing a rectangle boundary.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
        /// </summary>
        private void DrawRectangleOutline(SpriteBatch sb, Rectangle rect, int t, Color c)
        {
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, t), c);
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom - t, rect.Width, t), c);
            sb.Draw(_pixel, new Rectangle(rect.Left, rect.Top, t, rect.Height), c);
            sb.Draw(_pixel, new Rectangle(rect.Right - t, rect.Top, t, rect.Height), c);
        }

        /// <summary>
        /// Standard render sweep processing nodes, interactive wires, headers and context panels.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
        /// </summary>
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            SpriteFont font = GameManager.GetGameManager()._font;
            Vector2 mousePos = GameManager.GetGameManager().InputManager.CurrentMouseState.Position.ToVector2();
            Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
            
            float globalAlpha = _entranceProgress;

            // Draw main panel
            Color panelBg = new Color(20, 22, 26, 245);
            Color borderOuter = new Color(25, 28, 32);
            Color borderInner = new Color(70, 75, 85);
            DrawPremiumPanel(spriteBatch, _mainPanel, panelBg, borderOuter, borderInner, globalAlpha, 3);

            // Subtle background blueprint grid
            for (int x = _mainPanel.X; x < _mainPanel.Right; x += 40)
                spriteBatch.Draw(_pixel, new Rectangle(x, _mainPanel.Y, 1, _mainPanel.Height), Fade(Color.White * 0.02f, globalAlpha));
            for (int y = _mainPanel.Y; y < _mainPanel.Bottom; y += 40)
                spriteBatch.Draw(_pixel, new Rectangle(_mainPanel.X, y, _mainPanel.Width, 1), Fade(Color.White * 0.02f, globalAlpha));

            // Top and bottom headers
            DrawPremiumPanel(spriteBatch, _topBarRect, new Color(15, 17, 20, 255), borderOuter, new Color(90, 95, 105), globalAlpha, 2);
            DrawPremiumPanel(spriteBatch, _bottomBarRect, new Color(15, 17, 20, 255), borderOuter, new Color(90, 95, 105), globalAlpha, 2);

            if (font != null)
            {
                // Title
                string title = $"{_tree.GetData().ClassId.ToUpper()} TALENT TREE";
                Vector2 titleSize = font.MeasureString(title) * 0.65f;
                spriteBatch.DrawString(font, title, new Vector2(_topBarRect.Center.X - titleSize.X/2, _topBarRect.Center.Y - titleSize.Y/2), Fade(new Color(255, 215, 100), globalAlpha), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);

                // Available and pending block
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

                // Draw bottom buttons
                float pulse = 0f;
                if (_tree.PendingPoints > 0)
                {
                    pulse = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 4.0) * 0.5f + 0.5f; 
                }

                Color confirmBase = Color.Lerp(new Color(30, 80, 40), new Color(100, 160, 50), pulse);
                Color confirmHigh = Color.Lerp(new Color(60, 150, 70), new Color(150, 255, 80), pulse);
                
                DrawPremiumButton(spriteBatch, font, _btnConfirm, "CONFIRM", confirmBase, confirmHigh, _btnConfirmHover, globalAlpha, 0.30f);
                DrawPremiumButton(spriteBatch, font, _btnCancel, "SAVE & CLOSE", new Color(40, 45, 50), new Color(80, 85, 95), _btnCancelHover, globalAlpha, 0.26f);
                DrawPremiumButton(spriteBatch, font, _btnReset, "RESET", new Color(90, 30, 25), new Color(160, 50, 40), _btnResetHover, globalAlpha, 0.30f);

                string hint = _isKeyboardMode
                    ? "E Assign | Q Remove | Enter/Space Confirm | Esc Save & Close"
                    : "LMB Assign | RMB Remove | Enter/Space Confirm | Esc Save & Close";
                float hintScale = 0.22f;
                Vector2 hintSize = font.MeasureString(hint) * hintScale;
                spriteBatch.DrawString(font, hint, new Vector2(_bottomBarRect.Center.X - hintSize.X / 2f, _bottomBarRect.Center.Y - hintSize.Y / 2f), Fade(new Color(130, 135, 145), globalAlpha), 0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
            }

            // Draw connections
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
                
                spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, (int)length, 6), null, Fade(new Color(55, 60, 65), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, (int)length, 2), null, Fade(new Color(85, 95, 105), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                
                if (conn.GetFillProgress() > 0)
                {
                    int fillLength = (int)(length * conn.GetFillProgress());
                    spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, fillLength, 8), null, Fade(_theme.AccentGlowColor * 0.35f, globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                    spriteBatch.Draw(_pixel, new Rectangle((int)startPos.X, (int)startPos.Y, fillLength, 3), null, Fade(Color.Lerp(_theme.AccentGlowColor, Color.Yellow, 0.5f), globalAlpha), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                }
            }

            // Draw nodes
            foreach (var node in _uiNodes)
            {
                float currentScale = node.GetScale();
                Color nodeBg = new Color(35, 40, 45); 
                Color nodeBorder = node.GetColor(); 
                
                if (node.CurrentState == NodeState.Locked) nodeBorder = new Color(80, 85, 95); 
                if (node.HasPendingPoints) nodeBorder = new Color(255, 215, 0); 

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
                else
                {
                    int r = (int)(18 * currentScale);
                    DrawFilledCircle(spriteBatch, _pixel, pos, r + 2, Fade(nodeBorder, globalAlpha));
                    DrawFilledCircle(spriteBatch, _pixel, pos, r, Fade(nodeBg, globalAlpha));
                    DrawFilledCircle(spriteBatch, _pixel, pos, r - 2, Fade(node.GetColor(), globalAlpha));
                }
                
                if (node.Data.Type == SkillNodeType.Major && font != null)
                {
                    string rankTxt = $"{_tree.GetAllocatedPoints(node.Data.Id, true)}/{node.Data.MaxPoints}";
                    Vector2 rSize = font.MeasureString(rankTxt) * 0.35f;
                    spriteBatch.DrawString(font, rankTxt, new Vector2(pos.X - rSize.X/2, pos.Y - rSize.Y/2 + 1), Fade(Color.White, globalAlpha), 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
                }

                if (_isKeyboardMode && _selectedNode == node)
                {
                    int selSize = (int)(60 * currentScale);
                    Rectangle selBox = new Rectangle(pos.X - selSize/2, pos.Y - selSize/2, selSize, selSize);
                    float pulse = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 6) * 0.5 + 0.5);
                    Color selColor = Color.Lerp(Color.LightYellow, Color.Goldenrod, pulse);
                    DrawRectangleOutline(spriteBatch, selBox, 3, Fade(selColor, globalAlpha));
                }
            }

            // Draw hover tooltip on top
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
                    var missing = _tree.GetUnlockMissingRequirements(_hoveredNode.Data.Id, true);
                    foreach(var m in missing) {
                        tooltips.Add("[LOCKED] " + m);
                    }
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

                Vector2 tooltipBasePos;
                if (_isKeyboardMode)
                {
                    tooltipBasePos = new Vector2(_hoveredNode.Position.X + 60, _hoveredNode.Position.Y - tipHeight / 2);
                }
                else
                {
                    tooltipBasePos = new Vector2(mousePos.X + 20, mousePos.Y + 20);
                }

                Rectangle bgTip = new Rectangle((int)tooltipBasePos.X, (int)tooltipBasePos.Y, (int)tipWidth, (int)tipHeight);
                
                if (bgTip.Right > viewport.Width) 
                {
                    bgTip.X = _isKeyboardMode 
                        ? (int)(_hoveredNode.Position.X - bgTip.Width - 60) 
                        : (int)(mousePos.X - bgTip.Width - 20);
                }
                if (bgTip.Bottom > viewport.Height) bgTip.Y -= (int)(bgTip.Bottom - viewport.Height + 10);
                if (bgTip.Y < 0) bgTip.Y = 10;
                
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
        
        /// <summary>
        /// Combines layout calculation boundaries with localized sprite text to project buttons.
        /// </summary>
        private void DrawPremiumButton(SpriteBatch sb, SpriteFont font, Rectangle bounds, string text, Color baseColor, Color highlightColor, float hoverAlpha, float globalAlpha, float txtScale = 0.45f)
        {
            Color currentBg = Color.Lerp(baseColor, highlightColor, hoverAlpha);
            Color currentBorderInner = Color.Lerp(new Color(60, 65, 70), new Color(200, 200, 200), hoverAlpha);
            
            DrawPremiumPanel(sb, bounds, currentBg, new Color(20, 22, 25), currentBorderInner, globalAlpha, 2);
            
            Vector2 size = font.MeasureString(text) * txtScale;
            Vector2 pos = new Vector2(bounds.Center.X - size.X/2, bounds.Center.Y - size.Y/2);
            
            sb.DrawString(font, text, pos + new Vector2(1, 1), Fade(Color.Black * 0.8f, globalAlpha), 0f, Vector2.Zero, txtScale, SpriteEffects.None, 0f);
            sb.DrawString(font, text, pos, Fade(Color.White, globalAlpha), 0f, Vector2.Zero, txtScale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Loops out structural lines around a center node vector to build out rasterized circles.
        /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
        /// </summary>
        private void DrawFilledCircle(SpriteBatch sb, Texture2D pixel, Point center, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int x = (int)Math.Sqrt((radius * radius) - (y * y));
                sb.Draw(pixel, new Rectangle(center.X - x, center.Y + y, (x * 2) + 1, 1), color);
            }
        }
        
        /// <summary>
        /// Maps diamond layouts by linearly clamping tracking line segments relative to offsets.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.math.abs
        /// </summary>
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