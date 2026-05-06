using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Spell;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Boss : BaseEnemy
{
    private const float SpriteScale = 2.5f;

    private Vector2 _precisePosition;

    private AnimationManager _walkAnimation;
    private AnimationManager _chargeAnimation;
    private AnimationManager _launchAnimation;

    private bool _isCharging = false;
    private bool _isLaunching = false;
    private bool _isFacingLeft = false;
    private bool _lastFacingLeft = false;   // tracks direction changes for walk anim reset
    private bool _attackFacingLeft = false; // direction locked at charge start

    private float _attackCooldownTimer = 0f;
    private const float AttackCooldownSeconds = 3f;
    private const float AttackRange = 300f;

    // Sprite sheet: 64x64 per frame, 8 columns, 2 rows
    private const int FrameSize = 64;
    private const int SheetColumns = 8;
    private const int FacingRightRow = 0;
    private const int FacingLeftRow = 1;

    // Right-facing row (row 0): neutral(0), walk(1-2), charge(3-6), launch(7)
    private const int RightWalkStart = 1;
    private const int RightChargeStart = 3;
    private const int RightLaunch = 7;

    // Left-facing row (row 1) is mirrored: launch(0), charge(1-4), walk(5-6), neutral(7)
    private const int LeftWalkStart = 5;
    private const int LeftChargeStart = 1;
    private const int LeftLaunch = 0;

    private const int WalkFrameCount = 2;
    private const int ChargeFrameCount = 4;
    private const int LaunchFrameCount = 1;
    
    private float FullSize => FrameSize * SpriteScale;
    private float HitboxSize => FullSize * 0.55f;
    private float HitboxOffset => (FullSize - HitboxSize) / 2f;

    public Boss(Point position)
        : base(maxHealth: 400, currentHealth: 400, speed: 60f, experienceValue: 100f)
    {
        int size = (int)(FrameSize * SpriteScale);
        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);
        _precisePosition = position.ToVector2();
    }

    /// <summary>
    /// Loads the graphics content for the boss, including textures and animations.
    /// </summary>
    /// <param name="content">The ContentManager to load from.</param>
    public override void Load(ContentManager content)
    {
        base.Load(content);

        _texture = content.Load<Texture2D>("demon");

        _walkAnimation = CreateWalkAnimation(_isFacingLeft);
        _chargeAnimation = CreateChargeAnimation(_isFacingLeft);
        _launchAnimation = CreateLaunchAnimation(_isFacingLeft);

        _collider.shape.Size = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _precisePosition = _collider.shape.Location.ToVector2();
    }

    /// <summary>
    /// Updates the logic and behavior of the boss each frame, including attacking and pathfinding.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void UpdateBehavior(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        var player = gm._player;
        var decoy = gm.ActiveDecoy;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer = Math.Max(0f, _attackCooldownTimer - dt);

        if (player == null && decoy == null)
            return;

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();
        float distanceToTarget = toTarget.Length();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        // Recreate walk animation when direction changes (only while walking freely)
        if (_isFacingLeft != _lastFacingLeft && !_isCharging && !_isLaunching)
        {
            _lastFacingLeft = _isFacingLeft;
            _walkAnimation = CreateWalkAnimation(_isFacingLeft);
        }

        if (_isLaunching)
        {
            _launchAnimation.Update();
            if (_launchAnimation.isFinished)
            {
                _isLaunching = false;
                _attackCooldownTimer = AttackCooldownSeconds;
                _walkAnimation = CreateWalkAnimation(_isFacingLeft);
                _lastFacingLeft = _isFacingLeft;
            }
        }
        else if (_isCharging)
        {
            _chargeAnimation.Update();
            if (_chargeAnimation.isFinished)
            {
                _isCharging = false;
                _isLaunching = true;
                _launchAnimation = CreateLaunchAnimation(_attackFacingLeft);

                Vector2 dir = toTarget;
                if (dir != Vector2.Zero) dir.Normalize();
                gm.AddGameObject(new Fireball(GetPosition(), dir, this));
            }
        }
        else
        {
            if (distanceToTarget <= AttackRange && _attackCooldownTimer <= 0f)
            {
                _isCharging = true;
                _attackFacingLeft = _isFacingLeft;
                _chargeAnimation = CreateChargeAnimation(_attackFacingLeft);
            }
            else
            {
                Vector2 direction;
                if (gm.NavigationGrid != null &&
                    gm.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir) && pathDir != Vector2.Zero)
                {
                    direction = pathDir;
                }
                else
                {
                    direction = toTarget;
                    if (direction != Vector2.Zero) direction.Normalize();
                }

                Vector2 movement = direction * Speed * dt;

                Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
                if (!WouldCollideAt(newPosX, HitboxSize, HitboxOffset))
                    _precisePosition = newPosX;

                Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
                if (!WouldCollideAt(newPosY, HitboxSize, HitboxOffset))
                    _precisePosition = newPosY;

                _collider.shape = new Rectangle((int)(_precisePosition.X + HitboxOffset), (int)(_precisePosition.Y + HitboxOffset), (int)HitboxSize, (int)HitboxSize);
                _walkAnimation.Update();
            }
        }
    }

    /// <summary>
    /// Draws the boss to the screen, handling sprite sheets and animations.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="spriteBatch">The SpriteBatch used to draw the texture.</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _precisePosition + new Vector2(FullSize / 2f);

        // Row and X offsets are baked into each AnimationManager — no Y override needed
        Rectangle sourceRect;
        if (_isLaunching)
            sourceRect = _launchAnimation.GetSourceRect();
        else if (_isCharging)
            sourceRect = _chargeAnimation.GetSourceRect();
        else
            sourceRect = _walkAnimation.GetSourceRect();

        Color tint = DrawTint;
        if (tint == HurtFlashColor) 
            tint = Color.Cyan; // Cyan contrasts strongly with the demon's red

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

    /// <summary>
    /// Called when the boss collides with another game object.
    /// </summary>
    /// <param name="other">The other GameObject involved in the collision.</param>
    public override void OnCollision(GameObject other) { }

    /// <summary>
    /// Gets the current position of the boss based on its collision box.
    /// </summary>
    /// <returns>A Vector2 representing the exact center of the boss.</returns>
    public override Vector2 GetPosition() => _collider.shape.Center.ToVector2();

    /// <summary>
    /// Creates and configures the walking animation for the boss.
    /// </summary>
    /// <param name="facingLeft">True if the boss should be facing left; otherwise, false.</param>
    /// <returns>An AnimationManager for the walking state.</returns>
    private AnimationManager CreateWalkAnimation(bool facingLeft) => new AnimationManager(
        WalkFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        10, true,
        (facingLeft ? LeftWalkStart : RightWalkStart) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );

    /// <summary>
    /// Creates and configures the charging/wind-up animation for the boss attack.
    /// </summary>
    /// <param name="facingLeft">True if the boss should be facing left; otherwise, false.</param>
    /// <returns>An AnimationManager for the charging state.</returns>
    private AnimationManager CreateChargeAnimation(bool facingLeft) => new AnimationManager(
        ChargeFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        6, false,
        (facingLeft ? LeftChargeStart : RightChargeStart) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );

    /// <summary>
    /// Creates and configures the attack launch animation for the boss.
    /// </summary>
    /// <param name="facingLeft">True if the boss should be facing left; otherwise, false.</param>
    /// <returns>An AnimationManager for the attack launch state.</returns>
    private AnimationManager CreateLaunchAnimation(bool facingLeft) => new AnimationManager(
        LaunchFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        20, false,
        (facingLeft ? LeftLaunch : RightLaunch) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );
}
