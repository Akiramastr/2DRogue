using System;
using UnityEngine;

public class Atributos : MonoBehaviour
{
    [Header("Fuerza")]
    [SerializeField, Tooltip("Valor inicial de fuerza al comenzar la partida.")]
    private int fuerzaInicial = 0;

    [SerializeField, Tooltip("Valor máximo permitido. 0 = sin tope.")]
    private int fuerzaMaxima = 0;

    // Valor actual en tiempo de ejecución
    private int fuerzaActual;

    // Evento para notificar cambios de fuerza
    public event Action<int> OnFuerzaCambiada;

    [Header("Velocidad de ataque")]
    [SerializeField, Tooltip("Valor inicial de velocidad de ataque al comenzar la partida.")]
    private int velocidadAtaqueInicial = 0;

    [SerializeField, Tooltip("Valor máximo de velocidad de ataque. 0 = sin tope.")]
    private int velocidadAtaqueMaxima = 0;

    // Valor actual de la velocidad de ataque en tiempo de ejecución
    private int velocidadAtaqueActual;

    // Evento para notificar cambios de velocidad de ataque
    public event Action<int> OnVelocidadAtaqueCambiada;

    [Header("Escala")]
    [SerializeField, Tooltip("Valor inicial de escala del arma al comenzar la partida.")]
    private int escalaInicial = 0;

    [SerializeField, Tooltip("Valor máximo de escala. 0 = sin tope.")]
    private int escalaMaxima = 0;

    // Valor actual de la escala en tiempo de ejecución
    private int escalaActual;

    // Evento para notificar cambios de escala
    public event Action<int> OnEscalaCambiada;

    [Header("Velocidad de movimiento")]
    [SerializeField, Tooltip("Valor inicial de velocidad de movimiento al comenzar la partida.")]
    private int velocidadMovimientoInicial = 0;

    [SerializeField, Tooltip("Valor máximo de velocidad de movimiento. 0 = sin tope.")]
    private int velocidadMovimientoMaxima = 0;

    // Valor actual de la velocidad de movimiento en tiempo de ejecución
    private int velocidadMovimientoActual;

    // Evento para notificar cambios de velocidad de movimiento
    public event Action<int> OnVelocidadMovimientoCambiada;

    [Header("Enfriamiento")]
    [SerializeField, Tooltip("Valor inicial de enfriamiento al comenzar la partida.")]
    private int enfriamientoInicial = 0;

    [SerializeField, Tooltip("Valor máximo de enfriamiento. 0 = sin tope.")]
    private int enfriamientoMaxima = 0;

    // Valor actual de enfriamiento en tiempo de ejecución
    private int enfriamientoActual;

    // Evento para notificar cambios de enfriamiento
    public event Action<int> OnEnfriamientoCambiada;

    [Header("Salud")]
    [SerializeField, Tooltip("Valor inicial de salud (vida máxima) al comenzar la partida.")]
    private int saludInicial = 0;

    [SerializeField, Tooltip("Valor máximo de salud. 0 = sin tope.")]
    private int saludMaxima = 0;

    // Valor actual de salud en tiempo de ejecución
    private int saludActual;

    // Evento para notificar cambios de salud
    public event Action<int> OnSaludCambiada;

    void Awake()
    {
        InicializarAtributos();
    }

    void Start()
    {
        // Asegurar que los atributos estén inicializados
        if (!Application.isPlaying) return;
        InicializarAtributos();
    }

    private void InicializarAtributos()
    {
        // Inicializar fuerza
        fuerzaActual = Mathf.Max(0, fuerzaInicial);
        if (fuerzaMaxima > 0)
            fuerzaActual = Mathf.Clamp(fuerzaActual, 0, fuerzaMaxima);
        OnFuerzaCambiada?.Invoke(fuerzaActual);

        // Inicializar velocidad de ataque
        velocidadAtaqueActual = Mathf.Max(0, velocidadAtaqueInicial);
        if (velocidadAtaqueMaxima > 0)
            velocidadAtaqueActual = Mathf.Clamp(velocidadAtaqueActual, 0, velocidadAtaqueMaxima);
        OnVelocidadAtaqueCambiada?.Invoke(velocidadAtaqueActual);

        // Inicializar escala
        escalaActual = Mathf.Max(0, escalaInicial);
        if (escalaMaxima > 0)
            escalaActual = Mathf.Clamp(escalaActual, 0, escalaMaxima);
        OnEscalaCambiada?.Invoke(escalaActual);

        // Inicializar velocidad de movimiento
        velocidadMovimientoActual = Mathf.Max(0, velocidadMovimientoInicial);
        if (velocidadMovimientoMaxima > 0)
            velocidadMovimientoActual = Mathf.Clamp(velocidadMovimientoActual, 0, velocidadMovimientoMaxima);
        OnVelocidadMovimientoCambiada?.Invoke(velocidadMovimientoActual);

        // Inicializar enfriamiento
        enfriamientoActual = Mathf.Max(0, enfriamientoInicial);
        if (enfriamientoMaxima > 0)
            enfriamientoActual = Mathf.Clamp(enfriamientoActual, 0, enfriamientoMaxima);
        OnEnfriamientoCambiada?.Invoke(enfriamientoActual);

        // Inicializar salud
        saludActual = Mathf.Max(1, saludInicial);
        if (saludMaxima > 0)
            saludActual = Mathf.Clamp(saludActual, 1, saludMaxima);
        OnSaludCambiada?.Invoke(saludActual);
    }

    // Propiedades públicas de lectura - CORREGIDO: Ahora retorna el valor inicial en modo edición
    public int FuerzaActual => Application.isPlaying ? fuerzaActual : Mathf.Max(0, fuerzaInicial);
    public int FuerzaMaxima => fuerzaMaxima;

    public int VelocidadAtaqueActual => Application.isPlaying ? velocidadAtaqueActual : Mathf.Max(0, velocidadAtaqueInicial);
    public int VelocidadAtaqueMaxima => velocidadAtaqueMaxima;

    public int EscalaActual => Application.isPlaying ? escalaActual : Mathf.Max(0, escalaInicial);
    public int EscalaMaxima => escalaMaxima;

    public int VelocidadMovimientoActual => Application.isPlaying ? velocidadMovimientoActual : Mathf.Max(0, velocidadMovimientoInicial);
    public int VelocidadMovimientoMaxima => velocidadMovimientoMaxima;

    public int EnfriamientoActual => Application.isPlaying ? enfriamientoActual : Mathf.Max(0, enfriamientoInicial);
    public int EnfriamientoMaxima => enfriamientoMaxima;

    public int SaludActual => Application.isPlaying ? saludActual : Mathf.Max(1, saludInicial);
    public int SaludMaxima => saludMaxima;

    // Modifica la fuerza sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarFuerza(int delta)
    {
        if (delta == 0) return;

        if (fuerzaMaxima > 0)
            fuerzaActual = Mathf.Clamp(fuerzaActual + delta, 0, fuerzaMaxima);
        else
            fuerzaActual = Mathf.Max(0, fuerzaActual + delta);

        OnFuerzaCambiada?.Invoke(fuerzaActual);
    }

    // Asigna un valor directo a la fuerza (se aplica clamp según tope)
    public void SetFuerza(int valor)
    {
        if (fuerzaMaxima > 0)
            fuerzaActual = Mathf.Clamp(valor, 0, fuerzaMaxima);
        else
            fuerzaActual = Mathf.Max(0, valor);

        OnFuerzaCambiada?.Invoke(fuerzaActual);
    }

    // Modifica la velocidad de ataque sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarVelocidadAtaque(int delta)
    {
        if (delta == 0) return;

        if (velocidadAtaqueMaxima > 0)
            velocidadAtaqueActual = Mathf.Clamp(velocidadAtaqueActual + delta, 0, velocidadAtaqueMaxima);
        else
            velocidadAtaqueActual = Mathf.Max(0, velocidadAtaqueActual + delta);

        OnVelocidadAtaqueCambiada?.Invoke(velocidadAtaqueActual);
    }

    // Asigna un valor directo a la velocidad de ataque (se aplica clamp según tope)
    public void SetVelocidadAtaque(int valor)
    {
        if (velocidadAtaqueMaxima > 0)
            velocidadAtaqueActual = Mathf.Clamp(valor, 0, velocidadAtaqueMaxima);
        else
            velocidadAtaqueActual = Mathf.Max(0, valor);

        OnVelocidadAtaqueCambiada?.Invoke(velocidadAtaqueActual);
    }

    // Modifica la escala sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarEscala(int delta)
    {
        if (delta == 0) return;

        if (escalaMaxima > 0)
            escalaActual = Mathf.Clamp(escalaActual + delta, 0, escalaMaxima);
        else
            escalaActual = Mathf.Max(0, escalaActual + delta);

        OnEscalaCambiada?.Invoke(escalaActual);
    }

    // Asigna un valor directo a la escala (se aplica clamp según tope)
    public void SetEscala(int valor)
    {
        if (escalaMaxima > 0)
            escalaActual = Mathf.Clamp(valor, 0, escalaMaxima);
        else
            escalaActual = Mathf.Max(0, valor);

        OnEscalaCambiada?.Invoke(escalaActual);
    }

    // Modifica la velocidad de movimiento sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarVelocidadMovimiento(int delta)
    {
        if (delta == 0) return;

        if (velocidadMovimientoMaxima > 0)
            velocidadMovimientoActual = Mathf.Clamp(velocidadMovimientoActual + delta, 0, velocidadMovimientoMaxima);
        else
            velocidadMovimientoActual = Mathf.Max(0, velocidadMovimientoActual + delta);

        OnVelocidadMovimientoCambiada?.Invoke(velocidadMovimientoActual);
    }

    // Asigna un valor directo a la velocidad de movimiento (se aplica clamp según tope)
    public void SetVelocidadMovimiento(int valor)
    {
        if (velocidadMovimientoMaxima > 0)
            velocidadMovimientoActual = Mathf.Clamp(valor, 0, velocidadMovimientoMaxima);
        else
            velocidadMovimientoActual = Mathf.Max(0, valor);

        OnVelocidadMovimientoCambiada?.Invoke(velocidadMovimientoActual);
    }

    // Modifica el enfriamiento sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarEnfriamiento(int delta)
    {
        if (delta == 0) return;

        if (enfriamientoMaxima > 0)
            enfriamientoActual = Mathf.Clamp(enfriamientoActual + delta, 0, enfriamientoMaxima);
        else
            enfriamientoActual = Mathf.Max(0, enfriamientoActual + delta);

        OnEnfriamientoCambiada?.Invoke(enfriamientoActual);
    }

    // Asigna un valor directo al enfriamiento (se aplica clamp según tope)
    public void SetEnfriamiento(int valor)
    {
        if (enfriamientoMaxima > 0)
            enfriamientoActual = Mathf.Clamp(valor, 0, enfriamientoMaxima);
        else
            enfriamientoActual = Mathf.Max(0, valor);

        OnEnfriamientoCambiada?.Invoke(enfriamientoActual);
    }

    // Modifica la salud sumando el delta (positivo aumenta, negativo reduce)
    public void ModificarSalud(int delta)
    {
        if (delta == 0) return;

        if (saludMaxima > 0)
            saludActual = Mathf.Clamp(saludActual + delta, 1, saludMaxima);
        else
            saludActual = Mathf.Max(1, saludActual + delta);

        OnSaludCambiada?.Invoke(saludActual);
    }

    // Asigna un valor directo a la salud (se aplica clamp según tope)
    public void SetSalud(int valor)
    {
        if (saludMaxima > 0)
            saludActual = Mathf.Clamp(valor, 1, saludMaxima);
        else
            saludActual = Mathf.Max(1, valor);

        OnSaludCambiada?.Invoke(saludActual);
    }

    private void OnValidate()
    {
        fuerzaInicial = Mathf.Max(0, fuerzaInicial);
        fuerzaMaxima = Mathf.Max(0, fuerzaMaxima);

        velocidadAtaqueInicial = Mathf.Max(0, velocidadAtaqueInicial);
        velocidadAtaqueMaxima = Mathf.Max(0, velocidadAtaqueMaxima);

        escalaInicial = Mathf.Max(0, escalaInicial);
        escalaMaxima = Mathf.Max(0, escalaMaxima);

        velocidadMovimientoInicial = Mathf.Max(0, velocidadMovimientoInicial);
        velocidadMovimientoMaxima = Mathf.Max(0, velocidadMovimientoMaxima);

        enfriamientoInicial = Mathf.Max(0, enfriamientoInicial);
        enfriamientoMaxima = Mathf.Max(0, enfriamientoMaxima);

        saludInicial = Mathf.Max(1, saludInicial);
        saludMaxima = Mathf.Max(0, saludMaxima);

        // Si estamos jugando, sincronizar los valores actuales con los cambios del inspector
        if (Application.isPlaying)
        {
            InicializarAtributos();
        }
    }
}
