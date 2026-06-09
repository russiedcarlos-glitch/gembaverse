using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UnityFactorySceneHDRP
{
	public class CameraMove : MonoBehaviour
	{
		[SerializeField] private CharacterController _characterController;
		[SerializeField] private Transform _playerRoot;
		[SerializeField] private Transform _camera;

		[Space(10)]
		[SerializeField] private float _moveSpeed = 2;
		[SerializeField] private float _rotateSpeed = 0.1f; // Ajustado para o delta do novo Input System

		[Space(10)]
		[SerializeField] private float _minWorldY;

		private float _yaw = 0;
		private float _tilt = 0;
		private bool _isRunning = false;
		private bool _isWalkMode = true;

		private void Awake()
		{
			_yaw = _playerRoot.eulerAngles.y;
			_tilt = _camera.localEulerAngles.x;
		}

		private void Update()
		{
			float mouseX = 0;
			float mouseY = 0;
			bool isRightMouseDown = false;

			float moveH = 0;
			float moveV = 0;
			bool moveDown = false;
			bool moveUp = false;

			bool shiftPressedThisFrame = false;
			bool fPressedThisFrame = false;

#if ENABLE_INPUT_SYSTEM
			// Novo Input System
			if (Mouse.current != null)
			{
				isRightMouseDown = Mouse.current.rightButton.isPressed;
				Vector2 mouseDelta = Mouse.current.delta.ReadValue();
				mouseX = mouseDelta.x;
				mouseY = mouseDelta.y;
			}

			if (Keyboard.current != null)
			{
				if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveH = -1f;
				if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveH = 1f;
				if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveV = 1f;
				if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveV = -1f;

				moveDown = Keyboard.current.qKey.isPressed;
				moveUp = Keyboard.current.eKey.isPressed;

				shiftPressedThisFrame = Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame;
				fPressedThisFrame = Keyboard.current.fKey.wasPressedThisFrame;
			}
#else
			// Fallback para Input Antigo (caso o projeto seja revertido)
			isRightMouseDown = Input.GetMouseButton(1);
			mouseX = Input.GetAxis("Mouse X") * 10f; // Multiplicado para compensar a diferença de escala de delta
			mouseY = Input.GetAxis("Mouse Y") * 10f;

			moveH = Input.GetAxis("Horizontal");
			moveV = Input.GetAxis("Vertical");

			moveDown = Input.GetKey(KeyCode.Q);
			moveUp = Input.GetKey(KeyCode.E);

			shiftPressedThisFrame = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
			fPressedThisFrame = Input.GetKeyDown(KeyCode.F);
#endif

			// Rotate
			if (isRightMouseDown)
			{
				_yaw  += mouseX * _rotateSpeed;
				_tilt -= mouseY * _rotateSpeed;

				_tilt = Mathf.Clamp(_tilt, -89, 89);

				_playerRoot.eulerAngles = new Vector3(0, _yaw, 0);
				_camera.localEulerAngles = new Vector3(_tilt, 0, 0);
			}

			// Move
			Vector3 dir = new Vector3(moveH, 0 , moveV);
			float heightInput = (moveDown ? -_moveSpeed : 0) + (moveUp ? _moveSpeed : 0);
			float height = Mathf.Max(0, _camera.localPosition.y + heightInput * Time.deltaTime);

			if (shiftPressedThisFrame)
			{
				_isRunning = !_isRunning;
			}

			if (_isWalkMode)
			{
				dir = Quaternion.Euler(0, _playerRoot.localEulerAngles.y, 0) * dir;
				_characterController.SimpleMove(dir * _moveSpeed * (_isRunning ? 3 : 1));
				_camera.localPosition = new Vector3(0, height, 0);
			}
			else
			{
				dir = Quaternion.Euler(_camera.localEulerAngles.x, _playerRoot.localEulerAngles.y, _camera.localEulerAngles.z) * dir;
				_characterController.Move(dir * _moveSpeed * (_isRunning ? 3 : 1) * Time.deltaTime);
			}

			if (_playerRoot.position.y < _minWorldY)
			{
				Vector3 position = _playerRoot.position;
				position.y = _minWorldY;
				_playerRoot.position = position;
			}

			// Change mode
			if (fPressedThisFrame)
			{
				_isWalkMode = !_isWalkMode;
				if (_isWalkMode)
				{
					_playerRoot.position = new Vector3(_playerRoot.position.x, _minWorldY, _playerRoot.position.z);
					_camera.localPosition = new Vector3(0, 1.5f, 0);
				}
				else
				{
					_playerRoot.position = new Vector3(_playerRoot.position.x, _camera.position.y, _playerRoot.position.z);
					_camera.localPosition = Vector3.zero;
				}
			}
		}
	}
}