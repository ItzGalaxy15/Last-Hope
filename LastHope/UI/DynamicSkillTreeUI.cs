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
        public float HoldProgress { get; set; } = 0f;

        public SkillNodeUI(SkillNodeData data, UIThemeData theme)
        {
            Data = data;
            _theme = theme;
            _currentColor = theme.LockedDesaturation;
        }

        public void UpdateState(NodeState newState, int allocatedPoints, int maxPoints)
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
            _targetFill = maxPoints > 0 ? (float)allocatedPoints / maxPoints : 0f;

            // Set Target Visuals based on state
            switch (CurrentState)
            {
                case NodeState.Locked:
                    _targetColor = _theme.LockedDesaturation;
                    break;
                case NodeState.Available:
                    _targetColor = Color.White; // Normal saturation
                    break;
                case NodeState.Partial:
                    _targetColor = Color.Lerp(Color.White, _theme.AccentGlowColor, _targetFill);
                    break;
                case NodeState.Maxed:
                    _targetColor = _theme.AccentGlowColor;
                    break;
            }
        }

        public void UpdateVisuals(GameTime gameTime, bool isHovered)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // 1. Hover Logic & Tooltip Activation
            if (isHovered)
            {
                if (_targetScale < 1.1f) 
                {
                    // [AUDIO HOOK]: AudioManager.PlaySound("UI_Hover_Soft");
                }
                _targetScale = 1.1f;
                
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
                _pulseTimer += dt * 3f;
                float pulseAlpha = (float)(Math.Sin(_pulseTimer) + 1f) * 0.5f;
                _targetColor = Color.Lerp(Color.White, _theme.AccentGlowColor * 0.5f, pulseAlpha);
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
            
            // If parent is maxed and child is available/unlocked, animate the connection line filling up
            bool isRouteActive = ParentNode.CurrentState == NodeState.Maxed && ChildNode.CurrentState != NodeState.Locked;
            
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

        private SkillNodeUI _hoveredNode;
        private const float RequiredHoldTime = 0.5f; // Hold click for half a second to buy

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
            Rectangle usableScreen = new Rectangle(0, 0, viewport.Width, viewport.Height);
            _layout.GenerateLayout(_uiNodes, usableScreen);
            
            // [DEBUG STEP 3]: Coordinate System Check
            if (_uiNodes.Count > 0)
            {
                System.Console.WriteLine($"[SkillTree UI] Canvas Center: {usableScreen.Center.X}, {usableScreen.Center.Y}");
                System.Console.WriteLine($"[SkillTree UI] First Node Generated at Absolute Position: {_uiNodes[0].Position.X}, {_uiNodes[0].Position.Y}");
            }
        }

        public override void Update(GameTime gameTime, Viewport viewport)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var input = GameManager.GetGameManager().InputManager;
            Vector2 mousePos = input.CurrentMouseState.Position.ToVector2();
            bool isLeftClickHeld = input.CurrentMouseState.LeftButton == ButtonState.Pressed;

            // Check 'R' for Respec
            if (input.IsKeyPress(Keys.R))
            {
                _tree.Respec();
            }

            _hoveredNode = null;
            foreach (var node in _uiNodes)
            {
                // Sync backend state into UI presentation
                NodeState state = _tree.GetNodeState(node.Data.Id);
                int pts = _tree.GetAllocatedPoints(node.Data.Id);
                node.UpdateState(state, pts, node.Data.MaxPoints);
                
                bool isHovered = Vector2.Distance(mousePos, node.Position) <= 30f;
                if (isHovered) _hoveredNode = node;

                // Hold-to-Purchase logic
                if (isHovered && state != NodeState.Locked && pts < node.Data.MaxPoints)
                {
                    if (isLeftClickHeld)
                    {
                        node.HoldProgress += dt / RequiredHoldTime;
                        if (node.HoldProgress >= 1f)
                        {
                            _tree.AllocatePoint(node.Data.Id);
                            node.HoldProgress = 0f; // Reset after purchase
                        }
                    }
                    else node.HoldProgress = 0f;
                }
                else node.HoldProgress = 0f;

                node.UpdateVisuals(gameTime, isHovered);
            }

            foreach (var conn in _uiConnections)
                conn.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            SpriteFont font = GameManager.GetGameManager()._font;

            // Draw Info Text
            if (font != null)
            {
                float infoScale = 0.4f;
                spriteBatch.DrawString(font, $"Points Available: {_tree.UnspentPoints}", new Vector2(20, 20), Color.Gold, 0f, Vector2.Zero, infoScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, "Hold Left Click to Unlock", new Vector2(20, 45), Color.LightGray, 0f, Vector2.Zero, infoScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, "Press [R] to Respec Points", new Vector2(20, 70), Color.LightGray, 0f, Vector2.Zero, infoScale, SpriteEffects.None, 0f);
            }

            // 1. Draw Connections
            foreach (var conn in _uiConnections)
            {
                Vector2 diff = conn.ChildNode.Position - conn.ParentNode.Position;
                float angle = (float)Math.Atan2(diff.Y, diff.X);
                float length = diff.Length();
                
                // Draw Base Line
                spriteBatch.Draw(_pixel, new Rectangle((int)conn.ParentNode.Position.X, (int)conn.ParentNode.Position.Y, (int)length, 4), null, new Color(40, 40, 40), angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                
                // Draw Fill Progress
                if (conn.GetFillProgress() > 0)
                {
                    int fillLength = (int)(length * conn.GetFillProgress());
                    spriteBatch.Draw(_pixel, new Rectangle((int)conn.ParentNode.Position.X, (int)conn.ParentNode.Position.Y, fillLength, 4), null, _theme.AccentGlowColor, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
                }
            }

            // 2. Draw Nodes
            foreach (var node in _uiNodes)
            {
                float currentScale = node.GetScale();
                int size = (int)(40 * currentScale);
                Rectangle rect = new Rectangle((int)node.Position.X - size / 2, (int)node.Position.Y - size / 2, size, size);
                
                spriteBatch.Draw(_pixel, rect, node.GetColor());

                // Draw Hold-to-Purchase ring
                if (node.HoldProgress > 0)
                {
                    int fillHeight = (int)(size * node.HoldProgress);
                    Rectangle fillRect = new Rectangle(rect.Left - 5, rect.Bottom - fillHeight, 5, fillHeight);
                    spriteBatch.Draw(_pixel, fillRect, Color.Gold);
                }
            }

            // 3. Draw Hover Tooltip on Top
            if (_hoveredNode != null && font != null)
            {
                Vector2 mousePos = GameManager.GetGameManager().InputManager.CurrentMouseState.Position.ToVector2();
                string title = $"{_hoveredNode.Data.Name} ({_tree.GetAllocatedPoints(_hoveredNode.Data.Id)}/{_hoveredNode.Data.MaxPoints})";
                
                float tipScale = 0.4f;
                Vector2 titleSize = font.MeasureString(title) * tipScale;
                Vector2 descSize = font.MeasureString(_hoveredNode.Data.Description) * 0.35f;
                
                int tipWidth = (int)Math.Max(titleSize.X, descSize.X) + 20;
                int tipHeight = (int)(titleSize.Y + descSize.Y) + 25;
                
                Rectangle bgTip = new Rectangle((int)mousePos.X + 20, (int)mousePos.Y + 20, tipWidth, tipHeight);
                spriteBatch.Draw(_pixel, bgTip, new Color(20, 22, 25, 245));
                
                spriteBatch.DrawString(font, title, new Vector2(bgTip.X + 10, bgTip.Y + 10), Color.Gold, 0f, Vector2.Zero, tipScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, _hoveredNode.Data.Description, new Vector2(bgTip.X + 10, bgTip.Y + 10 + titleSize.Y + 5), Color.LightGray, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 0f);
            }
        }
    }
}