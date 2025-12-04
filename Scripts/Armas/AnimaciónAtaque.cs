using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ActivarAnimatorAlEntrar : MonoBehaviour
{
    [Header("Opciones de activación")]
    [SerializeField, Tooltip("Si es true usa SetTrigger(trigName). Si es false usa Play(stateName).")]
    private bool useTrigger = true;

    [SerializeField, Tooltip("Nombre del Trigger a disparar (si useTrigger)")]
    private string trigName = "Start";

    [SerializeField, Tooltip("Nombre del estado a reproducir (si !useTrigger)")]
    private string stateName = "";

    [SerializeField, Tooltip("Capa del Animator (-1 = capa por defecto)")]
    private int layer = -1;

    [SerializeField, Tooltip("Normalizado del tiempo de reproducción cuando se usa Play (0..1)")]
    private float normalizedTime = 0f;

    [Header("Sincronización de posición")]
    [SerializeField, Tooltip("Transform del player al que mantener la distancia. Si se deja vacío intentará buscar VidaPersonaje en los padres.")]
    private Transform target;

    [SerializeField, Tooltip("Mantener distancia en espacio mundial en lugar de local.")]
    private bool useWorldOffset = true;

    [Header("Control de escala")]
    [SerializeField, Tooltip("Factor multiplicador del tamaño de los sprites de la animación (1 = tamaño original).")]
    private float spriteScale = 1f; // única definición de spriteScale

    [Header("Ajuste por Atributos")]
    [SerializeField, Tooltip("Escala base del sprite antes de aplicar el atributo 'Escala'.")]
    private float spriteScaleBase = 1f;

    [SerializeField, Tooltip("Incremento de escala por cada punto del atributo 'Escala'.")]
    private float incrementoEscalaPorPunto = 0.1f;

    private Animator animator;
    private Vector3 offset;
    private Vector3 initialScale;
    private Atributos _atributos;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("ActivarAnimatorAlEntrar: no se encontró Animator.");
            return;
        }

        // Guardar escala inicial
        initialScale = transform.localScale;

        // Si no se asignó target, intentar resolverlo buscando VidaPersonaje en los padres
        if (target == null)
        {
            var vida = GetComponentInParent<VidaPersonaje>();
            if (vida != null)
                target = vida.transform;
            else if (transform.parent != null)
                target = transform.parent; // fallback al padre inmediato
        }

        // Resolver Atributos desde esta jerarquía o desde el target
        _atributos = GetComponentInParent<Atributos>();
        if (_atributos == null && target != null)
            _atributos = target.GetComponentInParent<Atributos>();

        if (_atributos != null)
        {
            ActualizarEscalaDesdeAtributos(_atributos.EscalaActual);
            _atributos.OnEscalaCambiada += ActualizarEscalaDesdeAtributos;
        }
        else
        {
            // Fallback: usar spriteScale actual o la base si fuese <= 0
            spriteScale = Mathf.Max(0.01f, spriteScale <= 0f ? spriteScaleBase : spriteScale);
        }

        // Calcular offset inicial para mantener la distancia fija respecto al target
        if (target != null)
        {
            if (useWorldOffset)
                offset = transform.position - target.position;
            else
                offset = transform.localPosition; // si se quiere en local, usar localPosition
            // Asegurar posición y escala inicial exacta
            ApplyOffset();
            ApplyScale();
        }

        // Activar animator (después de posicionar para evitar salto visual)
        if (useTrigger)
        {
            if (string.IsNullOrEmpty(trigName))
            {
                Debug.LogWarning("ActivarAnimatorAlEntrar: trigName está vacío.");
                return;
            }

            animator.ResetTrigger(trigName);
            animator.SetTrigger(trigName);
            return;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning("ActivarAnimatorAlEntrar: stateName está vacío.");
            return;
        }

        if (layer < 0) animator.Play(stateName, 0, normalizedTime);
        else animator.Play(stateName, layer, normalizedTime);
    }

    void OnDestroy()
    {
        if (_atributos != null)
            _atributos.OnEscalaCambiada -= ActualizarEscalaDesdeAtributos;
    }

    void LateUpdate()
    {
        // Mantener la distancia fija en cada frame si hay target
        if (target != null)
        {
            ApplyOffset();
            ApplyScale();
        }
    }

    private void ApplyOffset()
    {
        if (useWorldOffset)
            transform.position = target.position + offset;
        else
        {
            // mantener localPosition fija respecto al padre (si estamos parentados)
            if (transform.parent == target)
                transform.localPosition = offset;
            else
                transform.position = target.TransformPoint(offset);
        }
    }

    private void ApplyScale()
    {
        float s = Mathf.Max(0.01f, spriteScale);
        transform.localScale = initialScale * s;
    }

    private void ActualizarEscalaDesdeAtributos(int escalaActual)
    {
        // Escala efectiva: base + (escalaActual * incremento)
        spriteScale = Mathf.Max(0.01f, spriteScaleBase + escalaActual * Mathf.Max(0f, incrementoEscalaPorPunto));
        ApplyScale();
    }
}