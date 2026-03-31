using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Last_Hope.Classes.Weapon
{
    internal class Slash : GameObject
    {
        private Collider collider;
        private Texture2D sprite;
        private AnimationManager animation;
        private int attackDamage;
        private float critChance;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
        private Vector2 origin;
        private Vector2 direction;

        public Slash(Collider collider, int attackDamage, float critChance, Vector2 origin, Vector2 direction)
        {
            this.collider = collider;
            this.attackDamage = attackDamage;
            this.critChance = critChance;
            this.origin = origin;
            this.direction = direction;
            SetCollider(collider);
        }

        public override void Load(ContentManager content)
        {
            base.Load(content);
            sprite = content.Load<Texture2D>("Slash");
            animation = new AnimationManager(3, 3, new Vector2(sprite.Width / 3, sprite.Height), 5);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            animation.Update();
            if (animation.isFinished)
                GameManager.GetGameManager().RemoveGameObject(this);
        }

        public override void OnCollision(GameObject other)
        {
            // Only hit each enemy once per slash
            if (hitEnemies.Contains(other))
                return;

            // Calculate and apply damage
            int damage = CalculateDamage();

            if (other is BaseEnemy enemy)
            {
                
                hitEnemies.Add(other);
                enemy.Damage(damage);
                if (enemy.CurrentHealth <= 0)
                {
                    GameManager.GetGameManager()._player.AddExperience(enemy.ExperienceValue);
                    GameManager.GetGameManager().RemoveGameObject(enemy);
                }
            }


            // TODO: Implement when you have an enemy/health system

        }

        private int CalculateDamage()
        {
            // Roll for crit
            if (GameManager.GetGameManager().RNG.NextSingle() < critChance)
            {
                return (int)(attackDamage * 2f);  // 1.5x damage on crit
            }
            return attackDamage;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw the arc slash sprite once, rotated to face the attack direction
            float rotation = (float)Math.Atan2(direction.Y, direction.X);
            Rectangle sourceRect = animation.GetSourceRect();
            spriteBatch.Draw(sprite, origin, sourceRect, Color.White, rotation,
                new Vector2(sourceRect.Width * 0.5f, sourceRect.Height * 0.5f), 3f, SpriteEffects.None, 0);

            base.Draw(gameTime, spriteBatch);
        }
    }
}
