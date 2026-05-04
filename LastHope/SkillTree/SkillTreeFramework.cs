using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Last_Hope.SkillTree
{
    // ==========================================
    // 1. ENUMS & CONSTANTS
    // ==========================================
    public enum SkillNodeType { Standard = 0, Minor = 1, Major = 2 }
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
        public float GridX { get; set; }
        public float GridY { get; set; }
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
        
        // Pending state for point allocation planning
        private readonly Dictionary<string, int> _pendingAllocations = new Dictionary<string, int>();

        // Fast lookup cache
        private readonly Dictionary<string, SkillNodeData> _nodeMap;

        // --- The Stats Bus ---
        public event Action<NodeEffect> OnEffectApplied;
        public event Action OnTreeRespec;
        public int UnspentPoints => _state.UnspentSkillPoints - _pendingAllocations.Values.Sum();
        public int PendingPoints => _pendingAllocations.Values.Sum();

        public BaseSkillTree(ClassSkillTreeData data, SkillTreeState state)
        {
            _data = data;
            _state = state;
            _nodeMap = _data.Nodes.ToDictionary(n => n.Id);
        }

        public NodeState GetNodeState(string nodeId, bool includePending = true)
        {
            var nodeData = _nodeMap[nodeId];
            int allocated = GetAllocatedPoints(nodeId, includePending);

            if (allocated >= nodeData.MaxPoints) return NodeState.Maxed;
            if (allocated > 0) return NodeState.Partial;
            return CanUnlockNode(nodeId, includePending) ? NodeState.Available : NodeState.Locked;
        }

        public int GetAllocatedPoints(string nodeId, bool includePending = true)
        {
            int pts = _state.AllocatedNodes.TryGetValue(nodeId, out int p) ? p : 0;
            if (includePending && _pendingAllocations.TryGetValue(nodeId, out int pend))
            {
                pts += pend;
            }
            return pts;
        }

        public bool CanUnlockNode(string nodeId, bool includePending = true)
        {
            var node = _nodeMap[nodeId];

            // Check 1: Do we have enough total points to access this layer?
            if (_data.LayerUnlockRequirements.TryGetValue(node.Layer, out int reqPoints))
            {
                int totalSpent = _state.TotalPointsSpent + (includePending ? PendingPoints : 0);
                if (totalSpent < reqPoints) return false;
            }

            // Check 2: Are all prerequisite nodes MAXED out?
            foreach (var depId in node.Dependencies)
            {
                var depNode = _nodeMap[depId];
                if (GetAllocatedPoints(depId, includePending) < depNode.MaxPoints)
                {
                    return false;
                }
            }

            // Check 3: Validate visual Connections. If there are incoming connections,
            // AT LEAST ONE parent MUST have points allocated (> 0).
            var parentConnections = _data.Connections.Where(c => c.ToNodeId == nodeId).ToList();
            if (parentConnections.Count > 0)
            {
                if (!parentConnections.Any(conn => GetAllocatedPoints(conn.FromNodeId, includePending) > 0))
                {
                    return false;
                }
            }

            // Check 4: Exclusivity rules (Only 1 Stance and 1 Active allowed across the whole tree)
            if (node.Tags.Contains("Stance") && HasOtherAllocatedNodeWithTag("Stance", nodeId, includePending)) return false;
            if (node.Tags.Contains("Active") && HasOtherAllocatedNodeWithTag("Active", nodeId, includePending)) return false;

            return true;
        }

        private bool HasOtherAllocatedNodeWithTag(string tag, string excludeNodeId, bool includePending)
        {
            return _data.Nodes.Any(n => n.Id != excludeNodeId && n.Tags.Contains(tag) && GetAllocatedPoints(n.Id, includePending) > 0);
        }

        public bool AddPendingPoint(string nodeId)
        {
            if (UnspentPoints <= 0) return false;
            if (!CanUnlockNode(nodeId, true)) return false;
            
            var node = _nodeMap[nodeId];
            int currentPts = GetAllocatedPoints(nodeId, true);
            
            if (currentPts >= node.MaxPoints) return false; // Already maxed

            if (!_pendingAllocations.ContainsKey(nodeId))
                _pendingAllocations[nodeId] = 0;
            
            _pendingAllocations[nodeId]++;
            return true;
        }

        public bool RemovePendingPoint(string nodeId)
        {
            if (!_pendingAllocations.ContainsKey(nodeId) || _pendingAllocations[nodeId] <= 0)
                return false;

            _pendingAllocations[nodeId]--;
            if (_pendingAllocations[nodeId] == 0)
                _pendingAllocations.Remove(nodeId);
            
            // Validation: Ensure removing this doesn't break dependencies for other pending nodes.
            // If it does, we might need a recursive check, but for a clean UX, wiping all downstream pending is safest.
            ValidatePendingDownstream();
            return true;
        }

        private void ValidatePendingDownstream()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var pend in _pendingAllocations.Keys.ToList())
                {
                    if (!CanUnlockNode(pend, true))
                    {
                        _pendingAllocations.Remove(pend);
                        changed = true;
                    }
                }
            }
        }

        public void ConfirmPendingPoints()
        {
            if (_pendingAllocations.Count == 0) return;

            foreach (var kvp in _pendingAllocations)
            {
                string nodeId = kvp.Key;
                int ptsToApply = kvp.Value;
                
                if (!_state.AllocatedNodes.ContainsKey(nodeId))
                    _state.AllocatedNodes[nodeId] = 0;
                
                _state.AllocatedNodes[nodeId] += ptsToApply;
                _state.TotalPointsSpent += ptsToApply;
                _state.UnspentSkillPoints -= ptsToApply;

                var node = _nodeMap[nodeId];
                for (int i = 0; i < ptsToApply; i++)
                {
                    foreach(var effect in node.Effects)
                        OnEffectApplied?.Invoke(effect);
                }
            }

            _pendingAllocations.Clear();
            SkillTreeSaveManager.Save(_state);
        }

        public void CancelPendingPoints()
        {
            _pendingAllocations.Clear();
        }

        public void Respec()
        {
            CancelPendingPoints();
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
