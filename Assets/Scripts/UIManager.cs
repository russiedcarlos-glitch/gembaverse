using UnityEngine;
using TMPro; // Requer TextMeshPro na Unity

public class UIManager : MonoBehaviour
{
    [Header("Referências da UI (Text/TextMeshPro)")]
    public TextMeshProUGUI productivityText;
    public TextMeshProUGUI leadTimeText;
    public TextMeshProUGUI totalItemsText;
    
    [Tooltip("Exibe o rendimento de qualidade (Yield %)")]
    public TextMeshProUGUI qualityText; 
    
    [Tooltip("Painel de avisos/alertas de produção")]
    public TextMeshProUGUI alertText;

    private void Update()
    {
        if (KPIManager.Instance != null)
        {
            if (productivityText != null)
            {
                productivityText.text = $"<size=12><color=#A0A5B5>PRODUTIVIDADE</color></size>\n<b>{KPIManager.Instance.GetProductivity():F2}</b> <size=13>u/min</size>";
            }
            if (leadTimeText != null)
            {
                leadTimeText.text = $"<size=12><color=#A0A5B5>LEAD TIME MÉDIO</color></size>\n<b>{KPIManager.Instance.AverageLeadTime:F2}</b> <size=13>seg</size>";
            }
            if (totalItemsText != null)
            {
                totalItemsText.text = $"<size=12><color=#A0A5B5>TOTAL PRODUZIDO</color></size>\n<b>{KPIManager.Instance.TotalProductsFinished}</b> <size=13>peças</size>";
            }

            if (qualityText != null)
            {
                qualityText.text = $"<size=12><color=#A0A5B5>QUALIDADE (YIELD)</color></size>\n<b>{KPIManager.Instance.qualityYield:F1}%</b>";
            }

            if (alertText != null)
            {
                // Busca em tempo real se há alguma máquina com problemas na cena
                Workstation[] stations = GameObject.FindObjectsByType<Workstation>(FindObjectsSortMode.None);
                bool hasBroken = false;
                bool hasDefect = false;
                string problematicStationName = "";

                foreach (Workstation ws in stations)
                {
                    if (ws.isBroken)
                    {
                        hasBroken = true;
                        problematicStationName = ws.stationName;
                        break;
                    }
                    if (ws.defectRate > 0f)
                    {
                        hasDefect = true;
                        problematicStationName = ws.stationName;
                    }
                }

                if (hasBroken)
                {
                    alertText.text = "<size=11><color=#FF6B6B>[!] ALERTA DE PRODUÇÃO</color></size>\n<b>MÁQUINA PARADA!</b>\n<size=11>Fale com os operários para identificar.</size>";
                }
                else if (hasDefect)
                {
                    alertText.text = "<size=11><color=#FFD25A>[!] ALERTA DE QUALIDADE</color></size>\n<b>REFUGOS NA ESTEIRA!</b>\n<size=11>Fale com os operários sobre o problema.</size>";
                }
                else
                {
                    alertText.text = "<size=11><color=#51CF66>[OK] STATUS OPERACIONAL</color></size>\n<b>FLUXO NORMAL</b>\n<size=11>Linha estável e produtiva.</size>";
                }
            }
        }
    }
}
