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

public class Wolf : BaseEnemy
{
    private const float SpriteScale = 2.5f;
    private const bool DebugDrawHitbox = false;

    private Vector2 _precisePosition;
    private AnimationManager _walkingAnimation;
    private bool _isFacingLeft = false;

    // Lunge state machine
    private enum WolfState { Walking, Lunging, Recovering }
    private WolfState _state = WolfState.Walking;
    private Vector2 _lungeDirection = Vector2.Zero;
    private float _lungeTimer = 0f;
    private float _recoveryTimer = 0f;
    private float _lungeCooldownTimer = 0f;
    private const float LungeDuration = 0.5f;
    private const float LungeRecoverySeconds = 0.8f;
    private const float LungeSpeed = 500f;
    private const float LungeMinRange = 150f;
    private const float LungeMaxRange = 380f;
    private const float LungeCooldownSeconds = 4f;

    // Melee cooldown (for Decoy hits only — player contact damage comes from Warrior.OnCollision)
    private float _meleeCooldownTimer = 0f;
    private const float MeleeCooldownSeconds = 0.75f;

    // Sprite sheet constants — shares the orc sheet as a placeholder until a wolf sprite is ready
    private const int FrameSize = 32;
    private const int SheetColumns = 8;
    private const int WolfFacingRightRow = 0;
    private const int WalkingStartColumn = 0;
    private const int WalkingFrameCount = 3;

    private float FullSize => FrameSize * SpriteScale;
    private float HitboxSize => FullSize * 0.55f;
    private float HitboxOffset => (FullSize - HitboxSize) / 2f;

        // Base Goblin stats
    public override float BaseMaxHp { get; } = 20f;
    public override int BaseDamage { get; } = 5;
    public override float BaseCritChance { get; } = 0f;
    public override float BaseHaste { get; } = 2f; // Attack cooldown
    public override float BaseSpeed { get; } = 100f;
    public override float ExperienceValue { get; protected set; } = 2f;

    // Current Goblin Stats
    public override float CurrentMaxHp { get; protected set; }
    public override int CurrentDamage { get; protected set; }
    public override float CurrentCritChance { get; protected set; }
    public override float CurrentHaste { get; protected set; }
    public override float CurrentSpeed { get; protected set; }

    // Brownish-gray tint distinguishes the wolf from the orc on the placeholder sprite
    private static readonly Color WolfBaseTint = new Color(110, 95, 75);

    public Wolf(Point position)
    {
        const int size = (int)(FrameSize * SpriteScale);
        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);
        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);

        // Load dedicated wolf sprite when available; fall back to orc placeholder in the meantime
        try { _texture = content.Load<Texture2D>("wolf"); }
        catch { _texture = content.Load<Texture2D>("orc"); }

        _walkingAnimation = new AnimationManager(
            WalkingFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            12,
            true,
            WalkingStartColumn * FrameSize,
            WolfFacingRightRow * FrameSize
        );

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
        if (_meleeCooldownTimer > 0f)
            _meleeCooldownTimer = Math.Max(0f, _meleeCooldownTimer - dt);
        if (_lungeCooldownTimer > 0f)
            _lungeCooldownTimer = Math.Max(0f, _lungeCooldownTimer - dt);

        if (player == null && decoy == null)
            return;

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        switch (_state)
        {
            case WolfState.Lunging:
                UpdateLunge(dt);
                break;

            case WolfState.Recovering:
                _recoveryTimer -= dt;
                if (_recoveryTimer <= 0f)
                    _state = WolfState.Walking;
                break;

            case WolfState.Walking:
                Vector2 toTarget = targetPos - GetPosition();
                float distance = toTarget.Length();

                if (toTarget.X != 0)
                    _isFacingLeft = toTarget.X < 0;

                if (_lungeCooldownTimer <= 0f && distance >= LungeMinRange && distance <= LungeMaxRange)
                    StartLunge(toTarget);
                else
                    WalkToward(targetPos, dt, gm);

                _walkingAnimation.Update();
                break;
        }
    }

    private void StartLunge(Vector2 toTarget)
    {
        _lungeDirection = toTarget;
        if (_lungeDirection != Vector2.Zero)
            _lungeDirection.Normalize();
        _state = WolfState.Lunging;
        _lungeTimer = LungeDuration;
    }

    private void UpdateLunge(float dt)
    {
        _lungeTimer -= dt;
        if (_lungeTimer <= 0f)
        {
            EndLunge();
            return;
        }

        Vector2 movement = _lungeDirection * LungeSpeed * dt;
        if (_isPoisoned) movement *= 0.75f;

        // Slide along walls rather than hard-stopping mid-lunge
        Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
        if (!WouldCollideAt(newPosX, HitboxSize, HitboxOffset))
            _precisePosition = newPosX;

        Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
        if (!WouldCollideAt(newPosY, HitboxSize, HitboxOffset))
            _precisePosition = newPosY;

        _collider.shape = new Rectangle(
            (int)(_precisePosition.X + HitboxOffset),
            (int)(_precisePosition.Y + HitboxOffset),
            (int)HitboxSize, (int)HitboxSize
        );
    }

    private void EndLunge()
    {
        _state = WolfState.Recovering;
        _recoveryTimer = LungeRecoverySeconds;
        _lungeCooldownTimer = LungeCooldownSeconds;
    }

    private void WalkToward(Vector2 targetPos, float dt, GameManager gm)
    {
        Vector2 toTarget = targetPos - GetPosition();
        Vector2 direction;

        if (gm.NavigationGrid != null &&
            gm.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir) && pathDir != Vector2.Zero)
            direction = pathDir;
        else
        {
            direction = toTarget;
            if (direction != Vector2.Zero) direction.Normalize();
        }

        Vector2 movement = direction * BaseSpeed * dt;

        Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
        if (!WouldCollideAt(newPosX, HitboxSize, HitboxOffset))
            _precisePosition = newPosX;

        Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
        if (!WouldCollideAt(newPosY, HitboxSize, HitboxOffset))
            _precisePosition = newPosY;

        _collider.shape = new Rectangle(
            (int)(_precisePosition.X + HitboxOffset),
            (int)(_precisePosition.Y + HitboxOffset),
            (int)HitboxSize, (int)HitboxSize
        );
    }

    public override void OnCollision(GameObject other)
    {
        if (_isPoisoned && _poisonSpreads && other is BaseEnemy otherEnemy && !otherEnemy._isPoisoned)
        {
            otherEnemy.isPoisoned(true, PoisonDamagePerTick);
            otherEnemy.EnablePoisonSpreading();
        }

        if (other is Decoy decoy)
        {
            if (_meleeCooldownTimer > 0f) return;
            decoy.Damage(10f);
            _meleeCooldownTimer = MeleeCooldownSeconds;
            if (_state == WolfState.Lunging) EndLunge();
            return;
        }

        // Player contact damage is applied by Warrior.OnCollision — we only add the bleed on a lunge hit
        if (other is Warrior warrior && _state == WolfState.Lunging)
        {
            warrior.ApplyBleeding(dps: 2f, duration: 5f);
            EndLunge();
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _precisePosition + new Vector2(FullSize / 2f);

        if (DebugDrawHitbox && _collider is not null)
            HitboxHelper.DrawHitbox(spriteBatch, _collider.shape, Color.Purple);

        int row = _isFacingLeft ? WolfFacingRightRow + 1 : WolfFacingRightRow;
        Rectangle sourceRect = _walkingAnimation.GetSourceRect();
        sourceRect.Y = row * FrameSize;

        // Apply wolf tint only when no status-effect tint is active
        Color tint = DrawTint;
        if (tint == Color.White)
            tint = WolfBaseTint;

        spriteBatch.Draw(
            _texture,
            center,
            sourceRect,
            tint,
            0f,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            SpriteScale,
            SpriteEffects.None,
            0f
        );

        base.Draw(gameTime, spriteBatch);
    }

    public override Vector2 GetPosition() => _collider.shape.Center.ToVector2();
}
