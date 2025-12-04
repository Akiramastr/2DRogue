using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Proyectil : MonoBehaviour
{
    [Header("Configuración de Daño")]
    [SerializeField, Tooltip("Daño que inflige el proyectil al impactar.")]
    private int damage = 10;

    [SerializeField, Tooltip("Tag del objetivo (ej: 'Player' para proyectiles enemigos, 'Enemy' para proyectiles del jugador).")]
    private string targetTag = "Player";

    [Header("Movimiento")]
    [SerializeField, Tooltip("Velocidad del proyectil.")]
    private float velocidad = 10f;

    [SerializeField, Tooltip("Tiempo de vida del proyectil antes de autodestruirse (segundos).")]
    private float tiempoDeVida = 5f;

    [Header("Colisiones")]
    [SerializeField, Tooltip("Tags de objetos que destruyen el proyectil al impactar.")]
    private string[] tagsObstaculos = { "Wall", "obstaculo", "Ground" };

    [Header("Efectos Visuales")]
    [SerializeField, Tooltip("Prefab de partículas al impactar (opcional).")]
    private GameObject impactVfxPrefab;

    [SerializeField, Tooltip("Clip de sonido al impactar (opcional).")]
    private AudioClip impactSound;

    private Rigidbody2D rb;
    private Vector2 direccion;

    public int Damage => damage;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        // Autodestruir después del tiempo de vida
        Destroy(gameObject, tiempoDeVida);
    }

    /// <summary>
    /// Inicializa el proyectil con una dirección específica
    /// </summary>
    public void Inicializar(Vector2 direccion)
    {
        this.direccion = direccion.normalized;
        
        if (rb != null)
        {
            rb.linearVelocity = this.direccion * velocidad;
        }

        // Rotar el proyectil para que apunte en la dirección de movimiento
        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angulo);
    }

    /// <summary>
    /// Inicializa el proyectil hacia un objetivo específico
    /// </summary>
    public void InicializarHaciaObjetivo(Vector3 posicionObjetivo)
    {
        Vector2 direccionHaciaObjetivo = (posicionObjetivo - transform.position).normalized;
        Inicializar(direccionHaciaObjetivo);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        // Verificar si impactó con el objetivo
        if (other.CompareTag(targetTag))
        {
            AplicarDano(other.gameObject);
            ReproducirEfectos();
            Destroy(gameObject);
            return;
        }

        // Verificar si impactó con algún obstáculo
        if (EsObstaculo(other))
        {
            ReproducirEfectos();
            Destroy(gameObject);
        }
    }

    private bool EsObstaculo(Collider2D collider)
    {
        foreach (string tag in tagsObstaculos)
        {
            if (collider.CompareTag(tag))
                return true;
        }
        return false;
    }

    private void AplicarDano(GameObject objetivo)
    {
        // Intentar aplicar daño mediante VidaPersonaje
        var vidaPersonaje = objetivo.GetComponent<VidaPersonaje>();
        if (vidaPersonaje != null)
        {
            vidaPersonaje.AplicarDano(damage);
            return;
        }

        // Fallback: intentar con SendMessage
        objetivo.SendMessage("SetDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private void ReproducirEfectos()
    {
        // Instanciar partículas de impacto
        if (impactVfxPrefab != null)
        {
            var vfx = Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);
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

        // Reproducir sonido de impacto
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position);
        }
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        velocidad = Mathf.Max(0f, velocidad);
        tiempoDeVida = Mathf.Max(0.1f, tiempoDeVida);
    }
}