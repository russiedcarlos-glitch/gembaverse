using UnityEngine;

public class KPIManager : MonoBehaviour
{
    public static KPIManager Instance { get; private set; }

    [Header("Métricas de Produção")]
    public int TotalProductsFinished = 0;
    public int goodProductsFinished = 0;
    public int defectiveProductsFinished = 0;
    public int consecutiveGoodProducts = 0;
    public float qualityYield = 100f; // Percentual de peças boas (0% a 100%)
    
    [Header("Métricas de Tempo")]
    public float SimulationStartTime;
    public float AverageLeadTime = 0f;

    private float totalLeadTimeSum = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        ResetMetrics();
    }

    // Método para reiniciar as métricas ao mudar de cenário
    public void ResetMetrics()
    {
        TotalProductsFinished = 0;
        goodProductsFinished = 0;
        defectiveProductsFinished = 0;
        consecutiveGoodProducts = 0;
        qualityYield = 100f;
        AverageLeadTime = 0f;
        totalLeadTimeSum = 0f;
        SimulationStartTime = Time.time;
    }

    public void RegisterFinishedProduct(float spawnTime, bool isDefective)
    {
        TotalProductsFinished++;
        
        if (isDefective)
        {
            defectiveProductsFinished++;
            consecutiveGoodProducts = 0;
        }
        else
        {
            goodProductsFinished++;
            consecutiveGoodProducts++;
        }

        // Calcula a Qualidade (Yield %)
        qualityYield = ((float)goodProductsFinished / TotalProductsFinished) * 100f;
        
        // Calcula o Lead Time (tempo do início ao fim)
        float leadTime = Time.time - spawnTime;
        totalLeadTimeSum += leadTime;
        AverageLeadTime = totalLeadTimeSum / TotalProductsFinished;
    }

    public float GetProductivity()
    {
        float elapsedTimeMinutes = (Time.time - SimulationStartTime) / 60f;
        if (elapsedTimeMinutes <= 0) return 0f;
        
        // Retorna Unidades por Minuto (UPM)
        return TotalProductsFinished / elapsedTimeMinutes;
    }
}
