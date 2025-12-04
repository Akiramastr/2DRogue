using UnityEngine;

public class Rondacion : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Tag del jugador para detectar colisiones.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Referencias")]
    [SerializeField] private EnemyController enemyController;

    [Header("UI (opcional)")]
    [SerializeField] private GameObject indicadorInteraccion;

    [Header("Comportamiento de oleadas")]
    [SerializeField, Tooltip("Si está activado, la primera oleada se inicia por contacto.")]
    private bool primeraOleadaPorContacto = true;

    private bool primeraOleadaIniciada = false;

    private void Start()
    {
        VerificarConfiguracion();
    }

    // MÉTODO PÚBLICO: Llamado directamente por MejoraAtributos
    public void OnMejoraRecogida()
    {
        Debug.Log("[Rondacion] ✓✓✓ Mejora recogida detectada. Iniciando siguiente oleada...");
        IniciarOleada();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Rondacion] *** OnTriggerEnter2D *** Objeto: {other.gameObject.name}, Tag: '{other.tag}'");
        
        if (other.CompareTag(playerTag))
        {
            if (primeraOleadaPorContacto && !primeraOleadaIniciada)
            {
                Debug.Log("[Rondacion] ✓✓✓ ¡JUGADOR DETECTADO! Iniciando primera oleada automáticamente.");
                primeraOleadaIniciada = true;
                IniciarOleada();
            }
        }
    }

    private void IniciarOleada()
    {
        Debug.Log("[Rondacion] >>> IniciarOleada ejecutado.");
        
        if (enemyController == null)
        {
            Debug.LogError("[Rondacion] ✗ No se ha asignado EnemyController en el Inspector.");
            return;
        }

        if (enemyController.EstaOleadaActiva())
        {
            Debug.LogWarning("[Rondacion] Ya hay una oleada activa. Espera a que finalice.");
            return;
        }

        Debug.Log("[Rondacion] ✓✓✓ Iniciando nueva oleada...");
        enemyController.IniciarOleada();

        if (indicadorInteraccion != null)
            indicadorInteraccion.SetActive(false);
    }

    private void VerificarConfiguracion()
    {
        Debug.Log($"[Rondacion] Script inicializado en: {gameObject.name}");
        
        if (enemyController == null)
            Debug.LogError("[Rondacion] ⚠ EnemyController no está asignado en el Inspector.");
        else
            Debug.Log($"[Rondacion] ✓ EnemyController asignado: {enemyController.gameObject.name}");
    }
}
