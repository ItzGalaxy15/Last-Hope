using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Weapon
{
    internal class EnemyArrow : GameObject
    {
        private RectangleCollider _collider;
        private Texture2D _sprite;
        private Vector2 _position;
        private Vector2 _velocity;
        private GameObject _owner;
        private float _damage;
        private float _critChance;

        private float scale = 1.5f;
        private int size = 10;

        public EnemyArrow(Vector2 origin, Vector2 direction, float speed, GameObject owner, float damage, float critChance)
        {
            _owner = owner;
            _position = origin;
            _velocity = direction * speed;
            _damage = damage;
            _critChance = critChance;
            _collider = new RectangleCollider(new Rectangle(origin.ToPoint(), new Point(size, size)));
            _collider.shape.Size = new Point(size, size);
            SetCollider(_collider);
        }

        public override void Load(ContentManager content)
        {
            _sprite = content.Load<Texture2D>("EnemyArrow");
            _collider.shape.Size = new Point((int)(_sprite.Bounds.Size.X * scale), (int)(_sprite.Bounds.Size.Y * scale));
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _position += _velocity * dt;
            _collider.shape.Location = (_position - new Vector2(_collider.shape.Width / 2f, _collider.shape.Height / 2f)).ToPoint();
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            float angle = (float)System.Math.Atan2(_velocity.Y, _velocity.X);
            Vector2 origin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);

            spriteBatch.Draw(_sprite, _position, null, Color.White, angle, origin, scale, SpriteEffects.None, 0f);
            base.Draw(gameTime, spriteBatch);
        }

        public override void OnCollision(GameObject other)
        {
            int damage = CalculateDamage();

            switch (_owner)
            {
                case BasePlayer:
                    if (other is BaseEnemy enemy)
                    {
                        enemy.Damage(damage);
                        if (enemy.CurrentHealth <= 0)
                        {
                            GameManager.GetGameManager().RemoveGameObject(enemy);
                        }
                        GameManager.GetGameManager().RemoveGameObject(this);
                    }
                    break;

                case BaseEnemy:
                    if (other is BasePlayer player)
                    {
                        player.Damage(damage);
                        if (player._currentHp <= 0)
                        {
                            GameManager.GetGameManager().RemoveGameObject(player);
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