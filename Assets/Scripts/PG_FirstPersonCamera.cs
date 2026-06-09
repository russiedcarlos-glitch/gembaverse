using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PG_FirstPersonCamera : MonoBehaviour
{
    [Header("Alvos")]
    public Transform characterBody; // Arraste o objeto do Player aqui
    public Transform characterHead; // Arraste o objeto vazio "Head" do Player aqui

    [Header("Configurações de Sensibilidade")]
    public float sensitivityX = 1.0f;
    public float sensitivityY = 1.0f;
    [Tooltip("Fator de escala para ajustar os deltas em pixels do New Input System")]
    public float mouseSensitivityScale = 0.03f;

    [Header("Limites de Ângulo")]
    public float angleYmin = -85f;
    public float angleYmax = 85f;

    [Header("Configurações de Suavização")]
    public float smoothCoefx = 0.05f;
    public float smoothCoefy = 0.05f;

    [Header("Mira / Crosshair (Modo Travado)")]
    public bool showCrosshair = true;
    public Color crosshairColor = Color.green;
    public int crosshairFontSize = 24;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private float smoothRotx = 0f;
    private float smoothRoty = 0f;

    private bool isLocked = true;

    void Start()
    {
        // Define o estado inicial da rotação baseado na orientação inicial
        if (characterBody != null)
        {
            rotationX = characterBody.localEulerAngles.y;
        }
        rotationY = -transform.localEulerAngles.x;

        LockCursor();
    }

    private void LateUpdate()
    {
        // Mantém a câmera na posição da "cabeça" do player
        if (characterHead != null)
        {
            transform.position = characterHead.position;
        }
    }

    void Update()
    {
        // Tecla ESC destrava o cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        // Se clicar e estiver destravado, tenta travar novamente se não estiver sobre a UI
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !isLocked)
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                LockCursor();
            }
        }

        if (isLocked)
        {
            float horizontalDelta = 0f;
            float verticalDelta = 0f;

            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                horizontalDelta = mouseDelta.x * mouseSensitivityScale * sensitivityX;
                verticalDelta = mouseDelta.y * mouseSensitivityScale * sensitivityY;
            }

            // Suavização do movimento da câmera (Lerp)
            smoothRotx = Mathf.Lerp(smoothRotx, horizontalDelta, smoothCoefx);
            smoothRoty = Mathf.Lerp(smoothRoty, verticalDelta, smoothCoefy);

            rotationX += smoothRotx;
            rotationY += smoothRoty;

            // Limita a rotação vertical (evita cambalhotas da câmera)
            rotationY = Mathf.Clamp(rotationY, angleYmin, angleYmax);

            // Rotaciona o corpo do player horizontalmente (Y)
            if (characterBody != null)
            {
                characterBody.localEulerAngles = new Vector3(0, rotationX, 0);
            }

            // Rotaciona a câmera verticalmente (X)
            // Se a câmera for filha/descendente do corpo, ela já herda a rotação horizontal (Y),
            // então definimos o Y local como 0 para evitar rotação dupla e desalinhamentos.
            bool isDescendant = false;
            if (characterBody != null)
            {
                Transform p = transform.parent;
                while (p != null)
                {
                    if (p == characterBody)
                    {
                        isDescendant = true;
                        break;
                    }
                    p = p.parent;
                }
            }

            if (isDescendant)
            {
                transform.localEulerAngles = new Vector3(-rotationY, 0, 0);
            }
            else
            {
                transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
            }
        }
    }

    public void LockCursor()
    {
        isLocked = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void UnlockCursor()
    {
        isLocked = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResetCameraRotation(float bodyYRotation, float cameraXRotation)
    {
        rotationX = bodyYRotation;
        rotationY = -cameraXRotation;
        smoothRotx = 0f;
        smoothRoty = 0f;
        
        if (characterBody != null)
        {
            characterBody.localEulerAngles = new Vector3(0, rotationX, 0);
        }
        transform.localEulerAngles = new Vector3(-rotationY, 0, 0);
    }

    private void OnGUI()
    {
        if (isLocked && showCrosshair)
        {
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = crosshairFontSize;
            style.normal.textColor = crosshairColor;
            
            // Desenha um caractere '+' no centro exato da tela
            float width = 40f;
            float height = 40f;
            float x = (Screen.width / 2f) - (width / 2f);
            float y = (Screen.height / 2f) - (height / 2f);
            
            GUI.Label(new Rect(x, y, width, height), "+", style);
        }
    }
}
