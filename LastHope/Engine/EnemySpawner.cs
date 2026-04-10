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

        if (waitingForNextWave)
        {
            if (currentEnemyCount == 0)
            {
                waveWaitTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (waveWaitTimer >= 5f)
                {
                    waitingForNextWave = false;
                    waveWaitTimer = 0f;
                    currentWave++;
                    spawnedThisWave = 0;
                }
            }
            return;
        }

        if (currentEnemyCount >= 20 || spawnedThisWave >= 20)
        {
            if (spawnedThisWave >= 20 && !waitingForNextWave)
            {
                if (currentWave == 4)
                {
                    // Spawn boss on 4th wave after spawning the 20 normal enemies
                    Point bossSpawnPos = RandomOffScreenLocation().ToPoint();
                    gm.AddGameObject(new Boss(bossSpawnPos));
                    // Boss spawned, wait indefinitely until boss dies (handled in GameManager)
                    waitingForNextWave = true;
                    waveWaitTimer = float.MinValue; // effectively stop waves
                }
                else
                {
                    waitingForNextWave = true;
                }
            }
            return;
        }

        spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            // spawn one enemy
            Point spawnPosition = RandomOffScreenLocation().ToPoint();
            if (gm.RNG.NextDouble() < 0.5)
                gm.AddGameObject(new Goblin(spawnPosition, new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
            else
                gm.AddGameObject(new Orc(spawnPosition));

            spawnedThisWave++;
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
    }
}
