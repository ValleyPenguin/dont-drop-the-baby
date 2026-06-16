using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Pause UI")]
    [FormerlySerializedAs("_pausePanel")]
    [SerializeField] private GameObject pausePanel;
    [FormerlySerializedAs("_resumeButton")]
    [SerializeField] private Button resumeButton;
    [FormerlySerializedAs("_quitButton")]
    [SerializeField] private Button quitButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Gameplay")]
    [SerializeField] private BabyBalanceGame babyBalanceGame;

    private bool isPaused;
    private bool isEnding;
    private AudioManager audioManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindReferences();
        SetPausePanelVisible(false);
    }

    private void OnEnable()
    {
        FindReferences();
        RegisterButtonListeners();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isEnding = false;
        audioManager = AudioManager.Instance;
        audioManager?.PlayGameplayMusic();
        ShowMouse(false);
        SetPausePanelVisible(false);
    }

    private void Update()
    {
        if (WasPausePressed())
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
            return;
        }

        PauseGame();
    }

    public void Pause()
    {
        PauseGame();
    }

    public void PauseGame()
    {
        if (isEnding || IsGameEnded())
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;
        SetPausePanelVisible(true);
        ShowMouse(true);
    }

    public void ResumeGame()
    {
        if (isEnding)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;
        SetPausePanelVisible(false);
        ShowMouse(false);
    }

    public void HandleGameWon()
    {
        isEnding = true;
        isPaused = false;
        Time.timeScale = 1f;
        SetPausePanelVisible(false);
        ShowMouse(true);

        audioManager = AudioManager.Instance;
        audioManager?.StopBabyCrying();
        audioManager?.PlayWinSound();
        audioManager?.PlayWinMusic();
    }

    public void HandleGameLost()
    {
        isEnding = true;
        isPaused = false;
        Time.timeScale = 1f;
        SetPausePanelVisible(false);
        ShowMouse(true);

        audioManager = AudioManager.Instance;
        audioManager?.StopBabyCrying();
        audioManager?.PlayBabyDropScream();
        audioManager?.PlayLoseMusic();
    }

    public void HandleGameRestarted()
    {
        isEnding = false;
        isPaused = false;
        Time.timeScale = 1f;
        SetPausePanelVisible(false);
        ShowMouse(false);

        audioManager = AudioManager.Instance;
        audioManager?.StopBabyCrying();
        audioManager?.PlayGameplayMusic();
    }

    public void OnQuit()
    {
        ReturnToMainMenu();
    }

    public void ReturnToMainMenu()
    {
        isPaused = false;
        isEnding = false;
        Time.timeScale = 1f;
        SetPausePanelVisible(false);
        ShowMouse(true);

        audioManager = AudioManager.Instance;
        audioManager?.StopBabyCrying();

        if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
        }
    }

    public void CheckWinCondition()
    {
        UIManager.Instance?.ShowWinScreen();
        HandleGameWon();
    }

    public void ResetButtons()
    {
        SetPausePanelVisible(false);
    }

    private void RegisterButtonListeners()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuit);
            quitButton.onClick.AddListener(OnQuit);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuit);
        }
    }

    private void FindReferences()
    {
        if (babyBalanceGame == null)
        {
            babyBalanceGame = GetComponent<BabyBalanceGame>();
        }

        if (babyBalanceGame == null)
        {
            babyBalanceGame = FindFirstObjectByType<BabyBalanceGame>();
        }

        if (pausePanel == null)
        {
            GameObject pauseObject = GameObject.Find("PausePanel");
            pausePanel = pauseObject;
        }

        if (resumeButton == null)
        {
            resumeButton = FindButton("ResumeButton");
        }

        if (quitButton == null)
        {
            quitButton = FindButton("QuitButton");
        }
    }

    private void SetPausePanelVisible(bool visible)
    {
        if (pausePanel != null && pausePanel.activeSelf != visible)
        {
            pausePanel.SetActive(visible);
        }
    }

    private bool IsGameEnded()
    {
        return babyBalanceGame != null && (babyBalanceGame.IsGameOver || babyBalanceGame.IsGameWon);
    }

    private static Button FindButton(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private static void ShowMouse(bool value)
    {
        Cursor.visible = value;
        Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private static bool WasPausePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
