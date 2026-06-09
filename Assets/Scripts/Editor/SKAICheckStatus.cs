using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using UnityEngine.Splines;
using Unity.Factory.Sample;

public class SKAICheckStatus : EditorWindow
{
    [MenuItem("SKAI/Check Scene Status (Diagnosticar)")]
    public static void DiagnoseScene()
    {
        Debug.ClearDeveloperConsole();
        Debug.Log("==================================================");
        Debug.Log("📊 [SKAI DIAGNÓSTICO] Iniciando varredura da cena...");
        Debug.Log("==================================================");

        // 1. Verificar GameManager e Scripts Centrais
        GameObject gameManager = GameObject.Find("GameManager");
        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager não encontrado na hierarquia! Execute o 'SKAI > Automated Setup (One-Click)'.");
        }
        else
        {
            Debug.Log("✅ GameManager encontrado.");
            CheckComponent<KPIManager>(gameManager);
            CheckComponent<UIManager>(gameManager);
            CheckComponent<DisasterManager>(gameManager);
            CheckComponent<ScenarioUIController>(gameManager);
        }

        // 2. Verificar Spawner e Sink
        GameObject spawner = GameObject.Find("ItemSpawner") ?? GameObject.Find("Spawner");
        if (spawner == null)
        {
            Debug.LogError("❌ Spawner (ItemSpawner) não encontrado na cena!");
        }
        else
        {
            ItemSpawner isScript = spawner.GetComponent<ItemSpawner>();
            if (isScript == null) Debug.LogError("❌ Objeto Spawner encontrado, mas sem o script 'ItemSpawner'!");
            else if (isScript.itemPrefab == null) Debug.LogWarning("⚠️ Spawner está sem Prefab de Item associado!");
            else Debug.Log("✅ Spawner configurado com Prefab.");
        }

        GameObject sink = GameObject.Find("ProductSink") ?? GameObject.Find("Sink");
        if (sink == null)
        {
            Debug.LogError("❌ Sink (ProductSink) não encontrado na cena!");
        }
        else
        {
            CheckComponent<ProductSink>(sink);
        }

        // 3. Verificar Canvas de KPI e Diálogo
        GameObject kpiCanvas = GameObject.Find("SKAI_KPI_Canvas");
        if (kpiCanvas == null) Debug.LogError("❌ SKAI_KPI_Canvas não encontrado na cena!");
        else Debug.Log("✅ SKAI_KPI_Canvas encontrado.");

        GameObject dialogueCanvas = GameObject.Find("SKAI_Dialogue_Canvas");
        if (dialogueCanvas == null) Debug.LogError("❌ SKAI_Dialogue_Canvas (de diálogos dos NPCs) não encontrado na cena!");
        else Debug.Log("✅ SKAI_Dialogue_Canvas encontrado.");

        // 4. Verificar NPCs e Animação
        NPCWorker[] npcs = GameObject.FindObjectsByType<NPCWorker>(FindObjectsSortMode.None);
        Debug.Log($"👤 NPCs: Encontrados {npcs.Length} operários com componente 'NPCWorker' na cena.");
        
        int missingWorkstationOnNpc = 0;
        foreach (var npc in npcs)
        {
            if (npc.targetWorkstation == null) missingWorkstationOnNpc++;
        }
        if (missingWorkstationOnNpc > 0)
        {
            Debug.LogWarning($"⚠️ Existem {missingWorkstationOnNpc} NPCs sem máquina (targetWorkstation) associada!");
        }

        // 5. Verificar Splines e Movimentação (CustomSplineAnimate)
        CustomSplineAnimate[] splineAnimators = GameObject.FindObjectsByType<CustomSplineAnimate>(FindObjectsSortMode.None);
        Debug.Log($"🏃 Spline Animators: Encontrados {splineAnimators.Length} personagens com script 'CustomSplineAnimate' na cena.");
        
        SplineContainer[] containers = GameObject.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);
        Debug.Log($"🗺️ SplineContainers: Encontrados {containers.Length} caminhos de Spline na cena.");
        
        int missingSplineContainer = 0;
        foreach (var sa in splineAnimators)
        {
            // Acessa via reflexão ou serialização o campo privado m_SplineContainer
            SerializedObject so = new SerializedObject(sa);
            SerializedProperty sp = so.FindProperty("m_SplineContainer");
            if (sp == null || sp.objectReferenceValue == null)
            {
                missingSplineContainer++;
            }
        }
        if (missingSplineContainer > 0)
        {
            Debug.LogWarning($"⚠️ Existem {missingSplineContainer} personagens com 'CustomSplineAnimate' sem o caminho de Spline (SplineContainer) configurado!");
        }

        // 6. Verificar scripts na Câmera Principal e Player Root
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            int cameraMissing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(mainCam.gameObject);
            if (cameraMissing > 0)
            {
                Debug.LogWarning($"⚠️ A 'Main Camera' possui {cameraMissing} componentes com scripts ausentes (Missing MonoBehaviour).");
            }
            CheckComponent<MouseInteraction>(mainCam.gameObject);
            CheckComponent<PG_FirstPersonCamera>(mainCam.gameObject);
        }
        else
        {
            Debug.LogError("❌ Main Camera não encontrada na cena!");
        }

        GameObject playerRoot = GameObject.Find("SKAI_Player") ?? GameObject.Find("Player");
        if (playerRoot != null)
        {
            int playerMissing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(playerRoot);
            if (playerMissing > 0)
            {
                Debug.LogWarning($"⚠️ O '{playerRoot.name}' possui {playerMissing} componentes com scripts ausentes (Missing MonoBehaviour).");
            }
            
            if (playerRoot.GetComponent<CharacterController>() == null)
            {
                Debug.LogError($"❌ Script ausente no {playerRoot.name}: 'CharacterController'!");
            }
            else
            {
                Debug.Log($"✅ Componente 'CharacterController' está presente no {playerRoot.name}.");
            }

            CheckComponent<PG_Player>(playerRoot);
        }
        else
        {
            Debug.LogError("❌ Player Root (SKAI_Player) não encontrado na hierarquia!");
        }

        Debug.Log("==================================================");
        Debug.Log("📊 [SKAI DIAGNÓSTICO] Varredura concluída. Verifique os avisos acima.");
        Debug.Log("==================================================");
        
        EditorUtility.DisplayDialog("SKAI Diagnóstico", 
            $"Varredura concluída!\n\n" +
            $"- NPCs com NPCWorker: {npcs.Length}\n" +
            $"- Personagens com Spline Anim: {splineAnimators.Length}\n" +
            $"- Personagens sem Spline configurado: {missingSplineContainer}\n\n" +
            $"Veja os detalhes no Console do Editor.", "Fechar");
    }

    private static void CheckComponent<T>(GameObject go) where T : MonoBehaviour
    {
        if (go.GetComponent<T>() == null)
        {
            Debug.LogError($"❌ Script ausente no {go.name}: '{typeof(T).Name}'!");
        }
        else
        {
            Debug.Log($"✅ Script '{typeof(T).Name}' está presente no {go.name}.");
        }
    }

    [MenuItem("SKAI/Fix Spline References (Corrigir Splines)")]
    public static void FixSplineReferences()
    {
        CustomSplineAnimate[] splineAnimators = GameObject.FindObjectsByType<CustomSplineAnimate>(FindObjectsSortMode.None);
        SplineContainer[] containers = GameObject.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None);

        if (containers.Length == 0)
        {
            EditorUtility.DisplayDialog("SKAI Spline Fixer", "Nenhum SplineContainer (caminho de spline) encontrado na cena para associar!", "OK");
            return;
        }

        int fixedCount = 0;
        foreach (var sa in splineAnimators)
        {
            SerializedObject so = new SerializedObject(sa);
            SerializedProperty sp = so.FindProperty("m_SplineContainer");

            if (sp != null && sp.objectReferenceValue == null)
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
                    fixedCount++;
                }
            }
        }

        if (fixedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"🔧 [SKAI Spline Fixer] Sucesso: {fixedCount} referências de Spline re-associadas!");
            EditorUtility.DisplayDialog("SKAI Spline Fixer", $"Sucesso: {fixedCount} referências de Spline foram re-associadas automaticamente para os personagens mais próximos!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("SKAI Spline Fixer", "Todas as referências de Spline já estavam configuradas corretamente.", "OK");
        }
    }
}
