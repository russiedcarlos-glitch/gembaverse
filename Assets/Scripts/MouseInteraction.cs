using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MouseInteraction : MonoBehaviour
{
    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        bool clicked = false;
        Vector2 mousePosition = Vector2.zero;

        if (Mouse.current != null)
        {
            clicked = Mouse.current.leftButton.wasPressedThisFrame;
            mousePosition = Mouse.current.position.ReadValue();
        }

        if (clicked)
        {
            // Se o cursor estiver travado (modo primeira pessoa), a mira é o centro da tela.
            // Caso contrário (modo ponteiro livre), usamos a posição atual do cursor na tela.
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                InteractWithObject(new Vector2(Screen.width / 2f, Screen.height / 2f));
            }
            else
            {
                InteractWithObject(mousePosition);
            }
        }
    }

    private void InteractWithObject(Vector2 mousePos)
    {
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // 1. Tenta interagir primeiro com os Canvas de UI 3D na cena (KPI Canvas ou Dialogue Canvas)
        // para contornar problemas de compatibilidade do EventSystem com o New Input System
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.PointerEventData eventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            eventData.position = mousePos;

            System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult> results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            GraphicRaycaster[] raycasters = FindObjectsByType<GraphicRaycaster>(FindObjectsSortMode.None);
            foreach (var raycaster in raycasters)
            {
                results.Clear();
                raycaster.Raycast(eventData, results);
                if (results.Count > 0)
                {
                    foreach (var result in results)
                    {
                        Button button = result.gameObject.GetComponentInParent<Button>();
                        if (button != null && button.interactable && button.onClick != null)
                        {
                            button.onClick.Invoke();
                            Debug.Log($"[MouseInteraction] Clicou e acionou o botão da UI: {button.gameObject.name}");
                            return; // Consome o clique
                        }
                    }
                }
            }
        }

        // 2. Se não clicou em UI, faz o raycast físico para NPCs/Máquinas
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            // Procura primeiro por VideoPlayerManager ou VideoPlayer (Interação com o Telão)
            VideoPlayerManager vpm = hit.collider.GetComponentInParent<VideoPlayerManager>();
            if (vpm == null) vpm = hit.collider.GetComponent<VideoPlayerManager>();
            if (vpm != null)
            {
                var vp = vpm.GetComponent<UnityEngine.Video.VideoPlayer>();
                if (vp != null)
                {
                    if (vp.isPlaying)
                    {
                        vpm.PauseVideo();
                        Debug.Log("[VideoPlayer] Vídeo pausado pelo clique do jogador.");
                    }
                    else
                    {
                        vpm.PlayVideo();
                        Debug.Log("[VideoPlayer] Vídeo iniciado pelo clique do jogador.");
                    }
                }
                return;
            }

            // Procura por NPCWorker
            NPCWorker npc = hit.collider.GetComponentInParent<NPCWorker>();
            if (npc == null) npc = hit.collider.GetComponent<NPCWorker>();

            if (npc != null)
            {
                if (DialogueCanvasManager.Instance != null)
                {
                    DialogueCanvasManager.Instance.ShowDialogue(npc);
                }
                return;
            }

            // Procura o componente Workstation no objeto clicado ou em seus pais
            Workstation ws = hit.collider.GetComponentInParent<Workstation>();
            if (ws == null) ws = hit.collider.GetComponent<Workstation>();

            if (ws != null)
            {
                // Ação 1: Se a máquina estiver quebrada (Cenário TPM), conserta ela
                if (ws.isBroken)
                {
                    ws.Repair();
                    if (DisasterManager.Instance != null)
                    {
                        DisasterManager.Instance.RegisterRepair();
                    }
                    return;
                }

                // Ação 2: Se a máquina estiver descalibrada (Cenário Jidoka), recalibra ela
                if (ws.defectRate > 0f)
                {
                    ws.Calibrate();
                    if (DisasterManager.Instance != null)
                    {
                        DisasterManager.Instance.RegisterPlayerAction();
                    }
                    return;
                }

                // Ação 3: Se for Cenário de Balanceamento, clica para alternar o tempo de ciclo (0.5s -> 2.0s -> 6.0s)
                if (DisasterManager.Instance != null && DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Balanceamento)
                {
                    float nextTime = 2.0f;

                    if (Mathf.Abs(ws.processingTime - 0.5f) < 0.1f)
                    {
                        nextTime = 2.0f;
                    }
                    else if (Mathf.Abs(ws.processingTime - 2.0f) < 0.1f)
                    {
                        nextTime = 6.0f;
                    }
                    else
                    {
                        nextTime = 0.5f;
                    }

                    ws.processingTime = nextTime;
                    DisasterManager.Instance.RegisterPlayerAction();
                    Debug.Log($"[Kaizen] Tempo de processamento de {ws.stationName} alterado para {nextTime}s.");
                    
                    // Atualiza o nome para o jogador ver a velocidade atual no Inspector ou UI
                    string speedLabel = nextTime == 2.0f ? "Equilibrada" : (nextTime == 0.5f ? "Rápida" : "Lenta");
                    ws.stationName = $"{ws.gameObject.name} ({speedLabel})";
                    return;
                }
            }
        }
    }
}
