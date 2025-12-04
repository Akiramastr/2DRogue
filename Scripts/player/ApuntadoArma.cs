using UnityEngine;

public class ApuntadoArma : MonoBehaviour
{
    [SerializeField] private Transform objetoHijo;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float offsetRotacion = 0f; // Ajusta este valor según la orientación base del sprite

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        // Busca el SpriteRenderer en los hijos de objetoHijo (incluyendo nietos)
        spriteRenderer = objetoHijo.GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        Vector3 mousePosition = Input.mousePosition;
        mousePosition.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        Vector2 direccion = (worldPosition - transform.position).normalized;

        float angulo = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        objetoHijo.rotation = Quaternion.Euler(0, 0, angulo + offsetRotacion);

        // Flip en el eje Y si el ángulo está entre -30 y -165 grados
        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = angulo <= 75f && angulo >= -95f;
        }
    }
}