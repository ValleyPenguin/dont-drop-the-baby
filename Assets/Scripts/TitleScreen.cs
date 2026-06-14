#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [Tooltip("Scene loaded by the Play button. Leave empty to load the next scene in build order.")]
    [FormerlySerializedAs("SceneName")]
    [SerializeField] private string sceneName = "Game";

    [FormerlySerializedAs("_playGameButton")]
    [SerializeField] private Button playGameButton;

    [FormerlySerializedAs("_quitButton")]
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        FindButtons();
    }

    private void OnEnable()
    {
        FindButtons();
        RegisterButtonListeners();
    }

    private void OnDisable()
    {
        UnregisterButtonListeners();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioManager.Instance?.PlayMainMenuMusic();
    }

    public void PlayGame()
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void OnButtonClicked()
    {
        PlayGame();
    }

    public void OnQuitButtonClicked()
    {
        QuitGame();
    }

    private void RegisterButtonListeners()
    {
        if (playGameButton != null)
        {
            playGameButton.onClick.RemoveListener(PlayGame);
            playGameButton.onClick.AddListener(PlayGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void UnregisterButtonListeners()
    {
        if (playGameButton != null)
        {
            playGameButton.onClick.RemoveListener(PlayGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }

    private void FindButtons()
    {
        if (playGameButton == null)
        {
            playGameButton = FindButton("PlayButton");
        }

        if (playGameButton == null)
        {
            playGameButton = FindButton("PlayGameButton");
        }

        if (quitButton == null)
        {
            quitButton = FindButton("QuitButton");
        }
    }

    private static Button FindButton(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }
}
