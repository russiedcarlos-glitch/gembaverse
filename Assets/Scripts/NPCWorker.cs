using UnityEngine;

public class NPCWorker : MonoBehaviour
{
    [Header("Identificação do Operário")]
    public string workerName = "Operário";
    
    [Tooltip("A máquina associada a este operário. Se nulo, buscará a mais próxima no Start.")]
    public Workstation targetWorkstation;

    private void Start()
    {
        // Se não houver estação atribuída, busca a mais próxima fisicamente
        if (targetWorkstation == null)
        {
            Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
            float minDistance = float.MaxValue;
            Workstation closest = null;

            foreach (Workstation ws in stations)
            {
                float dist = Vector3.Distance(transform.position, ws.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = ws;
                }
            }

            if (closest != null)
            {
                targetWorkstation = closest;
                Debug.Log($"[NPC] {workerName} associado automaticamente à máquina {closest.stationName}.");
            }
        }
    }

    public string GetCycleTimeDialogue()
    {
        if (targetWorkstation == null) 
            return "Eu não estou operando nenhuma máquina no momento.";

        float cycle = targetWorkstation.processingTime;
        return $"[Tempo de Ciclo]: O tempo de processamento atual da {targetWorkstation.stationName} é de {cycle:F1} segundos por peça.";
    }

    public string GetQualityDialogue()
    {
        if (targetWorkstation == null) 
            return "Não estou processando peças para avaliar a qualidade.";

        float defect = targetWorkstation.defectRate;
        if (defect > 0f)
        {
            return $"[Qualidade]: Ruim! Estamos gerando cerca de {defect * 100f:F0}% de peças com defeito (rebarbas ou fora de medida)! Precisamos calibrar a máquina.";
        }
        else
        {
            return "[Qualidade]: Excelente! Nenhuma peça com defeito gerada recentemente. O rendimento é de 100%.";
        }
    }

    public string GetStatusDialogue()
    {
        if (targetWorkstation == null) 
            return "Olá! Estou apenas supervisionando a área.";

        if (targetWorkstation.isBroken)
        {
            return $"[Status]: <color=red>[!] A máquina {targetWorkstation.stationName} QUEBROU!</color> Ela parou totalmente e a luz vermelha acendeu. Preciso que alguém a conserte clicando nela!";
        }

        if (targetWorkstation.defectRate > 0f)
        {
            return $"[Status]: <color=yellow>[!] A máquina {targetWorkstation.stationName} está DESCALIBRADA!</color> Ela está amarela e cuspindo peças com defeito. Precisamos de uma calibração imediata.";
        }

        // Lógica de Balanceamento
        if (DisasterManager.Instance != null && DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Balanceamento)
        {
            float time = targetWorkstation.processingTime;
            if (time < 1.0f)
            {
                return $"[Status]: <color=cyan>[MAX] Superprodução!</color> Estou processando muito rápido ({time:F1}s). Mas o operário seguinte é lento, então as peças estão acumulando no chão de fábrica! Clique em mim ou na máquina para reajustar para 2.0s.";
            }
            else if (time > 4.0f)
            {
                return $"[Status]: <color=orange>[SLOW] Gargalo!</color> Eu levo muito tempo ({time:F1}s) para processar e estou atrasando toda a linha de produção! Há muito tempo de espera. Clique para acelerar meu ritmo para 2.0s.";
            }
            else
            {
                return "[Status]: <color=green>[OK] Balanceado!</color> O ritmo de 2.0s está ótimo. Fluxo contínuo sem gargalos.";
            }
        }

        return $"[Status]: <color=green>[OK] Operação normal.</color> A máquina {targetWorkstation.stationName} está rodando estável a {targetWorkstation.processingTime:F1}s por peça.";
    }
}
