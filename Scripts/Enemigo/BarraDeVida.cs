using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BarraDeVida : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Referencia al jugador. Si no se asigna, se busca automáticamente por tag 'Player'.")]
    private GameObject player;

    [SerializeField, Tooltip("Imagen hijo con fillAmount que representa la vida (debe tener Image Type: Filled).")]
    private Image imagenFrente;

    [Header("Configuración")]
    [SerializeField, Tooltip("Tag del jugador para búsqueda automática.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("Buscar imagen 'Frente' automáticamente en los hijos si no se asigna manualmente.")]
    private bool buscarImagenAutomaticamente = true;

    [SerializeField, Tooltip("Suavizado de la animación de la barra (0 = instantáneo, mayor = más suave).")]
    private float velocidadSuavizado = 5f;

    [Header("Efecto de Vibración al Recibir Daño")]
    [SerializeField, Tooltip("Activar efecto de vibración cuando el jugador recibe daño.")]
    private bool activarVibracion = true;

    [SerializeField, Tooltip("Intensidad de la vibración (amplitud del movimiento en píxeles).")]
    private float intensidadVibracion = 10f;

    [SerializeField, Tooltip("Duración de la vibración en segundos.")]
    private float duracionVibracion = 0.3f;

    [SerializeField, Tooltip("Dirección de la sacudida: (1,0) = horizontal, (0,1) = vertical, (1,1) = diagonal.")]
    private Vector2 direccionSacudida = new Vector2(1f, 0.5f);

    [SerializeField, Tooltip("Frecuencia de la vibración (velocidad de oscilación). Mayor = más rápido.")]
    private float frecuenciaVibracion = 25f;

    [SerializeField, Tooltip("Usar atenuación progresiva (la vibración disminuye gradualmente).")]
    private bool usarAtenuacion = true;

    private VidaPersonaje vidaPersonaje;
    private float fillAmountObjetivo = 1f;
    private int vidaAnterior = 0;

    // Control de vibración
    private Vector3 posicionOriginal;
    private Coroutine vibracionCoroutine;

    void Start()
    {
        // Guardar posición original para el efecto de vibración
        posicionOriginal = transform.localPosition;

        InicializarReferencias();
        SuscribirEventos();
        ActualizarBarraInicial();
    }

    void OnDestroy()
    {
        DesuscribirEventos();
    }

    void Update()
    {
        // Suavizar el cambio del fillAmount
        if (imagenFrente != null && velocidadSuavizado > 0)
        {
            imagenFrente.fillAmount = Mathf.Lerp(imagenFrente.fillAmount, fillAmountObjetivo, Time.deltaTime * velocidadSuavizado);
        }
    }

    private void InicializarReferencias()
    {
        // Buscar jugador si no está asignado
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null)
            {
                Debug.LogError("[BarraDeVida] No se encontró el jugador con tag '" + playerTag + "'.")
;
                return;
            }
        }

        // Obtener componente VidaPersonaje
        vidaPersonaje = player.GetComponent<VidaPersonaje>();
        if (vidaPersonaje == null)
        {
            vidaPersonaje = player.GetComponentInChildren<VidaPersonaje>();
        }

        if (vidaPersonaje == null)
        {
            Debug.LogError("[BarraDeVida] No se encontró el componente VidaPersonaje en el jugador.");
            return;
        }

        // Buscar imagen Frente si no está asignada
        if (imagenFrente == null && buscarImagenAutomaticamente)
        {
            Transform frenteTransform = transform.Find("Frente");
            if (frenteTransform != null)
            {
                imagenFrente = frenteTransform.GetComponent<Image>();
            }

            if (imagenFrente == null)
            {
                Debug.LogError("[BarraDeVida] No se encontró la imagen 'Frente' en los hijos. Asígnala manualmente o asegúrate de que existe.");
            }
        }

        // Inicializar vida anterior
        if (vidaPersonaje != null)
        {
            vidaAnterior = vidaPersonaje.VidaActual;
        }
    }

    private void SuscribirEventos()
    {
        if (vidaPersonaje != null)
        {
            vidaPersonaje.OnVidaCambiada += ActualizarBarra;
            vidaPersonaje.OnVidaMaximaCambiada += OnVidaMaximaCambiada;
        }
    }

    private void DesuscribirEventos()
    {
        if (vidaPersonaje != null)
        {
            vidaPersonaje.OnVidaCambiada -= ActualizarBarra;
            vidaPersonaje.OnVidaMaximaCambiada -= OnVidaMaximaCambiada;
        }
    }

    private void ActualizarBarraInicial()
    {
        if (vidaPersonaje != null && imagenFrente != null)
        {
            fillAmountObjetivo = CalcularFillAmount();
            imagenFrente.fillAmount = fillAmountObjetivo;
            vidaAnterior = vidaPersonaje.VidaActual;
        }
    }

    private void ActualizarBarra(int vidaActual)
    {
        if (imagenFrente == null) return;

        fillAmountObjetivo = CalcularFillAmount();

        // Si velocidadSuavizado es 0, actualizar instantáneamente
        if (velocidadSuavizado <= 0)
        {
            imagenFrente.fillAmount = fillAmountObjetivo;
        }

        // Detectar si ha recibido daño (vida disminuyó)
        if (activarVibracion && vidaActual < vidaAnterior)
        {
            IniciarVibracion();
        }

        vidaAnterior = vidaActual;
    }

    private void OnVidaMaximaCambiada(int nuevaVidaMaxima)
    {
        // Recalcular el fillAmount cuando cambia la vida máxima
        if (vidaPersonaje != null)
        {
            ActualizarBarra(vidaPersonaje.VidaActual);
        }
    }

    private float CalcularFillAmount()
    {
        if (vidaPersonaje == null || vidaPersonaje.VidaMaxima <= 0)
            return 0f;

        return Mathf.Clamp01((float)vidaPersonaje.VidaActual / vidaPersonaje.VidaMaxima);
    }

    private void IniciarVibracion()
    {
        // Detener vibración anterior si existe
        if (vibracionCoroutine != null)
        {
            StopCoroutine(vibracionCoroutine);
            transform.localPosition = posicionOriginal;
        }

        vibracionCoroutine = StartCoroutine(VibracionCoroutine());
    }

    private IEnumerator VibracionCoroutine()
    {
        float tiempoTranscurrido = 0f;
        Vector2 direccionNormalizada = direccionSacudida.normalized;

        while (tiempoTranscurrido < duracionVibracion)
        {
            // Calcular progreso (0 a 1)
            float progreso = tiempoTranscurrido / duracionVibracion;

            // Calcular factor de atenuación si está activado
            float factorAtenuacion = usarAtenuacion ? (1f - progreso) : 1f;

            // Generar offset usando una función seno para movimiento suave
            float offsetX = Mathf.Sin(tiempoTranscurrido * frecuenciaVibracion) * intensidadVibracion * direccionNormalizada.x * factorAtenuacion;
            float offsetY = Mathf.Sin(tiempoTranscurrido * frecuenciaVibracion * 1.3f) * intensidadVibracion * direccionNormalizada.y * factorAtenuacion;

            // Aplicar offset a la posición
            transform.localPosition = posicionOriginal + new Vector3(offsetX, offsetY, 0f);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Restaurar posición original al finalizar
        transform.localPosition = posicionOriginal;
        vibracionCoroutine = null;
    }

    private void OnValidate()
    {
        velocidadSuavizado = Mathf.Max(0f, velocidadSuavizado);
        intensidadVibracion = Mathf.Max(0f, intensidadVibracion);
        duracionVibracion = Mathf.Max(0f, duracionVibracion);
        frecuenciaVibracion = Mathf.Max(0f, frecuenciaVibracion);

        // Normalizar dirección si es muy pequeña
        if (direccionSacudida.magnitude < 0.01f)
        {
            direccionSacudida = Vector2.right;
        }
    }

    // Método público para forzar vibración manualmente (útil para testing)
    public void ForzarVibracion()
    {
        if (activarVibracion)
        {
            IniciarVibracion();
        }
    }

    // Método público para detener vibración manualmente
    public void DetenerVibracion()
    {
        if (vibracionCoroutine != null)
        {
            StopCoroutine(vibracionCoroutine);
            transform.localPosition = posicionOriginal;
            vibracionCoroutine = null;
        }
    }
}
