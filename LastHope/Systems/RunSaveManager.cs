using System;
using System.IO;
using System.Text.Json;
using Last_Hope.Engine;
using Last_Hope.Classes.Items;

namespace Last_Hope.Systems
{
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

    public static class RunSaveManager
    {
        private static readonly string SaveFilePath = Path.Combine(AppContext.BaseDirectory, "run_save.json");

        public static bool HasSave()
        {
            return File.Exists(SaveFilePath);
        }

        public static void DeleteSave()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }
            
            // Ensures starting a "New Run" or dying completely wipes the skill tree
            // if we have persistence turned off in the config.
            if (!global::Last_Hope.SkillTree.SkillTreeConfig.PersistSkillTreeOnDeath)
            {
                global::Last_Hope.SkillTree.SkillTreeSaveManager.DeleteSave();
            }
        }

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

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFilePath, json);
            
            // SYNCHRONIZED SAVE: Write the skill tree to disk EXACTLY when the wave progress is saved.
            // This closes the loophole where players could spend a point mid-wave, force quit, and double-dip XP.
            global::Last_Hope.SkillTree.SkillTreeSaveManager.SaveCurrent();
        }

        public static RunSaveData LoadRunData()
        {
            if (!HasSave()) return null;

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                return JsonSerializer.Deserialize<RunSaveData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }
    }
}