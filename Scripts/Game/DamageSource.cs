using UnityEngine;
using Assets.Scripts.Game; // Asegura que usas el namespace correcto para tu interfaz

namespace Assets.Scripts.Game
{
    // Componente simple que expone el valor de daño de forma explícita
    public class DamageSource : MonoBehaviour, Assets.Scripts.Game.IDamageSource
    {
        [SerializeField] private int damageAmount;
        public int DamageAmount => damageAmount;

        public void SetAmount(int amount) => damageAmount = amount;
    }
}