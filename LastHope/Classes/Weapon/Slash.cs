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
        private double lifespan = .25f;
        private int attackDamage;
        private float critChance;
        private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();

        public Slash(Collider collider, int attackDamage, float critChance)
        {
            this.collider = collider;
            this.attackDamage = attackDamage;
            this.critChance = critChance;
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
            if (collider is ArcCollider arcCollider)
            {
                foreach (var linePiece in arcCollider.ArcSegments)
                {
                    Rectangle target = new Rectangle((int)linePiece.Start.X, (int)linePiece.Start.Y, sprite.Width, (int)linePiece.Length);
                    spriteBatch.Draw(sprite, target, null, Color.White, linePiece.GetAngle(), new Vector2(sprite.Width * 0.03f, sprite.Height * 0.03f), SpriteEffects.None, 1);
                }
            }
            base.Draw(gameTime, spriteBatch);
        }
    }
}
