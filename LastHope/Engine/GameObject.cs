using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace Last_Hope.Engine;

/// <summary>
/// The foundational abstract base class for all interactive entities in the game.
/// </summary>
/// <remarks>
/// Follows a standard object-oriented game entity pattern based on the MonoGame/XNA application lifecycle 
/// (Load, HandleInput, Update, Draw). It provides built-in support for collision detection and visual damage feedback.
/// </remarks>
public abstract class GameObject
{
    /// <summary>
    /// Retrieves the active spatial collider for this object.
    /// </summary>
    /// <returns>The <see cref="Collider"/> associated with this object, or null if none is set.</returns>
    public Collider GetCollider() => collider;

    /// <summary>
    /// The spatial boundary used for intersection testing within the game world.
    /// </summary>
    protected Collider collider;

    /// <summary>The base duration, in seconds, for the damage visual feedback.</summary>
    protected const float HurtFlashDurationSeconds = 0.12f;
    
    /// <summary>The color applied when the object takes damage.</summary>
    protected static readonly Color HurtFlashColor = Color.Lerp(Color.White, Color.Red, 0.35f);
    
    private float _hurtFlashTimer;
    
    /// <summary>
    /// The current tint color to apply during rendering. 
    /// Automatically transitions back to White when the hurt timer expires.
    /// </summary>
    protected Color DrawTint => _hurtFlashTimer > 0f ? HurtFlashColor : Color.White;

    /// <summary>
    /// Starts the hurt flash visual feedback. Call this from Damage implementations
    /// whenever HP is removed so subclasses can render <see cref="DrawTint"/>.
    /// </summary>
    protected void TriggerHurtFlash()
    {
        _hurtFlashTimer = HurtFlashDurationSeconds;
    }

    /// <summary>
    /// Assigns the spatial boundary used for physical interactions.
    /// </summary>
    /// <param name="collider"> The collider to be used. </param>
    public void SetCollider(Collider collider)
    {
        this.collider = collider;
    }

    /// <summary>
    /// Override this method to load graphical and audio assets for your class.
    /// </summary>
    /// <param name="content"> The MonoGame ContentManager that handles file loading. </param>
    /// <example>
    /// To load a texture, use: 
    /// content.Load<Texture2D>([texture name]);
    /// </example>
    public virtual void Load(ContentManager content)
    {

    }

    /// <summary>
    /// Override this if you want your GameObject to interpret and react to user input.
    /// </summary>
    /// <param name="inputManager"> Keeps track of user input. </param>
    public virtual void HandleInput(InputManager inputManager)
    {

    }

    /// <summary>
    /// Checks if this GameObject intersects with another specified GameObject.
    /// </summary>
    /// <param name="other"> The GameObject to check collision with. </param>
    /// <returns><c>true</c> if the two object colliders are overlapping; otherwise, <c>false</c>.</returns>
    public bool CheckCollision(GameObject other)
    {
        if (other == null || collider == null || other.collider == null)
            return false;
        return collider.CheckIntersection(other.collider);
    }

    /// <summary>
    /// Override this to execute specific logic when a collision occurs.
    /// Keep in mind that OnCollision will be called once on both Objects.
    /// </summary>
    /// <param name="other"> The GameObject that intersected with this object. </param>
    public virtual void OnCollision(GameObject other)
    {

    }

    /// <summary>
    /// Called every game step. Override this to perform logic updates like movement, AI, or timers.
    /// </summary>
    /// <param name="gameTime"> The amount of time that has elapsed since the last update call. </param>
    public virtual void Update(GameTime gameTime)
    {
        if (_hurtFlashTimer > 0f)
        {
            _hurtFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hurtFlashTimer < 0f)
                _hurtFlashTimer = 0f;
        }
    }

    /// <summary>
    /// Called every game step. Override this with any drawing code you wish to implement.
    /// </summary>
    /// <remarks>
    /// SpriteBatch.Begin() and SpriteBatch.End() are managed centrally by the GameManager, 
    /// so do not call them directly within this method.
    /// </remarks>
    /// <param name="gameTime"> The amount of time that has elapsed since the last draw call. </param>
    /// <param name="spriteBatch"> The MonoGame SpriteBatch used to render 2D textures. </param>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) 
    {

    }

    /// <summary>
    /// Lifecycle hook called just before the GameObject is permanently removed from the engine simulation.
    /// Override this to clean up resources, drop items, or trigger death effects.
    /// </summary>
    public virtual void Destroy() 
    {

    }
}