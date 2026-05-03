using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Abilities;

public abstract class BaseAbility
{
    public float Cooldown { get; protected set; }
    public float CooldownTimer { get; set; }

    protected BaseAbility(float cooldown)
    {
        Cooldown = cooldown;
    }

    public virtual void Update(BasePlayer player, GameTime gameTime)
    {
        if (CooldownTimer > 0f)
        {
            CooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }

    public bool CanExecute() => CooldownTimer <= 0f;

    public void Execute(BasePlayer player)
    {
        if (CanExecute())
        {
            OnExecute(player);
            CooldownTimer = Cooldown;
        }
    }

    protected abstract void OnExecute(BasePlayer player);
}