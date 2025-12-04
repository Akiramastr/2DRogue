using UnityEngine;
using System.Collections;

public class DisparadorProyectiles : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Prefab del proyectil a disparar.")]
    private GameObject proyectilPrefab;

    [SerializeField, Tooltip("Punto de spawn del proyectil. Si no se asigna, usa la posición del enemigo.")]
    private Transform puntoDisparo;

    [SerializeField, Tooltip("Referencia al jugador. Si no se asigna, se busca automáticamente.")]
    private GameObject player;

    [Header("Configuración de Disparo")]
    [SerializeField, Tooltip("Tag del jugador para búsqueda automática.")]
    private string playerTag = "Player";

    [SerializeField, Tooltip("Intervalo entre disparos (segundos).")]
    private float intervaloDisparo = 2f;

    [SerializeField, Tooltip("Rango máximo de disparo. Si el jugador está más lejos, no dispara.")]
    private float rangoDisparo = 15f;

    [SerializeField, Tooltip("Iniciar disparando automáticamente al comenzar.")]
    private bool dispararAlIniciar = true;

    [SerializeField, Tooltip("Requiere línea de visión directa al jugador para disparar.")]
    private bool requiereLineaDeVision = true;

    [SerializeField, Tooltip("Capas que bloquean la línea de visión (ej: paredes).")]
    private LayerMask capasObstaculos;

    [Header("Tipo de Disparo")]
    [SerializeField, Tooltip("Tipo de patrón de disparo.")]
    private TipoDisparo tipoDisparo = TipoDisparo.HaciaJugador;

    [SerializeField, Tooltip("Número de proyectiles en ráfaga (solo para tipo Rafaga).")]
    private int proyectilesPorRafaga = 3;

    [SerializeField, Tooltip("Ángulo de dispersión entre proyectiles (solo para tipo Rafaga/Circular).")]
    private float anguloDispersion = 15f;

    [Header("Previsualización")]
    [SerializeField, Tooltip("Mostrar líneas de previsualización en el editor.")]
    private bool mostrarPrevisualizacionEditor = true;

    [SerializeField, Tooltip("Mostrar aviso visual antes de disparar (LineRenderer).")]
    private bool mostrarAvisoDisparo = true;

    [SerializeField, Tooltip("Tiempo antes de disparar en el que aparece el aviso visual (segundos).")]
    private float tiempoAvisoAntes = 0.5f;

    [SerializeField, Tooltip("Longitud de las líneas de previsualización.")]
    private float longitudPreviewLinea = 5f;

    [SerializeField, Tooltip("Color de las líneas de previsualización.")]
    private Color colorPreviewLinea = new Color(1f, 0f, 0f, 0.5f);

    [SerializeField, Tooltip("Grosor de las líneas de previsualización.")]
    private float grosorLineaRuntime = 0.05f;

    [SerializeField, Tooltip("Material para las líneas de previsualización (opcional).")]
    private Material materialLinea;

    [SerializeField, Tooltip("Hacer que las líneas parpadeen antes de disparar.")]
    private bool lineasParpadean = true;

    [SerializeField, Tooltip("Velocidad de parpadeo (ciclos por segundo).")]
    private float velocidadParpadeo = 5f;

    public enum TipoDisparo
    {
        HaciaJugador,    // Dispara directo al jugador
        Rafaga,          // Múltiples proyectiles en abanico
        Circular,        // 360 grados
        Direccion        // Dirección fija (hacia donde mira el enemigo)
    }

    private Coroutine disparoCoroutine;
    private float ultimoDisparo = 0f;
    private LineRenderer[] lineasPreview;
    private bool lineasActivas = false;

    void Start()
    {
        // Buscar jugador si no está asignado
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }

        // Usar posición del enemigo si no hay punto de disparo
        if (puntoDisparo == null)
        {
            puntoDisparo = transform;
        }

        // Crear líneas de previsualización (ocultas inicialmente)
        if (mostrarAvisoDisparo)
        {
            CrearLineasPreview();
            OcultarLineasPreview();
        }

        if (dispararAlIniciar)
        {
            IniciarDisparo();
        }
    }

    void Update()
    {
        // Actualizar posición de líneas si están activas
        if (lineasActivas && lineasPreview != null)
        {
            ActualizarPosicionLineas();
        }
    }

    void OnDestroy()
    {
        DetenerDisparo();
        DestruirLineasPreview();
    }

    #region Sistema de Disparo

    /// <summary>
    /// Inicia el ciclo automático de disparo
    /// </summary>
    public void IniciarDisparo()
    {
        if (disparoCoroutine == null)
        {
            disparoCoroutine = StartCoroutine(CicloDisparo());
        }
    }

    /// <summary>
    /// Detiene el ciclo automático de disparo
    /// </summary>
    public void DetenerDisparo()
    {
        if (disparoCoroutine != null)
        {
            StopCoroutine(disparoCoroutine);
            disparoCoroutine = null;
        }
        OcultarLineasPreview();
    }

    /// <summary>
    /// Dispara un proyectil manualmente (útil para triggerearlo desde animaciones u otros scripts)
    /// </summary>
    public void DispararManual()
    {
        if (PuedeDisparar())
        {
            StartCoroutine(SecuenciaDisparoConAviso());
        }
    }

    private IEnumerator CicloDisparo()
    {
        while (true)
        {
            if (PuedeDisparar())
            {
                yield return SecuenciaDisparoConAviso();
            }
            else
            {
                // Si no puede disparar, esperar un poco antes de revisar de nuevo
                yield return new WaitForSeconds(intervaloDisparo * 0.5f);
            }
        }
    }

    private IEnumerator SecuenciaDisparoConAviso()
    {
        // 1. Mostrar aviso visual si está activado
        if (mostrarAvisoDisparo && tiempoAvisoAntes > 0)
        {
            MostrarLineasPreview();
            
            // Efecto de parpadeo durante el tiempo de aviso
            if (lineasParpadean)
            {
                yield return StartCoroutine(EfectoParpadeo(tiempoAvisoAntes));
            }
            else
            {
                yield return new WaitForSeconds(tiempoAvisoAntes);
            }
        }

        // 2. Disparar
        Disparar();
        ultimoDisparo = Time.time;

        // 3. Ocultar aviso inmediatamente después de disparar
        OcultarLineasPreview();

        // 4. Esperar el intervalo completo antes del siguiente disparo
        yield return new WaitForSeconds(intervaloDisparo);
    }

    private IEnumerator EfectoParpadeo(float duracion)
    {
        float tiempoTranscurrido = 0f;
        bool visible = true;

        while (tiempoTranscurrido < duracion)
        {
            // Alternar visibilidad
            visible = !visible;
            
            if (lineasPreview != null)
            {
                foreach (var linea in lineasPreview)
                {
                    if (linea != null)
                    {
                        linea.enabled = visible;
                    }
                }
            }

            // Esperar según la velocidad de parpadeo
            yield return new WaitForSeconds(1f / (velocidadParpadeo * 2f));
            tiempoTranscurrido += 1f / (velocidadParpadeo * 2f);
        }

        // Asegurar que están visibles al final
        if (lineasPreview != null)
        {
            foreach (var linea in lineasPreview)
            {
                if (linea != null)
                {
                    linea.enabled = true;
                }
            }
        }
    }

    private bool PuedeDisparar()
    {
        if (proyectilPrefab == null)
        {
            Debug.LogWarning("[DisparadorProyectiles] No hay prefab de proyectil asignado.");
            return false;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
            if (player == null) return false;
        }

        // Verificar rango
        float distancia = Vector2.Distance(transform.position, player.transform.position);
        if (distancia > rangoDisparo)
            return false;

        // Verificar línea de visión
        if (requiereLineaDeVision && !TieneLineaDeVision())
            return false;

        return true;
    }

    private bool TieneLineaDeVision()
    {
        if (player == null) return false;

        Vector2 direccion = (player.transform.position - puntoDisparo.position).normalized;
        float distancia = Vector2.Distance(puntoDisparo.position, player.transform.position);

        RaycastHit2D hit = Physics2D.Raycast(puntoDisparo.position, direccion, distancia, capasObstaculos);

        // Si el raycast no golpea nada, hay línea de visión
        return hit.collider == null;
    }

    private void Disparar()
    {
        switch (tipoDisparo)
        {
            case TipoDisparo.HaciaJugador:
                DispararHaciaJugador();
                break;

            case TipoDisparo.Rafaga:
                DispararRafaga();
                break;

            case TipoDisparo.Circular:
                DispararCircular();
                break;

            case TipoDisparo.Direccion:
                DispararEnDireccion();
                break;
        }
    }

    private void DispararHaciaJugador()
    {
        if (player == null) return;

        GameObject proyectil = Instantiate(proyectilPrefab, puntoDisparo.position, Quaternion.identity);
        var scriptProyectil = proyectil.GetComponent<Proyectil>();

        if (scriptProyectil != null)
        {
            scriptProyectil.InicializarHaciaObjetivo(player.transform.position);
        }
    }

    private void DispararRafaga()
    {
        if (player == null) return;

        Vector2 direccionBase = (player.transform.position - puntoDisparo.position).normalized;
        float anguloBase = Mathf.Atan2(direccionBase.y, direccionBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < proyectilesPorRafaga; i++)
        {
            float offset = (i - (proyectilesPorRafaga - 1) / 2f) * anguloDispersion;
            float anguloFinal = anguloBase + offset;

            Vector2 direccion = new Vector2(
                Mathf.Cos(anguloFinal * Mathf.Deg2Rad),
                Mathf.Sin(anguloFinal * Mathf.Deg2Rad)
            );

            GameObject proyectil = Instantiate(proyectilPrefab, puntoDisparo.position, Quaternion.identity);
            var scriptProyectil = proyectil.GetComponent<Proyectil>();

            if (scriptProyectil != null)
            {
                scriptProyectil.Inicializar(direccion);
            }
        }
    }

    private void DispararCircular()
    {
        int numeroProyectiles = Mathf.Max(8, proyectilesPorRafaga);
        float anguloIncremento = 360f / numeroProyectiles;

        for (int i = 0; i < numeroProyectiles; i++)
        {
            float angulo = i * anguloIncremento;
            Vector2 direccion = new Vector2(
                Mathf.Cos(angulo * Mathf.Deg2Rad),
                Mathf.Sin(angulo * Mathf.Deg2Rad)
            );

            GameObject proyectil = Instantiate(proyectilPrefab, puntoDisparo.position, Quaternion.identity);
            var scriptProyectil = proyectil.GetComponent<Proyectil>();

            if (scriptProyectil != null)
            {
                scriptProyectil.Inicializar(direccion);
            }
        }
    }

    private void DispararEnDireccion()
    {
        Vector2 direccion = transform.right; // Asume que el enemigo mira hacia la derecha

        GameObject proyectil = Instantiate(proyectilPrefab, puntoDisparo.position, Quaternion.identity);
        var scriptProyectil = proyectil.GetComponent<Proyectil>();

        if (scriptProyectil != null)
        {
            scriptProyectil.Inicializar(direccion);
        }
    }

    #endregion

    #region Sistema de Previsualización

    private void CrearLineasPreview()
    {
        int numeroLineas = CalcularNumeroLineas();
        lineasPreview = new LineRenderer[numeroLineas];

        for (int i = 0; i < numeroLineas; i++)
        {
            GameObject lineaObj = new GameObject($"LineaPreview_{i}");
            lineaObj.transform.SetParent(transform);
            lineaObj.transform.localPosition = Vector3.zero;

            LineRenderer lr = lineaObj.AddComponent<LineRenderer>();
            lr.startWidth = grosorLineaRuntime;
            lr.endWidth = grosorLineaRuntime;
            lr.positionCount = 2;
            lr.startColor = colorPreviewLinea;
            lr.endColor = colorPreviewLinea;
            lr.useWorldSpace = true;

            if (materialLinea != null)
            {
                lr.material = materialLinea;
            }
            else
            {
                // Usar material por defecto
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.material.color = colorPreviewLinea;
            }

            lineasPreview[i] = lr;
        }
    }

    private void MostrarLineasPreview()
    {
        lineasActivas = true;
        ActualizarPosicionLineas();
        
        if (lineasPreview != null)
        {
            foreach (var linea in lineasPreview)
            {
                if (linea != null)
                {
                    linea.enabled = true;
                }
            }
        }
    }

    private void OcultarLineasPreview()
    {
        lineasActivas = false;
        
        if (lineasPreview != null)
        {
            foreach (var linea in lineasPreview)
            {
                if (linea != null)
                {
                    linea.enabled = false;
                }
            }
        }
    }

    private void ActualizarPosicionLineas()
    {
        if (lineasPreview == null || lineasPreview.Length == 0)
            return;

        Vector2[] direcciones = ObtenerDireccionesDisparo();

        // Asegurar que hay suficientes líneas
        if (direcciones.Length != lineasPreview.Length)
        {
            DestruirLineasPreview();
            CrearLineasPreview();
            direcciones = ObtenerDireccionesDisparo();
        }

        for (int i = 0; i < direcciones.Length && i < lineasPreview.Length; i++)
        {
            if (lineasPreview[i] != null)
            {
                Vector3 inicio = puntoDisparo.position;
                Vector3 fin = inicio + (Vector3)direcciones[i] * longitudPreviewLinea;

                lineasPreview[i].SetPosition(0, inicio);
                lineasPreview[i].SetPosition(1, fin);
            }
        }
    }

    private void DestruirLineasPreview()
    {
        if (lineasPreview != null)
        {
            foreach (var linea in lineasPreview)
            {
                if (linea != null)
                {
                    Destroy(linea.gameObject);
                }
            }
            lineasPreview = null;
        }
        lineasActivas = false;
    }

    private int CalcularNumeroLineas()
    {
        switch (tipoDisparo)
        {
            case TipoDisparo.HaciaJugador:
            case TipoDisparo.Direccion:
                return 1;

            case TipoDisparo.Rafaga:
                return proyectilesPorRafaga;

            case TipoDisparo.Circular:
                return Mathf.Max(8, proyectilesPorRafaga);

            default:
                return 1;
        }
    }

    private Vector2[] ObtenerDireccionesDisparo()
    {
        switch (tipoDisparo)
        {
            case TipoDisparo.HaciaJugador:
                return ObtenerDireccionesHaciaJugador();

            case TipoDisparo.Rafaga:
                return ObtenerDireccionesRafaga();

            case TipoDisparo.Circular:
                return ObtenerDireccionesCircular();

            case TipoDisparo.Direccion:
                return ObtenerDireccionFija();

            default:
                return new Vector2[] { Vector2.right };
        }
    }

    private Vector2[] ObtenerDireccionesHaciaJugador()
    {
        if (player == null)
            return new Vector2[] { Vector2.right };

        Vector2 direccion = (player.transform.position - puntoDisparo.position).normalized;
        return new Vector2[] { direccion };
    }

    private Vector2[] ObtenerDireccionesRafaga()
    {
        Vector2[] direcciones = new Vector2[proyectilesPorRafaga];

        if (player == null)
        {
            for (int i = 0; i < proyectilesPorRafaga; i++)
                direcciones[i] = Vector2.right;
            return direcciones;
        }

        Vector2 direccionBase = (player.transform.position - puntoDisparo.position).normalized;
        float anguloBase = Mathf.Atan2(direccionBase.y, direccionBase.x) * Mathf.Rad2Deg;

        for (int i = 0; i < proyectilesPorRafaga; i++)
        {
            float offset = (i - (proyectilesPorRafaga - 1) / 2f) * anguloDispersion;
            float anguloFinal = anguloBase + offset;

            direcciones[i] = new Vector2(
                Mathf.Cos(anguloFinal * Mathf.Deg2Rad),
                Mathf.Sin(anguloFinal * Mathf.Deg2Rad)
            );
        }

        return direcciones;
    }

    private Vector2[] ObtenerDireccionesCircular()
    {
        int numeroProyectiles = Mathf.Max(8, proyectilesPorRafaga);
        Vector2[] direcciones = new Vector2[numeroProyectiles];
        float anguloIncremento = 360f / numeroProyectiles;

        for (int i = 0; i < numeroProyectiles; i++)
        {
            float angulo = i * anguloIncremento;
            direcciones[i] = new Vector2(
                Mathf.Cos(angulo * Mathf.Deg2Rad),
                Mathf.Sin(angulo * Mathf.Deg2Rad)
            );
        }

        return direcciones;
    }

    private Vector2[] ObtenerDireccionFija()
    {
        return new Vector2[] { transform.right };
    }

    #endregion

    #region Validación y Visualización en Editor

    private void OnValidate()
    {
        intervaloDisparo = Mathf.Max(0.1f, intervaloDisparo);
        rangoDisparo = Mathf.Max(0f, rangoDisparo);
        proyectilesPorRafaga = Mathf.Max(1, proyectilesPorRafaga);
        anguloDispersion = Mathf.Clamp(anguloDispersion, 0f, 180f);
        longitudPreviewLinea = Mathf.Max(0.1f, longitudPreviewLinea);
        grosorLineaRuntime = Mathf.Max(0.01f, grosorLineaRuntime);
        tiempoAvisoAntes = Mathf.Clamp(tiempoAvisoAntes, 0f, intervaloDisparo);
        velocidadParpadeo = Mathf.Max(0.1f, velocidadParpadeo);

        // Recrear líneas si están activas y cambió algún parámetro
        if (Application.isPlaying && mostrarAvisoDisparo && lineasPreview != null)
        {
            bool estabanActivas = lineasActivas;
            DestruirLineasPreview();
            CrearLineasPreview();
            if (!estabanActivas)
            {
                OcultarLineasPreview();
            }
        }
    }

    // Visualización en el editor
    private void OnDrawGizmosSelected()
    {
        if (puntoDisparo == null)
            puntoDisparo = transform;

        // Dibujar rango de disparo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDisparo);

        // Dibujar línea hacia el jugador si existe
        if (player != null)
        {
            Gizmos.color = TieneLineaDeVision() ? Color.green : Color.yellow;
            Gizmos.DrawLine(puntoDisparo.position, player.transform.position);
        }

        // Dibujar previsualización de direcciones de disparo
        if (mostrarPrevisualizacionEditor)
        {
            DibujarPrevisualizacionEditor();
        }
    }

    private void DibujarPrevisualizacionEditor()
    {
        Vector2[] direcciones = ObtenerDireccionesDisparo();
        Gizmos.color = colorPreviewLinea;

        foreach (Vector2 direccion in direcciones)
        {
            Vector3 inicio = puntoDisparo.position;
            Vector3 fin = inicio + (Vector3)direccion * longitudPreviewLinea;
            
            Gizmos.DrawLine(inicio, fin);
            
            // Dibujar una pequeña flecha al final
            Vector3 perpendicular = Vector3.Cross(direccion, Vector3.forward).normalized * 0.2f;
            Gizmos.DrawLine(fin, fin - (Vector3)direccion * 0.3f + perpendicular);
            Gizmos.DrawLine(fin, fin - (Vector3)direccion * 0.3f - perpendicular);
        }
    }

    #endregion
}