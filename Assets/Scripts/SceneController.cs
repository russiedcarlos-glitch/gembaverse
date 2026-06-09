using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [Tooltip("Nome da cena que esta porta vai carregar")]
    public string targetSceneName;

    [Tooltip("Se for verdadeiro, ao invés de carregar cena, ativará/desativará os GameObjects abaixo")]
    public bool useToggleObjects = true;
    
    public GameObject studyRoomGroup;
    public GameObject factoryGroup;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se quem entrou na porta foi o jogador (pela tag "Player" ou XR Rig)
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            TeleportToNextArea();
        }
    }

    public void TeleportToNextArea()
    {
        if (useToggleObjects)
        {
            // Alterna entre a Sala de Estudos e a Fábrica
            bool isStudyRoomActive = studyRoomGroup.activeSelf;
            studyRoomGroup.SetActive(!isStudyRoomActive);
            factoryGroup.SetActive(isStudyRoomActive);
        }
        else
        {
            // Carrega outra cena
            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }
}
