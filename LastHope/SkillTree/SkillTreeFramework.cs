using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.SkillTree
{
    // ==========================================
    // 1. ENUMS & CONSTANTS
    // ==========================================
    public enum SkillNodeType { Stat = 0, Modifier = 1, Ultimate = 2 }
    public enum NodeShape { Circle, Square }
    public enum NodeState { Locked, Available, Partial, Maxed }

    // ==========================================
    // 2. DATA LAYER (Loaded from JSON)
    // ==========================================
    
    public class NodeEffect
    {
        public string EffectId { get; set; } // e.g., "base_damage", "unlock_whirlwind"
        public float ValuePerPoint { get; set; } // e.g., 5.0f for +5 Damage per point
    }

    public class SkillNodeData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxPoints { get; set; }
        public SkillNodeType Type { get; set; }
        public int Layer { get; set; }
        
        // Thematic & Metadata
        public string Rarity { get; set; }
        public string IconId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        
        // Logic Dependencies
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<NodeEffect> Effects { get; set; } = new List<NodeEffect>();
        
        // UI/Controller Grid Coordinates for spatial navigation (e.g. X:0 is left, X:1 is mid, X:2 is right)
        public int GridX { get; set; }
        public int GridY { get; set; }
    }

    public class SkillConnectionData
    {
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
    }

    public class SkillTreeTheme
    {
        // Hex colors representing the thematic vibe (e.g., Warrior = Red/Steel)
        public string PrimaryColorHex { get; set; } 
        public string SecondaryColorHex { get; set; }
        public NodeShape DefaultShape { get; set; }
    }

    public class ClassSkillTreeData
    {
        public string ClassId { get; set; }
        public SkillTreeTheme Theme { get; set; }
        // Key = Layer Index, Value = Total Points Spent required to unlock this layer
        public Dictionary<int, int> LayerUnlockRequirements { get; set; } = new Dictionary<int, int>();
        public List<SkillNodeData> Nodes { get; set; } = new List<SkillNodeData>();
        public List<SkillConnectionData> Connections { get; set; } = new List<SkillConnectionData>();
    }

    // ==========================================
    // 3. STATE LAYER (Player's current progress)
    // ==========================================

    /// <summary>
    /// Represents the player's mutable state for a skill tree. 
    /// This is strictly separated from the configuration data so it can be serialized easily.
    /// </summary>
    public class SkillTreeState
    {
        public string ClassId { get; set; }
        public int TotalPointsSpent { get; set; }
        public int UnspentSkillPoints { get; set; }
        
        // Maps NodeId -> Points Allocated
        public Dictionary<string, int> AllocatedNodes { get; set; } = new Dictionary<string, int>();
    }

    // ==========================================
    // 4. LOGIC LAYER (Validation & Allocation)
    // ==========================================

    public class BaseSkillTree
    {
        private readonly ClassSkillTreeData _data;
        private SkillTreeState _state;
        
        // Fast lookup cache
        private readonly Dictionary<string, SkillNodeData> _nodeMap;

        // --- The Stats Bus ---
        public event Action<NodeEffect> OnEffectApplied;
        public event Action OnTreeRespec;
        public int UnspentPoints => _state.UnspentSkillPoints;

        public BaseSkillTree(ClassSkillTreeData data, SkillTreeState state)
        {
            _data = data;
            _state = state;
            _nodeMap = _data.Nodes.ToDictionary(n => n.Id);
        }

        public NodeState GetNodeState(string nodeId)
        {
            var nodeData = _nodeMap[nodeId];
            int allocated = GetAllocatedPoints(nodeId);

            if (allocated >= nodeData.MaxPoints) return NodeState.Maxed;
            if (allocated > 0) return NodeState.Partial;
            return CanUnlockNode(nodeId) ? NodeState.Available : NodeState.Locked;
        }

        public int GetAllocatedPoints(string nodeId)
        {
            return _state.AllocatedNodes.TryGetValue(nodeId, out int pts) ? pts : 0;
        }

        public bool CanUnlockNode(string nodeId)
        {
            var node = _nodeMap[nodeId];

            // Check 1: Do we have enough total points to access this layer?
            if (_data.LayerUnlockRequirements.TryGetValue(node.Layer, out int reqPoints))
            {
                if (_state.TotalPointsSpent < reqPoints) return false;
            }

            // Check 2: Are all prerequisite nodes MAXED out?
            foreach (var depId in node.Dependencies)
            {
                var depNode = _nodeMap[depId];
                if (GetAllocatedPoints(depId) < depNode.MaxPoints)
                {
                    return false;
                }
            }

            return true;
        }

        public bool AllocatePoint(string nodeId)
        {
            if (_state.UnspentSkillPoints <= 0) return false;
            if (!CanUnlockNode(nodeId)) return false;
            
            var node = _nodeMap[nodeId];
            int currentPts = GetAllocatedPoints(nodeId);
            
            if (currentPts >= node.MaxPoints) return false; // Already maxed

            // Apply allocation
            _state.AllocatedNodes[nodeId] = currentPts + 1;
            _state.TotalPointsSpent++;
            _state.UnspentSkillPoints--;
            
            // Fire events to tell the Player to update their runtime stats
            foreach(var effect in node.Effects)
                OnEffectApplied?.Invoke(effect);

            // Auto-save whenever a point is allocated
            SkillTreeSaveManager.Save(_state);
            return true;
        }

        public void Respec()
        {
            // Refund points and wipe dictionary for easy recalculation
            _state.UnspentSkillPoints += _state.TotalPointsSpent;
            _state.TotalPointsSpent = 0;
            _state.AllocatedNodes.Clear();
            
            // Fire event to strip applied NodeEffects from player
            OnTreeRespec?.Invoke();
            
            // Auto-save respec
            SkillTreeSaveManager.Save(_state);
        }
        
        public ClassSkillTreeData GetData() => _data;
    }

    // ==========================================
    // 5. PRESENTATION LAYER (UI Renderer Stub)
    // ==========================================
    
    public class SkillTreeUIRenderer
    {
        private BaseSkillTree _activeTree;
        private NodeShape _currentShapeOverride;

        public void Initialize(BaseSkillTree tree)
        {
            _activeTree = tree;
            _currentShapeOverride = tree.GetData().Theme.DefaultShape;
        }

        public void ToggleNodeShape()
        {
            _currentShapeOverride = _currentShapeOverride == NodeShape.Circle ? NodeShape.Square : NodeShape.Circle;
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_activeTree == null) return;
            
            var data = _activeTree.GetData();

            // 1. Draw Connections (Lines)
            foreach (var conn in data.Connections)
            {
                var fromState = _activeTree.GetNodeState(conn.FromNodeId);
                var toState = _activeTree.GetNodeState(conn.ToNodeId);
                
                bool isPathActive = fromState == NodeState.Maxed && toState != NodeState.Locked;
                Color lineColor = isPathActive ? ParseHex(data.Theme.SecondaryColorHex) : Color.DarkGray * 0.5f;
                
                // TODO: Draw line between node UI coordinates with `lineColor`
            }

            // 2. Draw Nodes
            foreach (var node in data.Nodes)
            {
                NodeState state = _activeTree.GetNodeState(node.Id);
                int pts = _activeTree.GetAllocatedPoints(node.Id);
                float fillPercentage = (float)pts / node.MaxPoints;
                
                // Theming
                Color baseColor = ParseHex(data.Theme.PrimaryColorHex);
                Color renderColor = state switch
                {
                    NodeState.Locked => Color.DarkSlateGray,
                    NodeState.Available => Color.LightGray,
                    NodeState.Partial => Color.Lerp(Color.LightGray, baseColor, fillPercentage),
                    NodeState.Maxed => baseColor,
                    _ => Color.White
                };

                // TODO: Draw _currentShapeOverride at (node.GridX, node.GridY) * Spacing 
                // Draw progress slice using `fillPercentage` (e.g., pie fill for circle, bottom-to-top fill for square)
            }
        }

        // Helper for theme hex strings
        private Color ParseHex(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return new Color(r, g, b);
        }
    }
    
    // ==========================================
    // 6. CONTROLLER NAVIGATION SYSTEM
    // ==========================================
    /// <summary>
    /// Helper class to resolve Gamepad D-PAD / Left Stick inputs.
    /// By relying on GridX and GridY, we construct a virtual 2D grid of nodes.
    /// </summary>
    public class ControllerGridNavigator
    {
        public string FindNearestNode(ClassSkillTreeData data, string currentNodeId, Vector2 inputDirection)
        {
            var current = data.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (current == null) return null;

            SkillNodeData bestMatch = null;
            float closestDist = float.MaxValue;

            foreach (var node in data.Nodes)
            {
                if (node.Id == currentNodeId) continue;
                
                Vector2 dirToNode = new Vector2(node.GridX - current.GridX, node.GridY - current.GridY);
                if (dirToNode.LengthSquared() == 0) continue;

                // Check if the node is broadly in the direction of the analog stick
                if (Vector2.Dot(Vector2.Normalize(dirToNode), inputDirection) > 0.7f)
                {
                    if (dirToNode.Length() < closestDist) { closestDist = dirToNode.Length(); bestMatch = node; }
                }
            }
            return bestMatch?.Id;
        }
    }

    // ==========================================
    // 7. SERIALIZATION (Save & Load)
    // ==========================================
    public static class SkillTreeSaveManager
    {
        private const string SaveFile = "skilltree_save.json";

        public static void Save(SkillTreeState state)
        {
            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFile, json);
        }

        public static SkillTreeState Load(string classId)
        {
            if (File.Exists(SaveFile))
            {
                try
                {
                    string json = File.ReadAllText(SaveFile);
                    return JsonSerializer.Deserialize<SkillTreeState>(json);
                }
                catch { /* Handle parsing error gracefully if needed */ }
            }
            
            // Return default fresh state if no save exists. Starts with 5 points for testing.
            return new SkillTreeState { ClassId = classId, TotalPointsSpent = 0, UnspentSkillPoints = 5 };
        }
    }
}
