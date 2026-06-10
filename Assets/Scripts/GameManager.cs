using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;
    
    private bool _gameOver;
    private AudioManager _audioManager;
    private UIManager _uiManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }
    
    private void Start()
    {
        Time.timeScale = 1;
        _audioManager = AudioManager.Instance;
        _uiManager = UIManager.Instance;
        ShowMouse(false);
        _audioManager.PlayBGMusic();
        _quitButton.onClick.AddListener(OnQuit);
        _resumeButton.onClick.AddListener(OnResume);
    }

    private void OnEnable()
    {
        _resumeButton.onClick.AddListener(OnResume);
        _quitButton.onClick.AddListener(OnQuit);
    }
    
    private void OnDisable()
    {
        _resumeButton.onClick.RemoveListener(OnResume);
        _quitButton.onClick.RemoveListener(OnQuit);
    }

    private void ShowMouse(bool value)
    {
        Cursor.visible = value;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    public void Pause()
    {
        if (_gameOver) return;
        ShowMouse(true);
        Time.timeScale = 0;
        UIManager.Instance.ShowPauseMenu(true);
    }
    
    private void OnResume()
    {
        if (_gameOver) return;
        ShowMouse(false);
        Time.timeScale = 1;
        UIManager.Instance.ShowPauseMenu(false);
    }
    
    public void CheckWinCondition()
    {
      //if statement for the win
        {
            UIManager.Instance.ShowWinScreen();
            ShowMouse(true);
            Time.timeScale = 0;
            _gameOver = true;
        }
    }

    private void LoseFunction()
    {
        if (_gameOver) return;
        _gameOver = true;
        UIManager.Instance.ShowLoseScreen();
        ShowMouse(true);
        Time.timeScale = 0;
    }
    
    private void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

}

