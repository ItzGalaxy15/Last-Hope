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
        private double lifespan = 1.0f;
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
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (lifespan < 0)
                GameManager.GetGameManager().RemoveGameObject(this);
            lifespan -= gameTime.ElapsedGameTime.TotalSeconds;
        }

        public override void OnCollision(GameObject other)
        {
            // Only hit each enemy once per slash
            if (hitEnemies.Contains(other))
                return;

            // Calculate and apply damage
            int damage = CalculateDamage();
            
            // TODO: Implement when you have an enemy/health system

        }

        private int CalculateDamage()
        {
            // Roll for crit
            if (GameManager.GetGameManager().RNG.NextSingle() < critChance)
            {
                return (int)(attackDamage * 1.5f);  // 1.5x damage on crit
            }
            return attackDamage;
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw the arc slash sprite once, rotated to face the attack direction
            float rotation = (float)Math.Atan2(direction.Y, direction.X);
            spriteBatch.Draw(sprite, origin, null, Color.White, rotation,
                new Vector2(sprite.Width * 0.5f, sprite.Height * 0.5f), 3f, SpriteEffects.None, 0);

            base.Draw(gameTime, spriteBatch);
        }
    }
}
