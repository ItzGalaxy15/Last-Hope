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
        private Vector2 lastOffset = Vector2.Zero;
        private Vector2 lastPlayerPos;
        private float visualExpand;
        private float hitboxExpand;
        private const bool DebugDrawHitbox = false;

        public Slash(Collider collider, int attackDamage, float critChance, Vector2 origin, Vector2 direction, float visualExpand, float hitboxExpand)
        {
            this.collider = collider;
            this.attackDamage = attackDamage;
            this.critChance = critChance;
            this.origin = origin;
            this.direction = direction;
            this.visualExpand = visualExpand;
            this.hitboxExpand = hitboxExpand;
            this.lastPlayerPos = GameManager.GetGameManager()._player.GetPosition();
            SetCollider(collider);
        }

        public override void Load(ContentManager content)
        {
            base.Load(content);
            sprite = content.Load<Texture2D>("Slash-sheet");
            animation = new AnimationManager(5, 5, new Vector2(sprite.Width / 5, sprite.Height), 5);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            animation.Update();

            Vector2 playerPos = GameManager.GetGameManager()._player.GetPosition();
            Vector2 playerDelta = playerPos - lastPlayerPos;
            lastPlayerPos = playerPos;
            origin += playerDelta;

            float t = (animation.ActiveFrame + animation.FrameProgress) / 4f;
            Vector2 currentOffset = direction * (t * hitboxExpand);
            Vector2 expansionDelta = currentOffset - lastOffset;
            lastOffset = currentOffset;

            Vector2 totalDelta = playerDelta + expansionDelta;
            if (collider is ArcCollider arc && totalDelta != Vector2.Zero)
            {
                foreach (var segment in arc.ArcSegments)
                {
                    segment.Start += totalDelta;
                    segment.End += totalDelta;
                }
                arc.Center += totalDelta;
            }

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
            float t = (animation.ActiveFrame + animation.FrameProgress) / 4f;
            Vector2 drawPos = origin + direction * (t * visualExpand);
            spriteBatch.Draw(sprite, drawPos, sourceRect, Color.White, rotation,
                new Vector2(sourceRect.Width * 0.5f, sourceRect.Height * 0.5f), 3.3f, SpriteEffects.None, 0);

            // Debug: draw arc hitbox
            if (DebugDrawHitbox && collider is ArcCollider arc)
            {
                Texture2D pixel = GameManager.GetGameManager().Pixel;
                foreach (var segment in arc.ArcSegments)
                {
                    Vector2 diff = segment.End - segment.Start;
                    float len = diff.Length();
                    float angle = (float)Math.Atan2(diff.Y, diff.X);
                    spriteBatch.Draw(pixel, segment.Start, null, Color.Red * 0.8f, angle, Vector2.Zero, new Vector2(len, 2f), SpriteEffects.None, 0f);
                }
            }

            base.Draw(gameTime, spriteBatch);
        }
    }
}
