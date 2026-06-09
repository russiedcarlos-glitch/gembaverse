using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerManager : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private RenderTexture videoRT;

    [Header("Configurações do Vídeo")]
    [Tooltip("Arraste o clipe de vídeo local (.mp4) para cá se preferir reproduzir localmente.")]
    public VideoClip videoClip;

    [Tooltip("Insira a URL direta do vídeo (.mp4). Exemplo padrão: Videoaula de Introdução ao Kaizen/Lean.")]
    public string videoUrl = "https://archive.org/download/BigBuckBunny_328/BigBuckBunny_512kb.mp4"; 

    [Header("Configurações do Telão")]
    [Tooltip("Arraste o objeto do Telão (que possui o MeshRenderer) para cá.")]
    public Renderer screenRenderer;

    [Tooltip("Índice do material do Telão que exibirá o vídeo (geralmente 0).")]
    public int materialIndex = 0;

    [Tooltip("Reproduzir o vídeo automaticamente assim que iniciar a cena.")]
    public bool playOnStart = true;

    [Header("Efeito de Brilho (Emissão)")]
    [Tooltip("Ativar brilho luminoso no telão para simular luz de tela no escuro.")]
    public bool enableScreenGlow = true;
    
    [Range(0.5f, 2.0f)]
    public float glowIntensity = 1.2f;

    private void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        
        // 1. Configura o player para usar clipe local ou URL da web
        if (videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;
        }
        videoPlayer.playOnAwake = false;

        // 2. Cria o Render Texture dinamicamente se o Renderer for associado
        if (screenRenderer != null)
        {
            // Cria uma textura de renderização 16:9 em alta definição
            videoRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            videoRT.name = "DynamicVideoRT_" + gameObject.name;
            videoRT.Create();

            // Direciona a saída do VideoPlayer para a textura dinâmica
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRT;

            // Aplica a textura no material URP Lit do telão
            Material mat = screenRenderer.materials[materialIndex];
            if (mat != null)
            {
                // Define a textura principal (URP usa _BaseMap em vez de _MainTex)
                if (mat.HasProperty("_BaseMap"))
                {
                    mat.SetTexture("_BaseMap", videoRT);
                }
                else if (mat.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", videoRT);
                }

                // Configura o telão para brilhar (Emissão URP)
                if (enableScreenGlow && mat.HasProperty("_EmissionMap"))
                {
                    mat.SetTexture("_EmissionMap", videoRT);
                    mat.SetColor("_EmissionColor", Color.white * glowIntensity);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
        else
        {
            Debug.LogWarning("VideoPlayerManager: Nenhum ScreenRenderer associado. Arraste o Telão no Inspector!");
        }

        // 3. Inicia a reprodução
        if (playOnStart)
        {
            PlayVideo();
        }
    }

    private void OnDestroy()
    {
        // Libera a memória da textura de renderização quando o objeto for destruído
        if (videoRT != null)
        {
            videoRT.Release();
            Destroy(videoRT);
        }
    }

    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    // Altera o vídeo em tempo real (útil para trocar de aula)
    public void ChangeVideoUrl(string newUrl)
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.url = newUrl;
            videoPlayer.Prepare();
            videoPlayer.Play();
        }
    }
}
