using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    
    /// <summary>
    /// this should let you be able to quit at any point, I know you had some other ideas for the game but
    /// I think its better to offer the choice to the player to quit rather than just playing over and over.
    /// </summary>
    ///
    /// 
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
        ShowResumeButton();
        ShowQuitButton();
    }
    
    private void OnResume()
    {
        if (_gameOver) return;
        ShowMouse(false);
        Time.timeScale = 1;
        ResetButtons();
        UIManager.Instance.ShowPauseMenu(false);
    }

    private void ShowResumeButton()
    {
        _resumeButton.gameObject.SetActive(true);
    }

    private void ShowQuitButton()
    {
        _quitButton.gameObject.SetActive(true); 
    }

    public void ResetButtons()
    {
        _resumeButton.gameObject.SetActive(false);
        _quitButton.gameObject.SetActive(false);
    }
    
    
    public void CheckWinCondition()
    {
      //if statement for the win
        {
            UIManager.Instance.ShowWinScreen();
            ShowQuitButton();
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
        ShowQuitButton();
        ShowMouse(true);
        Time.timeScale = 0;
    }
    
    private void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        ResetButtons();
#endif
        Application.Quit();
    }

}

