using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Abilities;

/// <summary>
/// Serves as the foundational class for player abilities, managing execution rules and cooldown timers.
/// </summary>
/// <remarks>
/// Based on the standard Gang of Four Template Method design pattern. 
/// The <see cref="Execute"/> method acts as the template, enforcing standard cooldown logic, 
/// while delegating the specific ability behavior to the abstract <see cref="OnExecute"/> method in subclasses.
/// </remarks>
public abstract class BaseAbility
{
    /// <summary>
    /// The total duration, in seconds, before the ability can be used again.
    /// </summary>
    public float Cooldown { get; protected set; }
    
    /// <summary>
    /// The remaining time, in seconds, until the ability is ready. A value of 0 or less indicates it is off cooldown.
    /// </summary>
    public float CooldownTimer { get; set; }

    /// <summary>
    /// Initializes a new ability with a specified base cooldown.
    /// </summary>
    /// <param name="cooldown">The total cooldown duration in seconds.</param>
    protected BaseAbility(float cooldown)
    {
        Cooldown = cooldown;
    }

    /// <summary>
    /// Progresses the cooldown timer. Should be called during the main game loop.
    /// </summary>
    /// <param name="player">The player context attached to this ability.</param>
    /// <param name="gameTime">The current game time used to calculate the elapsed delta time.</param>
    public virtual void Update(BasePlayer player, GameTime gameTime)
    {
        if (CooldownTimer > 0f)
        {
            CooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }

    /// <summary>
    /// Determines whether the ability is currently available to be used.
    /// </summary>
    /// <returns><c>true</c> if the ability is off cooldown; otherwise, <c>false</c>.</returns>
    public bool CanExecute() => CooldownTimer <= 0f;

    /// <summary>
    /// Attempts to trigger the ability. If ready, invokes the specific ability logic and immediately resets the cooldown.
    /// </summary>
    /// <param name="player">The player attempting to cast the ability.</param>
    public void Execute(BasePlayer player)
    {
        if (CanExecute())
        {
            OnExecute(player);
            CooldownTimer = Cooldown;
        }
    }

    /// <summary>
    /// The underlying implementation of the ability's gameplay effect. Must be overridden by subclasses.
    /// </summary>
    /// <param name="player">The player casting the ability.</param>
    protected abstract void OnExecute(BasePlayer player);

    /// <summary>
    /// Executes the actual damage or effect of the ability. Called by the player when the casting animation reaches the impact frame.
    /// </summary>
    /// <param name="player">The player casting the ability.</param>
    public virtual void PerformHit(BasePlayer player) { }

    public virtual void Draw(SpriteBatch spriteBatch) { }
}