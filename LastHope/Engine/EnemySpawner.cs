using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;

public class EnemySpawner
{
    public int TotalWaves { get; set; } = 1;
    public float EnemyMultiplierPerWave { get; set; } = 1.5f;
    public int StartingEnemies { get; set; } = 20;
    public bool BossAppearsOnLastWave { get; set; } = true;
    public bool UseMaxEnemyLimit { get; set; } = true;
    public int MaxEnemiesPerWave { get; set; } = 35;

    private const float MinSpawnInterval = 0.2f;

    private float spawnTimer = 0f;
    private float spawnInterval = 0.2f; // spawn an enemy every 0.2s

    private int currentWave = 1;
    public int CurrentWave => currentWave;
    private int spawnedThisWave = 0;
    private float waveWaitTimer = 0f;
    private bool waitingForNextWave = false;
    private float wavePause = 3f; // pause between the waves
    private bool bossSpawned = false;
    public bool BossSpawned => bossSpawned;

    public int GetEnemiesLeftCount()
    {
        var gm = GameManager.GetGameManager();
        int currentEnemyCount = 0;
        foreach (var gameObject in gm._gameObjects)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }
        foreach (var gameObject in gm._toBeAdded)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }

        int unspawned = GetTargetEnemiesForWave(currentWave) - spawnedThisWave;
        if (unspawned < 0) unspawned = 0;

        int total = currentEnemyCount + unspawned;

        if (BossAppearsOnLastWave && currentWave == TotalWaves && !bossSpawned)
        {
            total++;
        }

        return total;
    }

    public void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();

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

        if (waitingForNextWave)
        {
            if (currentEnemyCount == 0)
            {
                if (currentWave == finalWave)
                {
                    gm._state = GameState.Winner;
                    return;
                }

                waveWaitTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (waveWaitTimer >= wavePause)
                {
                    waitingForNextWave = false;
                    waveWaitTimer = 0f;
                    currentWave++;
                    spawnedThisWave = 0;
                }
            }
            return;
        }

        if (BossAppearsOnLastWave && currentWave == finalWave && !bossSpawned)
        {
            Point bossSpawnPos = GetValidSpawnPoint();
            gm.AddGameObject(new Boss(bossSpawnPos));
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

            Point spawnPosition = GetValidSpawnPoint();
            if (gm.RNG.NextDouble() < 0.5)
                gm.AddGameObject(new Goblin(spawnPosition, new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
            else
                gm.AddGameObject(new Orc(spawnPosition));

            spawnedThisWave++;
        }
    }

    private Point GetValidSpawnPoint(float radius = 1500f)
    {
        var gm = GameManager.GetGameManager();

        const int maxAttempts = 25;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 pos = RandomOffScreenLocation(radius);

            int size = 96; // goblin/orc approx size

            var rect = new Rectangle((int)pos.X, (int)pos.Y, size, size);
            var collider = new RectangleCollider(rect);

            if (!CollisionWorld.CollidesWithStatic(collider))
                return rect.Location;
        }

        // fallback (safe but rare)
        return RandomOffScreenLocation(radius).ToPoint();
    }

    public Vector2 RandomOffScreenLocation(float distance = 1400f)
    {
        var gm = GameManager.GetGameManager();
        if (gm._player == null)
            return Vector2.Zero;

        Vector2 playerPos = gm._player.GetPosition();

        float angle = (float)(gm.RNG.NextDouble() * Math.PI * 2);

        return playerPos + new Vector2(
            (float)Math.Cos(angle),
            (float)Math.Sin(angle)
        ) * distance;
    }

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

        // Keep enemies inside world boundaries
        spawnPos.X = MathHelper.Clamp(spawnPos.X, 0, GameManager.WorldWidth);
        spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 0, GameManager.WorldHeight);

        return spawnPos;
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
    }
}
