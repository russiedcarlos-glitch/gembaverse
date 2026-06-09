using UnityEngine;
using UnityEngine.UI;

public class ScenarioUIController : MonoBehaviour
{
    private void Start()
    {
        // Encontra os botões do Canvas de KPIs na cena e associa as funções correspondentes em tempo de execução
        AssociateButton("BtnFreePlay", SelectFreePlay);
        AssociateButton("BtnTPM", SelectTPM);
        AssociateButton("BtnJidoka", SelectJidoka);
        AssociateButton("BtnBalance", SelectBalanceamento);

        AssociateButton("BtnProfileMetal", SelectProfileMetalurgica);
        AssociateButton("BtnProfileElec", SelectProfileEletronicos);

        // Associa o botão de voltar na tela de vitória
        AssociateButton("BtnDismissWin", DismissWinScreen);

        // Inicializa as cores dos botões
        UpdateActiveButtonVisuals();
    }

    private void AssociateButton(string buttonName, UnityEngine.Events.UnityAction action)
    {
        // Tenta achar na cena geral ou sob o Canvas de KPIs
        GameObject btnObj = GameObject.Find(buttonName);
        if (btnObj == null)
        {
            GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
            if (canvasObj != null)
            {
                Transform t = canvasObj.transform.Find(buttonName);
                if (t == null)
                {
                    // Tenta achar dentro do painel de vitória
                    Transform winBg = canvasObj.transform.Find("WinStatusBackground");
                    if (winBg != null)
                    {
                        t = winBg.Find(buttonName);
                    }
                }
                if (t != null) btnObj = t.gameObject;
            }
        }

        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
                Debug.Log($"[ScenarioUI] Associado botão '{buttonName}' com sucesso no Start.");
            }
        }
    }

    public void DismissWinScreen()
    {
        GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
        if (canvasObj != null)
        {
            Transform winBg = canvasObj.transform.Find("WinStatusBackground");
            if (winBg != null)
            {
                winBg.gameObject.SetActive(false);
            }
        }
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.TeleportPlayerToStudyRoom();
        }
    }

    public void UpdateActiveButtonVisuals()
    {
        if (DisasterManager.Instance == null) return;

        // 1. Cenários
        SetButtonActiveVisual("BtnFreePlay", DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.FreePlay);
        SetButtonActiveVisual("BtnTPM", DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.TPM);
        SetButtonActiveVisual("BtnJidoka", DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Jidoka);
        SetButtonActiveVisual("BtnBalance", DisasterManager.Instance.activeScenario == DisasterManager.ScenarioType.Balanceamento);

        // 2. Perfis
        SetButtonActiveVisual("BtnProfileMetal", DisasterManager.Instance.currentProfile == DisasterManager.IndustryProfile.Metalurgica);
        SetButtonActiveVisual("BtnProfileElec", DisasterManager.Instance.currentProfile == DisasterManager.IndustryProfile.Eletronicos);
    }

    private void SetButtonActiveVisual(string buttonName, bool isActive)
    {
        GameObject canvasObj = GameObject.Find("SKAI_KPI_Canvas");
        if (canvasObj == null) return;

        Transform btnTrans = canvasObj.transform.Find(buttonName);
        if (btnTrans != null)
        {
            Image img = btnTrans.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? new Color(0.2f, 0.45f, 0.85f, 1f) : new Color(0.15f, 0.15f, 0.18f, 1f);
            }
        }
    }

    // Métodos que os botões do Canvas chamarão ao serem clicados em VR/PC
    
    public void SelectFreePlay()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.StartScenario(DisasterManager.ScenarioType.FreePlay);
            Debug.Log("[UI] Selecionado: Simulação Livre");
            UpdateActiveButtonVisuals();
        }
    }

    public void SelectTPM()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.StartScenario(DisasterManager.ScenarioType.TPM);
            Debug.Log("[UI] Selecionado: Cenário TPM (Quebras)");
            UpdateActiveButtonVisuals();
        }
    }

    public void SelectJidoka()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.StartScenario(DisasterManager.ScenarioType.Jidoka);
            Debug.Log("[UI] Selecionado: Cenário Jidoka (Qualidade)");
            UpdateActiveButtonVisuals();
        }
    }

    public void SelectBalanceamento()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.StartScenario(DisasterManager.ScenarioType.Balanceamento);
            Debug.Log("[UI] Selecionado: Cenário Balanceamento");
            UpdateActiveButtonVisuals();
        }
    }

    public void SelectProfileMetalurgica()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.currentProfile = DisasterManager.IndustryProfile.Metalurgica;
            DisasterManager.Instance.StartScenario(DisasterManager.Instance.activeScenario);
            Debug.Log("[UI] Perfil alterado para: Metalúrgica");
            UpdateActiveButtonVisuals();
        }
    }

    public void SelectProfileEletronicos()
    {
        if (DisasterManager.Instance != null)
        {
            DisasterManager.Instance.currentProfile = DisasterManager.IndustryProfile.Eletronicos;
            DisasterManager.Instance.StartScenario(DisasterManager.Instance.activeScenario);
            Debug.Log("[UI] Perfil alterado para: Eletrônicos");
            UpdateActiveButtonVisuals();
        }
    }
}
