using UnityEngine;

public class Item : MonoBehaviour
{
    public float SpawnTime { get; private set; }
    
    private bool _isDefective = false;
    public bool isDefective
    {
        get => _isDefective;
        set
        {
            _isDefective = value;
            UpdateVisuals();
        }
    }

    private void Awake()
    {
        SpawnTime = Time.time;
    }

    private void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            if (_isDefective)
            {
                // Vermelho brilhante/opaco para defeituosa
                r.material.color = new Color(0.9f, 0.15f, 0.15f);
                if (r.material.HasProperty("_EmissionColor"))
                {
                    r.material.SetColor("_EmissionColor", new Color(0.8f, 0.1f, 0.1f));
                    r.material.EnableKeyword("_EMISSION");
                }
            }
            else
            {
                // Se não for defeituosa, usa a cor base dependendo do perfil do DisasterManager
                if (DisasterManager.Instance != null)
                {
                    if (DisasterManager.Instance.currentProfile == DisasterManager.IndustryProfile.Metalurgica)
                    {
                        // Cinza metálico brilhante (Aço)
                        r.material.color = new Color(0.7f, 0.73f, 0.78f);
                        if (r.material.HasProperty("_Metallic")) r.material.SetFloat("_Metallic", 0.9f);
                        if (r.material.HasProperty("_Glossiness")) r.material.SetFloat("_Glossiness", 0.7f);
                        if (r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", 0.7f);
                    }
                    else
                    {
                        // Verde PCB fosco (Eletrônicos)
                        r.material.color = new Color(0.1f, 0.55f, 0.25f);
                        if (r.material.HasProperty("_Metallic")) r.material.SetFloat("_Metallic", 0.1f);
                        if (r.material.HasProperty("_Glossiness")) r.material.SetFloat("_Glossiness", 0.2f);
                        if (r.material.HasProperty("_Smoothness")) r.material.SetFloat("_Smoothness", 0.2f);
                    }
                }
                else
                {
                    // Branco padrão se não achar o manager
                    r.material.color = Color.white;
                }

                // Desativa emissão para peças normais
                if (r.material.HasProperty("_EmissionColor"))
                {
                    r.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
