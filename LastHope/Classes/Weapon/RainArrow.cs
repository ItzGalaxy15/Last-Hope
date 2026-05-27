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
    internal class RainArrow : GameObject
    {
        private RectangleCollider _collider;
        private Texture2D _sprite;
        private Vector2 _position;
        private Vector2 _velocity;
        private GameObject _owner;
        private float _damage;
        private float _critChance;
        private float _targetY;
        private bool _landed;
        private bool hasPiercingArrows;
        private bool hasPoisonArrows;
        private bool hasSpreadPoison;
        private bool hasIncreasedPoisonDamage;
        private bool hasExplosiveArrows;
        private bool hasIncreasedExplosionRadius;
        private bool hasIncreasedExplosionDamage;
        private bool _hasClusterBomb;
        private Action<BaseEnemy> _onHitEnemy;

        private float ExplosionRadius = 100f;

        private const float PoisonDamagePerTick = 5f;
        private const float ExplosionDamageMultiplier = 1.5f;
        private const float ExplosionRadiusMultiplier = 1.5f;
        private const float ExplosionSplashMultiplier = 0.5f;
        private const float PenetrationDepth = 80f;

        private HashSet<GameObject> _alreadyHit = new HashSet<GameObject>();

        public RainArrow(Vector2 origin, float targetY, float speed, GameObject owner, float damage,
                     float critChance, bool piercingArrows, bool poisonArrows, bool spreadPoison,
                     bool increasedPoisonDamage, bool explosiveArrows, bool increasedExplosionRadius,
                     bool increasedExplosionDamage, bool clusterBomb, Action<BaseEnemy> onHitEnemy = null)
        {
            _owner = owner;
            _position = origin;
            _velocity = new Vector2(0f, speed); // always falls straight down
            _damage = damage;
            _critChance = critChance;
            _targetY = targetY;
            _landed = false;
            hasPiercingArrows = piercingArrows;
            hasPoisonArrows = poisonArrows;
            hasSpreadPoison = spreadPoison;
            hasIncreasedPoisonDamage = increasedPoisonDamage;
            hasExplosiveArrows = explosiveArrows;
            hasIncreasedExplosionRadius = increasedExplosionRadius;
            hasIncreasedExplosionDamage = increasedExplosionDamage;
            _hasClusterBomb = clusterBomb;
            _onHitEnemy = onHitEnemy;

            // No collider until landed
            _collider = null;
        }

        public override void Load(ContentManager content)
        {
            _sprite = content.Load<Texture2D>(hasPoisonArrows ? "PoisonArrow" : "Arrow");
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _position += _velocity * dt;

            // Activate hitbox once the arrow reaches its target Y
            if (!_landed && _position.Y >= _targetY)
            {
                _landed = true;
                _collider = new RectangleCollider(
                    new Rectangle(_position.ToPoint(), new Point(10, 10)));
                SetCollider(_collider);
            }

            // Sync collider position while active
            if (_landed && _collider != null)
            {
                _collider.shape.Location = _position.ToPoint();
                SetCollider(_collider);
            }

            // Despawn after sticking into the ground
            if (_position.Y >= _targetY + PenetrationDepth)
            {
                GameManager.GetGameManager().RemoveGameObject(this);
                return;
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (_sprite is null) return;

            float angle = (float)Math.Atan2(_velocity.Y, _velocity.X);
            Vector2 origin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);
            spriteBatch.Draw(_sprite, _position, null, Color.Blue, angle, origin, 1f, SpriteEffects.None, 0f);
            base.Draw(gameTime, spriteBatch);
        }

        public override void OnCollision(GameObject other)
        {
            // Ignore all collisions until landed
            if (!_landed) return;

            if (_alreadyHit.Contains(other)) return;
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
                            enemy.isPoisoned(true, hasIncreasedPoisonDamage
                                ? PoisonDamagePerTick * 2
                                : PoisonDamagePerTick);
                        }

                        if (hasSpreadPoison)
                            enemy.EnablePoisonSpreading();

                        if (hasExplosiveArrows)
                        {
                            if (hasIncreasedExplosionRadius)
                                ExplosionRadius *= ExplosionRadiusMultiplier;

                            if (hasIncreasedExplosionDamage)
                                damage = (int)(damage * ExplosionDamageMultiplier);

                            int splashDamage = (int)(damage * ExplosionSplashMultiplier);
                            ExplosionHelper.Explode(_position, ExplosionRadius, splashDamage, enemy);

                            if (_hasClusterBomb)
                            {
                                Vector2 direction = Vector2.Normalize(_velocity);
                                Vector2 perpendicular = new Vector2(-direction.Y, direction.X);

                                float behindDistance = ExplosionRadius * 0.75f;
                                float sideOffset = ExplosionRadius * 0.4f;
                                float clusterRadius = ExplosionRadius * 0.9f;
                                Vector2 behindPosition = _position + direction * behindDistance;

                                ExplosionHelper.Explode(behindPosition, clusterRadius, splashDamage, enemy);
                                ExplosionHelper.Explode(behindPosition - perpendicular * sideOffset, clusterRadius, splashDamage, enemy);
                                ExplosionHelper.Explode(behindPosition + perpendicular * sideOffset, clusterRadius, splashDamage, enemy);
                            }

                            GameManager.GetGameManager().RemoveGameObject(this);
                            return;
                        }

                        if (hasPiercingArrows) return;

                        GameManager.GetGameManager().RemoveGameObject(this);
                    }
                    break;
            }
        }

        private int CalculateDamage()
        {
            return GameManager.GetGameManager().RNG.NextSingle() < _critChance
                ? (int)(_damage * 1.5f)
                : (int)_damage;
        }
    }
}