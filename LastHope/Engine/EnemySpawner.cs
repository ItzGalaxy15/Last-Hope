using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;

/// <summary>
/// Manages the procedural generation of enemy waves, spawning logic, and wave progression.
/// </summary>
/// <remarks>
/// Based on standard wave based survival game mechanics. Utilizes an exponential scaling curve for enemy counts 
/// and incorporates safe spawn placement by checking against the game's static collision geometry.
/// </remarks>
public class EnemySpawner
{
    /// <summary>The total number of waves per zone (village runs 1..N, then forest runs 1..N).</summary>
    public int TotalWaves { get; set; } = 6;
    
    /// <summary>The exponential multiplier applied to the base enemy count per wave.</summary>
    public float EnemyMultiplierPerWave { get; set; } = 1.5f;
    
    /// <summary>The base number of enemies that will spawn on the first wave.</summary>
    public int StartingEnemies { get; set; } = 20;
    
    /// <summary>Whether a Boss enemy should be spawned at the end of the final wave.</summary>
    public bool BossAppearsOnLastWave { get; set; } = true;
    
    /// <summary>Whether to cap the maximum number of enemies spawned per wave.</summary>
    public bool UseMaxEnemyLimit { get; set; } = true;
    
    /// <summary>The absolute maximum number of enemies allowed per wave, if <see cref="UseMaxEnemyLimit"/> is true.</summary>
    public int MaxEnemiesPerWave { get; set; } = 100;

    private float spawnTimer = 0f;
    private float spawnInterval = 0.2f; // spawn an enemy every 0.2s

    private int currentWave = 1;
    
    /// <summary>The current wave the player is on. Starts at 1.</summary>
    public int CurrentWave => currentWave;
    
    private int spawnedThisWave = 0;
    private float waveWaitTimer = 0f;
    private bool waitingForNextWave = false;
    private float wavePause = 3f; // pause between the waves
    private bool bossSpawned = false;
    private Zone _previousZone = Zone.Village;
    
    /// <summary>Indicates whether the final wave's boss has been spawned yet.</summary>
    public bool BossSpawned => bossSpawned;

    /// <summary>True when the final village wave has no enemies left and the forest can be entered.</summary>
    public bool VillageFinalWaveCleared
    {
        get
        {
            var gm = GameManager.GetGameManager();
            return gm.CurrentZone == Zone.Village &&
                   currentWave == TotalWaves &&
                   bossSpawned &&
                   spawnedThisWave >= GetTargetEnemiesForWave(currentWave) &&
                   CountActiveEnemies(gm) == 0;
        }
    }

    /// <summary>
    /// Calculates the total number of enemies remaining in the current wave, including both alive and yet to spawn enemies.
    /// </summary>
    /// <returns>The number of remaining enemies.</returns>
    public int GetEnemiesLeftCount()
    {
        var gm = GameManager.GetGameManager();
        int currentEnemyCount = CountActiveEnemies(gm);

        int unspawned = GetTargetEnemiesForWave(currentWave) - spawnedThisWave;
        if (unspawned < 0) unspawned = 0;

        int total = currentEnemyCount + unspawned;

        if (BossAppearsOnLastWave && currentWave == TotalWaves && !bossSpawned)
        {
            total++;
        }

        return total;
    }

    /// <summary>
    /// Evaluates the wave progression state and handles the instantiation of new enemies.
    /// Should be called during the main game loop.
    /// </summary>
    /// <param name="gameTime">The current game time, used for timer progression.</param>
    /// <remarks>
    /// Operates conceptually as a finite state machine (FSM), transitioning between active spawning, waiting for wave clearance, and inter wave pauses.
    /// </remarks>
    public void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();

        if (gm.CurrentZone != _previousZone)
        {
            if (_previousZone == Zone.Village && gm.CurrentZone == Zone.Forest)
            {
                currentWave = 1;
                spawnedThisWave = 0;
                spawnTimer = 0f;
                waveWaitTimer = 0f;
                waitingForNextWave = false;
                bossSpawned = false;
            }
            _previousZone = gm.CurrentZone; 
            global::Last_Hope.Systems.RunSaveManager.SaveRun(gm);
        }

        int currentEnemyCount = 0;
        foreach (var gameObject in gm._gameObjects)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }
        foreach (var gameObject in gm._toBeAdded)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }

        int targetEnemiesForWave = GetTargetEnemiesForWave(currentWave);
        int finalWave = TotalWaves;

        if (gm.CurrentZone == Zone.Village &&
            currentWave == finalWave &&
            bossSpawned &&
            spawnedThisWave >= targetEnemiesForWave &&
            currentEnemyCount == 0)
        {
            if (!gm.VillageCleared)
            {
                gm.VillageCleared = true;
                Systems.RunSaveManager.SaveRun(gm);
            }
            return;
        }

        if (waitingForNextWave)
        {
            if (currentEnemyCount == 0)
            {
                if (currentWave == finalWave)
                {
                    if (gm.CurrentZone == Zone.Village)
                    {
                        if (!gm.VillageCleared) { gm.VillageCleared = true; Systems.RunSaveManager.SaveRun(gm); }                    }
                    else
                    {
                        gm._state = GameState.Winner;
                    }
                    return;
                }

                waveWaitTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (waveWaitTimer >= wavePause)
                {
                    waitingForNextWave = false;
                    waveWaitTimer = 0f;
                    currentWave++;
                    spawnedThisWave = 0;
                    Systems.RunSaveManager.SaveRun(gm);
                }
            }
            return;
        }

        if (BossAppearsOnLastWave && currentWave == finalWave && !bossSpawned)
        {
            if (gm.CurrentZone == Zone.Village)
            {
                Point bossSpawnPos = GetValidSpawnPoint();
                gm.AddGameObject(new Boss(bossSpawnPos));
            }
            else
            {
                Point bossSpawnPos = GetValidSpawnPoint(inForest: true);
                gm.AddGameObject(new SpiderBoss(bossSpawnPos));
            }
            bossSpawned = true;
        }

        if (spawnedThisWave >= targetEnemiesForWave)
        {
            if (!waitingForNextWave)
            {
                waitingForNextWave = true;
                waveWaitTimer = 0f;
            }
            return;
        }

        spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            bool inForest = gm.CurrentZone == Zone.Forest;
            Point spawnPosition = GetValidSpawnPoint(inForest: inForest, collisionSize: 96);
            if (inForest)
            {
                gm.AddGameObject(new Troll(spawnPosition));
                gm.AddGameObject(new Wolf(spawnPosition));

            }
            else
            {
                if (gm.RNG.NextDouble() < 0.5)
                    gm.AddGameObject(new Goblin(spawnPosition, new Bow(name: "Goblin Bow", speed: 300f, owner: null)));
                else
                    gm.AddGameObject(new Orc(spawnPosition));
            }
            spawnedThisWave++;
        }
    }

    /// <summary>
    /// Attempts to find a safe, off screen location to spawn an enemy that does not overlap with static map geometry.
    /// </summary>
    /// <param name="radius">The distance from the player to search for a spawn point.</param>
    /// <returns>A valid world coordinate for spawning, or a fallback location if no valid spot is found within the allowed attempts.</returns>
    private Point GetValidSpawnPoint(float radius = 1500f, bool inForest = false, int collisionSize = 160)
    {
        const int maxAttempts = 25;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 pos = RandomOffScreenLocation(radius, inForest);

            var rect = new Rectangle((int)pos.X, (int)pos.Y, collisionSize, collisionSize);
            var collider = new RectangleCollider(rect);

            if (!CollisionWorld.CollidesWithStatic(collider))
                return rect.Location;
        }

        // fallback (safe but rare)
        return RandomOffScreenLocation(radius, inForest).ToPoint();
    }

    /// <summary>
    /// Calculates a random point at a fixed distance around the player.
    /// </summary>
    /// <param name="distance">The radius of the circle around the player.</param>
    /// <returns>A vector coordinate at the specified distance.</returns>
    public Vector2 RandomOffScreenLocation(float distance = 1400f, bool inForest = false)
    {
        var gm = GameManager.GetGameManager();
        if (gm._player == null)
            return Vector2.Zero;

        Vector2 playerPos = gm._player.GetPosition();

        float angle = (float)(gm.RNG.NextDouble() * Math.PI * 2);

        Vector2 pos = playerPos + new Vector2(
            (float)Math.Cos(angle),
            (float)Math.Sin(angle)
        ) * distance;

        // Clamp into the active zone so enemies don't spawn across the boundary.
        if (gm.ForestBoundaryX > 0f)
        {
            if (inForest)
                pos.X = Math.Min(pos.X, gm.ForestBoundaryX);
            else
                pos.X = Math.Max(pos.X, gm.ForestBoundaryX);
        }

        return pos;
    }

    /// <summary>
    /// Calculates the number of enemies that should spawn in a specific wave using an exponential growth curve.
    /// </summary>
    /// <param name="wave">The wave number to calculate for.</param>
    /// <returns>The calculated number of enemies, capped by the configured maximum limits.</returns>
    private int GetTargetEnemiesForWave(int wave)
    {
        // Calculate the target enemies based on the starting amount and the multiplier.
        int enemies = (int)(StartingEnemies * Math.Pow(EnemyMultiplierPerWave, wave - 1));

        if (UseMaxEnemyLimit)
        {
            enemies = Math.Min(enemies, MaxEnemiesPerWave);
        }

        return Math.Max(1, enemies); // Make sure there is always at least 1 enemy
    }

    private static int CountActiveEnemies(GameManager gm)
    {
        int currentEnemyCount = 0;
        foreach (var gameObject in gm._gameObjects)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }
        foreach (var gameObject in gm._toBeAdded)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }

        return currentEnemyCount;
    }

    /// <summary>
    /// Gets a random location off the screen relative to the player position, within World bounds.
    /// </summary>
    public Vector2 RandomOffScreenLocation()
    {
        var gm = GameManager.GetGameManager();
        if (gm._player == null) return gm.RandomScreenLocation();

        Vector2 playerPos = gm._player.GetPosition();

        // Pick a random angle (0 to 360 degrees)
        float angle = (float)(gm.RNG.NextDouble() * Math.PI * 2);

        float distance = gm.RNG.Next(1200, 1500);

        Vector2 spawnPos = playerPos + new Vector2(
            (float)Math.Cos(angle) * distance, 
            (float)Math.Sin(angle) * distance
        );

        // Keep enemies inside world boundaries and in the village zone
        float minX = MathHelper.Max(0, gm.ForestBoundaryX);
        spawnPos.X = MathHelper.Clamp(spawnPos.X, minX, GameManager.WorldWidth);
        spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 0, GameManager.WorldHeight);

        return spawnPos;
    }

    /// <summary>
    /// Resets the spawner state back to the beginning of wave 1. Used when starting a new run.
    /// </summary>
    public void LoadWaveState(int wave, bool boss)
    {
        currentWave = wave;
        bossSpawned = boss;
    }

    public void Reset()
    {
        spawnTimer = 0f;
        spawnInterval = 0.2f;
        currentWave = 1;
        spawnedThisWave = 0;
        waveWaitTimer = 0f;
        waitingForNextWave = false;
        bossSpawned = false;
        _previousZone = Zone.Village;
    }
}
