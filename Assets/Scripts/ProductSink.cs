using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProductSink : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            Item itemComp = other.GetComponent<Item>();
            if (itemComp != null)
            {
                // Registra nas métricas incluindo a qualidade (se é defeituoso ou não)
                if (KPIManager.Instance != null)
                {
                    KPIManager.Instance.RegisterFinishedProduct(itemComp.SpawnTime, itemComp.isDefective);
                }
            }
            
            // Destrói o item pois o processo finalizou
            Destroy(other.gameObject);
        }
    }
}
