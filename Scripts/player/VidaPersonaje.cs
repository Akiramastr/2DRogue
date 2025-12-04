using UnityEngine;
using Assets.Scripts.Game;
using System;
using System.Reflection;
using System.Collections;

// Requiere los componentes que se obtienen con GetComponent en Start
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class VidaPersonaje : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField, Tooltip("Vida máxima base del personaje (sin bonus de atributos).")]
    private int vidaMaximaBase = 50;

    [SerializeField, Tooltip("Cantidad de vida que se añade por cada punto de Salud en Atributos.")]
    private int vidaPorPuntoSalud = 10;

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

    [SerializeField, Tooltip("Animator opcional: se disparará el trigger 'Hit' si existe.")]
    private Animator animator;

    [Header("Invulnerabilidad")]
    [SerializeField, Tooltip("Tiempo de invulnerabilidad tras recibir daño (segundos).")]
    private float invulnerabilidad = 0.5f;

    [SerializeField, Tooltip("Intervalo mínimo entre comprobaciones de contacto (segundos). 0 = cada FixedUpdate.")]
    private float contactoIntervalo = 0f;

    private AudioSource _audioSource;
    // --------------------------------

    // Vida actual en tiempo de ejecución
    private int vidaActual;
    
    // Vida máxima calculada (base + bonus de atributos)
    private int vidaMaxima;

    // Referencia al componente Atributos
    private Atributos atributos;

    // Referencia al coroutine de flash (si aplica) y color original
    private Coroutine _flashCoroutine = null;
    private Color spriteOriginalColor;

    // Timer de invulnerabilidad
    private float _invulnerableHasta = -1f;
    public bool EsInvulnerable => Time.time < _invulnerableHasta;

    // Campos para rate-limit
    private float _ultimoChequeoContacto;

    // Eventos para otros sistemas
    public event Action<int> OnVidaCambiada;
    public event Action OnMuerto;
    public event Action<int> OnVidaMaximaCambiada;

    void Awake()
    {
        if (!TryGetComponent(out atributos))
            atributos = GetComponentInParent<Atributos>();
    }

    void OnEnable()
    {
        // Suscribirse al evento de cambio de salud
        if (atributos != null)
        {
            atributos.OnSaludCambiada += ActualizarVidaMaxima;
        }
    }

    void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        if (atributos != null)
        {
            atributos.OnSaludCambiada -= ActualizarVidaMaxima;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (atributos == null)
            atributos = GetComponentInParent<Atributos>();

        // Calcular vida máxima inicial
        CalcularVidaMaxima();

        vidaActual = (vidaInicial > 0) ? Mathf.Clamp(vidaInicial, 0, vidaMaxima) : vidaMaxima;
        OnVidaCambiada?.Invoke(vidaActual);
        OnVidaMaximaCambiada?.Invoke(vidaMaxima);

        if (!TryGetComponent<AudioSource>(out _audioSource) && hitSound != null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteOriginalColor = spriteRenderer.color;

        if (animator == null)
            TryGetComponent<Animator>(out animator);
    }

    // Calcula la vida máxima basándose en la base + bonus de salud
    private void CalcularVidaMaxima()
    {
        int saludActual = (atributos != null) ? atributos.SaludActual : 0;
        vidaMaxima = vidaMaximaBase + (saludActual * vidaPorPuntoSalud);
        vidaMaxima = Mathf.Max(1, vidaMaxima);
    }

    // Callback para evento de cambio de salud
    private void ActualizarVidaMaxima(int nuevaSalud)
    {
        int vidaMaximaAnterior = vidaMaxima;
        CalcularVidaMaxima();

        // Ajustar vida actual si cambió la vida máxima
        int diferencia = vidaMaxima - vidaMaximaAnterior;
        if (diferencia != 0)
        {
            // Si aumentó la vida máxima, aumentar proporcionalmente la vida actual
            if (diferencia > 0)
            {
                vidaActual = Mathf.Min(vidaActual + diferencia, vidaMaxima);
            }
            // Si disminuyó, asegurar que no exceda el nuevo máximo
            else if (vidaActual > vidaMaxima)
            {
                vidaActual = vidaMaxima;
            }

            OnVidaMaximaCambiada?.Invoke(vidaMaxima);
            OnVidaCambiada?.Invoke(vidaActual);
        }

        Debug.Log($"[VidaPersonaje] Salud actualizada a: {nuevaSalud}. Vida máxima: {vidaMaxima}");
    }

    // Propiedades públicas de lectura
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;

    public void ModificarVidaMaxima(int delta)
    {
        vidaMaximaBase = Mathf.Max(1, vidaMaximaBase + delta);
        CalcularVidaMaxima();
        
        if (vidaActual > vidaMaxima)
            vidaActual = vidaMaxima;

        OnVidaMaximaCambiada?.Invoke(vidaMaxima);
        OnVidaCambiada?.Invoke(vidaActual);
    }

    // Aplica daño (cantidad positiva reduce vida). La vida queda entre 0 y vidaMaxima.
    public void AplicarDano(int cantidad)
    {
        if (cantidad == 0) return;

        // Ignorar si está invulnerable y el daño es positivo
        if (cantidad > 0 && EsInvulnerable)
            return;

        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0, vidaMaxima);
        OnVidaCambiada?.Invoke(vidaActual);

        if (cantidad > 0)
        {
            // Iniciar ventana de invulnerabilidad
            _invulnerableHasta = Time.time + Mathf.Max(0f, invulnerabilidad);
            PlayHitEffects();
        }

        if (vidaActual <= 0)
            Morir();
    }

    // Nuevo: controlar si se acepta daño por SendMessage externo (p. ej. prefabs con tag "Ataque")
    [Header("Compatibilidad con SendMessage")]
    [SerializeField, Tooltip("Si está activo, el Player aceptará daño recibido vía SendMessage('SetDamage'). Desactívalo para evitar auto-daño por prefabs hijos con tag 'Ataque'.")]
    private bool aceptarDamagePorMensaje = false;

    // Método para compatibilidad con SendMessage desde DañoContacto
    // DañoContacto hace: playerAncestor.gameObject.SendMessage("SetDamage", damageValue, DontRequireReceiver);
    public void SetDamage(int cantidad)
    {
        // Evitar que ataques propios (invocados por hijos) dañen al Player por ruta SendMessage
        if (!aceptarDamagePorMensaje)
            return;

        AplicarDano(cantidad);
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

        if (animator != null)
            animator.SetTrigger("Hit");

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

        spriteRenderer.color = FlashColorFallback(flashColor);
        yield return new WaitForSeconds(Mathf.Max(0f, flashDuration));
        spriteRenderer.color = spriteOriginalColor;
        _flashCoroutine = null;
    }

    // pequeño helper para evitar potencial shadowing con otros nombres
    private Color FlashColorFallback(Color c) => c;

    public void Curar(int cantidad)
    {
        if (cantidad <= 0) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0, vidaMaxima);
        OnVidaCambiada?.Invoke(vidaActual);
    }

    private void Morir()
    {
        OnMuerto?.Invoke();
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

    // Reconoce impactos originados por objetos con tag "Enemy" en su jerarquía.
    private void HandleCollision(GameObject other)
    {
        if (other == null) return;

        // Ignorar colisiones provenientes de los hijos propios (no deben dañarme)
        if (other.transform == transform || other.transform.IsChildOf(transform))
            return;

        if (!IsFromPlayerOrChild(other)) return;

        int damageValue = ExtractDamageValue(other);

        if (damageValue != 0)
            AplicarDano(damageValue);
    }

    // Reemplaza el método para no usar la etiqueta "Ataque"
    private bool IsFromPlayerOrChild(GameObject go)
    {
        if (go == null) return false;
        var t = go.transform;
        while (t != null)
        {
            // Solo considerar objetos con tag "Enemy" en la cadena de padres
            if (t.gameObject.CompareTag("Enemy"))
                return true;
            t = t.parent;
        }
        return false;
    }

    // Extracción optimizada de valor de daño:
    // 1) intenta interfaces/concretos conocidos (sin reflexión)
    // 2) si no encuentra, busca en los ancestros
    // 3) como último recurso usa reflexión como compatibilidad
    private int ExtractDamageValue(GameObject go)
    {
        if (go == null) return 0;

        // 1) comprobar componentes del propio objeto (sin reflexión)
        var comps = go.GetComponents<Component>();
        if (TryReadDamageFromComponents(comps, out int val))
            return val;

        // 2) comprobar en los padres
        var parent = go.transform.parent;
        while (parent != null)
        {
            var compsParent = parent.GetComponents<Component>();
            if (TryReadDamageFromComponents(compsParent, out int valParent))
                return valParent;
            parent = parent.parent;
        }

        // 3) Fallback: mantener la lógica reflexiva original para máxima compatibilidad
        string[] candidateNames = { "Damage", "damage", "Dano", "dano", "daño", "Daño", "damageAmount", "damage_value" };

        foreach (var comp in comps)
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

        return 0;
    }

    // Helper que intenta leer Damage / DamageAmount desde una lista de Component sin usar reflexión
    private bool TryReadDamageFromComponents(Component[] components, out int outValue)
    {
        outValue = 0;
        if (components == null || components.Length == 0) return false;

        foreach (var comp in components)
        {
            if (comp == null) continue;

            // interfaz IDamage (Damage property) - convencional en tu proyecto
            if (comp is IDamage idmg)
            {
                outValue = idmg.Damage;
                return true;
            }

            // interfaz IDamageSource (DamageAmount read-only)
            if (comp is IDamageSource idsrc)
            {
                outValue = idsrc.DamageAmount;
                return true;
            }

            // Componentes concretos conocidos (optimización rápida)
            if (comp is DamageDealer dd)
            {
                outValue = dd.Damage;
                return true;
            }

            // DamageSource concreto del namespace Assets.Scripts.Game
            if (comp is DamageSource ds)
            {
                outValue = ds.DamageAmount;
                return true;
            }

            // VidaEnemigo en la jerarquía del enemigo
            if (comp is VidaEnemigo ve)
            {
                outValue = ve.Damage;
                return true;
            }
        }

        return false;
    }

    private void OnValidate()
    {
        vidaMaximaBase = Mathf.Max(1, vidaMaximaBase);
        vidaPorPuntoSalud = Mathf.Max(0, vidaPorPuntoSalud);
        
        if (vidaInicial > 0)
        {
            // En modo edición, calcular vida máxima temporal para validación
            if (!Application.isPlaying)
            {
                if (atributos == null)
                {
                    TryGetComponent(out atributos);
                    if (atributos == null) atributos = GetComponentInParent<Atributos>();
                }

                int saludTemp = (atributos != null) ? atributos.SaludActual : 0;
                int vidaMaxTemp = vidaMaximaBase + (saludTemp * vidaPorPuntoSalud);
                vidaInicial = Mathf.Clamp(vidaInicial, 0, vidaMaxTemp);
            }
            else
            {
                vidaInicial = Mathf.Clamp(vidaInicial, 0, vidaMaxima);
            }
        }

        flashDuration = Mathf.Max(0f, flashDuration);
        invulnerabilidad = Mathf.Max(0f, invulnerabilidad);
    }

    // Throttle en Stay para reducir frecuencia de daño en contacto mantenido
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == null) return;
        if (contactoIntervalo > 0f && Time.time - _ultimoChequeoContacto < contactoIntervalo) return;
        _ultimoChequeoContacto = Time.time;
        HandleCollision(other.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision == null) return;
        if (contactoIntervalo > 0f && Time.time - _ultimoChequeoContacto < contactoIntervalo) return;
        _ultimoChequeoContacto = Time.time;
        HandleCollision(collision.gameObject);
    }
}
