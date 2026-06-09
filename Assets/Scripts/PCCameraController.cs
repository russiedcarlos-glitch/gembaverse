using UnityEngine;
using UnityEngine.InputSystem;

public class PCCameraController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float moveSpeed = 6.0f;
    public float lookSpeed = 2.0f;

    [Header("PC Debug Override")]
    [Tooltip("Força o uso dos controles de PC mesmo se houver VR detectado")]
    public bool forcePCControls = false;

    [Header("Restrições")]
    [Tooltip("Só ativa o controle por mouse se não houver um headset VR ativo no momento")]
    public bool onlyActiveWithoutVR = true;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        Vector3 rot = transform.localEulerAngles;
        rotationX = rot.y;
        rotationY = rot.x;

        if (ShouldUsePCControls())
        {
            DisableTrackingComponents();

            // Se a câmera principal estiver colada no chão (ex: localPosition.y == 0 ou < 0.2f),
            // eleva ela para uma altura padrão de olhos (1.65m) para que o jogador não spawne no chão.
            if (transform.localPosition.y < 0.2f)
            {
                Vector3 pos = transform.localPosition;
                pos.y = 1.65f;
                transform.localPosition = pos;
                Debug.Log("[PCCameraController] Câmera estava no chão. Elevada para 1.65m para depuração no PC.");
            }
        }
    }

    private bool IsHMDConnected()
    {
        var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, devices);
        return devices.Count > 0;
    }

    private bool ShouldUsePCControls()
    {
        if (forcePCControls) return true;
        
        // Se não houver HMD físico conectado, estamos no PC
        if (!IsHMDConnected()) return true;

        // Caso contrário, obedece à configuração
        return !onlyActiveWithoutVR;
    }

    private void DisableTrackingComponents()
    {
        Behaviour[] components = GetComponents<Behaviour>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            string typeName = comp.GetType().FullName;
            if (typeName.Contains("TrackedPoseDriver") || typeName.Contains("PoseDriver") || typeName.Contains("TrackedPose"))
            {
                comp.enabled = false;
                Debug.Log($"[PCCameraController] Desativado componente de tracking: {typeName} para permitir controle manual.");
            }
        }
    }

    private void Update()
    {
        if (!ShouldUsePCControls())
        {
            return;
        }

        // 1. ROTAÇÃO DA CÂMERA (Olhar ao redor ao SEGURAR o botão direito do mouse)
        bool rightMousePressed = false;
        float mouseX = 0f;
        float mouseY = 0f;

        if (Mouse.current != null)
        {
            rightMousePressed = Mouse.current.rightButton.isPressed;
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            // Multiplicador menor para compensar delta em pixels do New Input System
            mouseX = mouseDelta.x * 0.1f * lookSpeed;
            mouseY = mouseDelta.y * 0.1f * lookSpeed;
        }

        if (rightMousePressed)
        {
            // Oculta o cursor enquanto olha
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            rotationX += mouseX;
            rotationY -= mouseY;
            rotationY = Mathf.Clamp(rotationY, -85f, 85f);

            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }
        else
        {
            // Devolve o cursor ao soltar
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 2. MOVIMENTAÇÃO (WASD ou Setas)
        float moveH = 0f;
        float moveV = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveV += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveV -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveH += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveH -= 1f;
        }

        if (Mathf.Abs(moveH) > 0.02f || Mathf.Abs(moveV) > 0.02f)
        {
            Vector3 inputDir = new Vector3(moveH, 0f, moveV);
            
            // Converte a direção local da câmera para mundo, ignorando inclinação vertical (Y) para não "voar" ao andar
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 moveDir = (forward * inputDir.z + right * inputDir.x).normalized;

            // Move o objeto no espaço global (funciona para a câmera direta ou para o rig se for o alvo)
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
        }

        // 3. SUBIR E DESCER (Q / E) - Excelente para ajustar a altura da visão no computador
        bool upPressed = false;
        bool downPressed = false;

        if (Keyboard.current != null)
        {
            upPressed = Keyboard.current.eKey.isPressed;
            downPressed = Keyboard.current.qKey.isPressed;
        }

        if (upPressed)
        {
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);
        }
        if (downPressed)
        {
            transform.Translate(Vector3.down * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}

public class CameraDiagnostics : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[CameraDiagnostics] Iniciando verificação de câmeras na cena...");
        
        Camera[] allCameras = Resources.FindObjectsOfTypeAll<Camera>();
        Debug.Log($"[CameraDiagnostics] Total de câmeras encontradas na memória: {allCameras.Length}");

        foreach (var cam in allCameras)
        {
            if (cam.gameObject.hideFlags == HideFlags.NotEditable || cam.gameObject.hideFlags == HideFlags.HideAndDontSave)
                continue;
                
            string path = GetGameObjectPath(cam.gameObject);
            Debug.Log($"[CameraDiagnostics] Câmera: '{path}'\n" +
                      $"  - GameObject Ativo (Self): {cam.gameObject.activeSelf}\n" +
                      $"  - GameObject Ativo (Hierarchy): {cam.gameObject.activeInHierarchy}\n" +
                      $"  - Componente Habilitado: {cam.enabled}\n" +
                      $"  - Tag: {cam.gameObject.tag}\n" +
                      $"  - Target Texture: {(cam.targetTexture != null ? cam.targetTexture.name : "Nenhuma")}\n" +
                      $"  - Render Path / URP Data: {cam.renderingPath}");
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
