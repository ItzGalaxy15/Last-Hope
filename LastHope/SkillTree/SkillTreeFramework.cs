using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace Last_Hope.SkillTree
{
    /// <summary>
    /// Global configuration flags for the skill tree system.
    /// </summary>
    public static class SkillTreeConfig
    {
        public static bool PersistSkillTreeOnDeath = false;
        public static bool EnableBranchLocking = true;
    }

    public enum SkillNodeType { Standard = 0, Minor = 1, Major = 2 }
    
    public enum NodeShape { Circle, Square }
    
    public enum NodeState { Locked, Available, Partial, Maxed }

    /// <summary>
    /// Defines a specific gameplay effect granted by allocating points to a skill node.
    /// </summary>
    public class NodeEffect
    {
        public string EffectId { get; set; } 
        public float ValuePerPoint { get; set; } 
    }

    /// <summary>
    /// Contains the static configuration data for a single skill node within the skill tree.
    /// Defines its attributes, dependencies, visual coordinates, and thematic metadata.
    /// </summary>
    public class SkillNodeData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxPoints { get; set; }
        public SkillNodeType Type { get; set; }
        public int Layer { get; set; }
        
        public string Rarity { get; set; }
        public string IconId { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<NodeEffect> Effects { get; set; } = new List<NodeEffect>();
        
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
        public Dictionary<int, int> LayerUnlockRequirements { get; set; } = new Dictionary<int, int>();
        public List<SkillNodeData> Nodes { get; set; } = new List<SkillNodeData>();
        public List<SkillConnectionData> Connections { get; set; } = new List<SkillConnectionData>();
    }

    /// <summary>
    /// Represents the player's mutable state for a skill tree. 
    /// This is strictly separated from the configuration data to facilitate clean JSON serialization.
    /// </summary>
    /// <remarks>
    /// Separating mutable state from static data follows the Data Transfer Object (DTO) pattern.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/data-transfer-objects">Microsoft DTO Pattern</see>
    /// </remarks>
    public class SkillTreeState
    {
        public string ClassId { get; set; }
        public int TotalPointsSpent { get; set; }
        public int UnspentSkillPoints { get; set; }
        public Dictionary<string, int> AllocatedNodes { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Manages the logic, validation, and point allocation for a player's interaction with a skill tree.
    /// Combines static tree data (<see cref="ClassSkillTreeData"/>) with the player's mutable state (<see cref="SkillTreeState"/>).
    /// </summary>
    /// <remarks>
    /// Utilizes the Observer pattern via C# Events/Actions to notify other systems when stats change.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/events/observer-design-pattern">Microsoft Observer Design Pattern</see>
    /// </remarks>
    public class BaseSkillTree
    {
        private readonly ClassSkillTreeData _data;
        private SkillTreeState _state;
        
        private readonly Dictionary<string, int> _pendingAllocations = new Dictionary<string, int>();
        private readonly Dictionary<string, SkillNodeData> _nodeMap;
        private readonly Dictionary<string, string> _nodeRoots = new Dictionary<string, string>();

        /// <summary>
        /// Event fired when a point is confirmed, applying its associated effect to the player.
        /// </summary>
        public event Action<NodeEffect> OnEffectApplied;
        
        /// <summary>
        /// Event fired when the entire skill tree is reset, signaling that all node effects should be removed.
        /// </summary>
        public event Action OnTreeRespec;
        
        public int UnspentPoints => _state.UnspentSkillPoints - _pendingAllocations.Values.Sum();
        public int PendingPoints => _pendingAllocations.Values.Sum();

        /// <summary>
        /// Initializes a new instance of the skill tree manager, precomputing traversal paths.
        /// </summary>
        public BaseSkillTree(ClassSkillTreeData data, SkillTreeState state)
        {
            _data = data;
            _state = state;
            _nodeMap = _data.Nodes.ToDictionary(n => n.Id);
            
            foreach (var rootNode in _data.Nodes.Where(n => n.Layer == 0))
            {
                var queue = new Queue<string>();
                queue.Enqueue(rootNode.Id);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    _nodeRoots[current] = rootNode.Id; 
                    
                    var children = _data.Connections.Where(c => c.FromNodeId == current).Select(c => c.ToNodeId);
                    foreach (var child in children)
                    {
                        if (!_nodeRoots.ContainsKey(child))
                            queue.Enqueue(child);
                    }
                }
            }
        }

        private string GetActiveRoot(bool includePending)
        {
            foreach (var kvp in _state.AllocatedNodes)
                if (kvp.Value > 0 && _nodeRoots.TryGetValue(kvp.Key, out string rootId)) return rootId;
            
            if (includePending)
                foreach (var kvp in _pendingAllocations)
                    if (kvp.Value > 0 && _nodeRoots.TryGetValue(kvp.Key, out string rootId)) return rootId;
            
            return null;
        }

        /// <summary>
        /// Gets the current unlock and allocation state of a node.
        /// </summary>
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
        public int GetAllocatedPoints(string nodeId, bool includePending = true)
        {
            int pts = _state.AllocatedNodes.TryGetValue(nodeId, out int p) ? p : 0;
            if (includePending && _pendingAllocations.TryGetValue(nodeId, out int pend))
            {
                pts += pend;
            }
            return pts;
        }

        /// <summary>
        /// Compiles a list of missing requirements preventing the allocation of points to a node.
        /// </summary>
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
                string activeRoot = GetActiveRoot(includePending);
                if (activeRoot != null && _nodeRoots.TryGetValue(nodeId, out string nodeRoot) && activeRoot != nodeRoot)
                    missing.Add("Locked: You have already specialized into another tree branch.");

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
        /// </summary>
        public bool CanUnlockNode(string nodeId, bool includePending = true)
        {
            var node = _nodeMap[nodeId];

            if (_data.LayerUnlockRequirements.TryGetValue(node.Layer, out int reqPoints))
            {
                int totalSpent = _state.TotalPointsSpent + (includePending ? PendingPoints : 0);
                if (totalSpent < reqPoints) return false;
            }

            foreach (var depId in node.Dependencies)
            {
                var depNode = _nodeMap[depId];
                if (GetAllocatedPoints(depId, includePending) < depNode.MaxPoints)
                {
                    return false;
                }
            }

            var parentConnections = _data.Connections.Where(c => c.ToNodeId == nodeId).ToList();
            if (parentConnections.Count > 0)
            {
                if (!parentConnections.Any(conn => GetAllocatedPoints(conn.FromNodeId, includePending) > 0))
                {
                    return false;
                }
            }

            if (SkillTreeConfig.EnableBranchLocking)
            {
                string activeRoot = GetActiveRoot(includePending);
                if (activeRoot != null && _nodeRoots.TryGetValue(nodeId, out string nodeRoot) && activeRoot != nodeRoot)
                    return false;

                if (node.Tags.Contains("Stance") && HasOtherAllocatedNodeWithTag("Stance", nodeId, includePending)) return false;
            }
            if (node.Tags.Contains("Active") && HasOtherAllocatedNodeWithTag("Active", nodeId, includePending)) return false;

            return true;
        }

        private bool HasOtherAllocatedNodeWithTag(string tag, string excludeNodeId, bool includePending)
        {
            return _data.Nodes.Any(n => n.Id != excludeNodeId && n.Tags.Contains(tag) && GetAllocatedPoints(n.Id, includePending) > 0);
        }

        /// <summary>
        /// Attempts to add a pending skill point to the specified node.
        /// </summary>
        public bool AddPendingPoint(string nodeId)
        {
            if (UnspentPoints <= 0) return false;
            if (!CanUnlockNode(nodeId, true)) return false;
            
            var node = _nodeMap[nodeId];
            int currentPts = GetAllocatedPoints(nodeId, true);
            
            if (currentPts >= node.MaxPoints) return false; 

            if (!_pendingAllocations.ContainsKey(nodeId))
                _pendingAllocations[nodeId] = 0;
            
            _pendingAllocations[nodeId]++;
            return true;
        }

        /// <summary>
        /// Attempts to remove a pending skill point from the specified node.
        /// </summary>
        public bool RemovePendingPoint(string nodeId)
        {
            if (!_pendingAllocations.ContainsKey(nodeId) || _pendingAllocations[nodeId] <= 0)
                return false;

            _pendingAllocations[nodeId]--;
            if (_pendingAllocations[nodeId] == 0)
                _pendingAllocations.Remove(nodeId);
            
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

        /// <summary>
        /// Confirms all pending point allocations, permanently applying them to the state and triggering effect events.
        /// </summary>
        /// <remarks>
        /// Relies on external managers to sync file writes to prevent exploit loops.
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
            global::Last_Hope.Systems.RunSaveManager.SaveRun(global::Last_Hope.Engine.GameManager.GetGameManager());
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
        /// Fires <see cref="OnTreeRespec"/> to allow external systems to clear applied stats.
        /// </summary>
        public void Respec()
        {
            CancelPendingPoints();
            _state.UnspentSkillPoints += _state.TotalPointsSpent;
            _state.TotalPointsSpent = 0;
            _state.AllocatedNodes.Clear();
            
            OnTreeRespec?.Invoke();
        }
        
        public void RecalculateStats() { foreach (var kvp in _state.AllocatedNodes) { string nodeId = kvp.Key; int points = kvp.Value; if (_nodeMap.TryGetValue(nodeId, out var node)) { for (int i = 0; i < points; i++) { foreach(var effect in node.Effects) OnEffectApplied?.Invoke(effect); } } } } public ClassSkillTreeData GetData() => _data;

        public void AddUnspentPoint()
        {
            _state.UnspentSkillPoints++;
        }
    }

    /// <summary>
    /// Handles loading and saving the player's skill tree progression state to disk using JSON serialization.
    /// Explicitly tracks CurrentState to allow external systems to trigger synchronized saves.
    /// </summary>
    /// <remarks>
    /// Utilizes System.Text.Json for serialization operations.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview">Microsoft JSON Serialization</see>
    /// </remarks>
    public static class SkillTreeSaveManager
    {
        private static string GetSaveDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
            string systemsPath = Path.Combine(projectRoot, "Systems");
            
            if (!Directory.Exists(systemsPath))
            {
                systemsPath = Path.Combine(baseDir, "Systems");
                if (!Directory.Exists(systemsPath))
                {
                    Directory.CreateDirectory(systemsPath);
                }
            }
            return systemsPath;
        }

        private static string SaveFilePath => Path.Combine(GetSaveDirectory(), "skilltree_save.json");

        public static SkillTreeState CurrentState { get; set; }

        // Points granted at character creation, and how many levels earn one talent point.
        // Must match the fresh-state grant below and BasePlayer.TalentPointInterval.
        public const int InitialSkillPoints = 10;
        public const int LevelsPerSkillPoint = 5;

        /// <summary>
        /// Recomputes unspent points from the authoritative level so a rolled-back run
        /// (quitting before a checkpoint) cannot keep points earned past that checkpoint.
        /// Prevents farming infinite points by leveling, quitting, and continuing.
        /// </summary>
        public static void ReconcileUnspentPoints(int level)
        {
            if (CurrentState == null) return;

            int earned = InitialSkillPoints + (level / LevelsPerSkillPoint);
            int unspent = earned - CurrentState.TotalPointsSpent;
            CurrentState.UnspentSkillPoints = unspent < 0 ? 0 : unspent;
            SaveCurrent();
        }

        /// <summary>
        /// Saves the given skill tree state to a JSON file.
        /// </summary>
        public static void Save(SkillTreeState state)
        {
            CurrentState = state;
            try
            {
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save skill tree: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves whatever state is currently active in memory. Used for cross-system synchronization.
        /// </summary>
        public static void SaveCurrent()
        {
            if (CurrentState != null)
            {
                Save(CurrentState);
            }
        }

        /// <summary>
        /// Safely deletes the active save file if it exists.
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete skill tree save: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the skill tree state from disk. If the file does not exist or is corrupted, returns a fresh state object.
        /// </summary>
        public static SkillTreeState Load(string classId)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    SkillTreeState? state = JsonSerializer.Deserialize<SkillTreeState>(json, jsonOptions);
                    if (state != null)
                    {
                        state.ClassId = string.IsNullOrEmpty(state.ClassId) ? classId : state.ClassId;
                        state.AllocatedNodes ??= new Dictionary<string, int>();
                        int sumAllocated = state.AllocatedNodes.Values.Sum();
                        if (state.TotalPointsSpent != sumAllocated)
                            state.TotalPointsSpent = sumAllocated;
                            
                        CurrentState = state; 
                        return state;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load skill tree save or save is corrupted: {ex.Message}");
                }
            }

            var freshState = new SkillTreeState
            {
                ClassId = classId,
                AllocatedNodes = new Dictionary<string, int>(),
                UnspentSkillPoints = InitialSkillPoints,
                TotalPointsSpent = 0
            };
            CurrentState = freshState;
            return freshState;
        }
    }
}