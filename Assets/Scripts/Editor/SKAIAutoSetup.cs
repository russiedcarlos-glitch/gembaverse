using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Factory.Sample;

public class SKAIAutoSetup : EditorWindow
{
    [MenuItem("SKAI/Fix and Setup Everything (Consertar Tudo)")]
    public static void FixAndSetupEverything()
    {
        Debug.Log("[SKAI Fixer] Iniciando correcao geral do projeto...");

        // 0. Garantir que as tags do projeto existam
        EnsureTagsExist();

        // 1. Criar ou configurar o Player de forma totalmente automatizada
        ConfigureOrCreatePlayer();

        // 2. Corrigir referencias de Spline em todos os personagens
        CustomSplineAnimate[] splineAnimators = GameObject.FindObjectsByType<CustomSplineAnimate>(FindObjectsSortMode.None);
        SplineContainer[] containers = GameObject.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);

        int splinesFixed = 0;
        if (containers.Length > 0)
        {
            foreach (var sa in splineAnimators)
            {
                SerializedObject so = new SerializedObject(sa);
                SerializedProperty sp = so.FindProperty("m_SplineContainer");

                if (sp != null)
                {
                    float minDist = float.MaxValue;
                    SplineContainer closest = null;

                    foreach (var container in containers)
                    {
                        float dist = Vector3.Distance(sa.transform.position, container.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = container;
                        }
                    }

                    if (closest != null)
                    {
                        sp.objectReferenceValue = closest;
                        so.ApplyModifiedProperties();
                        splinesFixed++;
                    }
                }
            }
        }
        Debug.Log($"[SKAI Fixer] Referencias de Spline re-associadas: {splinesFixed}/{splineAnimators.Length} (Total Splines na cena: {containers.Length})");

        // 3. Associar permanentemente os NPCs operarios as suas estacoes de trabalho correspondentes no Editor
        NPCWorker[] npcs = GameObject.FindObjectsByType<NPCWorker>(FindObjectsSortMode.None);
        Workstation[] stations = GameObject.FindObjectsByType<Workstation>(FindObjectsSortMode.None);
        int npcsFixed = 0;

        foreach (var npc in npcs)
        {
            if (npc.targetWorkstation == null)
            {
                SerializedObject so = new SerializedObject(npc);
                SerializedProperty sp = so.FindProperty("targetWorkstation");

                if (sp != null)
                {
                    float minDist = float.MaxValue;
                    Workstation closest = null;

                    foreach (var ws in stations)
                    {
                        float dist = Vector3.Distance(npc.transform.position, ws.transform.position);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = ws;
                        }
                    }

                    if (closest != null)
                    {
                        sp.objectReferenceValue = closest;
                        so.ApplyModifiedProperties();
                        npcsFixed++;
                    }
                }
            }
        }
        Debug.Log($"[SKAI Fixer] NPCs vinculados as estacoes de trabalho: {npcsFixed}/{npcs.Length}");

        // 4. Executar a rotina padrao de Auto Setup para alinhar todos os componentes, Canvas de UI, e botoes
        RunAutoSetup();

        // 5. Salvar a cena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog("SKAI Consertador Geral", 
            $"Correcao Concluida!\n\n" +
            $"- Scripts ausentes removidos da Camera.\n" +
            $"- Referencias de Splines re-associadas: {splinesFixed} (Total de Splines: {containers.Length})\n" +
            $"- NPCs vinculados permanentemente no Editor: {npcsFixed}\n" +
            $"- Setup de UI, Spawner e Video atualizados.\n\n" +
            $"Salve a cena (Ctrl+S) e entre no modo Play para testar!", "OK");
    }

    [MenuItem("SKAI/Automated Setup (One-Click)")]
    public static void RunAutoSetup()
    {
        Debug.Log("[SKAI Setup] Iniciando configuracao automatizada...");

        // 0. Garantir que as tags do projeto existam
        EnsureTagsExist();

        // Limpar qualquer canvas antigo inútil que possa ter ficado de versões anteriores (HUD ou controles antigos)
        GameObject oldHUD = GameObject.Find("SKAI_HUD_Canvas");
        if (oldHUD != null)
        {
            DestroyImmediate(oldHUD);
            Debug.Log("[SKAI Setup] Removido SKAI_HUD_Canvas antigo da cena.");
        }

        GameObject oldVideoControls = GameObject.Find("SKAI_VideoControls_Canvas");
        if (oldVideoControls != null)
        {
            DestroyImmediate(oldVideoControls);
            Debug.Log("[SKAI Setup] Removido SKAI_VideoControls_Canvas antigo da cena.");
        }

        // 1. GERAR PREFAB DO ITEM (Matéria-prima)
        GameObject itemPrefab = CreateOrGetItemPrefab();
        if (itemPrefab == null)
        {
            Debug.LogError("[SKAI Setup] Falha ao criar o prefab do Item!");
            return;
        }

        // 2. CONFIGURAR GERENCIADOR E METRICAS (GameManager)
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            gameManager = new GameObject("GameManager");
        }

        KPIManager kpi = gameManager.GetComponent<KPIManager>();
        if (kpi == null) kpi = gameManager.AddComponent<KPIManager>();

        UIManager ui = gameManager.GetComponent<UIManager>();
        if (ui == null) ui = gameManager.AddComponent<UIManager>();

        DisasterManager disaster = gameManager.GetComponent<DisasterManager>();
        if (disaster == null) disaster = gameManager.AddComponent<DisasterManager>();

        // Adiciona controlador de botões de cenários
        ScenarioUIController scenarioUI = gameManager.GetComponent<ScenarioUIController>();
        if (scenarioUI == null) scenarioUI = gameManager.AddComponent<ScenarioUIController>();

        // Adiciona componente de diagnóstico de câmera
        CameraDiagnostics diagnostics = gameManager.GetComponent<CameraDiagnostics>();
        if (diagnostics == null) diagnostics = gameManager.AddComponent<CameraDiagnostics>();

        // 3. ANIMAR OPERÁRIOS/PERSONAGENS (Fábrica Viva e Configurar NPCs)
        AnimateAllCharactersInScene();

        // 4. CONFIGURAR MÁQUINAS (Workstations)
        ConfigureWorkstations();

        // 5. CONFIGURAR ESTEIRAS E COLOCAR SPAWNER/SINK
        ConfigureConveyorsAndSpawning(itemPrefab);

        // 6. CONFIGURAR TELA DE APRENDIZAGEM (Telão)
        ConfigureVideoPlayerScreen();

        // 7. CONFIGURAR CANVAS DE UI EM WORLD SPACE COM BOTÕES DE SCENARIO E PERFIL
        ConfigureUICanvas(ui, disaster, scenarioUI);

        // 7b. CONFIGURAR CANVAS DE DIALOGO FLUTUANTE DOS NPCs
        ConfigureDialogueCanvas();

        // 8. CRIAR OU CONFIGURAR O PLAYER (Com Câmera e Controles)
        ConfigureOrCreatePlayer();

        // Marcar cena como modificada para salvar
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("SKAI Setup Concluído", 
            "A cena foi configurada com sucesso!\n\n" +
            "- Operários foram animados e configurados como NPCs interativos.\n" +
            "- O Canvas de Diálogos dos NPCs foi gerado na cena.\n" +
            "- Esteiras receberam física de movimento automático.\n" +
            "- O Spawner e o Sink foram posicionados exatamente nas pontas das esteiras.\n" +
            "- O Telão de Vídeo foi configurado com a URL padrão.\n" +
            "- O Canvas de UI foi gerado com indicadores, botões de cenários e perfis.\n" +
            "- O script de cliques foi adicionado à Câmera.", "OK");
    }

    private static GameObject CreateOrGetItemPrefab()
    {
        string folderPath = "Assets/Prefabs";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string prefabPath = folderPath + "/Item.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            // Cria uma esfera simples temporária
            GameObject tempObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempObj.name = "ItemPrefab";
            tempObj.tag = "Item";
            tempObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            // Adiciona física e lógica
            Rigidbody rb = tempObj.GetComponent<Rigidbody>();
            if (rb == null) rb = tempObj.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            tempObj.AddComponent<Item>();

            // Salva como prefab
            prefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
            DestroyImmediate(tempObj);
            
            Debug.Log("[SKAI Setup] Novo Prefab de Item gerado em: " + prefabPath);
        }
        return prefab;
    }

    private static void AnimateAllCharactersInScene()
    {
        int animatedCount = 0;
        string controllerDir = "Assets/UnityFactorySceneHDRP/Scene_Factory/AnimationSample/AnimatorController";

        RuntimeAnimatorController walkCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerDir + "/Walk.controller");
        RuntimeAnimatorController stand1Ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerDir + "/Standing1.controller");
        RuntimeAnimatorController stand2Ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerDir + "/Standing2.controller");
        RuntimeAnimatorController tabletCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerDir + "/UsingTablet.controller");

        // Encontra todos os Animadores ativos na cena (incluindo sub-objetos dos operários)
        Animator[] animators = GameObject.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (Animator anim in animators)
        {
            GameObject charObj = anim.gameObject;
            string nameLower = charObj.name.ToLower();

            // Verifica se é um personagem (operário, pessoa, avatar ou contém SkinnedMeshRenderer)
            bool isCharacter = nameLower.Contains("worker") || nameLower.Contains("operator") || 
                               nameLower.Contains("people") || nameLower.Contains("character") || 
                               nameLower.Contains("person") || nameLower.Contains("male") || 
                               nameLower.Contains("female") || nameLower.Contains("body") || 
                               nameLower.Contains("avatar") || nameLower.Contains("operator") ||
                               charObj.GetComponentInChildren<SkinnedMeshRenderer>() != null;

            if (!isCharacter) continue;

            // Se o animador não tiver um controller, atribui um baseado no nome
            if (anim.runtimeAnimatorController == null)
            {
                if (nameLower.Contains("walk") || nameLower.Contains("andando"))
                {
                    anim.runtimeAnimatorController = walkCtrl;
                }
                else if (nameLower.Contains("tablet") || nameLower.Contains("prancheta") || nameLower.Contains("note"))
                {
                    anim.runtimeAnimatorController = tabletCtrl;
                }
                else
                {
                    anim.runtimeAnimatorController = (Random.value > 0.5f) ? stand1Ctrl : stand2Ctrl;
                }
                animatedCount++;
            }

            // Adiciona colisor físico para possibilitar cliques (Mouse / Raycast)
            // Marcamos como Trigger para não ter conflito de colisão com o chão/paredes, 
            // evitando que o personagem ande travado ou trepidação física.
            Collider col = charObj.GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider cap = charObj.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.9f, 0f);
                cap.radius = 0.35f;
                cap.height = 1.8f;
                cap.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            // Configura o componente NPCWorker para interação de diálogo
            NPCWorker worker = charObj.GetComponent<NPCWorker>();
            if (worker == null)
            {
                worker = charObj.AddComponent<NPCWorker>();
                worker.workerName = charObj.name;
            }
        }
        Debug.Log($"[SKAI Setup] Total de {animatedCount} personagens animados e configurados como NPCs.");
    }

    private static void ConfigureWorkstations()
    {
        int wsCount = 0;
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;
            string nameLower = go.name.ToLower();
            if (nameLower.Contains("workstation") || nameLower.Contains("machine") || 
                nameLower.Contains("maquina") || nameLower.Contains("torno") || 
                nameLower.Contains("fresadora") || nameLower.Contains("prensa"))
            {
                // Configura colisor de gatilho principal
                BoxCollider boxCol = go.GetComponent<BoxCollider>();
                if (boxCol == null)
                {
                    Collider existingCol = go.GetComponent<Collider>();
                    if (existingCol != null) DestroyImmediate(existingCol);
                    boxCol = go.AddComponent<BoxCollider>();
                }
                boxCol.isTrigger = true;
                boxCol.size = new Vector3(1.6f, 1.6f, 1.6f);

                // Configura barreira física como filha
                Transform barrierTrans = go.transform.Find("SKAI_Workstation_Barrier");
                if (barrierTrans != null)
                {
                    DestroyImmediate(barrierTrans.gameObject);
                }

                GameObject barrierObj = new GameObject("SKAI_Workstation_Barrier");
                barrierObj.transform.parent = go.transform;
                barrierObj.transform.localPosition = Vector3.zero;
                barrierObj.transform.localRotation = Quaternion.identity;
                barrierObj.transform.localScale = Vector3.one;

                BoxCollider barrierBox = barrierObj.AddComponent<BoxCollider>();
                barrierBox.isTrigger = false;
                barrierBox.size = new Vector3(1.2f, 1.2f, 1.2f);

                Workstation ws = go.GetComponent<Workstation>();
                if (ws == null) ws = go.AddComponent<Workstation>();

                ws.stationName = go.name;

                if (ws.outputPoint == null)
                {
                    Transform opt = go.transform.Find("OutputPoint");
                    if (opt == null)
                    {
                        GameObject optObj = new GameObject("OutputPoint");
                        optObj.transform.parent = go.transform;
                        optObj.transform.localPosition = new Vector3(0, 0, 1.8f);
                        opt = optObj.transform;
                    }
                    ws.outputPoint = opt;
                }

                wsCount++;
                ws.UpdateVisuals();
            }
        }
        Debug.Log($"[SKAI Setup] {wsCount} Workstations configuradas com barreiras físicas.");
    }

    private static void ConfigureConveyorsAndSpawning(GameObject itemPrefab)
    {
        List<GameObject> conveyors = new List<GameObject>();
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;
            string nameLower = go.name.ToLower();
            if (nameLower.Contains("conveyor") || nameLower.Contains("belt") || nameLower.Contains("esteira"))
            {
                conveyors.Add(go);
                
                ConveyorBelt belt = go.GetComponent<ConveyorBelt>();
                if (belt == null) belt = go.AddComponent<ConveyorBelt>();
                
                belt.speed = 1.2f;
                belt.direction = Vector3.forward;

                Collider col = go.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider box = go.AddComponent<BoxCollider>();
                    box.isTrigger = false;
                }
                else
                {
                    col.isTrigger = false;
                }
            }
        }

        Debug.Log($"[SKAI Setup] {conveyors.Count} esteiras físicas configuradas com ConveyorBelt.");

        Vector3 spawnPos = new Vector3(-8f, 1.5f, 0f);
        Vector3 sinkPos = new Vector3(8f, 1.0f, 0f);

        if (conveyors.Count > 0)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            GameObject startConveyor = conveyors[0];
            GameObject endConveyor = conveyors[0];

            foreach (GameObject c in conveyors)
            {
                if (c.transform.position.x < minX)
                {
                    minX = c.transform.position.x;
                    startConveyor = c;
                }
                if (c.transform.position.x > maxX)
                {
                    maxX = c.transform.position.x;
                    endConveyor = c;
                }
            }

            spawnPos = startConveyor.transform.position + Vector3.up * 0.8f;
            sinkPos = endConveyor.transform.position + Vector3.forward * 1.5f + Vector3.up * 0.2f;
        }

        GameObject spawnerObj = GameObject.Find("ItemSpawner") ?? GameObject.Find("Spawner") ?? new GameObject("ItemSpawner");
        spawnerObj.transform.position = spawnPos;
        ItemSpawner spawner = spawnerObj.GetComponent<ItemSpawner>();
        if (spawner == null) spawner = spawnerObj.AddComponent<ItemSpawner>();
        spawner.itemPrefab = itemPrefab;
        spawner.spawnInterval = 4.5f;

        GameObject sinkObj = GameObject.Find("ProductSink") ?? GameObject.Find("Sink") ?? new GameObject("ProductSink");
        sinkObj.transform.position = sinkPos;
        
        Collider sinkCol = sinkObj.GetComponent<Collider>();
        if (sinkCol == null) sinkCol = sinkObj.AddComponent<BoxCollider>();
        sinkCol.isTrigger = true;
        sinkObj.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

        ProductSink sink = sinkObj.GetComponent<ProductSink>();
        if (sink == null) sink = sinkObj.AddComponent<ProductSink>();

        Debug.Log($"[SKAI Setup] Spawner posicionado em {spawnPos} e Sink posicionado em {sinkPos}.");
    }

    private static void ConfigureVideoPlayerScreen()
    {
        GameObject screenObj = GameObject.Find("Telão_Sala_Estudo") ?? GameObject.Find("Telão") ?? GameObject.Find("Tela") ?? GameObject.Find("Screen") ?? GameObject.Find("TV");
        if (screenObj == null)
        {
            screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.name = "Telão_Sala_Estudo";
            screenObj.transform.position = new Vector3(0f, 2.5f, 10f);
            screenObj.transform.localScale = new Vector3(4.8f, 2.7f, 1f);
        }

        VideoPlayer vp = screenObj.GetComponent<VideoPlayer>();
        if (vp == null) vp = screenObj.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;

        VideoPlayerManager vpm = screenObj.GetComponent<VideoPlayerManager>();
        if (vpm == null) vpm = screenObj.AddComponent<VideoPlayerManager>();

        vpm.screenRenderer = screenObj.GetComponent<Renderer>();
        
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/video/Design sem nome.mp4");
        if (clip != null)
        {
            vpm.videoClip = clip;
            Debug.Log("[SKAI Setup] Telão configurado com vídeo local do pai: Assets/video/Design sem nome.mp4");
        }
        else
        {
            vpm.videoUrl = "https://archive.org/download/BigBuckBunny_328/BigBuckBunny_512kb.mp4";
            Debug.LogWarning("[SKAI Setup] Vídeo local não encontrado em Assets/video/Design sem nome.mp4. Usando URL da Web padrão.");
        }
        vpm.playOnStart = true;

        // Garante que o telão possui um colisor para receber cliques do jogador
        Collider col = screenObj.GetComponent<Collider>();
        if (col == null)
        {
            screenObj.AddComponent<MeshCollider>();
            Debug.Log("[SKAI Setup] Adicionado MeshCollider ao Telão.");
        }

        Debug.Log("[SKAI Setup] Telão de vídeo configurado.");
    }

    private static void ConfigureDialogueCanvas()
    {
        GameObject dialogueObj = GameObject.Find("SKAI_Dialogue_Canvas");
        if (dialogueObj != null && dialogueObj.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(dialogueObj);
            dialogueObj = null;
        }

        if (dialogueObj == null)
        {
            dialogueObj = new GameObject("SKAI_Dialogue_Canvas", typeof(RectTransform));
            Canvas canvas = dialogueObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            dialogueObj.AddComponent<CanvasScaler>();
            dialogueObj.AddComponent<GraphicRaycaster>();
            
            RectTransform rect = dialogueObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400, 300);
            dialogueObj.transform.localScale = new Vector3(0.003f, 0.003f, 1f);
        }
        else
        {
            // Limpa todos os filhos antigos para reconstruir do zero de forma limpa e evitar duplicidade
            for (int i = dialogueObj.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(dialogueObj.transform.GetChild(i).gameObject);
            }
        }

        Canvas dialogCanvas = dialogueObj.GetComponent<Canvas>();
        if (dialogCanvas != null)
        {
            dialogCanvas.worldCamera = FindOrCreateMainCamera();
        }

        DialogueCanvasManager dcm = dialogueObj.GetComponent<DialogueCanvasManager>();
        if (dcm == null) dcm = dialogueObj.AddComponent<DialogueCanvasManager>();

        // 1. Painel de fundo
        Transform panelTransform = dialogueObj.transform.Find("DialoguePanel");
        if (panelTransform != null && panelTransform.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(panelTransform.gameObject);
            panelTransform = null;
        }
        GameObject panelObj = panelTransform != null ? panelTransform.gameObject : new GameObject("DialoguePanel", typeof(RectTransform));
        panelObj.transform.SetParent(dialogueObj.transform, false);
        panelObj.transform.localPosition = Vector3.zero;
        panelObj.transform.localScale = Vector3.one;
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>() ?? panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 300);

        Image img = panelObj.GetComponent<Image>() ?? panelObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

        dcm.dialoguePanel = panelObj;

        // 2. Nome do Operário
        dcm.workerNameText = CreateTextElement(panelObj.transform, "WorkerName", new Vector2(0, 110), "Operário");
        dcm.workerNameText.fontSize = 20;
        dcm.workerNameText.alignment = TextAlignmentOptions.Center;
        dcm.workerNameText.color = new Color(0.9f, 0.7f, 0.1f);
        dcm.workerNameText.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 40);

        // 3. Corpo de Texto
        dcm.dialogueBodyText = CreateTextElement(panelObj.transform, "DialogueBody", new Vector2(0, 25), "Olá! Como posso ajudar?");
        dcm.dialogueBodyText.fontSize = 15;
        dcm.dialogueBodyText.alignment = TextAlignmentOptions.TopLeft;
        dcm.dialogueBodyText.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 100);

        // 4. Botões
        dcm.cycleTimeButton = CreateDialogueButton(panelObj.transform, "BtnCycleTime", new Vector2(-120, -70), "Tempo Ciclo", () => dcm.AskAboutCycleTime());
        dcm.qualityButton = CreateDialogueButton(panelObj.transform, "BtnQuality", new Vector2(0, -70), "Qualidade", () => dcm.AskAboutQuality());
        dcm.statusButton = CreateDialogueButton(panelObj.transform, "BtnStatus", new Vector2(120, -70), "Problemas", () => dcm.AskAboutStatus());
        dcm.closeButton = CreateDialogueButton(panelObj.transform, "BtnClose", new Vector2(0, -120), "Fechar Conversa", () => dcm.HideDialogue());
        
        dcm.closeButton.GetComponent<Image>().color = new Color(0.4f, 0.15f, 0.15f, 1f);

        System.Type pointableType = System.Type.GetType("Oculus.Interaction.PointableCanvas");
        if (pointableType != null && dialogueObj.GetComponent(pointableType) == null)
        {
            dialogueObj.AddComponent(pointableType);
        }

        Debug.Log("[SKAI Setup] Painel de Diálogo configurado.");
    }

    private static Button CreateDialogueButton(Transform parent, string name, Vector2 localPos, string labelText, UnityEngine.Events.UnityAction callback)
    {
        Transform child = parent.Find(name);
        if (child != null && child.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(child.gameObject);
            child = null;
        }
        GameObject btnObj = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        btnObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        btnObj.transform.localScale = Vector3.one;

        RectTransform rect = btnObj.GetComponent<RectTransform>() ?? btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(110, 35);

        Image img = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        Button btn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(callback);

        Transform txtChild = btnObj.transform.Find("Text");
        if (txtChild != null && txtChild.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(txtChild.gameObject);
            txtChild = null;
        }
        GameObject txtObj = txtChild != null ? txtChild.gameObject : new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        txtObj.transform.localPosition = Vector3.zero;
        txtObj.transform.localScale = Vector3.one;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>() ?? txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 11;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(100, 25);

        return btn;
    }

    private static void ConfigureUICanvas(UIManager ui, DisasterManager disaster, ScenarioUIController scenarioUI)
    {
        GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
        if (canvasObj != null && canvasObj.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(canvasObj);
            canvasObj = null;
        }

        bool isNewCanvas = false;
        if (canvasObj == null)
        {
            canvasObj = new GameObject("SKAI_KPI_Canvas", typeof(RectTransform));
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            isNewCanvas = true;
        }
        else
        {
            // Limpa todos os filhos antigos para reconstruir do zero de forma limpa e evitar duplicidade
            for (int i = canvasObj.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(canvasObj.transform.GetChild(i).gameObject);
            }
        }

        // Posiciona na Sala de Estudos, ao lado esquerdo do telão original apenas se for um Canvas novo
        if (isNewCanvas)
        {
            canvasObj.transform.position = new Vector3(88.6f, 2.2f, 45.28f);
            canvasObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Rotacionado 180 graus para ficar de frente para o player
            canvasObj.transform.localScale = new Vector3(0.004f, 0.004f, 1f);
        }
        
        RectTransform rect = canvasObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 600);

        Canvas kpiCanvas = canvasObj.GetComponent<Canvas>();
        if (kpiCanvas != null)
        {
            kpiCanvas.worldCamera = FindOrCreateMainCamera();
        }

        if (canvasObj.GetComponent<ScenarioUIController>() == null)
        {
            ScenarioUIController uiLink = canvasObj.AddComponent<ScenarioUIController>();
        }

        // Remove componente Image do Canvas raiz, se houver
        Image oldBg = canvasObj.GetComponent<Image>();
        if (oldBg != null) DestroyImmediate(oldBg);

        // 1. Criar Borda e Fundo Elegantes como filhos
        Transform borderTrans = canvasObj.transform.Find("SKAI_KPI_Border");
        GameObject borderObj = borderTrans != null ? borderTrans.gameObject : new GameObject("SKAI_KPI_Border", typeof(RectTransform));
        borderObj.transform.SetParent(canvasObj.transform, false);
        borderObj.transform.localPosition = Vector3.zero;
        borderObj.transform.localScale = Vector3.one;
        RectTransform borderRect = borderObj.GetComponent<RectTransform>() ?? borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = new Vector2(4, 4); // Borda de 2px ao redor
        Image borderImg = borderObj.GetComponent<Image>() ?? borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.2f, 0.22f, 0.28f, 0.8f);
        borderObj.transform.SetAsFirstSibling();

        Transform bgTrans = canvasObj.transform.Find("SKAI_KPI_Background");
        GameObject bgObj = bgTrans != null ? bgTrans.gameObject : new GameObject("SKAI_KPI_Background", typeof(RectTransform));
        bgObj.transform.SetParent(canvasObj.transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = Vector3.one;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>() ?? bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.GetComponent<Image>() ?? bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        bgObj.transform.SetSiblingIndex(1);

        // Título Principal do Painel
        CreateTextElement(canvasObj.transform, "MainTitle", new Vector2(0, 260), "<b>PAINEL KAIZEN - CONTROLE E DESAFIOS</b>", 800, 45, 22, TextAlignmentOptions.Center);

        // Divisor visual central (ajustado para bater exatamente com a altura dos cards)
        Transform dividerTrans = canvasObj.transform.Find("CentralDivider");
        GameObject dividerObj = dividerTrans != null ? dividerTrans.gameObject : new GameObject("CentralDivider", typeof(RectTransform));
        dividerObj.transform.SetParent(canvasObj.transform, false);
        dividerObj.transform.localPosition = new Vector3(0, 47.5f, 0);
        RectTransform divRect = dividerObj.GetComponent<RectTransform>() ?? dividerObj.AddComponent<RectTransform>();
        divRect.sizeDelta = new Vector2(2, 290);
        Image divImg = dividerObj.GetComponent<Image>() ?? dividerObj.AddComponent<Image>();
        divImg.color = new Color(0.25f, 0.25f, 0.3f, 0.35f);

        // Criar textos TMPro em Cards (Lado Esquerdo - KPIs)
        ui.productivityText = CreateKPICard(canvasObj.transform, "ProductivityText", new Vector2(-220, 160), "Produtividade: -- u/min", 380, 65, 15);
        ui.leadTimeText = CreateKPICard(canvasObj.transform, "LeadTimeText", new Vector2(-220, 85), "Lead Time: -- seg", 380, 65, 15);
        ui.totalItemsText = CreateKPICard(canvasObj.transform, "TotalItemsText", new Vector2(-220, 10), "Total Produzido: 0", 380, 65, 15);
        ui.qualityText = CreateKPICard(canvasObj.transform, "QualityText", new Vector2(-220, -65), "Qualidade: 100%", 380, 65, 15);

        // Criar textos TMPro em Cards (Lado Direito - Instruções e Alertas)
        ui.alertText = CreateKPICard(canvasObj.transform, "AlertText", new Vector2(220, 155), "<color=green>[OK] OPERANDO NORMAL</color>", 380, 75, 14);
        disaster.scenarioText = CreateKPICard(canvasObj.transform, "ScenarioText", new Vector2(220, 10), "Carregando cenário...", 380, 180, 13);
        
        // Painel de vitória (escondido por padrão, cobre todo o Canvas)
        Transform winBgTrans = canvasObj.transform.Find("WinStatusBackground");
        GameObject winBgObj = winBgTrans != null ? winBgTrans.gameObject : new GameObject("WinStatusBackground", typeof(RectTransform));
        winBgObj.transform.SetParent(canvasObj.transform, false);
        winBgObj.transform.localPosition = new Vector3(0, 0, -1);
        RectTransform winBgRect = winBgObj.GetComponent<RectTransform>() ?? winBgObj.AddComponent<RectTransform>();
        winBgRect.anchorMin = Vector2.zero;
        winBgRect.anchorMax = Vector2.one;
        winBgRect.sizeDelta = Vector2.zero; // Fullscreen stretch!
        
        Image winBgImg = winBgObj.GetComponent<Image>() ?? winBgObj.AddComponent<Image>();
        winBgImg.color = new Color(0.04f, 0.08f, 0.12f, 0.98f); // Fundo azul escuro premium
        winBgObj.SetActive(false);

        // Relatório final (filho do painel de vitória) com margem
        disaster.winStatusText = CreateTextElement(winBgObj.transform, "WinStatusText", new Vector2(0, 0), "", 820, 500, 15, TextAlignmentOptions.TopLeft);
        RectTransform winTextRect = disaster.winStatusText.GetComponent<RectTransform>();
        winTextRect.anchorMin = Vector2.zero;
        winTextRect.anchorMax = Vector2.one;
        winTextRect.offsetMin = new Vector2(40, 100); // Sobra 100px embaixo para o botão
        winTextRect.offsetMax = new Vector2(-40, -40);

        // Criar botão de voltar na tela de vitória (filho de WinStatusBackground)
        CreateButton(winBgObj.transform, "BtnDismissWin", new Vector2(0, -230), "Voltar ao Painel", () => {
            winBgObj.SetActive(false);
            if (DisasterManager.Instance != null) DisasterManager.Instance.TeleportPlayerToStudyRoom();
        });

        // Criar Painel e Botões para Seleção de Cenários e Perfis
        CreateScenarioSelectionPanel(canvasObj.transform, scenarioUI);

        System.Type pointableType = System.Type.GetType("Oculus.Interaction.PointableCanvas");
        if (pointableType != null && canvasObj.GetComponent(pointableType) == null)
        {
            canvasObj.AddComponent(pointableType);
        }

        Debug.Log("[SKAI Setup] Canvas com indicadores e botões configurado.");
    }

    private static TextMeshProUGUI CreateKPICard(Transform parent, string name, Vector2 localPos, string defaultText, float width, float height, int fontSize)
    {
        // 1. Encontra ou cria o GameObject do Card
        Transform cardTrans = parent.Find(name + "_Card");
        if (cardTrans != null && cardTrans.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(cardTrans.gameObject);
            cardTrans = null;
        }
        GameObject cardObj = cardTrans != null ? cardTrans.gameObject : new GameObject(name + "_Card", typeof(RectTransform));
        cardObj.transform.SetParent(parent, false);
        cardObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        cardObj.transform.localScale = Vector3.one;

        RectTransform cardRect = cardObj.GetComponent<RectTransform>() ?? cardObj.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(width, height);

        // Adiciona fundo ao Card
        Image img = cardObj.GetComponent<Image>() ?? cardObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.15f, 0.85f); // Cartão escuro

        // Adiciona um contorno de destaque sutil para o card
        Outline outline = cardObj.GetComponent<Outline>() ?? cardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.22f, 0.28f, 0.5f);
        outline.effectDistance = new Vector2(1, 1);

        // 2. Cria o texto como filho do Card
        Transform txtChild = cardObj.transform.Find("Text");
        if (txtChild != null && txtChild.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(txtChild.gameObject);
            txtChild = null;
        }
        GameObject txtObj = txtChild != null ? txtChild.gameObject : new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(cardObj.transform, false);
        txtObj.transform.localPosition = Vector3.zero;
        txtObj.transform.localScale = Vector3.one;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>() ?? txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(15, 5); // Padding
        txtRect.offsetMax = new Vector2(-15, -5);

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>() ?? txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        return tmp;
    }

    private static TextMeshProUGUI CreateTextElement(Transform parent, string name, Vector2 localPos, string defaultText, float width = 400f, float height = 70f, int fontSize = 24, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        Transform child = parent.Find(name);
        if (child != null && child.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(child.gameObject);
            child = null;
        }
        GameObject txtObj = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        txtObj.transform.SetParent(parent, false);
        txtObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        txtObj.transform.localScale = Vector3.one;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>() ?? txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        
        RectTransform rect = txtObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        return tmp;
    }

    private static void CreateScenarioSelectionPanel(Transform parent, ScenarioUIController scenarioUI)
    {
        // 1. Painel de Cenários (Alinhamento central inferior)
        TextMeshProUGUI labelScen = CreateTextElement(parent, "ScenarioPanelLabel", new Vector2(0, -120), "<b>ESCOLHER TREINAMENTO KAIZEN</b>", 800, 30, 14, TextAlignmentOptions.Center);

        CreateButton(parent, "BtnFreePlay", new Vector2(-270, -170), "Simul. Livre", () => scenarioUI.SelectFreePlay());
        CreateButton(parent, "BtnTPM", new Vector2(-90, -170), "Desafio TPM", () => scenarioUI.SelectTPM());
        CreateButton(parent, "BtnJidoka", new Vector2(90, -170), "Qualidade Jidoka", () => scenarioUI.SelectJidoka());
        CreateButton(parent, "BtnBalance", new Vector2(270, -170), "Balanceamento", () => scenarioUI.SelectBalanceamento());

        // 2. Painel de Perfis (Alinhamento central inferior)
        TextMeshProUGUI labelProf = CreateTextElement(parent, "ProfilePanelLabel", new Vector2(0, -215), "<b>SETOR INDUSTRIAL (IA)</b>", 800, 30, 14, TextAlignmentOptions.Center);

        CreateButton(parent, "BtnProfileMetal", new Vector2(-120, -260), "Metalúrgica", () => scenarioUI.SelectProfileMetalurgica());
        CreateButton(parent, "BtnProfileElec", new Vector2(120, -260), "Eletrônica SMD", () => scenarioUI.SelectProfileEletronicos());
    }

    private static void CreateButton(Transform parent, string name, Vector2 localPos, string labelText, UnityEngine.Events.UnityAction callback)
    {
        Transform child = parent.Find(name);
        if (child != null && child.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(child.gameObject);
            child = null;
        }
        GameObject btnObj = child != null ? child.gameObject : new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        btnObj.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        btnObj.transform.localScale = Vector3.one;

        RectTransform rect = btnObj.GetComponent<RectTransform>() ?? btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(170, 45);

        Image img = btnObj.GetComponent<Image>() ?? btnObj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 1f);

        Button btn = btnObj.GetComponent<Button>() ?? btnObj.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(callback);

        // Adiciona transição de coler (efeito hover)
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        cb.highlightedColor = new Color(0.25f, 0.28f, 0.38f, 1f); // Hover
        cb.pressedColor = new Color(0.1f, 0.11f, 0.14f, 1f);
        cb.selectedColor = new Color(0.2f, 0.22f, 0.3f, 1f);
        btn.colors = cb;

        Transform txtChild = btnObj.transform.Find("Text");
        if (txtChild != null && txtChild.GetComponent<RectTransform>() == null)
        {
            DestroyImmediate(txtChild.gameObject);
            txtChild = null;
        }
        GameObject txtObj = txtChild != null ? txtChild.gameObject : new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        txtObj.transform.localPosition = Vector3.zero;
        txtObj.transform.localScale = Vector3.one;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>() ?? txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = labelText;
        tmp.fontSize = 13;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.sizeDelta = new Vector2(160, 35);
    }

    private static void ConfigureOrCreatePlayer()
    {
        Debug.Log("[SKAI Setup] Configurando ou criando Player...");

        // 1. Encontra ou cria a câmera principal
        Camera mainCam = FindOrCreateMainCamera();
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera";
            mainCam = camObj.GetComponent<Camera>();
            Debug.Log("[SKAI Setup] Nenhuma câmera encontrada. Criada 'Main Camera' do zero.");
        }

        // Garante que o GameObject da Câmera e o componente Camera estão ativos e renderizando na tela
        mainCam.gameObject.SetActive(true);
        mainCam.enabled = true;
        mainCam.targetTexture = null;
        mainCam.targetDisplay = 0; // Display 1

        // Configura propriedades de URP adicionais por reflexão para evitar erros de compilação sem referências de URP
        Component additionalData = mainCam.GetComponent("UniversalAdditionalCameraData");
        if (additionalData != null)
        {
            var renderTypeProp = additionalData.GetType().GetProperty("renderType");
            if (renderTypeProp != null)
            {
                System.Type renderTypeEnum = additionalData.GetType().Assembly.GetType("UnityEngine.Rendering.Universal.CameraRenderType");
                if (renderTypeEnum != null)
                {
                    var baseValue = System.Enum.Parse(renderTypeEnum, "Base");
                    renderTypeProp.SetValue(additionalData, baseValue);
                }
            }

            var xrRenderingProp = additionalData.GetType().GetProperty("xrRendering");
            if (xrRenderingProp != null)
            {
                xrRenderingProp.SetValue(additionalData, false);
                Debug.Log("[SKAI Setup] xrRendering desativado na câmera principal para exibição no PC.");
            }
        }

        // 2. Encontra ou cria o Player Root
        GameObject playerRoot = GameObject.Find("SKAI_Player");
        if (playerRoot == null)
        {
            playerRoot = GameObject.Find("Player");
        }

        // Se a câmera tem um pai que parece um VR Rig (ex: XR Origin ou OVRCameraRig), usa ele
        if (playerRoot == null && mainCam.transform.parent != null)
        {
            playerRoot = mainCam.transform.parent.gameObject;
            Debug.Log($"[SKAI Setup] Usando o pai da câmera '{playerRoot.name}' como Player Root.");
        }

        // Se não achou nenhum Player Root, cria um novo "SKAI_Player"
        bool isNewPlayer = false;
        if (playerRoot == null)
        {
            playerRoot = new GameObject("SKAI_Player");
            Debug.Log("[SKAI Setup] Criado novo GameObject 'SKAI_Player'.");
            isNewPlayer = true;
        }

        // Posiciona o jogador sempre na Sala de Estudos ao rodar o Setup apenas se for um player novo
        if (isNewPlayer)
        {
            Vector3 studyRoomPos = new Vector3(93.37f, 0.5f, 41f);
            GameObject screenObj = GameObject.Find("Telão_Sala_Estudo") ?? GameObject.Find("telão") ?? GameObject.Find("Telão") ?? GameObject.Find("Tela") ?? GameObject.Find("Screen") ?? GameObject.Find("TV");
            if (screenObj != null)
            {
                studyRoomPos = screenObj.transform.position - screenObj.transform.forward * 4.5f;
                studyRoomPos.y = 0.5f;
            }
            
            playerRoot.transform.position = studyRoomPos;
            if (screenObj != null)
            {
                Vector3 lookTarget = screenObj.transform.position;
                lookTarget.y = studyRoomPos.y;
                playerRoot.transform.rotation = Quaternion.LookRotation(lookTarget - studyRoomPos);
            }
            else
            {
                playerRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
            Debug.Log($"[SKAI Setup] Posicionado Player na Sala de Estudos em {studyRoomPos}.");
        }

        // Garante que o Player Root está ativo
        playerRoot.SetActive(true);

        // Remove scripts ausentes/quebrados do Player Root para limpeza geral
        int playerRemovedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(playerRoot);
        if (playerRemovedCount > 0)
        {
            Debug.Log($"[SKAI Setup] Removidos {playerRemovedCount} scripts ausentes (Missing MonoBehaviour) do Player Root.");
        }

        // Configura CharacterController no Player Root
        CharacterController charCtrl = playerRoot.GetComponent<CharacterController>();
        if (charCtrl == null)
        {
            charCtrl = playerRoot.AddComponent<CharacterController>();
        }
        charCtrl.center = new Vector3(0f, 0.9f, 0f);
        charCtrl.radius = 0.3f;
        charCtrl.height = 1.8f;

        // Configura PG_Player no Player Root
        PG_Player pgPlayer = playerRoot.GetComponent<PG_Player>();
        if (pgPlayer == null)
        {
            pgPlayer = playerRoot.AddComponent<PG_Player>();
        }
        pgPlayer.enabled = true;

        // Cria ou encontra o objeto vazio "Head" no Player Root para ser a âncora da câmera
        Transform headTransform = playerRoot.transform.Find("Head");
        GameObject headObj;
        if (headTransform == null)
        {
            headObj = new GameObject("Head");
            headObj.transform.SetParent(playerRoot.transform, false);
            headObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            headObj.transform.localRotation = Quaternion.identity;
            Debug.Log("[SKAI Setup] Criado objeto vazio 'Head' a 1.65m de altura sob o Player Root.");
        }
        else
        {
            headObj = headTransform.gameObject;
            headObj.transform.localPosition = new Vector3(0f, 1.65f, 0f);
        }

        // 3. Garante que a câmera seja filha do Player Root (para compatibilidade estrutural)
        if (mainCam.transform.parent != playerRoot.transform)
        {
            mainCam.transform.SetParent(playerRoot.transform, true);
            Debug.Log($"[SKAI Setup] Câmera '{mainCam.name}' associada como filha do '{playerRoot.name}'.");
        }

        // 4. Limpa scripts quebrados na câmera
        int removedCount = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(mainCam.gameObject);
        if (removedCount > 0)
        {
            Debug.Log($"[SKAI Setup] Removidos {removedCount} scripts ausentes (Missing MonoBehaviour) da Main Camera.");
        }

        // Remove o antigo PCCameraController para evitar conflito de inputs e movimentação dupla
        PCCameraController oldPcCam = mainCam.gameObject.GetComponent<PCCameraController>();
        if (oldPcCam != null)
        {
            DestroyImmediate(oldPcCam);
            Debug.Log("[SKAI Setup] Removido PCCameraController legado da Main Camera.");
        }

        // 5. Adiciona os scripts de controle e cliques na câmera
        MouseInteraction interact = mainCam.gameObject.GetComponent<MouseInteraction>();
        if (interact == null) interact = mainCam.gameObject.AddComponent<MouseInteraction>();

        PG_FirstPersonCamera pgCam = mainCam.gameObject.GetComponent<PG_FirstPersonCamera>();
        if (pgCam == null) pgCam = mainCam.gameObject.AddComponent<PG_FirstPersonCamera>();

        // Associa as referências necessárias da câmera
        pgCam.characterBody = playerRoot.transform;
        pgCam.characterHead = headObj.transform;

        // Força ativar se estiverem desativados
        interact.enabled = true;
        pgCam.enabled = true;

        Debug.Log("[SKAI Setup] Player configurado com sucesso com PG_Player, PG_FirstPersonCamera e MouseInteraction.");

        ConfigureEventSystem();
    }

    private static void ConfigureEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = GameObject.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        if (eventSystem != null)
        {
            // Remove o StandaloneInputModule antigo se existir no modo New Input System
            var legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (legacyModule != null)
            {
                DestroyImmediate(legacyModule);
            }

            var newModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            Debug.Log("[SKAI Setup] EventSystem configurado com o modulo de input adequado.");
        }
    }

    private static Camera FindOrCreateMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                string nameLower = cam.gameObject.name.ToLower();
                if (nameLower.Contains("main") || nameLower.Contains("principal") || nameLower.Contains("eye") || nameLower.Contains("center") || nameLower.Contains("player") || nameLower.Contains("vr"))
                {
                    mainCam = cam;
                    break;
                }
            }

            if (mainCam == null && cameras.Length > 0)
            {
                mainCam = cameras[0];
            }

            if (mainCam != null)
            {
                mainCam.gameObject.tag = "MainCamera";
                Debug.Log($"[SKAI Fixer] Camera encontrada: '{mainCam.gameObject.name}'. Marcada com a tag 'MainCamera'.");
            }
        }
        return mainCam;
    }

    [MenuItem("SKAI/Dump Scene Hierarchy (Diagnostico)")]
    public static void DumpSceneHierarchy()
    {
        Debug.Log("[SKAI Diagnostics] Iniciando dump da hierarquia da cena...");
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var obj in rootObjects)
        {
            DumpGameObject(obj, 0);
        }
        Debug.Log("[SKAI Diagnostics] Dump concluido.");
    }

    private static void DumpGameObject(GameObject obj, int indent)
    {
        string indentStr = new string('-', indent * 2);
        string components = "";
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (comp != null)
                components += comp.GetType().Name + ", ";
        }
        Debug.Log($"{indentStr} [{obj.name}] Active: {obj.activeSelf} | Tag: {obj.tag} | Components: {components}");
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            DumpGameObject(obj.transform.GetChild(i).gameObject, indent + 1);
        }
    }

    private static void EnsureTagsExist()
    {
        var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManagerAsset != null && tagManagerAsset.Length > 0)
        {
            SerializedObject tagManager = new SerializedObject(tagManagerAsset[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            bool hasItemTag = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Item")
                {
                    hasItemTag = true;
                    break;
                }
            }

            if (!hasItemTag)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Item";
                tagManager.ApplyModifiedProperties();
                tagManager.Update();
                Debug.Log("[SKAI Setup] Tag 'Item' registrada com sucesso no Project Settings.");
            }
        }
    }

    [MenuItem("SKAI/Build Standalone Player")]
    public static void BuildGame()
    {
        Debug.Log("[SKAI Build] Iniciando Build do Jogo...");
        
        string scenePath = "Assets/UnityFactorySceneHDRP/Scene_Factory/FactorySceneSample.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        
        RunAutoSetup();
        
        EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[] {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = buildScenes;
        
        string buildPath = "Builds/SKAI_Simulador.exe";
        string buildFolder = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(buildFolder))
        {
            Directory.CreateDirectory(buildFolder);
        }
        
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new string[] { scenePath };
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;
        
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;
        
        if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[SKAI Build] Build concluída com sucesso! Salva em " + buildPath);
        }
        else
        {
            Debug.LogError("[SKAI Build] Falha na Build: " + summary.result);
        }
    }
}
