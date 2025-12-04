using UnityEngine;
using UnityEngine.AI;

public class Playerfollow : MonoBehaviour
{
    // CAMBIADO: Ahora usa GameObject en lugar de Transform y el nombre correcto
    public GameObject Player;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Configuración para NavMesh 2D
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
        else
        {
            Debug.LogError($"[Playerfollow] No hay NavMeshAgent en {gameObject.name}");
        }

        // Debug: verificar si el player fue asignado
        if (Player == null)
        {
            Debug.LogWarning($"[Playerfollow] Player no asignado en {gameObject.name}");
        }
        else
        {
            Debug.Log($"[Playerfollow] Player asignado correctamente: {Player.name}");
        }
    }

    void Update()
    {
        // Verificación mejorada
        if (Player == null)
        {
            Debug.LogWarning($"[Playerfollow] Player es null en {gameObject.name}");
            return;
        }

        if (agent == null)
        {
            Debug.LogError($"[Playerfollow] NavMeshAgent es null en {gameObject.name}");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"[Playerfollow] {gameObject.name} no está sobre el NavMesh");
            return;
        }

        // Establecer el destino hacia el jugador
        agent.SetDestination(Player.transform.position);
    }

    // Corrección: asegurar que la componente Z del transform quede siempre en -1
    void LateUpdate()
    {
        Vector3 p = transform.position;
        if (p.z != -1f)
        {
            p.z = -1f;
            transform.position = p;
        }
    }
}