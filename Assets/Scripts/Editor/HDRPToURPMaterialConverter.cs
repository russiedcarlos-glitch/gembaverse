using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class HDRPToURPMaterialConverter : EditorWindow
{
    private Vector2 scrollPos;
    private string logOutput = "Pronto para reparar a cena!";

    [MenuItem("SKAI/URP Scene Repair Tool")]
    public static void ShowWindow()
    {
        GetWindow<HDRPToURPMaterialConverter>("SKAI URP Repair");
    }

    private void OnGUI()
    {
        GUILayout.Label("SKAI - Simulador de Kaizen Imersivo", EditorStyles.boldLabel);
        GUILayout.Label("Ferramenta de Reparo de Iluminação e Materiais (HDRP -> URP)", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // Painel de Status
        GUILayout.BeginVertical("box");
        GUILayout.Label("Status da Cena Ativa:", EditorStyles.boldLabel);
        GUILayout.Label("Cena: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        
        int lightCount = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
        int rendererCount = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
        
        GUILayout.Label($"Luzes na Cena: {lightCount}");
        GUILayout.Label($"Renderers na Cena: {rendererCount}");
        GUILayout.EndVertical();

        EditorGUILayout.Space();

        // Botões de Ação
        if (GUILayout.Button("Reparar TUDO de uma vez (Recomendado)", GUILayout.Height(40)))
        {
            RepararTudo();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Ações Individuais:", EditorStyles.boldLabel);

        if (GUILayout.Button("1. Limpar Lightmaps Gravados (Resolve Chão Branco)"))
        {
            LimparLightmaps();
        }

        if (GUILayout.Button("2. Converter Materiais da CENA (Deep Scan)"))
        {
            ConverterMateriaisDaCena();
        }

        if (GUILayout.Button("3. Converter Materiais da pasta Assets/UnityFactorySceneHDRP"))
        {
            ConvertMaterials();
        }

        if (GUILayout.Button("4. Corrigir Luzes, Nevoeiro e Volumes URP"))
        {
            FixLighting();
        }

        if (GUILayout.Button("5. Corrigir Objetos Muito Escuros / Pretos"))
        {
            CorrigirObjetosEscuros();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Logs de Execução:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(logOutput, GUILayout.Height(150));

        EditorGUILayout.EndScrollView();
    }

    private void Log(string message)
    {
        logOutput = $"[{System.DateTime.Now.ToString("HH:mm:ss")}] {message}\n" + logOutput;
        Debug.Log(message);
    }

    private void RepararTudo()
    {
        Log("Iniciando reparo completo da cena...");
        LimparLightmaps();
        ConverterMateriaisDaCena();
        ConvertMaterials();
        FixLighting();
        CorrigirObjetosEscuros();
        Log("Reparo completo finalizado com sucesso!");
        EditorUtility.DisplayDialog("Sucesso", "Cena reparada com sucesso! Verifique a viewport.", "OK");
    }

    private void LimparLightmaps()
    {
        Log("Limpando dados de iluminação gravados (Lightmaps / GI)...");
        Lightmapping.Clear();
        Lightmapping.ClearLightingDataAsset();
        Log("Lightmaps antigos e cache de GI foram limpos com sucesso! Isso resolve o chão branco estourado.");
    }

    private void ConverterMateriaisDaCena()
    {
        Log("Escaneando todos os renderers na cena ativa...");
        Renderer[] renderers = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        
        if (urpLitShader == null)
        {
            Log("ERRO: Shader URP Lit não encontrado!");
            return;
        }

        int convertedCount = 0;
        HashSet<Material> convertedMaterials = new HashSet<Material>();

        foreach (Renderer r in renderers)
        {
            Material[] sharedMaterials = r.sharedMaterials;
            bool modified = false;

            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material mat = sharedMaterials[i];
                if (mat == null || convertedMaterials.Contains(mat)) continue;

                // Se o shader não for URP ou Standard compatível
                if (mat.shader != urpLitShader && (mat.shader.name.Contains("HDRP") || mat.shader.name.Contains("Standard") || mat.shader.name.Contains("Layered")))
                {
                    Log($"Convertendo material da cena: {mat.name} (Shader antigo: {mat.shader.name})");
                    ConverterMaterialUnico(mat, urpLitShader);
                    convertedMaterials.Add(mat);
                    convertedCount++;
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(r);
            }
        }

        AssetDatabase.SaveAssets();
        Log($"Deep Scan concluído! {convertedCount} materiais da cena foram convertidos para URP.");
    }

    [MenuItem("SKAI/Convert HDRP Factory to URP")]
    public static void ConvertMaterials()
    {
        string path = "Assets/UnityFactorySceneHDRP";
        if (!Directory.Exists(path))
        {
            Debug.LogError("Pasta Assets/UnityFactorySceneHDRP nao encontrada!");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { path });
        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) return;

        int convertedCount = 0;
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (mat != null && mat.shader != urpLitShader)
            {
                ConverterMaterialUnico(mat, urpLitShader);
                convertedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Sucesso! {convertedCount} materiais convertidos da pasta Assets.");
    }

    private static void ConverterMaterialUnico(Material mat, Shader urpLitShader)
    {
        // Coleciona propriedades antes de resetar o shader
        Texture baseColorMap = null;
        if (mat.HasProperty("_BaseColorMap")) baseColorMap = mat.GetTexture("_BaseColorMap");
        else if (mat.HasProperty("_BaseMap")) baseColorMap = mat.GetTexture("_BaseMap");
        else if (mat.HasProperty("_MainTex")) baseColorMap = mat.GetTexture("_MainTex");

        Texture normalMap = null;
        if (mat.HasProperty("_NormalMap")) normalMap = mat.GetTexture("_NormalMap");
        else if (mat.HasProperty("_BumpMap")) normalMap = mat.GetTexture("_BumpMap");

        Texture maskMap = null;
        if (mat.HasProperty("_MaskMap")) maskMap = mat.GetTexture("_MaskMap");
        else if (mat.HasProperty("_MetallicGlossMap")) maskMap = mat.GetTexture("_MetallicGlossMap");

        Color baseColor = Color.white;
        if (mat.HasProperty("_BaseColor")) baseColor = mat.GetColor("_BaseColor");
        else if (mat.HasProperty("_Color")) baseColor = mat.GetColor("_Color");

        // Tratamento especial para Emissão (Evita estourar o URP)
        Color emissiveColor = Color.clear;
        Texture emissiveMap = null;
        if (mat.HasProperty("_EmissiveColor")) emissiveColor = mat.GetColor("_EmissiveColor");
        else if (mat.HasProperty("_EmissionColor")) emissiveColor = mat.GetColor("_EmissionColor");

        if (mat.HasProperty("_EmissiveColorMap")) emissiveMap = mat.GetTexture("_EmissiveColorMap");
        else if (mat.HasProperty("_EmissionMap")) emissiveMap = mat.GetTexture("_EmissionMap");

        // Aplica o novo shader URP
        mat.shader = urpLitShader;

        // Reaplica propriedades no formato URP
        if (baseColorMap != null) mat.SetTexture("_BaseMap", baseColorMap);
        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.EnableKeyword("_NORMALMAP");
        }
        if (maskMap != null)
        {
            mat.SetTexture("_MetallicGlossMap", maskMap);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        // Normalização da cor base (evita valores HDR fora da escala)
        baseColor.r = Mathf.Clamp01(baseColor.r);
        baseColor.g = Mathf.Clamp01(baseColor.g);
        baseColor.b = Mathf.Clamp01(baseColor.b);
        mat.SetColor("_BaseColor", baseColor);

        // Clampa a Emissão para não estourar
        if (emissiveColor != Color.clear && (emissiveColor.r > 0.01f || emissiveColor.g > 0.01f || emissiveColor.b > 0.01f))
        {
            // Reduz o brilho HDRP físico (Lux/Nits) para URP
            float maxVal = Mathf.Max(emissiveColor.r, emissiveColor.g, emissiveColor.b);
            if (maxVal > 1.5f)
            {
                emissiveColor /= maxVal; // Normaliza o vetor de cor
                emissiveColor *= 1.2f;   // Define brilho controlado e agradável
            }
            mat.SetColor("_EmissionColor", emissiveColor);
            if (emissiveMap != null) mat.SetTexture("_EmissionMap", emissiveMap);
            mat.EnableKeyword("_EMISSION");
        }
        else
        {
            mat.DisableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(mat);
    }

    [MenuItem("SKAI/Fix Scene URP Lighting")]
    public static void FixLighting()
    {
        Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
        int fixedLightsCount = 0;
        int shadowsDisabledCount = 0;

        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 1.0f; // Sol suave para URP
                light.color = new Color(0.95f, 0.95f, 0.9f);
                light.shadows = LightShadows.Soft; // Sol mantém sombras suaves
                fixedLightsCount++;
            }
            else if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                // Desativa sombras em luzes adicionais (essencial para VR e remove os avisos de Shadow Atlas)
                if (light.shadows != LightShadows.None)
                {
                    light.shadows = LightShadows.None;
                    shadowsDisabledCount++;
                }

                // Clampa intensidades altíssimas do HDRP
                if (light.intensity > 15f)
                {
                    light.intensity = Mathf.Clamp(light.intensity / 200f, 0.2f, 1.5f);
                }
                fixedLightsCount++;
            }
        }

        // 1. Desativa Nevoeiro lavado
        RenderSettings.fog = false;

        // 2. Configura Luz Ambiente Escura Industrial
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.21f); // Tom de cinza azulado profissional
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.reflectionIntensity = 0.9f;

        // Remove skybox incompatível que clareia tudo artificialmente
        RenderSettings.skybox = null;

        // 3. Desativa volumes HDRP e objetos de Sky/Fog
        string[] volumeNames = { "SceneSettings", "Global Volume", "HDRP Volume", "Sky and Fog Volume", "Sky", "PostProcessVolume", "Skybox" };
        foreach (string name in volumeNames)
        {
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        foreach (GameObject go in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.name.Contains("Volume") || go.name.Contains("Fog") || go.name.Contains("Sky"))
            {
                go.SetActive(false);
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"Sucesso! {fixedLightsCount} luzes e iluminação ambiente corrigidas. Sombras desativadas em {shadowsDisabledCount} luzes secundárias.");
    }

    private void CorrigirObjetosEscuros()
    {
        Log("Corrigindo objetos escuros/pretos na cena...");
        Renderer[] renderers = GameObject.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                // Se a cor base for preta pura e não tiver textura, ou se a suavidade estiver em 1.0
                if (mat.HasProperty("_BaseColor"))
                {
                    Color col = mat.GetColor("_BaseColor");
                    if (col.r < 0.05f && col.g < 0.05f && col.b < 0.05f && mat.GetTexture("_BaseMap") == null)
                    {
                        // Provavelmente perdeu a textura ou está quebrado. Mudamos para cinza industrial.
                        mat.SetColor("_BaseColor", new Color(0.4f, 0.4f, 0.4f));
                        fixedCount++;
                    }
                }

                // Smoothness muito alta em URP sem Reflection Probes faz o objeto ficar preto
                if (mat.HasProperty("_Smoothness"))
                {
                    float sm = mat.GetFloat("_Smoothness");
                    if (sm > 0.9f)
                    {
                        mat.SetFloat("_Smoothness", 0.4f); // Reduz brilho para fosco realista
                        fixedCount++;
                    }
                }
            }
        }

        Log($"Correção concluída! {fixedCount} propriedades de materiais ajustadas para evitar silhuetas pretas.");
    }
}

