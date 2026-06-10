using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    public string SceneName;
    [SerializeField] private Button _playGameButton;
    [SerializeField] private Button _quitButton;
    private AudioManager _audioManager;
    
    private void Start()
    {
        _audioManager = AudioManager.Instance;
        _audioManager.PlayTitleMusic();
        _quitButton.onClick.AddListener(OnQuitButtonClicked);
    }
    
    private void OnEnable()
    {
        if (_playGameButton == null || _quitButton == null) return;
        _playGameButton.onClick.AddListener(OnButtonClicked);
        _quitButton.onClick.AddListener(OnQuitButtonClicked);
    }

    private void OnDisable()
    {
        if (_playGameButton == null || _quitButton == null) return;
        _playGameButton.onClick.RemoveListener(OnButtonClicked);
        _quitButton.onClick.RemoveListener(OnQuitButtonClicked);
    }

    public void OnButtonClicked()
    {
        SceneManager.LoadScene(SceneName);
    }

    private void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}