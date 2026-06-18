using System;
using System.IO;
using System.Text.Json;
using Last_Hope.Engine;
using Last_Hope.Classes.Items;

namespace Last_Hope.Systems
{
    /// <summary>
    /// Represents the serialized state of an active game run.
    /// Designed purely as a Data Transfer Object (DTO) for file I/O operations.
    /// </summary>
    public class RunSaveData
    {
        public int CurrentWave { get; set; }
        public Zone CurrentZone { get; set; }
        public bool VillageCleared { get; set; }
        public int Score { get; set; }
        public PlayerCharacterKind SelectedCharacter { get; set; }
        public bool BossSpawned { get; set; }
        
        public bool HasUsedOneUp { get; set; }
        public bool HasOneUpDropped { get; set; }

        public int Level { get; set; }
        public float Experience { get; set; }
        public float CurrentHp { get; set; }
        public int ExtraLives { get; set; }
        public ItemType[] Inventory { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }

    /// <summary>
    /// Static utility class responsible for reading and writing the player's current run state to disk.
    /// Manages file paths and ensures synchronization with external state managers like the SkillTreeSaveManager.
    /// </summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.file">Microsoft File I/O Operations</see>
    /// </remarks>
    public static class RunSaveManager
    {
        /// <summary>
        /// Dynamically resolves the target save directory, defaulting to the project's external Systems folder 
        /// or falling back to the local build directory.
        /// </summary>
        public static string GetSaveDirectory()
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

        private static string SaveFilePath => Path.Combine(GetSaveDirectory(), "run_save.json");

        /// <summary>
        /// Checks if an active run save file currently exists on disk.
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Deletes the current run save file. 
        /// Automatically delegates to the SkillTreeSaveManager to wipe progression data if the configuration requires it.
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
                Console.WriteLine($"Failed to delete run save: {ex.Message}");
            }
            
            if (!SkillTree.SkillTreeConfig.PersistSkillTreeOnDeath)
            {
                SkillTree.SkillTreeSaveManager.DeleteSave();
            }
        }

        /// <summary>
        /// Serializes the current active state of the GameManager to disk.
        /// Forces a synchronized save of the active Skill Tree to prevent data desyncs or save scumming exploits.
        /// </summary>
        /// <param name="gm">The global GameManager instance to harvest state from.</param>
        public static void SaveRun(GameManager gm)
        {
            if (gm._player == null) return;

            var data = new RunSaveData
            {
                CurrentWave = gm.EnemySpawner.CurrentWave,
                CurrentZone = gm.CurrentZone,
                VillageCleared = gm.VillageCleared,
                Score = gm.Score,
                SelectedCharacter = gm.SelectedCharacter,
                BossSpawned = gm.EnemySpawner.BossSpawned,

                HasUsedOneUp = gm.HasUsedOneUp,
                HasOneUpDropped = gm.HasOneUpDropped,

                Level = gm._player.Level,
                Experience = gm._player._Experience,
                CurrentHp = gm._player._currentHp,
                ExtraLives = gm._player.ExtraLives,
                Inventory = gm._player.Inventory,
                
                PositionX = gm._player.GetPosition().X,
                PositionY = gm._player.GetPosition().Y,
            };

            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save run data: {ex.Message}");
            }
            
            SkillTree.SkillTreeSaveManager.SaveCurrent();
        }

        /// <summary>
        /// Deserializes the run save file from disk into a RunSaveData object.
        /// </summary>
        /// <returns>The deserialized data, or null if loading fails.</returns>
        public static RunSaveData LoadRunData()
        {
            if (!HasSave()) return null;

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                return JsonSerializer.Deserialize<RunSaveData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load run data or save is corrupted: {ex.Message}");
                return null;
            }
        }
    }
}