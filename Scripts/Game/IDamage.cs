namespace Assets.Scripts.Game
{
    /// <summary>
    /// Núcleo de la interfaz usada para exponer valores de daño/vida y aplicar daño.
    /// </summary>
    public interface IDamage
    {
        /// <summary>Valor de daño o vida.</summary>
        int Damage { get; set; }

        /// <summary>Método para aplicar o asignar daño.</summary>
        void SetDamage(int value);
    }
}
