using UnityEngine;
using Assets.Scripts.Game;

/// <summary>
/// Componente simple para adjuntar al prefab del área de colisión.
/// Expone la propiedad `Damage` para que `VidaEnemigo.ExtractDamageValue` la lea.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DamageDealer : MonoBehaviour, IDamage
{
    [SerializeField, Tooltip("Daño que inflige este hitbox.")]
    private int damage = 10;

    public int Damage
    {
        get => damage;
        set => damage = Mathf.Max(0, value);
    }

    public void SetDamage(int value) => Damage = value;

    private void Reset()
    {
        // Asegurar que el collider sea trigger por defecto (útil para prefabs)
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
}