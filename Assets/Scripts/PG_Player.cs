using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PG_Player : MonoBehaviour
{
    private CharacterController controller;

    private Vector3 forward;
    private Vector3 strafe;
    private Vector3 vertical;

    [Header("Configurações de Movimento")]
    public float forwardSpeed = 6f;
    public float strafeSpeed = 6f;

    [Header("Configurações de Pulo / Gravidade")]
    public float maxJumpHeight = 2f;
    public float timeToMaxHeight = 0.5f;

    private float gravity;
    private float jumpSpeed;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Fórmulas para gravidade e velocidade do pulo baseadas em altura e tempo
        gravity = (-2 * maxJumpHeight) / (timeToMaxHeight * timeToMaxHeight);
        jumpSpeed = (2 * maxJumpHeight) / timeToMaxHeight;
    }

    void Update()
    {
        float forwardInput = 0f;
        float strafeInput = 0f;
        bool jumpPressed = false;

        // Leitura segura das entradas do New Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) forwardInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) forwardInput -= 1f;

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) strafeInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) strafeInput -= 1f;

            jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        // força = input * velocidade * direção
        forward = forwardInput * forwardSpeed * transform.forward;
        strafe = strafeInput * strafeSpeed * transform.right;

        // Aplica gravidade
        vertical += gravity * Time.deltaTime * Vector3.up;

        if (controller.isGrounded)
        {
            vertical = Vector3.down; // Mantém o player no chão
        }

        // Pulo
        if (jumpPressed && controller.isGrounded)
        {
            vertical = jumpSpeed * Vector3.up;
        }

        // Reseta velocidade vertical se bater a cabeça no teto
        if (vertical.y > 0 && (controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            vertical = Vector3.zero;
        }

        Vector3 finalVelocity = forward + strafe + vertical;
        controller.Move(finalVelocity * Time.deltaTime);

        // Teleporte para a sala de aprendizagem com a tecla H
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            TeleportToStudyRoom();
        }
    }

    public void TeleportToStudyRoom()
    {
        Vector3 studyRoomPos = new Vector3(93.37f, 0.5f, 41f);
        GameObject screenObj = GameObject.Find("Telão_Sala_Estudo") ?? GameObject.Find("telão") ?? GameObject.Find("Telão") ?? GameObject.Find("Tela") ?? GameObject.Find("Screen") ?? GameObject.Find("TV");
        if (screenObj != null)
        {
            studyRoomPos = screenObj.transform.position - screenObj.transform.forward * 4.5f;
            studyRoomPos.y = 0.5f;
        }

        if (controller != null)
        {
            controller.enabled = false;
        }
        transform.position = studyRoomPos;
        
        float bodyY = 180f;
        float camX = 0f;

        if (screenObj != null)
        {
            Vector3 lookTarget = screenObj.transform.position;
            lookTarget.y = studyRoomPos.y;
            Vector3 dir = (lookTarget - studyRoomPos).normalized;
            if (dir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                bodyY = rot.eulerAngles.y;
            }
        }

        transform.rotation = Quaternion.Euler(0f, bodyY, 0f);
        
        if (controller != null)
        {
            controller.enabled = true;
        }

        // Reseta rotação na câmera do primeiro personagem
        PG_FirstPersonCamera fpCam = GetComponentInChildren<PG_FirstPersonCamera>();
        if (fpCam != null)
        {
            fpCam.ResetCameraRotation(bodyY, camX);
            fpCam.LockCursor(); // Garante que o cursor está travado para a câmera funcionar imediatamente
        }
        
        Debug.Log($"[SKAI Teleport] Retornou para a sala de aprendizagem em {studyRoomPos} encarando {bodyY} graus.");
    }
}
