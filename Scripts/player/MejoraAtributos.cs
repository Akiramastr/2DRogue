using UnityEngine;

public class MejoraAtributos : MonoBehaviour
{
    // Enum para definir qué tipo de mejora es cada slot
    public enum TipoMejora
    {
        Ninguna,
        Fuerza,
        VelocidadAtaque,
        Escala,
        VelocidadMovimiento,
        Enfriamiento,
        Salud,
        Aleatorio
    }

    [System.Serializable]
    public class ConfiguracionMejora
    {
        [Tooltip("Tipo de atributo que mejorará este slot.")]
        public TipoMejora tipo = TipoMejora.Ninguna;

        [Tooltip("Cantidad a incrementar el atributo seleccionado.")]
        public int cantidad = 1;
    }

    [Header("Mejoras (hasta 6 atributos diferentes)")]
    [SerializeField, Tooltip("Primera mejora a aplicar.")]
    private ConfiguracionMejora mejora1 = new ConfiguracionMejora();

    [SerializeField, Tooltip("Segunda mejora a aplicar (opcional).")]
    private ConfiguracionMejora mejora2 = new ConfiguracionMejora();

    [SerializeField, Tooltip("Tercera mejora a aplicar (opcional).")]
    private ConfiguracionMejora mejora3 = new ConfiguracionMejora();

    [SerializeField, Tooltip("Cuarta mejora a aplicar (opcional).")]
    private ConfiguracionMejora mejora4 = new ConfiguracionMejora();

    [SerializeField, Tooltip("Quinta mejora a aplicar (opcional).")]
    private ConfiguracionMejora mejora5 = new ConfiguracionMejora();

    [SerializeField, Tooltip("Sexta mejora a aplicar (opcional).")]
    private ConfiguracionMejora mejora6 = new ConfiguracionMejora();

    [Header("Configuración de colisión")]
    [SerializeField, Tooltip("Tag del jugador para detectar colisiones.")]
    private string playerTag = "Player";

    [Header("Efectos visuales/sonoros (opcional)")]
    [SerializeField, Tooltip("Prefab de partículas que se instancia al recoger la mejora.")]
    private GameObject pickupVfxPrefab;

    [SerializeField, Tooltip("Clip de sonido al recoger la mejora.")]
    private AudioClip pickupSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag(playerTag))
        {
            OnPlayerCollision(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag(playerTag))
        {
            OnPlayerCollision(collision.gameObject);
        }
    }

    private void OnPlayerCollision(GameObject player)
    {
        // Aplicar efectos antes de destruir
        AplicarEfectos(player);

        // Reproducir efectos visuales/sonoros
        PlayPickupEffects();

        // Destruir el objeto
        Destroy(gameObject);
    }

    private void AplicarEfectos(GameObject player)
    {
        var atributos = player.GetComponent<Atributos>();
        if (atributos == null)
            atributos = player.GetComponentInParent<Atributos>();

        if (atributos == null)
        {
            Debug.LogWarning("[MejoraAtributos] No se encontró el componente Atributos en el jugador.");
            return;
        }

        // Aplicar cada mejora configurada
        if (!AplicarMejora(atributos, mejora1)) return;
        if (!AplicarMejora(atributos, mejora2)) return;
        if (!AplicarMejora(atributos, mejora3)) return;
        if (!AplicarMejora(atributos, mejora4)) return;
        if (!AplicarMejora(atributos, mejora5)) return;
        if (!AplicarMejora(atributos, mejora6)) return;

        // NUEVO: Notificar directamente al Rondacion
        NotificarMejoraRecogida();
        Debug.Log("[MejoraAtributos] Mejora recogida notificada.");
    }

    private void NotificarMejoraRecogida()
    {
        // Buscar el objeto Rondacion en la escena
        var rondacion = Object.FindFirstObjectByType<Rondacion>();
        if (rondacion != null)
        {
            // Usar SendMessage como alternativa si hay problemas de compilación
            rondacion.SendMessage("OnMejoraRecogida", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[MejoraAtributos] ✓ Rondacion notificado directamente.");
        }
        else
        {
            Debug.LogError("[MejoraAtributos] No se encontró objeto Rondacion en la escena.");
        }
    }

    private bool AplicarMejora(Atributos atributos, ConfiguracionMejora mejora)
    {
        // Detener búsqueda si no hay mejora configurada
        if (mejora.tipo == TipoMejora.Ninguna)
            return false;

        // Ignorar si cantidad es 0 pero continuar con las siguientes
        if (mejora.cantidad == 0)
            return true;

        // Determinar qué tipo de mejora aplicar
        TipoMejora tipoAAplicar = mejora.tipo;

        // Si es aleatorio, elegir uno al azar
        if (mejora.tipo == TipoMejora.Aleatorio)
        {
            tipoAAplicar = (TipoMejora)Random.Range(1, 7); // 1-6 (excluye Ninguna y Aleatorio)
        }

        // Aplicar la mejora correspondiente
        switch (tipoAAplicar)
        {
            case TipoMejora.Fuerza:
                atributos.ModificarFuerza(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Fuerza modificada en {mejora.cantidad}. Nuevo valor: {atributos.FuerzaActual}");
                break;

            case TipoMejora.VelocidadAtaque:
                atributos.ModificarVelocidadAtaque(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Velocidad de ataque modificada en {mejora.cantidad}. Nuevo valor: {atributos.VelocidadAtaqueActual}");
                break;

            case TipoMejora.Escala:
                atributos.ModificarEscala(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Escala modificada en {mejora.cantidad}. Nuevo valor: {atributos.EscalaActual}");
                break;

            case TipoMejora.VelocidadMovimiento:
                atributos.ModificarVelocidadMovimiento(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Velocidad de movimiento modificada en {mejora.cantidad}. Nuevo valor: {atributos.VelocidadMovimientoActual}");
                break;

            case TipoMejora.Enfriamiento:
                atributos.ModificarEnfriamiento(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Enfriamiento modificado en {mejora.cantidad}. Nuevo valor: {atributos.EnfriamientoActual}");
                break;

            case TipoMejora.Salud:
                atributos.ModificarSalud(mejora.cantidad);
                Debug.Log($"[MejoraAtributos] Salud modificada en {mejora.cantidad}. Nuevo valor: {atributos.SaludActual}");
                break;
        }

        // Continuar con las siguientes mejoras
        return true;
    }

    private void PlayPickupEffects()
    {
        if (pickupVfxPrefab != null)
        {
            var vfx = Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);
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

        if (pickupSound != null)
        {
            // Reproducir sonido en un AudioSource temporal para que se escuche completo
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
    }

    private void OnValidate()
    {
        // Validar que todas las cantidades sean >= 1 si el tipo no es Ninguna
        ValidarMejora(mejora1);
        ValidarMejora(mejora2);
        ValidarMejora(mejora3);
        ValidarMejora(mejora4);
        ValidarMejora(mejora5);
        ValidarMejora(mejora6);
    }

    private void ValidarMejora(ConfiguracionMejora mejora)
    {
        if (mejora.tipo != TipoMejora.Ninguna)
        {
            mejora.cantidad = Mathf.Max(1, mejora.cantidad);
        }
    }
}
