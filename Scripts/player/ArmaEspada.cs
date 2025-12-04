using System;
using System.Reflection;
using UnityEngine;
using Assets.Scripts.Game;

[RequireComponent(typeof(Atributos))]
public class ArmaEspada : MonoBehaviour
{
    [SerializeField] private GameObject areaColisionPrefab;
    [SerializeField] private float distanciaAtaque = 1.5f;
    [SerializeField] private float duracionColision = 0.2f;

    [Header("Velocidad de ataque")]
    [SerializeField, Tooltip("Tiempo base entre ataques (segundos) antes de aplicar Atributos.Velocidad de ataque.")]
    private float tiempoBaseEntreAtaques = 1.0f;

    [SerializeField, Tooltip("Reducción de tiempo (segundos) por cada punto de Velocidad de ataque.")]
    private float reduccionPorPuntoVelocidad = 0.05f;

    [SerializeField, Tooltip("Límite mínimo de tiempo entre ataques.")]
    private float tiempoMinEntreAtaques = 0.2f;

    private bool puedeAtacar = true;

    [Header("Daño")]
    [SerializeField, Tooltip("Daño base del jugador.")]
    private int dañoBase = 10;

    [SerializeField, Tooltip("Daño adicional por punto de Fuerza.")]
    private int dañoPorFuerza = 1;

    private Atributos atributos;

    [Header("Debug - Valores en tiempo real")]
    [SerializeField, Tooltip("Debug: último delay calculado (segundos).")]
    private float debugDelayCalculado;

    [SerializeField, Tooltip("Debug: última Velocidad de ataque leída de Atributos.")]
    private int debugVelocidadLeida;

    [SerializeField, Tooltip("Debug: ataques por segundo actual.")]
    private float debugAtaquesPorSegundo;

    [SerializeField, Tooltip("Debug: última Fuerza leída de Atributos.")]
    private int debugFuerzaLeida;

    [SerializeField, Tooltip("Debug: daño total calculado.")]
    private int debugDañoTotal;

    void Awake()
    {
        if (!TryGetComponent(out atributos))
            atributos = GetComponentInParent<Atributos>();
    }

    void OnEnable()
    {
        // Suscribirse a los eventos de cambio de atributos
        if (atributos != null)
        {
            atributos.OnVelocidadAtaqueCambiada += ActualizarVelocidadAtaque;
            atributos.OnFuerzaCambiada += ActualizarFuerza;
        }
    }

    void OnDisable()
    {
        // Desuscribirse para evitar memory leaks
        if (atributos != null)
        {
            atributos.OnVelocidadAtaqueCambiada -= ActualizarVelocidadAtaque;
            atributos.OnFuerzaCambiada -= ActualizarFuerza;
        }
    }

    void Start()
    {
        if (atributos == null)
            atributos = GetComponentInParent<Atributos>();
        
        ActualizarDebugInfo();
    }

    void Update()
    {
        // Actualizar info de debug cada frame para ver cambios en tiempo real
        ActualizarDebugInfo();

        if (Input.GetButtonDown("Fire1") && puedeAtacar)
        {
            puedeAtacar = false;

            Vector3 mouseScreenPosition = Input.mousePosition;
            var cam = Camera.main;
            if (cam != null)
            {
                mouseScreenPosition.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
                Vector3 mouseWorldPosition = cam.ScreenToWorldPoint(mouseScreenPosition);
                Vector2 direccion = (mouseWorldPosition - transform.position).normalized;

                if (areaColisionPrefab != null)
                {
                    Vector3 spawnPos = transform.position + (Vector3)direccion * distanciaAtaque;
                    GameObject colision = Instantiate(areaColisionPrefab, spawnPos, Quaternion.identity, transform);
                    colision.transform.right = direccion;

                    int damageValue = GetDamageValue();

                    var ds = colision.GetComponent<Assets.Scripts.Game.DamageSource>();
                    if (ds == null) ds = colision.AddComponent<Assets.Scripts.Game.DamageSource>();
                    ds.SetAmount(damageValue);

                    Destroy(colision, duracionColision);
                }
            }

            // Calcular delay con la nueva fórmula más clara
            float delay = CalcularDelayAtaque();
            Invoke(nameof(HabilitarAtaque), delay);
        }
    }

    private float CalcularDelayAtaque()
    {
        int velocidad = (atributos != null) ? atributos.VelocidadAtaqueActual : 0;
        float reduccionTotal = velocidad * reduccionPorPuntoVelocidad;
        float delay = Mathf.Max(tiempoMinEntreAtaques, tiempoBaseEntreAtaques - reduccionTotal);
        
        return delay;
    }

    private void ActualizarDebugInfo()
    {
        int vel = (atributos != null) ? atributos.VelocidadAtaqueActual : 0;
        int fuerza = (atributos != null) ? atributos.FuerzaActual : 0;
        
        debugVelocidadLeida = vel;
        debugFuerzaLeida = fuerza;
        debugDelayCalculado = CalcularDelayAtaque();
        debugAtaquesPorSegundo = debugDelayCalculado > 0 ? 1f / debugDelayCalculado : 0f;
        debugDañoTotal = GetDamageValue();
    }

    // Callbacks para eventos de atributos
    private void ActualizarVelocidadAtaque(int nuevaVelocidad)
    {
        Debug.Log($"[ArmaEspada] Velocidad de ataque actualizada a: {nuevaVelocidad}");
        ActualizarDebugInfo();
    }

    private void ActualizarFuerza(int nuevaFuerza)
    {
        Debug.Log($"[ArmaEspada] Fuerza actualizada a: {nuevaFuerza}");
        ActualizarDebugInfo();
    }

    private void HabilitarAtaque()
    {
        puedeAtacar = true;
    }

    private int GetDamageValue()
    {
        int fuerza = (atributos != null) ? atributos.FuerzaActual : 0;
        return dañoBase + fuerza * dañoPorFuerza;
    }

    private bool TrySetDamageOnInstance(GameObject instance, int value)
    {
        if (instance == null) return false;

        string[] candidateNames = { "Damage", "damage", "Dano", "dano", "daño", "Daño", "damageAmount", "damage_value", "DamageAmount" };
        var components = instance.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var t = comp.GetType();

            foreach (var name in candidateNames)
            {
                var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                {
                    try { fi.SetValue(comp, Convert.ChangeType(value, fi.FieldType)); return true; } catch { }
                }

                var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null && pi.CanWrite && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                {
                    try { pi.SetValue(comp, Convert.ChangeType(value, pi.PropertyType)); return true; } catch { }
                }
            }
        }

        return false;
    }

    private void OnValidate()
    {
        tiempoBaseEntreAtaques = Mathf.Max(0.01f, tiempoBaseEntreAtaques);
        reduccionPorPuntoVelocidad = Mathf.Max(0f, reduccionPorPuntoVelocidad);
        tiempoMinEntreAtaques = Mathf.Clamp(tiempoMinEntreAtaques, 0f, tiempoBaseEntreAtaques);

        if (!Application.isPlaying)
        {
            if (atributos == null)
            {
                TryGetComponent(out atributos);
                if (atributos == null) atributos = GetComponentInParent<Atributos>();
            }

            int vel = (atributos != null) ? atributos.VelocidadAtaqueActual : 0;
            debugVelocidadLeida = vel;
            float reduccionTotal = vel * reduccionPorPuntoVelocidad;
            debugDelayCalculado = Mathf.Max(tiempoMinEntreAtaques, tiempoBaseEntreAtaques - reduccionTotal);
            debugAtaquesPorSegundo = debugDelayCalculado > 0 ? 1f / debugDelayCalculado : 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        int damageValue = DamageUtils.GetDamageFrom(other.gameObject);
        if (damageValue <= 0) return;

        try
        {
            other.gameObject.SendMessageUpwards("SetDamage", damageValue, SendMessageOptions.DontRequireReceiver);
        }
        catch { }
    }
}

public interface IDamageSource
{
    int DamageAmount { get; }
}

public static class DamageUtils
{
    public static int GetDamageFrom(GameObject obj)
    {
        if (obj == null) return 0;

        var sources = obj.GetComponents<MonoBehaviour>();
        foreach (var src in sources)
        {
            if (src is IDamageSource damageSource)
                return damageSource.DamageAmount;
        }

        string[] candidateNames = { "Damage", "damage", "Dano", "dano", "daño", "Daño", "damageAmount", "damage_value", "DamageAmount" };
        var components = obj.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var t = comp.GetType();

            foreach (var name in candidateNames)
            {
                var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                {
                    try { return Convert.ToInt32(fi.GetValue(comp)); } catch { }
                }

                var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi != null && pi.CanRead && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                {
                    try { return Convert.ToInt32(pi.GetValue(comp)); } catch { }
                }
            }
        }

        return 0;
    }
}