using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputAction _pauseInput;
    
    private void OnEnable()
    {
        _pauseInput.Enable();
        _pauseInput.performed += OnPause;
    }

    private void OnDisable()
    {
        _pauseInput.Disable();
        _pauseInput.performed -= OnPause;
    }
    public void OnPause(InputAction.CallbackContext context)
    {
        GameManager.Instance.Pause();
    }
}