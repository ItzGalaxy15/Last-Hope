using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;

public class EnemySpawner
{
    private const float MinSpawnInterval = 0.2f;

    private float spawnTimer = 0f;
    private float spawnInterval = 0.2f; // spawn an enemy every 0.2s

    private int currentWave = 1;
    private int spawnedThisWave = 0;
    private float waveWaitTimer = 0f;
    private bool waitingForNextWave = false;
    private float wavePause = 3f; // pause between the waves
    private bool bossSpawned = false;

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

        if (waitingForNextWave)
        {
            if (currentEnemyCount == 0)
            {
                if (currentWave == 7) // wave 7 is the boss wave 
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

        if (currentWave == 7 && !bossSpawned)
        {
            Point bossSpawnPos = RandomOffScreenLocation().ToPoint();
            gm.AddGameObject(new Boss(bossSpawnPos));
            bossSpawned = true;
        }

        if (currentEnemyCount >= 20 || spawnedThisWave >= targetEnemiesForWave)
        {
            if (spawnedThisWave >= targetEnemiesForWave && !waitingForNextWave)
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

            Point spawnPosition = RandomOffScreenLocation().ToPoint();
            if (gm.RNG.NextDouble() < 0.5)
                gm.AddGameObject(new Goblin(spawnPosition, new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
            else
                gm.AddGameObject(new Orc(spawnPosition));

            spawnedThisWave++;
        }
    }

    private int GetTargetEnemiesForWave(int wave)
    {
        switch (wave)
        {
            case 1: return 1;
            case 2: return 2;
            case 3: return 4;
            case 4: return 15;
            case 5: return 20;
            case 6: return 25;
            case 7: return 25;
            default: return 25; 
        }
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
