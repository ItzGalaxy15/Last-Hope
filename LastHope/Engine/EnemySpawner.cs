using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine;

public class EnemySpawner
{
    private const float MinSpawnInterval = 0.5f;

    private float spawnTimer = 0f;
    private float spawnInterval = 5f;
    private int spawnCount = 1;

    public void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();

        // How many enemies are active now
        int currentEnemyCount = 0;
        foreach (var gameObject in gm._gameObjects)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }
        foreach (var gameObject in gm._toBeAdded)
        {
            if (gameObject is BaseEnemy) currentEnemyCount++;
        }

        if (currentEnemyCount >= 20)
        {
            return;
        }

        spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            // spawn up to the limit 
            int enemiesToSpawn = Math.Min(spawnCount, 20 - currentEnemyCount);

            for (int i = 0; i < enemiesToSpawn; i++)
            {
                Point spawnPosition = RandomOffScreenLocation().ToPoint();
                if (gm.RNG.NextDouble() < 0.5)
                    gm.AddGameObject(new Goblin(spawnPosition, new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
                else
                    gm.AddGameObject(new Orc(spawnPosition));
            }

            spawnInterval -= 0.5f;

            if (spawnInterval < MinSpawnInterval)
            {
                spawnInterval = MinSpawnInterval;
                spawnCount++;
            }
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
        spawnInterval = 5f;
        spawnCount = 1;
    }
}
