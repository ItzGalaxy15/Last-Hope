using Last_Hope.Animations;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Helpers;

public static class ExplosionHelper
{
    public static void Explode(Vector2 position, float radius, float damage, BaseEnemy exclude = null)
    {
        GameManager gm = GameManager.GetGameManager();
        foreach (GameObject obj in gm.GetObjectsInRadius(position, radius))
        {
            if (obj is not BaseEnemy enemy || enemy == exclude)
            {
                    continue;
            }

            enemy.Damage(damage);
            if (enemy._currentHp <= 0f)
            {
                gm._player?.AddExperience(enemy.ExperienceValue);
                gm.RemoveGameObject(enemy);
            }
        }

        gm.AddGameObject(new Explosion(
            position: position.ToPoint(),
            explosionFrameCount: 6,
            explosionColumns: 6,
            explosionRows: 1,
            explosionInterval: 4,
            scale: 2.0f,
            textureName: "explosion",
            hitboxRadius: radius));
    }
}