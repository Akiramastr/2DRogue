using UnityEngine;

// Si ya existe otra definición de MejoraAtributos en el proyecto,
// cambie el nombre de esta clase para evitar el conflicto.
public class MejoraAtributosPickup : MonoBehaviour
{
    // Añade esta declaración de evento en la clase MejoraAtributosPickup
    public static event System.Action OnMejoraRecogida;

    // Ejemplo de método donde se invoca el evento
    public void AplicarEfectos()
    {
        if (OnMejoraRecogida != null)
            OnMejoraRecogida.Invoke();
    }

    // Método público estático para invocar el evento desde otras clases
    public static void InvocarEventoMejoraRecogida()
    {
        OnMejoraRecogida?.Invoke();
    }
}