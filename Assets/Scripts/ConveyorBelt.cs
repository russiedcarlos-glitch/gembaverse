using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [Header("Configurações da Esteira")]
    [Tooltip("Velocidade de movimento dos itens sobre a esteira")]
    public float speed = 1.0f;

    [Tooltip("Direção física do movimento (eixo local ou global)")]
    public Vector3 direction = Vector3.forward;

    private void OnCollisionStay(Collision collision)
    {
        // Se um item colidir e permanecer sobre a esteira, move ele fisicamente
        if (collision.gameObject.CompareTag("Item"))
        {
            Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calcula a movimentação física de arraste sobre a esteira
                Vector3 movement = transform.TransformDirection(direction).normalized * speed * Time.deltaTime;
                rb.MovePosition(rb.position + movement);
            }
        }
    }
}
