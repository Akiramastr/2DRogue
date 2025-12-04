namespace Assets.Scripts.Game
{
    // Interfaz que expone el valor de daño
    public interface IDamageSource
    {
        int DamageAmount { get; }
        void SetAmount(int amount);
    }
}