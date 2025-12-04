using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using TMPro;

public class EnemyController : MonoBehaviour
{
    [Header("Prefabs de enemigos (se selecciona uno aleatoriamente)")]
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Respawn (por tag)")]
    [SerializeField] private string respawnTag = "Respawn";

    [Header("Control de generación")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxSimultaneousEnemies = 10;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Referencia al Player")]
    [Tooltip("Asigna manualmente el GameObject Player aquí.")]
    [SerializeField] private GameObject playerOverride = null;

    [Header("Sistema de Oleadas")]
    [SerializeField] private bool usarSistemaOleadas = true;
    [SerializeField] private int enemigosInicialPorOleada = 10;
    [SerializeField] private int incrementoEnemigoPorOleada = 5;
    [SerializeField] private float tiempoCuentaAtras = 5f;
    [SerializeField] private float tiempoMensajeFinal = 3f;
    [SerializeField] private bool inicioAutomatico = true;

    [Header("Oleadas Especiales")]
    [SerializeField] private bool activarOleadasEspeciales = true;
    [SerializeField] private int frecuenciaOleadaEspecial = 5; // Cada 5 rondas
    [SerializeField] private List<GameObject> enemigosEspecialesPrefabs;
    [SerializeField] private int multiplicadorEnemigosEspeciales = 2; // El doble de enemigos
    [SerializeField] private float multiplicadorRecompensa = 1.5f; // 50% más mejoras

    [Header("Prefab de Mejora")]
    [SerializeField] private GameObject mejoraAtributosPrefab;
    [SerializeField] private Transform puntoSpawnMejora;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoCuentaAtras;
    [SerializeField] private GameObject panelMensajeRonda;
    [SerializeField] private TextMeshProUGUI textoMensajeRonda;

    // Estado runtime
    private List<Transform> respawnPoints = new();
    private readonly List<GameObject> activeEnemies = new();
    private Coroutine spawnCoroutine;
    private Coroutine oleadaCoroutine;
    private GameObject player;
    
    // Estado oleadas
    private int rondaActual = 0;
    private bool oleadaActiva = false;
    private int enemigosMaximosPorOleada = 0;
    private int enemigosGeneradosEnOleada = 0;
    private bool esOleadaEspecial = false;

    private void Start()
    {
        CollectRespawnPoints();
        player = playerOverride;
        OcultarUI();

        if (spawnOnStart && !usarSistemaOleadas)
            StartSpawning();
    }

    private void CollectRespawnPoints()
    {
        respawnPoints.Clear();
        var gos = GameObject.FindGameObjectsWithTag(respawnTag);
        if (gos != null && gos.Length > 0)
        {
            foreach (var g in gos) respawnPoints.Add(g.transform);
        }

        if (respawnPoints.Count == 0)
            respawnPoints.Add(this.transform);
    }

    #region Sistema de Oleadas

    /// <summary>
    /// Inicia una nueva oleada (llamado desde Rondacion o automáticamente)
    /// </summary>
    public void IniciarOleada()
    {
        if (oleadaActiva)
        {
            Debug.LogWarning("[EnemyController] Ya hay una oleada activa.");
            return;
        }

        if (oleadaCoroutine != null)
            StopCoroutine(oleadaCoroutine);

        oleadaCoroutine = StartCoroutine(ProcesoOleada());
    }

    private IEnumerator ProcesoOleada()
    {
        oleadaActiva = true;
        rondaActual++;

        // Determinar si es oleada especial
        esOleadaEspecial = activarOleadasEspeciales && (rondaActual % frecuenciaOleadaEspecial == 0);

        // 1. Mostrar cuenta atrás
        yield return MostrarCuentaAtras();

        // 2. Configurar oleada
        int enemigosBase = enemigosInicialPorOleada + (rondaActual - 1) * incrementoEnemigoPorOleada;
        
        if (esOleadaEspecial)
        {
            enemigosMaximosPorOleada = enemigosBase * multiplicadorEnemigosEspeciales;
            Debug.Log($"[EnemyController] ⭐ RONDA ESPECIAL {rondaActual} ⭐ Enemigos a generar: {enemigosMaximosPorOleada}");
        }
        else
        {
            enemigosMaximosPorOleada = enemigosBase;
            Debug.Log($"[EnemyController] Ronda {rondaActual} iniciada. Enemigos a generar: {enemigosMaximosPorOleada}");
        }
        
        enemigosGeneradosEnOleada = 0;

        // 3. Iniciar generación de enemigos
        StartSpawning();

        // 4. Esperar a que todos los enemigos sean eliminados
        yield return EsperarFinalizacionEnemigos();

        // 5. Detener spawning
        StopSpawning();

        // 6. Mostrar mensaje de fin de ronda
        yield return MostrarMensajeFinRonda();

        // 7. Mostrar selección de mejoras EN LUGAR de instanciar mejora física
        yield return MostrarSeleccionMejoras();

        oleadaActiva = false;
        oleadaCoroutine = null;

        Debug.Log($"[EnemyController] Ronda {rondaActual} completada.");

        // 8. Iniciar automáticamente la siguiente oleada si está habilitado
        if (inicioAutomatico && usarSistemaOleadas)
        {
            Debug.Log("[EnemyController] Iniciando siguiente oleada automáticamente...");
            IniciarOleada();
        }
    }

    private IEnumerator MostrarSeleccionMejoras()
    {
        if (SistemaMejoras.Instance == null || UISeleccionMejoras.Instance == null)
        {
            Debug.LogError("[EnemyController] SistemaMejoras o UISeleccionMejoras no encontrado.");
            yield break;
        }

        // Generar opciones de mejora
        var opciones = SistemaMejoras.Instance.GenerarOpcionesMejoras();

        // Mostrar UI de selección
        UISeleccionMejoras.Instance.MostrarOpciones(opciones);

        // Esperar hasta que el jugador seleccione una mejora
        while (UISeleccionMejoras.Instance.EstaActivo())
        {
            yield return null;
        }

        Debug.Log("[EnemyController] Mejora seleccionada. Continuando...");
    }

    private IEnumerator MostrarCuentaAtras()
    {
        if (textoCuentaAtras != null)
            textoCuentaAtras.gameObject.SetActive(true);

        float tiempoRestante = tiempoCuentaAtras;
        while (tiempoRestante > 0)
        {
            if (textoCuentaAtras != null)
            {
                string tipoRonda = esOleadaEspecial ? "⭐ RONDA ESPECIAL ⭐" : $"RONDA {rondaActual}";
                textoCuentaAtras.text = $"{tipoRonda}\n{Mathf.CeilToInt(tiempoRestante)}";
            }
            
            yield return new WaitForSeconds(1f);
            tiempoRestante--;
        }

        if (textoCuentaAtras != null)
        {
            textoCuentaAtras.text = "¡COMIENZA!";
            yield return new WaitForSeconds(1f);
            textoCuentaAtras.gameObject.SetActive(false);
        }
    }

    private IEnumerator EsperarFinalizacionEnemigos()
    {
        // Esperar hasta que:
        // 1. Se hayan generado todos los enemigos de la oleada
        // 2. Todos los enemigos activos hayan sido eliminados
        
        while (enemigosGeneradosEnOleada < enemigosMaximosPorOleada)
        {
            yield return new WaitForSeconds(0.5f);
        }

        while (CantidadEnemigosActivos() > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"[EnemyController] Todos los {enemigosMaximosPorOleada} enemigos de la ronda {rondaActual} han sido eliminados.");
    }

    private IEnumerator MostrarMensajeFinRonda()
    {
        if (panelMensajeRonda != null && textoMensajeRonda != null)
        {
            string mensaje = esOleadaEspecial 
                ? $"⭐ RONDA ESPECIAL {rondaActual}\n¡COMPLETADA! ⭐" 
                : $"¡RONDA {rondaActual}\nCOMPLETADA!";
            
            textoMensajeRonda.text = mensaje;
            panelMensajeRonda.SetActive(true);
            yield return new WaitForSeconds(tiempoMensajeFinal);
            panelMensajeRonda.SetActive(false);
        }
    }

    private void InstanciarMejora()
    {
        if (mejoraAtributosPrefab == null)
        {
            Debug.LogWarning("[EnemyController] No se ha asignado el prefab de mejora.");
            return;
        }

        Vector3 posicionSpawn = puntoSpawnMejora != null ? puntoSpawnMejora.position : transform.position;
        
        // Si es oleada especial, instanciar mejoras adicionales
        if (esOleadaEspecial)
        {
            int cantidadMejoras = Mathf.CeilToInt(multiplicadorRecompensa);
            for (int i = 0; i < cantidadMejoras; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                Instantiate(mejoraAtributosPrefab, posicionSpawn + offset, Quaternion.identity);
            }
            Debug.Log($"[EnemyController] ⭐ {cantidadMejoras} mejoras especiales instanciadas!");
        }
        else
        {
            Instantiate(mejoraAtributosPrefab, posicionSpawn, Quaternion.identity);
            Debug.Log($"[EnemyController] Mejora instanciada en {posicionSpawn}");
        }
    }

    private void OcultarUI()
    {
        if (textoCuentaAtras != null)
            textoCuentaAtras.gameObject.SetActive(false);
        if (panelMensajeRonda != null)
            panelMensajeRonda.SetActive(false);
    }

    public bool EstaOleadaActiva()
    {
        return oleadaActiva;
    }

    public int GetRondaActual()
    {
        return rondaActual;
    }

    public int CantidadEnemigosActivos()
    {
        CleanActiveEnemies();
        return activeEnemies.Count;
    }

    public bool EsOleadaEspecial()
    {
        return esOleadaEspecial;
    }

    #endregion

    #region Sistema de Spawn Original

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanActiveEnemies();

            if (usarSistemaOleadas)
            {
                // Modo oleadas: respetar límite de enemigos por oleada
                if (enemyPrefabs.Count > 0 && 
                    activeEnemies.Count < maxSimultaneousEnemies &&
                    enemigosGeneradosEnOleada < enemigosMaximosPorOleada)
                {
                    SpawnEnemy();
                    enemigosGeneradosEnOleada++;
                }
            }
            else
            {
                // Modo continuo original
                if (enemyPrefabs.Count > 0 && activeEnemies.Count < maxSimultaneousEnemies)
                {
                    SpawnEnemy();
                }
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // Seleccionar prefab según si es oleada especial o no
        List<GameObject> prefabsAUsar = (esOleadaEspecial && enemigosEspecialesPrefabs.Count > 0) 
            ? enemigosEspecialesPrefabs 
            : enemyPrefabs;

        var prefab = prefabsAUsar[Random.Range(0, prefabsAUsar.Count)];
        if (prefab == null) return;

        var rp = respawnPoints[Random.Range(0, respawnPoints.Count)];
        var go = Instantiate(prefab, rp.position, rp.rotation);
        activeEnemies.Add(go);

        AssignPlayerToMovementComponents(go);
    }

    private void AssignPlayerToMovementComponents(GameObject enemy)
    {
        if (player == null || enemy == null) return;

        Debug.Log($"[EnemyController] Intentando asignar player '{player.name}' a enemigo '{enemy.name}'");

        var components = enemy.GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            var type = comp.GetType();
            
            // Buscar propiedades
            var prop = type.GetProperty("Player", BindingFlags.Public | BindingFlags.Instance)
                       ?? type.GetProperty("player", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite && prop.PropertyType.IsAssignableFrom(player.GetType()))
            {
                prop.SetValue(comp, player);
                Debug.Log($"[EnemyController] ✓ Player asignado a propiedad '{prop.Name}' de {type.Name}");
            }
            
            // Buscar campos
            var field = type.GetField("Player", BindingFlags.Public | BindingFlags.Instance)
                        ?? type.GetField("player", BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType.IsAssignableFrom(player.GetType()))
            {
                field.SetValue(comp, player);
                Debug.Log($"[EnemyController] ✓ Player asignado a campo '{field.Name}' de {type.Name}");
            }
        }
    }

    private void CleanActiveEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
    }

    #endregion

    // Callback para cuando se aplica la mejora
    public void OnMejoraAplicada()
    {
        Debug.Log("[EnemyController] Mejora aplicada. Listo para siguiente oleada.");
    }
}
