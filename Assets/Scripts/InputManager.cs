using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputAction pauseInput = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");

    private void OnEnable()
    {
        pauseInput.performed += OnPause;
        pauseInput.Enable();
    }

    private void OnDisable()
    {
        pauseInput.performed -= OnPause;
        pauseInput.Disable();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        GameManager.Instance?.TogglePause();
    }
}
