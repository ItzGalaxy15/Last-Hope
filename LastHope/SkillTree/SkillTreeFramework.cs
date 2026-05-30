using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Last_Hope.SkillTree
{

    /// <summary>
    /// Represents the tier or importance of a skill node.
    /// </summary>
    public static class SkillTreeConfig
    {
        public static bool PersistSkillTreeOnDeath = false;
        public static bool EnableBranchLocking = true;
    }

    public enum SkillNodeType { Standard = 0, Minor = 1, Major = 2 }
    
    /// <summary>
    /// Defines the visual shape of the skill node in the UI.
    /// </summary>
    public enum NodeShape { Circle, Square }
    
    /// <summary>
    /// Represents the current unlock and allocation state of a skill node for a specific player.
    /// </summary>
    public enum NodeState { Locked, Available, Partial, Maxed }

    /// <summary>
    /// Defines a specific gameplay effect granted by allocating points to a skill node.
    /// </summary>
    public class NodeEffect
    {
        public string EffectId { get; set; } // e.g., "base_damage", "unlock_whirlwind"
        public float ValuePerPoint { get; set; } // e.g., 5.0f for +5 Damage per point
    }

    /// <summary>
    /// Contains the static configuration data for a single skill node within the skill tree.
    /// Defines what the node does, its requirements, and its position.
    /// </summary>
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

    /// <summary>
    /// Defines a directed visual and logical dependency connection between two skill nodes.
    /// </summary>
    public class SkillConnectionData
    {
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
    }

    /// <summary>
    /// Holds visual styling information for a class's skill tree.
    /// </summary>
    public class SkillTreeTheme
    {
        // Hex colors representing the thematic vibe (e.g., Warrior = Red/Steel)
        public string PrimaryColorHex { get; set; } 
        public string SecondaryColorHex { get; set; }
        public NodeShape DefaultShape { get; set; }
    }

    /// <summary>
    /// The root data structure defining an entire skill tree for a specific character class.
    /// Contains all nodes, connections, styling, and progression requirements.
    /// </summary>
    public class ClassSkillTreeData
    {
        public string ClassId { get; set; }
        public SkillTreeTheme Theme { get; set; }
        // Key = Layer Index, Value = Total Points Spent required to unlock this layer
        public Dictionary<int, int> LayerUnlockRequirements { get; set; } = new Dictionary<int, int>();
        public List<SkillNodeData> Nodes { get; set; } = new List<SkillNodeData>();
        public List<SkillConnectionData> Connections { get; set; } = new List<SkillConnectionData>();
    }

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

    /// <summary>
    /// Manages the logic, validation, and point allocation for a player's interaction with a skill tree.
    /// Combines static tree data (<see cref="ClassSkillTreeData"/>) with the player's mutable state (<see cref="SkillTreeState"/>).
    /// </summary>
    /// <remarks>
    /// Follows a common data-driven design approach where static definition data is cleanly separated from mutable player progression state.
    /// </remarks>
    public class BaseSkillTree
    {
        private readonly ClassSkillTreeData _data;
        private SkillTreeState _state;
        
        // Pending state for point allocation planning
        private readonly Dictionary<string, int> _pendingAllocations = new Dictionary<string, int>();

        // Fast lookup cache
        private readonly Dictionary<string, SkillNodeData> _nodeMap;

        // --- The Stats Bus ---
        
        /// <summary>
        /// Event fired when a point is confirmed, applying its associated effect to the player.
        /// </summary>
        /// <remarks>
        /// Implements the Observer pattern to notify external systems (e.g., player stat modifiers) of applied node effects.
        /// </remarks>
        public event Action<NodeEffect> OnEffectApplied;
        
        /// <summary>
        /// Event fired when the entire skill tree is reset, signaling that all node effects should be removed.
        /// </summary>
        public event Action OnTreeRespec;
        
        public int UnspentPoints => _state.UnspentSkillPoints - _pendingAllocations.Values.Sum();
        public int PendingPoints => _pendingAllocations.Values.Sum();

        /// <summary>
        /// Initializes a new instance of the skill tree manager.
        /// </summary>
        /// <param name="data">The static configuration data for the skill tree.</param>
        /// <param name="state">The player's current progression state.</param>
        public BaseSkillTree(ClassSkillTreeData data, SkillTreeState state)
        {
            _data = data;
            _state = state;
            _nodeMap = _data.Nodes.ToDictionary(n => n.Id);
        }

        /// <summary>
        /// Gets the current state of a node (Locked, Available, Partial, Maxed).
        /// </summary>
        /// <param name="nodeId">The ID of the node to check.</param>
        /// <param name="includePending">Whether to consider pending (unconfirmed) point allocations.</param>
        /// <returns>The calculated <see cref="NodeState"/>.</returns>
        public NodeState GetNodeState(string nodeId, bool includePending = true)
        {
            var nodeData = _nodeMap[nodeId];
            int allocated = GetAllocatedPoints(nodeId, includePending);

            if (allocated >= nodeData.MaxPoints) return NodeState.Maxed;
            if (allocated > 0) return NodeState.Partial;
            return CanUnlockNode(nodeId, includePending) ? NodeState.Available : NodeState.Locked;
        }

        /// <summary>
        /// Gets the number of points currently allocated to a specific node.
        /// </summary>
        /// <param name="nodeId">The ID of the node to check.</param>
        /// <param name="includePending">Whether to include points that are allocated but not yet confirmed.</param>
        /// <returns>The total number of allocated points.</returns>
        public int GetAllocatedPoints(string nodeId, bool includePending = true)
        {
            int pts = _state.AllocatedNodes.TryGetValue(nodeId, out int p) ? p : 0;
            if (includePending && _pendingAllocations.TryGetValue(nodeId, out int pend))
            {
                pts += pend;
            }
            return pts;
        }

        public List<string> GetUnlockMissingRequirements(string nodeId, bool includePending = true)
        {
            var node = _nodeMap[nodeId];
            List<string> missing = new List<string>();

            if (_data.LayerUnlockRequirements.TryGetValue(node.Layer, out int reqPoints))
            {
                int totalSpent = _state.TotalPointsSpent + (includePending ? PendingPoints : 0);
                if (totalSpent < reqPoints) 
                    missing.Add($"Requires {reqPoints} total points spent (have {totalSpent}).");
            }

            foreach (var depId in node.Dependencies)
            {
                var depNode = _nodeMap[depId];
                if (GetAllocatedPoints(depId, includePending) < depNode.MaxPoints)
                {
                    missing.Add($"Requires '{depNode.Name}' to be maxed.");
                }
            }

            var parentConnections = _data.Connections.Where(c => c.ToNodeId == nodeId).ToList();
            if (parentConnections.Count > 0)
            {
                if (!parentConnections.Any(conn => GetAllocatedPoints(conn.FromNodeId, includePending) > 0))
                {
                    missing.Add("Requires an active path connected to this node.");
                }
            }

            if (SkillTreeConfig.EnableBranchLocking)
            {
                if (node.Tags.Contains("Stance") && HasOtherAllocatedNodeWithTag("Stance", nodeId, includePending)) 
                    missing.Add("Locked: You can only choose one Stance branch.");
            }
            if (node.Tags.Contains("Active") && HasOtherAllocatedNodeWithTag("Active", nodeId, includePending)) 
                missing.Add("Locked: You can only choose one Active ability.");

            if (missing.Count == 0 && !CanUnlockNode(nodeId, includePending))
            {
                missing.Add("Locked by unknown requirement.");
            }

            return missing;
        }

        /// <summary>
        /// Evaluates all validation rules to determine if a node can receive points.
        /// Checks layer unlock requirements, dependencies, connection paths, and exclusivity tags.
        /// </summary>
        /// <param name="nodeId">The ID of the node to validate.</param>
        /// <param name="includePending">Whether to include pending points in the validation checks.</param>
        /// <returns><c>true</c> if the node can be unlocked; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// The skill tree dependencies operate conceptually as a Directed Acyclic Graph (DAG). 
        /// This method enforces standard topological constraints (parent node prerequisites) before unlocking.
        /// </remarks>
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
            if (SkillTreeConfig.EnableBranchLocking)
            {
                if (node.Tags.Contains("Stance") && HasOtherAllocatedNodeWithTag("Stance", nodeId, includePending)) return false;
            }
            if (node.Tags.Contains("Active") && HasOtherAllocatedNodeWithTag("Active", nodeId, includePending)) return false;

            return true;
        }

        /// <summary>
        /// Checks if another node with a mutually exclusive tag has already been allocated.
        /// </summary>
        private bool HasOtherAllocatedNodeWithTag(string tag, string excludeNodeId, bool includePending)
        {
            return _data.Nodes.Any(n => n.Id != excludeNodeId && n.Tags.Contains(tag) && GetAllocatedPoints(n.Id, includePending) > 0);
        }

        /// <summary>
        /// Attempts to add a pending skill point to the specified node.
        /// </summary>
        /// <param name="nodeId">The ID of the node.</param>
        /// <returns><c>true</c> if the point was successfully added; otherwise, <c>false</c> (e.g., maxed out or insufficient points).</returns>
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

        /// <summary>
        /// Attempts to remove a pending skill point from the specified node.
        /// Triggers a cascade validation to ensure dependent pending nodes are also removed if their prerequisites are broken.
        /// </summary>
        /// <param name="nodeId">The ID of the node.</param>
        /// <returns><c>true</c> if a pending point was removed; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Iteratively validates all pending allocations, removing any that no longer meet unlock requirements.
        /// </summary>
        /// <remarks>
        /// Appears to use a standard fixed-point iteration approach to resolve and clean up cascading validation failures within the dependency graph.
        /// </remarks>
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

        /// <summary>
        /// Confirms all pending point allocations, permanently applying them to the state and triggering effect events.
        /// Automatically saves the new state.
        /// </summary>
        /// <remarks>
        /// Acts as a transactional commit, finalizing all tentative point allocations simultaneously to prevent partial progression states.
        /// </remarks>
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

        /// <summary>
        /// Reverts all currently pending point allocations without saving.
        /// </summary>
        public void CancelPendingPoints()
        {
            _pendingAllocations.Clear();
        }

        /// <summary>
        /// Resets the entire skill tree, refunding all spent points and clearing allocations.
        /// Fires <see cref="OnTreeRespec"/> to allow external systems to clear applied stats, then saves.
        /// </summary>
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
        
        public void RecalculateStats() { foreach (var kvp in _state.AllocatedNodes) { string nodeId = kvp.Key; int points = kvp.Value; if (_nodeMap.TryGetValue(nodeId, out var node)) { for (int i = 0; i < points; i++) { foreach(var effect in node.Effects) OnEffectApplied?.Invoke(effect); } } } } public ClassSkillTreeData GetData() => _data;

        public void AddUnspentPoint()
        {
            _state.UnspentSkillPoints++;
            SkillTreeSaveManager.Save(_state);
        }
    }

    /// <summary>
    /// Handles loading and saving the player's skill tree progression state to disk using JSON serialization.
    /// </summary>
    /// <remarks>
    /// Based on standard JSON file serialization patterns for game state persistence.
    /// </remarks>
    public static class SkillTreeSaveManager
    {
        private const string SaveFile = "skilltree_save.json";

        /// <summary>
        /// Saves the given skill tree state to a JSON file.
        /// </summary>
        /// <param name="state">The state to save.</param>
        public static void Save(SkillTreeState state)
        {
            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFile, json);
        }

        /// <summary>
        /// Loads the skill tree state from disk. If the file does not exist, returns a fresh state.
        /// </summary>
        /// <param name="classId">The character class ID to assign if creating a new state.</param>
        /// <returns>The loaded or newly created <see cref="SkillTreeState"/>.</returns>
        public static void DeleteSave()
        {
            if (File.Exists(SaveFile))
            {
                File.Delete(SaveFile);
            }
        }

        public static SkillTreeState Load(string classId)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (File.Exists(SaveFile))
            {
                try
                {
                    string json = File.ReadAllText(SaveFile);
                    SkillTreeState? state = JsonSerializer.Deserialize<SkillTreeState>(json, jsonOptions);
                    if (state != null)
                    {
                        state.ClassId = string.IsNullOrEmpty(state.ClassId) ? classId : state.ClassId;
                        state.AllocatedNodes ??= new Dictionary<string, int>();
                        int sumAllocated = state.AllocatedNodes.Values.Sum();
                        if (state.TotalPointsSpent != sumAllocated)
                            state.TotalPointsSpent = sumAllocated;
                        return state;
                    }
                }
                catch { /* fall through to fresh state */ }
            }

            // Return default fresh state if no save exists. Starts with 5 points for testing.
            return new SkillTreeState { ClassId = classId, TotalPointsSpent = 0, UnspentSkillPoints = 5 };
        }
    }
}
