using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Classes.Items;
using Last_Hope.Engine;
using Last_Hope.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Troll : BaseEnemy
{
    private const float SpriteScale = 1.5f;
    private const bool DebugDrawHitbox = false;
    // private const float StunDurationSeconds = 0.5f;

    private Vector2 _precisePosition;
    private AnimationManager _walkingAnimation;
    private AnimationManager _attackAnimation;
    private bool _isAttacking = false;
    private bool _isFacingLeft = false;
    private float _attackCooldownTimer = 0f;
    private Texture2D _clubTexture;

    private const int TrollFacingRightRow = 0;
    private const int WalkingStartColumn = 0;
    private const int WalkingFrameCount = 3;
    private const int AttackStartColumn = 3;
    private const int AttackFrameCount = 1;
    private const int SheetColumns = 8;
    private const int FrameSize = 64;
    private const float ClubSize = 32f;

    private static readonly Vector2 ClubOffsetRight = new Vector2(14f, -20f);
    private static readonly Vector2 ClubOffsetLeft = new Vector2(-17f, -20f);
    private static readonly Vector2 ClubAttackOffsetRight = new Vector2(20f, 8f);
    private static readonly Vector2 ClubAttackOffsetLeft = new Vector2(-20f, 8f);

    private float FullSize => FrameSize * SpriteScale;
    private float HitboxSize => FullSize * 0.55f;
    private float HitboxOffset => (FullSize - HitboxSize) / 2f;

    // Base Troll stats
    public override float BaseMaxHp { get; } = 140f;
    public override int BaseDamage { get; } = 5;
    public override float BaseCritChance { get; } = 0f;
    public override float BaseHaste { get; } = 0.7f; // Attack cooldown
    public override float BaseSpeed { get; } = 30f;
    public override float ExperienceValue { get; protected set; } = 4f;

    // Current Troll Stats
    public override float CurrentMaxHp { get; protected set; }
    public override int CurrentDamage { get; protected set; }
    public override float CurrentCritChance { get; protected set; }
    public override float CurrentHaste { get; protected set; }
    public override float CurrentSpeed { get; protected set; }

    public Troll(Point position) : base()
    {
        int size = (int)(FrameSize * SpriteScale);

        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);

        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Troll");
        _clubTexture = content.Load<Texture2D>("Club");

        _walkingAnimation = new AnimationManager(
            WalkingFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkingStartColumn * FrameSize,
            TrollFacingRightRow * FrameSize
        );

        _attackAnimation = new AnimationManager(
            AttackFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            6,
            false,
            AttackStartColumn * FrameSize,
            TrollFacingRightRow * FrameSize
        );

        var scaledSize = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _collider.shape.Size = scaledSize;

        _precisePosition = _collider.shape.Location.ToVector2();
    }

    protected override void UpdateBehavior(GameTime gameTime)
    {
        var gameManager = GameManager.GetGameManager();
        var player = gameManager._player;
        var decoy = gameManager.ActiveDecoy;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdatePoison(dt);

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer = Math.Max(0f, _attackCooldownTimer - dt);

        if (player == null && decoy == null)
            return;

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        Vector2 direction;

        if (gameManager.NavigationGrid != null &&
            gameManager.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir) && pathDir != Vector2.Zero)
        {
            direction = pathDir;
        }
        else
        {
            direction = toTarget;
            if (direction != Vector2.Zero)
                direction.Normalize();
        }

        Vector2 movement = direction * CurrentSpeed * dt;
        if (_isPoisoned)
        {
            movement *= 0.75f; // Apply slow effect when poisoned
        }

        // X axis
        Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
        if (!WouldCollideAt(newPosX, HitboxSize, HitboxOffset))
            _precisePosition = newPosX;

        // Y axis
        Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
        if (!WouldCollideAt(newPosY, HitboxSize, HitboxOffset))
            _precisePosition = newPosY;

        _collider.shape = new Rectangle((int)(_precisePosition.X + HitboxOffset), (int)(_precisePosition.Y + HitboxOffset), (int)HitboxSize, (int)HitboxSize);

        if (!_isAttacking)
        {
            _walkingAnimation.Update();
        }
        else
        {
            _attackAnimation.Update();
            if (_attackAnimation.isFinished)
            {
                _isAttacking = false;
                ResetWalkAnimation();
            }
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _precisePosition + new Vector2(FullSize / 2f);

        if (DebugDrawHitbox && _collider is not null)
            HitboxHelper.DrawHitbox(spriteBatch, _collider.shape, Color.Red);

        Rectangle sourceRect;
        int row = _isFacingLeft ? TrollFacingRightRow + 1 : TrollFacingRightRow;

        if (_isAttacking)
        {
            sourceRect = _attackAnimation.GetSourceRect();
            sourceRect.Y = row * FrameSize;
        }
        else
        {
            sourceRect = _walkingAnimation.GetSourceRect();
            sourceRect.Y = row * FrameSize;
        }

        float swingProgress = _isAttacking ? _attackAnimation.FrameProgress : 0f;
        Vector2 idleClubOffset = _isFacingLeft ? ClubOffsetLeft : ClubOffsetRight;
        Vector2 attackClubOffset = _isFacingLeft ? ClubAttackOffsetLeft : ClubAttackOffsetRight;
        Vector2 clubOffset = _isAttacking
            ? Vector2.Lerp(idleClubOffset, attackClubOffset, swingProgress)
            : idleClubOffset;

        Vector2 clubPosition = center + (clubOffset * SpriteScale);
        Vector2 clubOrigin = new Vector2(ClubSize / 2f, ClubSize);

        spriteBatch.Draw(
            _texture,
            center,
            sourceRect,
            DrawTint,
            0f,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            SpriteScale,
            SpriteEffects.None,
            0f
        );

        spriteBatch.Draw(
            _clubTexture,
            clubPosition,
            null,
            DrawTint,
            0f,
            clubOrigin,
            1f,
            SpriteEffects.None,
            0f
        );

        //spriteBatch.Draw(
        //    _texture,
        //    center,
        //    sourceRect,
        //    DrawTint,
        //    0f,
        //    new Vector2(FrameSize / 2f, FrameSize / 2f),
        //    SpriteScale,
        //    SpriteEffects.None,
        //    0f
        //);

        base.Draw(gameTime, spriteBatch);
    }

    private void ResetWalkAnimation()
    {
        _walkingAnimation = new AnimationManager(
            WalkingFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkingStartColumn * FrameSize,
            TrollFacingRightRow * FrameSize
        );
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
        {
            decoy.Damage(10f);
        }
        //else if (other is BasePlayer player)
        //{
        //    player.ApplyStun(StunDurationSeconds);
        //}

        _isAttacking = true;
        _attackCooldownTimer = CurrentHaste;

        _attackAnimation = new AnimationManager(
            AttackFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            6,
            false,
            AttackStartColumn * FrameSize,
            TrollFacingRightRow * FrameSize
        );
    }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }
}
