using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using Assets.Scripts.Game;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class VidaEnemigo : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField, Tooltip("Vida máxima inicial del enemigo.")]
    private int vidaMaxima = 50;

    [SerializeField, Tooltip("Vida inicial al comenzar la partida. Si es <= 0 se inicializa a vidaMaxima.")]
    private int vidaInicial = -1;

    // --- Efectos al recibir daño ---
    [Header("Efectos al recibir daño")]
    [SerializeField, Tooltip("Clip de sonido que se reproduce al recibir daño.")]
    private AudioClip hitSound;

    [SerializeField, Tooltip("Prefab de partículas (ParticleSystem) que se instanciará al recibir daño.")]
    private GameObject hitVfxPrefab;

    [SerializeField, Tooltip("SpriteRenderer usado para flash visual (opcional).")]
    private SpriteRenderer spriteRenderer;

    [SerializeField, Tooltip("Color de flash al recibir daño.")]
    private Color flashColor = Color.white;

    [SerializeField, Tooltip("Duración del flash (segundos).")]
    private float flashDuration = 0.12f;

    [Header("Animator opcional: se disparará el trigger 'Hit' si existe.")]
    [SerializeField, Tooltip("Animator opcional: se disparará el trigger 'Hit' si existe.")]
    private Animator enemigoAnimator;

    // Nuevo: cantidad de daño que este enemigo inflige al colisionar
    [Header("Daño")]
    [SerializeField, Tooltip("Cantidad de daño que inflige este enemigo.")]
    private int damage = 10;

    private AudioSource _audioSource;
    // --------------------------------

    // Vida actual en tiempo de ejecución
    private int vidaActual;

    // Referencia al coroutine de flash (si aplica) y color original
    private Coroutine _flashCoroutine = null;
    private Color spriteOriginalColor;

    // Eventos para otros sistemas
    public event Action<int> OnVidaCambiadaEvent;
    public event Action OnMuertoEvent;
    public event Action<int> OnVidaMaximaCambiadaEvent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        vidaActual = (vidaInicial > 0) ? Mathf.Clamp(vidaInicial, 0, vidaMaxima) : vidaMaxima;
        OnVidaCambiadaEvent?.Invoke(vidaActual);
        OnVidaMaximaCambiadaEvent?.Invoke(vidaMaxima);

        if (_audioSource == null && hitSound != null)
            TryGetComponent(out _audioSource);

        if (spriteRenderer == null)
            TryGetComponent(out spriteRenderer);
        if (spriteRenderer != null)
            spriteOriginalColor = spriteRenderer.color;

        if (enemigoAnimator == null)
            TryGetComponent(out enemigoAnimator);
    }

    // Propiedades públicas de lectura
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;

    public void ModificarVidaMaxima(int delta)
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima + delta);
        if (vidaActual > vidaMaxima)
            vidaActual = vidaMaxima;

        OnVidaMaximaCambiadaEvent?.Invoke(vidaMaxima);
        OnVidaCambiadaEvent?.Invoke(vidaActual);
    }

    // Aplica daño (cantidad positiva reduce vida). La vida queda entre 0 y vidaMaxima.
    public void AplicarDano(int cantidad)
    {
        if (cantidad == 0) return;

        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0, vidaMaxima);
        OnVidaCambiadaEvent?.Invoke(vidaActual);

        if (cantidad > 0)
            PlayHitEffects();

        if (vidaActual <= 0)
            Morir();
    }

    private void PlayHitEffects()
    {
        if (hitSound != null && _audioSource != null)
            _audioSource.PlayOneShot(hitSound);

        if (hitVfxPrefab != null)
        {
            var vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(vfx, 2f);
            }
        }

        if (enemigoAnimator != null)
            enemigoAnimator.SetTrigger("Hit");

        if (spriteRenderer != null)
        {
            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashCoroutine());
        }
    }

    private IEnumerator FlashCoroutine()
    {
        if (spriteRenderer == null)
            yield break;

        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(Mathf.Max(0f, flashDuration));
        spriteRenderer.color = spriteOriginalColor;
        _flashCoroutine = null;
    }

    public void Curar(int cantidad)
    {
        if (cantidad <= 0) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0, vidaMaxima);
        OnVidaCambiadaEvent?.Invoke(vidaActual);
    }

    private void Morir()
    {
        OnMuertoEvent?.Invoke();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null)
            HandleCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null)
            HandleCollision(collision.gameObject);
    }

    // Reconoce impactos originados por un objeto con tag "ataque" o cualquiera de sus padres.
    private void HandleCollision(GameObject other)
    {
        if (other == null) return;
        if (!IsFromPlayerOrChild(other)) return;

        int damageValue = ExtractDamageValue(other);

        // Debug: confirmar colisión con la fuente de daño y mostrar el daño detectado
        Debug.Log($"[VidaEnemigo] Colisión con fuente de daño '{other.name}' (tag: {other.tag}). Daño detectado: {damageValue}");

        if (damageValue != 0)
            AplicarDano(damageValue);
    }

    // Comprueba si el objeto o alguno de sus padres tiene la etiqueta "ataque".
    // Usar comparación de strings para evitar excepción si el tag no existe en el proyecto.
    private bool IsFromPlayerOrChild(GameObject go)
    {
        if (go == null) return false;
        var t = go.transform;
        while (t != null)
        {
            if (t.gameObject.CompareTag("Ataque"))
                return true;
            t = t.parent;
        }
        return false;
    }

    // Igual lógica de extracción de daño usada en VidaPersonaje.
    private int ExtractDamageValue(GameObject go)
    {
        if (go == null) return 0;

        var components = go.GetComponents<Component>();
        string[] candidateNames = { "Damage", "damage", "Dano", "dano", "daño", "Daño", "damageAmount", "damage_value" };

        foreach (var comp in components)
        {
            if (comp == null) continue;
            var t = comp.GetType();

            foreach (var name in candidateNames)
            {
                var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                {
                    try { return Convert.ToInt32(pi.GetValue(comp)); } catch { }
                }

                var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                {
                    try { return Convert.ToInt32(fi.GetValue(comp)); } catch { }
                }
            }
        }

        var parent = go.transform.parent;
        while (parent != null)
        {
            var compsParent = parent.GetComponents<Component>();
            foreach (var comp in compsParent)
            {
                if (comp == null) continue;
                var t = comp.GetType();
                foreach (var name in candidateNames)
                {
                    var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                    {
                        try { return Convert.ToInt32(pi.GetValue(comp)); } catch { }
                    }
                    var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                    {
                        try { return Convert.ToInt32(fi.GetValue(comp)); } catch { }
                    }
                }
            }
            parent = parent.parent;
        }

        return 0;
    }

    // Mantener interoperabilidad: propiedad Damage ahora expone la cantidad de daño infligida por este enemigo.
    public int Damage
    {
        get => damage;
        set => damage = Mathf.Max(0, value);
    }

    public void SetDamage(int value) => AplicarDano(value);

    private void OnValidate()
    {
        vidaMaxima = Mathf.Max(1, vidaMaxima);
        if (vidaInicial > 0)
            vidaInicial = Mathf.Clamp(vidaInicial, 0, vidaMaxima);

        flashDuration = Mathf.Max(0f, flashDuration);
        damage = Mathf.Max(0, damage);
    }
}
