using System.Collections;
using UnityEngine;
using TMPro;

public class DisasterManager : MonoBehaviour
{
    public static DisasterManager Instance { get; private set; }

    public enum ScenarioType { FreePlay, TPM, Jidoka, Balanceamento }
    public enum IndustryProfile { Metalurgica, Eletronicos }

    [Header("Configurações Gerais")]
    public ScenarioType activeScenario = ScenarioType.FreePlay;
    public IndustryProfile currentProfile = IndustryProfile.Metalurgica;

    [Header("Referências de UI")]
    [Tooltip("Campo de texto para instruções do cenário")]
    public TextMeshProUGUI scenarioText;
    
    [Tooltip("Campo de texto para exibir se venceu o cenário")]
    public TextMeshProUGUI winStatusText;

    [Header("Configurações dos Cenários")]
    [Tooltip("Tempo em segundos entre cada quebra no cenário TPM")]
    public float timeBetweenBreakdowns = 15f;

    private float breakdownTimer = 0f;
    private bool scenarioComplete = false;
    private int repairsMadeCount = 0;

    // Métricas para Avaliação da IA
    private float scenarioStartTime;
    private int playerActionsCount = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        StartScenario(activeScenario, false);
        TeleportPlayerToStudyRoom();
    }

    private void Update()
    {
        if (scenarioComplete) return;

        switch (activeScenario)
        {
            case ScenarioType.TPM:
                UpdateTPMScenario();
                break;
            case ScenarioType.Jidoka:
                UpdateJidokaScenario();
                break;
            case ScenarioType.Balanceamento:
                UpdateBalanceamentoScenario();
                break;
            default:
                if (scenarioText != null)
                    scenarioText.text = "Modo: Simulação Livre\nPergunte aos operários sobre a linha de produção ou mude os tempos.";
                if (winStatusText != null)
                    winStatusText.text = "";
                break;
        }
    }

    public void StartScenario(ScenarioType scenario, bool teleportPlayer = true)
    {
        activeScenario = scenario;
        scenarioComplete = false;
        repairsMadeCount = 0;
        playerActionsCount = 0;
        scenarioStartTime = Time.time;
        
        if (KPIManager.Instance != null)
        {
            KPIManager.Instance.ResetMetrics();
        }

        // Aplica o perfil industrial (renomeia máquinas e operários)
        SetIndustryProfile(currentProfile);

        // Reseta todas as máquinas para o estado normal de início
        Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        foreach (Workstation ws in stations)
        {
            ws.isBroken = false;
            ws.defectRate = 0f;
            ws.UpdateVisuals();
        }

        // Garante que o Spawner está ativo e gerando peças ao mudar de modo
        ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
        if (spawner != null)
        {
            spawner.isSpawning = true;
        }

        // Fecha qualquer diálogo pendente
        if (DialogueCanvasManager.Instance != null)
        {
            DialogueCanvasManager.Instance.HideDialogue();
        }

        // Teletransporta o jogador para o chão de fábrica de frente para os indicadores
        if (teleportPlayer)
        {
            TeleportPlayerToFactory();
        }

        if (winStatusText != null)
            winStatusText.text = "";

        // Desativa o painel de fundo de vitória
        GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
        if (canvasObj != null)
        {
            Transform winBg = canvasObj.transform.Find("WinStatusBackground");
            if (winBg != null) winBg.gameObject.SetActive(false);
        }

        switch (activeScenario)
        {
            case ScenarioType.TPM:
                // Provoca a primeira quebra IMEDIATAMENTE para o jogador poder praticar sem esperar
                TriggerRandomBreakdown();
                breakdownTimer = timeBetweenBreakdowns; 
                if (scenarioText != null)
                    scenarioText.text = "<size=11><color=#A0A5B5>INSTRUÇÕES DO CENÁRIO</color></size>\n<b>TPM (Quebras de Máquina)</b>\n<size=12>Conserte as máquinas clicando nelas quando quebrarem (ficarem vermelhas).\n<b>Objetivo:</b> Faça 3 reparos e mantenha a produtividade alta.</size>";
                break;

            case ScenarioType.Jidoka:
                // Descalibra uma máquina IMEDIATAMENTE para o jogador poder praticar sem esperar
                TriggerJidokaDefect();
                if (scenarioText != null)
                    scenarioText.text = "<size=11><color=#A0A5B5>INSTRUÇÕES DO CENÁRIO</color></size>\n<b>Jidoka (Qualidade na Fonte)</b>\n<size=12>Uma máquina descalibrada está gerando refugo (amarela). Fale com os operários para achar a máquina, recalibre-a e atinja 100% de qualidade em 5 peças seguidas.</size>";
                break;

            case ScenarioType.Balanceamento:
                TriggerBalanceamentoBottleneck(stations);
                if (scenarioText != null)
                    scenarioText.text = "<size=11><color=#A0A5B5>INSTRUÇÕES DO CENÁRIO</color></size>\n<b>Balanceamento de Linha</b>\n<size=12>Há um gargalo gerando acúmulo de estoque. Descubra com os operários e reajuste a velocidade de ambas as estações para 2.0s para sincronizar com o Takt Time.</size>";
                break;
        }

        Debug.Log($"[Cenário] Iniciado: {activeScenario} (Perfil: {currentProfile})");
    }

    public void RegisterPlayerAction()
    {
        playerActionsCount++;
    }

    public void SetIndustryProfile(IndustryProfile profile)
    {
        currentProfile = profile;
        Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        
        // Ordena pela posição X (da esquerda para a direita na linha)
        System.Array.Sort(stations, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        string[] metalurgicaNames = { "Torno CNC A", "Fresadora B", "Retificadora C" };
        string[] eletronicosNames = { "Insersora SMD A", "Forno de Refusão B", "Estação de Solda C" };

        for (int i = 0; i < stations.Length; i++)
        {
            string newName = "";
            if (profile == IndustryProfile.Metalurgica)
            {
                newName = i < metalurgicaNames.Length ? metalurgicaNames[i] : $"Máquina Metalúrgica {i + 1}";
            }
            else
            {
                newName = i < eletronicosNames.Length ? eletronicosNames[i] : $"Máquina Eletrônica {i + 1}";
            }
            
            stations[i].stationName = newName;
            stations[i].gameObject.name = newName;
            stations[i].UpdateVisuals();
            
            // Procura o NPC associado a essa estação e o renomeia
            NPCWorker npc = FindNPCForWorkstation(stations[i]);
            if (npc != null)
            {
                npc.workerName = GetWorkerNameForIndex(i, profile);
            }
        }
        
        // Renomeia o Spawner
        ItemSpawner spawner = FindFirstObjectByType<ItemSpawner>();
        if (spawner != null)
        {
            spawner.gameObject.name = profile == IndustryProfile.Metalurgica ? "Alimentador de Engrenagens" : "Alimentador de PCBs";
        }
    }

    private NPCWorker FindNPCForWorkstation(Workstation ws)
    {
        NPCWorker[] npcs = FindObjectsByType<NPCWorker>(FindObjectsSortMode.None);
        foreach (NPCWorker npc in npcs)
        {
            // O NPCWorker tem associação automática no Start, ou podemos checar a distância
            if (npc.targetWorkstation == ws) return npc;
            if (npc.targetWorkstation == null && Vector3.Distance(npc.transform.position, ws.transform.position) < 3.5f)
            {
                npc.targetWorkstation = ws;
                return npc;
            }
        }
        return null;
    }

    private string GetWorkerNameForIndex(int index, IndustryProfile profile)
    {
        if (profile == IndustryProfile.Metalurgica)
        {
            string[] names = { "Roberto (Operador)", "Marcos (Mecânico)", "José (Retificador)" };
            if (index < names.Length) return names[index];
            return $"Operário Metalúrgico {index + 1}";
        }
        else
        {
            string[] names = { "Juliana (Operadora SMD)", "Felipe (Técnico de Processo)", "Luciana (Inspetora Solda)" };
            if (index < names.Length) return names[index];
            return $"Operário Eletrônico {index + 1}";
        }
    }

    private void TeleportPlayerToFactory()
    {
        GameObject player = null;
        Camera activeCam = Camera.main;
        if (activeCam != null)
        {
            // Sobe na hierarquia da câmera principal ativa para encontrar o objeto pai do jogador
            Transform t = activeCam.transform;
            while (t.parent != null)
            {
                string pName = t.parent.gameObject.name;
                if (pName.Contains("Player") || pName.Contains("Rig") || pName.Contains("Origin"))
                {
                    player = t.parent.gameObject;
                }
                t = t.parent;
            }
            if (player == null)
            {
                player = activeCam.transform.parent != null ? activeCam.transform.parent.gameObject : activeCam.gameObject;
            }
        }

        if (player == null)
        {
            player = GameObject.Find("SKAI_Player")
                          ?? GameObject.Find("[BuildingBlock] Camera Rig") 
                          ?? GameObject.Find("Camera Rig") 
                          ?? GameObject.Find("OVRCameraRig") 
                          ?? GameObject.Find("PlayerController") 
                          ?? GameObject.FindWithTag("Player");
        }

        if (player != null)
        {
            Vector3 targetPos = new Vector3(0f, 0.5f, -4f);
            bool lookingAtCanvas = false;

            // Se for simulação livre, teletransporta na frente do Canvas de KPIs
            if (activeScenario == ScenarioType.FreePlay)
            {
                GameObject kpiCanvas = GameObject.Find("SKAI_KPI_Canvas");
                if (kpiCanvas != null)
                {
                    targetPos = kpiCanvas.transform.position - kpiCanvas.transform.forward * 2.5f;
                    targetPos.y = 0.5f;
                    lookingAtCanvas = true;
                }
            }
            else
            {
                // Para cenários de desafio (TPM, Jidoka, Balanceamento),
                // posiciona o jogador diretamente no centro do corredor da linha de produção!
                Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
                if (stations != null && stations.Length > 0)
                {
                    float sumX = 0f;
                    float sumY = 0f;
                    float sumZ = 0f;
                    foreach (var ws in stations)
                    {
                        sumX += ws.transform.position.x;
                        sumY += ws.transform.position.y;
                        sumZ += ws.transform.position.z;
                    }
                    float avgX = sumX / stations.Length;
                    float avgY = sumY / stations.Length;
                    float avgZ = sumZ / stations.Length;

                    // Posiciona o jogador recuado em Z, de frente para as máquinas
                    targetPos = new Vector3(avgX, 0.5f, avgZ - 3.5f);
                }
            }

            // Desativa temporariamente o CharacterController para evitar conflitos de física e travamentos
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = targetPos;

            // Define a rotação (olhar para o Canvas ou olhar para as máquinas)
            if (lookingAtCanvas)
            {
                GameObject kpiCanvas = GameObject.Find("SKAI_KPI_Canvas");
                if (kpiCanvas != null)
                {
                    Vector3 lookTarget = kpiCanvas.transform.position;
                    lookTarget.y = player.transform.position.y;
                    player.transform.rotation = Quaternion.LookRotation(lookTarget - player.transform.position);
                }
            }
            else
            {
                // Olha em direção à linha de produção
                Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
                if (stations != null && stations.Length > 0)
                {
                    float sumX = 0f;
                    float sumZ = 0f;
                    foreach (var ws in stations)
                    {
                        sumX += ws.transform.position.x;
                        sumZ += ws.transform.position.z;
                    }
                    Vector3 lookTarget = new Vector3(sumX / stations.Length, targetPos.y, sumZ / stations.Length);
                    player.transform.rotation = Quaternion.LookRotation(lookTarget - player.transform.position);
                }
            }

            if (cc != null) cc.enabled = true;

            Debug.Log($"[Teleporte] Jogador movido com sucesso para a Fábrica em {targetPos}!");
        }
    }

    // --- CENÁRIO TPM ---
    private void UpdateTPMScenario()
    {
        breakdownTimer -= Time.deltaTime;
        if (breakdownTimer <= 0f)
        {
            TriggerRandomBreakdown();
            breakdownTimer = timeBetweenBreakdowns;
        }

        // Condição de Vitória: Consertar 3 máquinas e manter boa produtividade
        if (repairsMadeCount >= 3 && KPIManager.Instance.GetProductivity() > 5f)
        {
            CompleteScenario("Você aplicou o TPM com sucesso e evitou o colapso da linha!");
        }
    }

    private void TriggerRandomBreakdown()
    {
        Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        if (stations.Length == 0) return;

        int attempts = 0;
        while (attempts < 10)
        {
            int idx = Random.Range(0, stations.Length);
            if (!stations[idx].isBroken)
            {
                stations[idx].isBroken = true;
                stations[idx].UpdateVisuals();
                Debug.Log($"[DESASTRE] {stations[idx].stationName} quebrou!");
                break;
            }
            attempts++;
        }
    }

    public void RegisterRepair()
    {
        repairsMadeCount++;
        RegisterPlayerAction();
        Debug.Log($"[Cenário] Reparo registrado: {repairsMadeCount}/3");
    }

    // --- CENÁRIO JIDOKA ---
    private void UpdateJidokaScenario()
    {
        // Vitória: Produziu pelo menos 5 peças boas consecutivas e a máquina descalibrada foi corrigida
        if (KPIManager.Instance != null && KPIManager.Instance.consecutiveGoodProducts >= 5)
        {
            Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
            bool anyCalibratingNeeded = false;
            foreach (Workstation ws in stations)
            {
                if (ws.defectRate > 0f) anyCalibratingNeeded = true;
            }

            if (!anyCalibratingNeeded)
            {
                CompleteScenario("Você identificou o refugo na fonte, recalibrou a máquina e o fluxo se normalizou!");
            }
        }
    }

    private void TriggerJidokaDefect()
    {
        Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        if (stations.Length == 0) return;

        int idx = Random.Range(0, stations.Length);
        stations[idx].defectRate = 0.5f;
        stations[idx].UpdateVisuals();
        Debug.Log($"[QUALIDADE] {stations[idx].stationName} está descalibrada e gerando refugos!");
    }

    // --- CENÁRIO BALANCEAMENTO ---
    private void TriggerBalanceamentoBottleneck(Workstation[] stations)
    {
        if (stations.Length < 2) return;

        stations[0].processingTime = 0.5f;
        stations[0].stationName = currentProfile == IndustryProfile.Metalurgica ? "Torno CNC A (Rápido)" : "Insersora SMD A (Rápida)";

        stations[1].processingTime = 6.0f;
        stations[1].stationName = currentProfile == IndustryProfile.Metalurgica ? "Fresadora B (Gargalo)" : "Forno de Refusão B (Gargalo)";
        
        stations[0].UpdateVisuals();
        stations[1].UpdateVisuals();
    }

    private void UpdateBalanceamentoScenario()
    {
        Workstation[] stations = FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        if (stations.Length < 2) return;

        // Ordena pela posição X (da esquerda para a direita) para garantir correspondência correta das máquinas
        System.Array.Sort(stations, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        float diff = Mathf.Abs(stations[0].processingTime - stations[1].processingTime);
        if (diff < 0.2f && Mathf.Abs(stations[0].processingTime - 2.0f) < 0.5f)
        {
            CompleteScenario("Linha balanceada em 2.0s (Takt Time). O estoque acumulado sumiu!");
        }
    }

    private void CompleteScenario(string message)
    {
        scenarioComplete = true;

        float duration = Time.time - scenarioStartTime;
        float finalYield = KPIManager.Instance != null ? KPIManager.Instance.qualityYield : 100f;
        float productivity = KPIManager.Instance != null ? KPIManager.Instance.GetProductivity() : 0f;

        string rating = "A";
        string aiFeedback = "";

        string profileStr = currentProfile == IndustryProfile.Metalurgica ? "Metalurgia" : "Montagem de Eletrônicos";

        if (activeScenario == ScenarioType.TPM)
        {
            if (playerActionsCount <= 4)
            {
                rating = "A+";
                aiFeedback = "Excelente! Você aplicou a manutenção produtiva total (TPM) com rapidez. As paradas foram solucionadas falando com os operadores corretos, maximizando a produtividade média da célula.";
            }
            else
            {
                rating = "B";
                aiFeedback = "Trabalho correto, mas você realizou ações adicionais desnecessárias. Na vida real, cliques excessivos sem análise geram desperdício de tempo e recursos.";
            }
        }
        else if (activeScenario == ScenarioType.Jidoka)
        {
            if (finalYield > 99f)
            {
                rating = "A+";
                aiFeedback = "Incrível! Você aplicou o princípio Jidoka (automação com toque humano): identificou a descalibração na esteira e recalibrou a máquina, atingindo 100% de qualidade nas peças subsequentes.";
            }
            else
            {
                rating = "B-";
                aiFeedback = "A máquina foi corrigida, porém houve demora no diagnóstico. O atraso permitiu a circulação de refugos, comprometendo a qualidade acumulada.";
            }
        }
        else if (activeScenario == ScenarioType.Balanceamento)
        {
            rating = "A";
            aiFeedback = "Perfeito! Você balanceou os tempos em 2.0s, correspondendo ao Takt Time calculado para a linha. Isso eliminou os gargalos (Superprodução na estação 1 e Espera na estação 2), otimizando o Lead Time.";
        }

        if (winStatusText != null)
        {
            winStatusText.text = $"<color=#51CF66><size=20><b>🏆 DESAFIO CONCLUÍDO COM SUCESSO!</b></size></color>\n" +
                                 $"<color=#FFD25A><b>{message}</b></color>\n\n" +
                                 $"<color=#A0A5B5><b>=== AVALIAÇÃO DO INSTRUTOR IA ===</b></color>\n" +
                                 $"<b>Setor Industrial:</b> {profileStr}\n" +
                                 $"<b>Tempo de Resolução:</b> <color=white>{duration:F1}s</color> | <b>Nota Geral:</b> <color=yellow>{rating}</color>\n" +
                                 $"<b>Cliques do Usuário:</b> {playerActionsCount} | <b>Produtividade Média:</b> {productivity:F1} u/min\n" +
                                 $"<b>Rendimento de Qualidade:</b> {finalYield:F1}%\n\n" +
                                 $"<b>Análise do Processo Kaizen:</b>\n<color=#D0D5E5>{aiFeedback}</color>";
        }

        // Ativa o painel de fundo de vitória e move para frente de todos os outros elementos do Canvas
        GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
        if (canvasObj != null)
        {
            Transform winBg = canvasObj.transform.Find("WinStatusBackground");
            if (winBg != null)
            {
                winBg.gameObject.SetActive(true);
                winBg.SetAsLastSibling();
            }
        }

        Debug.Log("[Cenário] Concluído com sucesso!");
    }

    public void TeleportPlayerToStudyRoom()
    {
        GameObject player = GameObject.Find("SKAI_Player")
                      ?? GameObject.Find("[BuildingBlock] Camera Rig") 
                      ?? GameObject.Find("Camera Rig") 
                      ?? GameObject.Find("OVRCameraRig") 
                      ?? GameObject.Find("PlayerController") 
                      ?? GameObject.FindWithTag("Player");

        if (player != null)
        {
            // Posição padrão na Sala de Estudos (X = 93.37, Z = 41.0)
            Vector3 targetPos = new Vector3(93.37f, 0.5f, 41.0f);
            GameObject screenObj = GameObject.Find("Telão_Sala_Estudo") ?? GameObject.Find("telão") ?? GameObject.Find("Telão") ?? GameObject.Find("Tela") ?? GameObject.Find("Screen") ?? GameObject.Find("TV");
            if (screenObj != null)
            {
                // Coloca o player recuado de frente para o telão
                targetPos = screenObj.transform.position - screenObj.transform.forward * 4.5f;
                targetPos.y = 0.5f;
            }

            // Desativa temporariamente o CharacterController para evitar conflitos de física e travamentos
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            player.transform.position = targetPos;

            // Faz o player olhar para a tela
            if (screenObj != null)
            {
                Vector3 lookTarget = screenObj.transform.position;
                lookTarget.y = player.transform.position.y;
                player.transform.rotation = Quaternion.LookRotation(lookTarget - player.transform.position);
            }
            else
            {
                player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (cc != null) cc.enabled = true;
            Debug.Log($"[Teleporte] Jogador movido com sucesso para a Sala de Estudos em {targetPos}!");
        }
    }
}
