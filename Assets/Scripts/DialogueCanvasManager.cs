using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueCanvasManager : MonoBehaviour
{
    public static DialogueCanvasManager Instance { get; private set; }

    [Header("Referências da UI")]
    public TextMeshProUGUI workerNameText;
    public TextMeshProUGUI dialogueBodyText;
    public GameObject dialoguePanel;

    [Header("Botões de Perguntas")]
    public Button cycleTimeButton;
    public Button qualityButton;
    public Button statusButton;
    public Button closeButton;

    private NPCWorker currentNPC;
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;
        
        // Configura os listeners dos botões
        if (cycleTimeButton != null) cycleTimeButton.onClick.AddListener(AskAboutCycleTime);
        if (qualityButton != null) qualityButton.onClick.AddListener(AskAboutQuality);
        if (statusButton != null) statusButton.onClick.AddListener(AskAboutStatus);
        if (closeButton != null) closeButton.onClick.AddListener(HideDialogue);

        // Começa oculto
        HideDialogue();
    }

    private void Update()
    {
        // Se estiver visível, faz o painel rotacionar para encarar a câmera (Billboard effect)
        if (dialoguePanel != null && dialoguePanel.activeSelf)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                // Encara a câmera mas mantem a rotação X/Z travada se quisermos verticalidade reta
                Vector3 targetDir = mainCam.transform.position - transform.position;
                targetDir.y = 0; // Trava o eixo Y para o canvas ficar em pé reto
                
                if (targetDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(-targetDir);
                }
            }
        }
    }

    public void ShowDialogue(NPCWorker npc)
    {
        currentNPC = npc;
        if (currentNPC == null) return;

        // Posiciona o Canvas logo acima do operário
        transform.position = currentNPC.transform.position + Vector3.up * 2.2f;

        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (workerNameText != null) 
            workerNameText.text = currentNPC.workerName;

        if (dialogueBodyText != null)
        {
            string machineName = currentNPC.targetWorkstation != null ? currentNPC.targetWorkstation.stationName : "área";
            dialogueBodyText.text = $"Olá, sou o(a) <b>{currentNPC.workerName}</b>. Como posso ajudar com a {machineName}?";
        }
        
        Debug.Log($"[NPC Diálogo] Abrindo conversa com {currentNPC.workerName}.");
    }

    public void AskAboutCycleTime()
    {
        if (currentNPC == null || dialogueBodyText == null) return;
        dialogueBodyText.text = currentNPC.GetCycleTimeDialogue();
    }

    public void AskAboutQuality()
    {
        if (currentNPC == null || dialogueBodyText == null) return;
        dialogueBodyText.text = currentNPC.GetQualityDialogue();
    }

    public void AskAboutStatus()
    {
        if (currentNPC == null || dialogueBodyText == null) return;
        dialogueBodyText.text = currentNPC.GetStatusDialogue();
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        currentNPC = null;
    }
}
