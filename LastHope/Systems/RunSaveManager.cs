using System;
using System.IO;
using System.Text.Json;
using Last_Hope.Engine;
using Last_Hope.Classes.Items;

namespace Last_Hope.Systems
{
    /// <summary>
    /// Data container structure holding all serializable states for a single gameplay run.
    /// Source: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes
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
    /// Static manager handling persistent run save file logic and skill tree synchronization.
    /// </summary>
    public static class RunSaveManager
    {
        private static readonly string SaveFilePath = Path.Combine(AppContext.BaseDirectory, "run_save.json");

        /// <summary>
        /// Checks whether a valid run save file exists on the local disk.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.exists
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Deletes the active run save file and enforces skill tree wipe configuration rules on death.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.delete
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
        /// Serializes the full operational run context and executes a synchronized skill tree save pass.
        /// Source: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/serialization
        /// </summary>
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
            
            // Synchronized save: write the skill tree to disk exactly when the wave progress is saved.
            // This closes the loophole where players could spend a point mid-wave, force quit, and double-dip XP.
            global::Last_Hope.SkillTree.SkillTreeSaveManager.SaveCurrent();
        }

        /// <summary>
        /// Deserializes local json records to reconstruct a cached run data model.
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalltext
        /// </summary>
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