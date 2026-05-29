using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Last_Hope.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class SpiderBoss : BaseEnemy
{
    private const float SpriteScale = 2f;
    private const bool DebugDrawHitbox = false;

    private Vector2 _precisePosition;
    private AnimationManager _walkAnimation;
    private AnimationManager _attackAnimation;
    private bool _isAttacking;
    private float _attackCooldownTimer;
    private float _postHitPauseTimer;
    private const float PostHitPauseDuration = 1f;
    private SpiderDir _facing = SpiderDir.Down;
    private SpiderDir _lastFacing = SpiderDir.Down;

    private const int FrameSize = 64;
    private const int SheetColumns = 5;
    private const int WalkStartColumn = 0;
    private const int WalkFrameCount = 3;
    private const int AttackStartColumn = 3;
    private const int AttackFrameCount = 2;

    private const float BitePoisonDamage = 3f;

    private float FullSize => FrameSize * SpriteScale;
    private float HitboxSize => FullSize * 0.5f;
    private float HitboxOffset => (FullSize - HitboxSize) / 2f;

    public override float BaseMaxHp { get; } = 1500;
    public override int BaseDamage { get; } = 15;
    public override float BaseCritChance { get; } = 0f;
    public override float BaseHaste { get; } = 0.8f;
    public override float BaseSpeed { get; } = 220f;
    public override float ExperienceValue { get; protected set; } = 100f;

    public override float CurrentMaxHp { get; protected set; }
    public override int CurrentDamage { get; protected set; }
    public override float CurrentCritChance { get; protected set; }
    public override float CurrentHaste { get; protected set; }
    public override float CurrentSpeed { get; protected set; }

    private enum SpiderDir
    {
        Down = 0,
        Up = 1,
        Left = 2,
        Right = 3
    }

    public SpiderBoss(Point position) : base()
    {
        int size = (int)(FrameSize * SpriteScale);
        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);
        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Spider");
        ResetWalkAnimation(_facing);
        ResetAttackAnimation(_facing);

        _collider.shape.Size = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _precisePosition = _collider.shape.Location.ToVector2();
    }

    protected override void UpdateBehavior(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        var player = gm._player;
        var decoy = gm.ActiveDecoy;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdatePoison(dt);

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer = Math.Max(0f, _attackCooldownTimer - dt);
        if (_postHitPauseTimer > 0f)
            _postHitPauseTimer = Math.Max(0f, _postHitPauseTimer - dt);

        if (player == null && decoy == null)
            return;

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();
        _facing = DirectionFromVector(toTarget);
        if (_facing != _lastFacing && !_isAttacking)
        {
            ResetWalkAnimation(_facing);
            _lastFacing = _facing;
        }

        // Walks through all collision: no WouldCollideAt check, no NavigationGrid pathing.
        Vector2 direction = toTarget;
        if (direction != Vector2.Zero)
            direction.Normalize();

        Vector2 movement = direction * CurrentSpeed * dt;
        if (_isPoisoned)
            movement *= 0.75f;

        if (_postHitPauseTimer <= 0f)
            _precisePosition += movement;
        _collider.shape = new Rectangle(
            (int)(_precisePosition.X + HitboxOffset),
            (int)(_precisePosition.Y + HitboxOffset),
            (int)HitboxSize,
            (int)HitboxSize);

        if (_isAttacking)
        {
            _attackAnimation.Update();
            if (_attackAnimation.isFinished)
            {
                _isAttacking = false;
                ResetWalkAnimation(_facing);
                _lastFacing = _facing;
            }
        }
        else
        {
            _walkAnimation.Update();
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _precisePosition + new Vector2(FullSize / 2f);

        if (DebugDrawHitbox && _collider is not null)
            HitboxHelper.DrawHitbox(spriteBatch, _collider.shape, Color.Red);

        Rectangle sourceRect = _isAttacking
            ? _attackAnimation.GetSourceRect()
            : _walkAnimation.GetSourceRect();

        spriteBatch.Draw(
            _texture,
            center,
            sourceRect,
            DrawTint,
            0f,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            SpriteScale,
            SpriteEffects.None,
            0f);

        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (_isPoisoned && _poisonSpreads && other is BaseEnemy otherEnemy && !otherEnemy._isPoisoned)
        {
            otherEnemy.isPoisoned(true, PoisonDamagePerTick);
            otherEnemy.EnablePoisonSpreading();
        }

        if ((other is not BasePlayer && other is not Decoy) || _isAttacking || _attackCooldownTimer > 0f)
            return;

        if (other is Decoy decoy)
            decoy.Damage(10f);
        else if (other is BasePlayer player)
        {
            player.ApplyPoison(BitePoisonDamage);
            _postHitPauseTimer = PostHitPauseDuration;
        }

        _isAttacking = true;
        _attackCooldownTimer = CurrentHaste;
        ResetAttackAnimation(_facing);
    }

    public override Vector2 GetPosition() => _collider.shape.Center.ToVector2();

    private static SpiderDir DirectionFromVector(Vector2 v)
    {
        if (v == Vector2.Zero)
            return SpiderDir.Down;
        if (Math.Abs(v.Y) >= Math.Abs(v.X))
            return v.Y >= 0f ? SpiderDir.Down : SpiderDir.Up;
        return v.X >= 0f ? SpiderDir.Right : SpiderDir.Left;
    }

    private void ResetWalkAnimation(SpiderDir dir) =>
        _walkAnimation = new AnimationManager(
            WalkFrameCount, SheetColumns,
            new Vector2(FrameSize, FrameSize),
            interval: 10, loop: true,
            offsetX: WalkStartColumn * FrameSize,
            offsetY: (int)dir * FrameSize);

    private void ResetAttackAnimation(SpiderDir dir) =>
        _attackAnimation = new AnimationManager(
            AttackFrameCount, SheetColumns,
            new Vector2(FrameSize, FrameSize),
            interval: 6, loop: false,
            offsetX: AttackStartColumn * FrameSize,
            offsetY: (int)dir * FrameSize);
}
