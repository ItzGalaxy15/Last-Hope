using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using LastHope.Helper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Weapon
{
    internal class Arrow : GameObject
    {
        private RectangleCollider _collider;
        private Texture2D _sprite;
        private Vector2 _velocity;
        private Point arrowSize = new Point(10, 10);
        private GameObject _owner;
        private float _damage;
        private float _critChance;


        public Arrow(Vector2 origin, Vector2 direction, float speed, GameObject owner, float damage, float critChance)
        {
            _owner = owner;
            _collider = new RectangleCollider(new Rectangle(origin.ToPoint(), arrowSize));
            SetCollider(_collider);
            _velocity = direction * speed;
            _damage = damage;
            _critChance = critChance;
        }

        public override void Load(ContentManager content)
        {
            _sprite = content.Load<Texture2D>("Arrow");
            base.Load(content);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            base.Update(gameTime);
            _collider.shape.Location += (_velocity * dt).ToPoint();
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
                        if (player._Hp <= 0)
                        {
                            GameManager.GetGameManager().RemoveGameObject(player);
                        }
                        GameManager.GetGameManager().RemoveGameObject(this);
                    }
                    break;
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            float angle = DirectionHelper.GetAngle(_velocity);
            Vector2 origin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);
            Vector2 position = _collider.shape.Center.ToVector2();

            spriteBatch.Draw(_sprite, position, null, Color.White, angle, origin, 1f, SpriteEffects.None, 0f);
            base.Draw(gameTime, spriteBatch);
        }

        private int CalculateDamage()
        {
            if (GameManager.GetGameManager().RNG.NextSingle() < _critChance)
            {
                return (int)(_damage * 1.5f);  // 1.5x damage on crit
            }
            return (int)_damage;
        }
    }
}
