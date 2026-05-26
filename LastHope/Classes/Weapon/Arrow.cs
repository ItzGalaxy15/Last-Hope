using System;
using System.Collections.Generic;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Last_Hope.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Weapon
{
    internal class Arrow : GameObject
    {
        private RectangleCollider _collider;
        private Texture2D _sprite;
        private Vector2 _position;
        private Vector2 _velocity;
        private GameObject _owner;
        private float _damage;
        private float _critChance;
        private bool hasPiercingArrows;
        private bool hasPoisonArrows;
        private bool hasSpreadPoison;
        private bool hasIncreasedPoisonDamage;
        private bool hasExplosiveArrows;
        private bool hasIncreasedExplosionRadius;
        private bool hasIncreasedExplosionDamage;
        private Action<BaseEnemy> _onHitEnemy;

        private float ExplosionRadius = 75f;

        private const float PoisonDamagePerTick = 5f;
        private const float ExplosionDamageMultiplier = 1.5f;
        private const float ExplosionRadiusMultiplier = 1.5f;
        private const float ExplosionSplashMultiplier = 0.5f;

        private HashSet<GameObject> _alreadyHit = new HashSet<GameObject>();

        public Arrow(Vector2 origin, Vector2 direction, float speed, GameObject owner, float damage,
                     float critChance, bool piercingArrows, bool poisonArrows, bool spreadPoison, 
                     bool increasedPoisonDamage, bool explosiveArrows, bool increasedExplosionRadius, 
                     bool increasedExplosionDamage, Action<BaseEnemy> onHitEnemy = null)
        {
            _owner = owner;
            _position = origin;
            _velocity = direction * speed;
            _damage = damage;
            _critChance = critChance;
            hasPiercingArrows = piercingArrows;
            hasPoisonArrows = poisonArrows;
            hasSpreadPoison = spreadPoison;
            hasIncreasedPoisonDamage = increasedPoisonDamage;
            hasExplosiveArrows = explosiveArrows;
            hasIncreasedExplosionRadius = increasedExplosionRadius;
            hasIncreasedExplosionDamage = increasedExplosionDamage;
            _onHitEnemy = onHitEnemy;
            _collider = new RectangleCollider(new Rectangle(origin.ToPoint(), new Point(10, 10)));
            SetCollider(_collider);
        }

        public override void Load(ContentManager content)
        {
            if (hasPoisonArrows)
            {
                _sprite = content.Load<Texture2D>("PoisonArrow");
            }
            else
            {
                _sprite = content.Load<Texture2D>("Arrow");
            }
            _collider.shape.Size = _sprite.Bounds.Size;
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 nextPosition = _position + _velocity * dt;

            Rectangle nextRect = new Rectangle(
                (int)(nextPosition.X - _collider.shape.Width / 2f),
                (int)(nextPosition.Y - _collider.shape.Height / 2f),
                _collider.shape.Width,
                _collider.shape.Height
            );

            var testCollider = new RectangleCollider(nextRect);

            // 🧱 BLOCKED BY WORLD
            if (CollisionWorld.CollidesWithStatic(testCollider))
            {
                GameManager.GetGameManager().RemoveGameObject(this);
                return;
            }

            // Move if no collision
            _position = nextPosition;
            _collider.shape.Location = nextRect.Location;

            base.Update(gameTime);
        }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            float angle = (float)System.Math.Atan2(_velocity.Y, _velocity.X);
            Vector2 origin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);

            spriteBatch.Draw(_sprite, _position, null, Color.White, angle, origin, 1f, SpriteEffects.None, 0f);
            base.Draw(gameTime, spriteBatch);
        }

        public override void OnCollision(GameObject other)
        {
            if (_alreadyHit.Contains(other))
            {
                return;
            }
            _alreadyHit.Add(other);

            int damage = CalculateDamage();

            switch (_owner)
            {
                case BasePlayer:
                    if (other is BaseEnemy enemy)
                    {
                        enemy.Damage(damage);
                        _onHitEnemy?.Invoke(enemy);
                        if (enemy._currentHp <= 0)
                        {
                            GameManager.GetGameManager()._player?.AddExperience(enemy.ExperienceValue);
                            GameManager.GetGameManager().RemoveGameObject(enemy);
                        }
                        if (hasPoisonArrows)
                        {
                            if (hasIncreasedPoisonDamage)
                            {
                                enemy.isPoisoned(true, PoisonDamagePerTick * 2);
                            }
                            else
                            {
                                enemy.isPoisoned(true, PoisonDamagePerTick);
                            }
                        }
                        if (hasSpreadPoison)
                        {
                            enemy.EnablePoisonSpreading();
                        }
                        if (hasExplosiveArrows)
                        {
                            if (hasIncreasedExplosionRadius)
                            {
                                ExplosionRadius = ExplosionRadius * ExplosionRadiusMultiplier;
                            }
                            if (hasIncreasedExplosionDamage)
                            {
                                damage = (int)(damage * ExplosionDamageMultiplier);
                            }
                            ExplosionHelper.Explode(_position, ExplosionRadius, (int)(damage * ExplosionSplashMultiplier), enemy);
                            GameManager.GetGameManager().RemoveGameObject(this);
                            return;
                        }
                        if (hasPiercingArrows)
                        {
                            // Piercing arrows continue flying, so we don't remove it on hit
                            return;
                        }
                        GameManager.GetGameManager().RemoveGameObject(this);
                    }
                    break;
            }
        }

        private int CalculateDamage()
        {
            if (GameManager.GetGameManager().RNG.NextSingle() < _critChance)
            {
                return (int)(_damage * 1.5f);
            }
            return (int)_damage;
        }
    }
}