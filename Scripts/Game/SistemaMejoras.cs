using System;
using System.Collections.Generic;
using UnityEngine;

public class SistemaMejoras : MonoBehaviour
{
    public static SistemaMejoras Instance { get; private set; }

    [Header("Configuración de Mejoras")]
    [SerializeField, Tooltip("Número de opciones de mejora a mostrar.")]
    private int numeroOpciones = 3;

    [SerializeField, Tooltip("Cantidad mínima de mejora por atributo.")]
    private int cantidadMinima = 1;

    [SerializeField, Tooltip("Cantidad máxima de mejora por atributo.")]
    private int cantidadMaxima = 3;

    [Header("Pesos de probabilidad (mayor = más probable)")]
    [SerializeField] private int pesoFuerza = 10;
    [SerializeField] private int pesoVelocidadAtaque = 10;
    [SerializeField] private int pesoEscala = 8;
    [SerializeField] private int pesoVelocidadMovimiento = 10;
    [SerializeField] private int pesoEnfriamiento = 8;
    [SerializeField] private int pesoSalud = 12;

    // Eventos
    public event Action<OpcionMejora[]> OnMejorasGeneradas;
    public event Action<OpcionMejora> OnMejoraSeleccionada;

    [Serializable]
    public class OpcionMejora
    {
        public MejoraAtributos.TipoMejora tipo;
        public int cantidad;
        public string nombre;
        public string descripcion;
        public Sprite icono;

        public OpcionMejora(MejoraAtributos.TipoMejora tipo, int cantidad)
        {
            this.tipo = tipo;
            this.cantidad = cantidad;
            this.nombre = ObtenerNombre(tipo);
            this.descripcion = ObtenerDescripcion(tipo, cantidad);
        }

        private string ObtenerNombre(MejoraAtributos.TipoMejora tipo)
        {
            return tipo switch
            {
                MejoraAtributos.TipoMejora.Fuerza => "Fuerza",
                MejoraAtributos.TipoMejora.VelocidadAtaque => "Velocidad de Ataque",
                MejoraAtributos.TipoMejora.Escala => "Tamaño de Arma",
                MejoraAtributos.TipoMejora.VelocidadMovimiento => "Velocidad",
                MejoraAtributos.TipoMejora.Enfriamiento => "Enfriamiento",
                MejoraAtributos.TipoMejora.Salud => "Vida Máxima",
                _ => "Desconocido"
            };
        }

        private string ObtenerDescripcion(MejoraAtributos.TipoMejora tipo, int cantidad)
        {
            return tipo switch
            {
                MejoraAtributos.TipoMejora.Fuerza => $"+{cantidad} de daño por golpe",
                MejoraAtributos.TipoMejora.VelocidadAtaque => $"+{cantidad} ataques más rápidos",
                MejoraAtributos.TipoMejora.Escala => $"+{cantidad} tamaño del arma",
                MejoraAtributos.TipoMejora.VelocidadMovimiento => $"+{cantidad} velocidad de movimiento",
                MejoraAtributos.TipoMejora.Enfriamiento => $"+{cantidad} reducción de cooldown",
                MejoraAtributos.TipoMejora.Salud => $"+{cantidad * 10} vida máxima",
                _ => "Mejora desconocida"
            };
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Genera un conjunto aleatorio de mejoras para que el jugador elija.
    /// </summary>
    public OpcionMejora[] GenerarOpcionesMejoras()
    {
        OpcionMejora[] opciones = new OpcionMejora[numeroOpciones];
        List<MejoraAtributos.TipoMejora> tiposUsados = new List<MejoraAtributos.TipoMejora>();

        for (int i = 0; i < numeroOpciones; i++)
        {
            MejoraAtributos.TipoMejora tipo = SeleccionarTipoAleatorio(tiposUsados);
            tiposUsados.Add(tipo);

            int cantidad = UnityEngine.Random.Range(cantidadMinima, cantidadMaxima + 1);
            opciones[i] = new OpcionMejora(tipo, cantidad);
        }

        OnMejorasGeneradas?.Invoke(opciones);
        Debug.Log($"[SistemaMejoras] {numeroOpciones} mejoras generadas.");
        return opciones;
    }

    /// <summary>
    /// Selecciona un tipo de mejora aleatorio basado en pesos, evitando repeticiones.
    /// </summary>
    private MejoraAtributos.TipoMejora SeleccionarTipoAleatorio(List<MejoraAtributos.TipoMejora> excluir)
    {
        List<MejoraAtributos.TipoMejora> tiposDisponibles = new List<MejoraAtributos.TipoMejora>
        {
            MejoraAtributos.TipoMejora.Fuerza,
            MejoraAtributos.TipoMejora.VelocidadAtaque,
            MejoraAtributos.TipoMejora.Escala,
            MejoraAtributos.TipoMejora.VelocidadMovimiento,
            MejoraAtributos.TipoMejora.Enfriamiento,
            MejoraAtributos.TipoMejora.Salud
        };

        // Eliminar tipos ya usados
        tiposDisponibles.RemoveAll(t => excluir.Contains(t));

        if (tiposDisponibles.Count == 0)
        {
            return MejoraAtributos.TipoMejora.Fuerza; // Fallback
        }

        // Calcular peso total
        int pesoTotal = 0;
        foreach (var tipo in tiposDisponibles)
        {
            pesoTotal += ObtenerPeso(tipo);
        }

        // Selección aleatoria ponderada
        int valorAleatorio = UnityEngine.Random.Range(0, pesoTotal);
        int acumulado = 0;

        foreach (var tipo in tiposDisponibles)
        {
            acumulado += ObtenerPeso(tipo);
            if (valorAleatorio < acumulado)
            {
                return tipo;
            }
        }

        return tiposDisponibles[0]; // Fallback
    }

    private int ObtenerPeso(MejoraAtributos.TipoMejora tipo)
    {
        return tipo switch
        {
            MejoraAtributos.TipoMejora.Fuerza => pesoFuerza,
            MejoraAtributos.TipoMejora.VelocidadAtaque => pesoVelocidadAtaque,
            MejoraAtributos.TipoMejora.Escala => pesoEscala,
            MejoraAtributos.TipoMejora.VelocidadMovimiento => pesoVelocidadMovimiento,
            MejoraAtributos.TipoMejora.Enfriamiento => pesoEnfriamiento,
            MejoraAtributos.TipoMejora.Salud => pesoSalud,
            _ => 1
        };
    }

    /// <summary>
    /// Aplica la mejora seleccionada al jugador.
    /// </summary>
    public void AplicarMejora(OpcionMejora mejora, GameObject player)
    {
        if (mejora == null || player == null)
        {
            Debug.LogError("[SistemaMejoras] Mejora o jugador nulo.");
            return;
        }

        var atributos = player.GetComponent<Atributos>();
        if (atributos == null)
            atributos = player.GetComponentInChildren<Atributos>();

        if (atributos == null)
        {
            Debug.LogError("[SistemaMejoras] No se encontró componente Atributos en el jugador.");
            return;
        }

        // Aplicar la mejora según el tipo
        switch (mejora.tipo)
        {
            case MejoraAtributos.TipoMejora.Fuerza:
                atributos.ModificarFuerza(mejora.cantidad);
                break;
            case MejoraAtributos.TipoMejora.VelocidadAtaque:
                atributos.ModificarVelocidadAtaque(mejora.cantidad);
                break;
            case MejoraAtributos.TipoMejora.Escala:
                atributos.ModificarEscala(mejora.cantidad);
                break;
            case MejoraAtributos.TipoMejora.VelocidadMovimiento:
                atributos.ModificarVelocidadMovimiento(mejora.cantidad);
                break;
            case MejoraAtributos.TipoMejora.Enfriamiento:
                atributos.ModificarEnfriamiento(mejora.cantidad);
                break;
            case MejoraAtributos.TipoMejora.Salud:
                atributos.ModificarSalud(mejora.cantidad);
                break;
        }

        OnMejoraSeleccionada?.Invoke(mejora);
        Debug.Log($"[SistemaMejoras] Mejora aplicada: {mejora.nombre} +{mejora.cantidad}");
    }

    private void OnValidate()
    {
        numeroOpciones = Mathf.Clamp(numeroOpciones, 1, 6);
        cantidadMinima = Mathf.Max(1, cantidadMinima);
        cantidadMaxima = Mathf.Max(cantidadMinima, cantidadMaxima);

        pesoFuerza = Mathf.Max(1, pesoFuerza);
        pesoVelocidadAtaque = Mathf.Max(1, pesoVelocidadAtaque);
        pesoEscala = Mathf.Max(1, pesoEscala);
        pesoVelocidadMovimiento = Mathf.Max(1, pesoVelocidadMovimiento);
        pesoEnfriamiento = Mathf.Max(1, pesoEnfriamiento);
        pesoSalud = Mathf.Max(1, pesoSalud);
    }
}