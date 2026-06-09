using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Workstation : MonoBehaviour
{
    [Header("Identificação e Configurações")]
    public string stationName = "Máquina";
    
    [Tooltip("Tempo em segundos que esta máquina leva para processar uma peça")]
    public float processingTime = 2.0f;
    
    [Tooltip("Local onde o item processado será solto (Ponto de saída)")]
    public Transform outputPoint;

    [Header("Estados Kaizen / Desastres")]
    [Tooltip("Define se a máquina está quebrada (para de funcionar)")]
    public bool isBroken = false;

    [Tooltip("Taxa de defeito gerado nas peças (0 = 0% de defeito, 0.5 = 50% de defeito)")]
    [Range(0f, 1f)]
    public float defectRate = 0f;

    private bool isProcessing = false;
    private Color originalColor;
    private Renderer myRenderer;
    private Collider barrierCollider;
    
    // Variáveis para calcular Utilização
    public float TotalActiveTime { get; private set; }

    private void Start()
    {
        myRenderer = GetComponent<Renderer>();
        if (myRenderer == null)
        {
            myRenderer = GetComponentInChildren<Renderer>();
        }
        if (myRenderer != null)
        {
            originalColor = myRenderer.material.color;
        }

        // Encontra a barreira física de fila
        Transform barrierTrans = transform.Find("SKAI_Workstation_Barrier");
        if (barrierTrans != null)
        {
            barrierCollider = barrierTrans.GetComponent<Collider>();
        }

        UpdateVisuals();
    }
    
    private void Update()
    {
        if (isProcessing)
        {
            TotalActiveTime += Time.deltaTime;
        }

        // Habilita a barreira física se a máquina estiver quebrada ou processando
        if (barrierCollider != null)
        {
            barrierCollider.enabled = isBroken || isProcessing;
        }
    }

    // Usa OnTriggerStay para garantir que se a máquina for consertada com uma peça em cima, ela volte a processar
    private void OnTriggerStay(Collider other)
    {
        if (!isProcessing && !isBroken && other.CompareTag("Item"))
        {
            // Evita processar um item que já está marcado como defeituoso se quisermos separar fluxos,
            // mas aqui processa normalmente
            StartCoroutine(ProcessItem(other.gameObject));
        }
    }

    private IEnumerator ProcessItem(GameObject item)
    {
        isProcessing = true;
        
        // Simula o item "dentro" da máquina desativando a visualização dele temporariamente
        item.SetActive(false);

        // Tempo de processamento
        yield return new WaitForSeconds(processingTime);

        // Finaliza o processamento
        item.SetActive(true);

        // Aplica defeito na peça com base na taxa de falha da máquina
        Item itemScript = item.GetComponent<Item>();
        if (itemScript != null)
        {
            if (defectRate > 0f && Random.value < defectRate)
            {
                itemScript.isDefective = true;

                // Se for cenário Jidoka, a máquina detecta o defeito e auto-para a linha (Jidoka Stop)
                if (DisasterManager.Instance != null && DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Jidoka)
                {
                    isBroken = true;
                    UpdateVisuals();
                    Debug.Log($"[Jidoka Stop] {stationName} detectou defeito na fonte e auto-parou a linha!");
                }
            }
        }

        if (outputPoint != null)
        {
            item.transform.position = outputPoint.position;
        }
        else
        {
            item.transform.position = transform.position + Vector3.forward * 1.5f; // Offset padrão
        }
        
        // Aplica uma pequena força se tiver RigidBody, apenas para ele não ficar parado
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Reseta velocidades anteriores para evitar impulsos estranhos acumulados
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(Vector3.forward * 2f, ForceMode.Impulse);
        }

        isProcessing = false;
    }

    // Método público para consertar a máquina (TPM)
    public void Repair()
    {
        if (isBroken)
        {
            isBroken = false;

            // Se for Jidoka, e o defeito ainda estiver ativo, nós também calibramos para facilitar
            if (DisasterManager.Instance != null && DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Jidoka)
            {
                defectRate = 0f;
            }

            UpdateVisuals();
            Debug.Log($"[Reparo/Jidoka] {stationName} foi consertada/recalibrada com sucesso!");
        }
    }

    // Método público para recalibrar a máquina (Jidoka / Qualidade)
    public void Calibrate()
    {
        if (defectRate > 0f)
        {
            defectRate = 0f;
            UpdateVisuals();
            Debug.Log($"[Jidoka] {stationName} foi recalibrada e agora produz peças 100% boas!");
        }
    }

    // Atualiza a aparência da máquina com base no seu estado
    public void UpdateVisuals()
    {
        if (myRenderer == null) return;

        if (isBroken)
        {
            // Vermelho vivo para máquina quebrada
            myRenderer.material.color = new Color(0.9f, 0.1f, 0.1f);
            if (myRenderer.material.HasProperty("_EmissionColor"))
            {
                myRenderer.material.SetColor("_EmissionColor", new Color(0.6f, 0f, 0f));
                myRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else if (defectRate > 0f)
        {
            // Amarelo para máquina descalibrada produzindo refugo
            myRenderer.material.color = new Color(0.9f, 0.7f, 0.1f);
            if (myRenderer.material.HasProperty("_EmissionColor"))
            {
                myRenderer.material.SetColor("_EmissionColor", new Color(0.5f, 0.35f, 0f));
                myRenderer.material.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            // Restaura a cor original se estiver operando normalmente
            myRenderer.material.color = originalColor;
            if (myRenderer.material.HasProperty("_EmissionColor"))
            {
                myRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}
