using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UISeleccionMejoras : MonoBehaviour
{
    public static UISeleccionMejoras Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject panelSeleccion;
    [SerializeField] private Button[] botonesMejoras;
    [SerializeField] private TextMeshProUGUI[] textosTitulos;
    [SerializeField] private TextMeshProUGUI[] textosDescripciones;
    [SerializeField] private Image[] imagenesIconos;

    [Header("Navegación")]
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorSeleccionado = Color.yellow;
    [SerializeField] private float escalaSeleccionado = 1.1f;

    [Header("Audio")]
    [SerializeField] private AudioClip sonidoNavegacion;
    [SerializeField] private AudioClip sonidoSeleccion;

    private SistemaMejoras.OpcionMejora[] opcionesActuales;
    private int indiceSeleccionado = 0;
    private bool esperandoSeleccion = false;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        panelSeleccion.SetActive(false);
    }

    private void Update()
    {
        if (!esperandoSeleccion) return;

        // Navegación horizontal
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0)
        {
            CambiarSeleccion(horizontal > 0 ? 1 : -1);
        }

        // Selección con Fire1
        if (Input.GetButtonDown("Fire1"))
        {
            ConfirmarSeleccion();
        }
    }

    /// <summary>
    /// Muestra el panel de selección de mejoras.
    /// </summary>
    public void MostrarOpciones(SistemaMejoras.OpcionMejora[] opciones)
    {
        opcionesActuales = opciones;
        indiceSeleccionado = 0;

        // Pausar el juego
        Time.timeScale = 0f;

        // Configurar botones con las opciones
        for (int i = 0; i < botonesMejoras.Length; i++)
        {
            if (i < opciones.Length)
            {
                botonesMejoras[i].gameObject.SetActive(true);
                ConfigurarBoton(i, opciones[i]);
            }
            else
            {
                botonesMejoras[i].gameObject.SetActive(false);
            }
        }

        ActualizarVisualizacionSeleccion();
        panelSeleccion.SetActive(true);
        esperandoSeleccion = true;

        Debug.Log("[UISeleccionMejoras] Panel mostrado. Esperando selección del jugador.");
    }

    private void ConfigurarBoton(int indice, SistemaMejoras.OpcionMejora opcion)
    {
        if (textosTitulos != null && indice < textosTitulos.Length && textosTitulos[indice] != null)
        {
            textosTitulos[indice].text = opcion.nombre;
        }

        if (textosDescripciones != null && indice < textosDescripciones.Length && textosDescripciones[indice] != null)
        {
            textosDescripciones[indice].text = opcion.descripcion;
        }

        if (imagenesIconos != null && indice < imagenesIconos.Length && imagenesIconos[indice] != null && opcion.icono != null)
        {
            imagenesIconos[indice].sprite = opcion.icono;
        }

        // Configurar callback del botón
        int indiceCapturado = indice;
        botonesMejoras[indice].onClick.RemoveAllListeners();
        botonesMejoras[indice].onClick.AddListener(() => SeleccionarMejoraConMouse(indiceCapturado));
    }

    private void CambiarSeleccion(int direccion)
    {
        if (opcionesActuales == null || opcionesActuales.Length == 0) return;

        // Reproducir sonido de navegación
        if (sonidoNavegacion != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoNavegacion);
        }

        indiceSeleccionado += direccion;

        // Wraparound
        if (indiceSeleccionado < 0)
            indiceSeleccionado = opcionesActuales.Length - 1;
        else if (indiceSeleccionado >= opcionesActuales.Length)
            indiceSeleccionado = 0;

        ActualizarVisualizacionSeleccion();
    }

    private void ActualizarVisualizacionSeleccion()
    {
        for (int i = 0; i < botonesMejoras.Length; i++)
        {
            if (!botonesMejoras[i].gameObject.activeSelf) continue;

            bool esSeleccionado = (i == indiceSeleccionado);

            // Cambiar color
            var imagen = botonesMejoras[i].GetComponent<Image>();
            if (imagen != null)
            {
                imagen.color = esSeleccionado ? colorSeleccionado : colorNormal;
            }

            // Cambiar escala
            botonesMejoras[i].transform.localScale = Vector3.one * (esSeleccionado ? escalaSeleccionado : 1f);
        }
    }

    private void ConfirmarSeleccion()
    {
        if (opcionesActuales == null || indiceSeleccionado >= opcionesActuales.Length) return;

        // Reproducir sonido de selección
        if (sonidoSeleccion != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoSeleccion);
        }

        SistemaMejoras.OpcionMejora mejoraSeleccionada = opcionesActuales[indiceSeleccionado];
        Debug.Log($"[UISeleccionMejoras] Mejora seleccionada: {mejoraSeleccionada.nombre}");

        AplicarYCerrar(mejoraSeleccionada);
    }

    private void SeleccionarMejoraConMouse(int indice)
    {
        indiceSeleccionado = indice;
        ConfirmarSeleccion();
    }

    private void AplicarYCerrar(SistemaMejoras.OpcionMejora mejora)
    {
        esperandoSeleccion = false;

        // Buscar el jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[UISeleccionMejoras] No se encontró el jugador.");
            return;
        }

        // Aplicar la mejora
        if (SistemaMejoras.Instance != null)
        {
            SistemaMejoras.Instance.AplicarMejora(mejora, player);
        }

        // Ocultar panel
        StartCoroutine(CerrarPanelConDelay());
    }

    private IEnumerator CerrarPanelConDelay()
    {
        // Pequeño delay para que el jugador vea la selección
        yield return new WaitForSecondsRealtime(0.3f);

        panelSeleccion.SetActive(false);

        // Reanudar el juego
        Time.timeScale = 1f;

        // Notificar al EnemyController que puede continuar
        var enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController != null)
        {
            enemyController.SendMessage("OnMejoraAplicada", SendMessageOptions.DontRequireReceiver);
        }

        Debug.Log("[UISeleccionMejoras] Panel cerrado. Juego reanudado.");
    }

    public bool EstaActivo()
    {
        return esperandoSeleccion;
    }
}