using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [FormerlySerializedAs("_pauseMenu")]
    [SerializeField] private GameObject pauseMenu;

    [FormerlySerializedAs("_winScreen")]
    [SerializeField] private GameObject winScreen;

    [FormerlySerializedAs("_loseScreen")]
    [SerializeField] private GameObject loseScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindPanels();
        ShowPauseMenu(false);
    }

    public void ShowPauseMenu(bool show)
    {
        SetPanelActive(pauseMenu, show);
    }

    public void ShowWinScreen()
    {
        SetPanelActive(winScreen, true);
    }

    public void ShowLoseScreen()
    {
        SetPanelActive(loseScreen, true);
    }

    public void HideEndScreens()
    {
        SetPanelActive(winScreen, false);
        SetPanelActive(loseScreen, false);
    }

    private void FindPanels()
    {
        pauseMenu = pauseMenu != null ? pauseMenu : GameObject.Find("PausePanel");
        winScreen = winScreen != null ? winScreen : GameObject.Find("WinPanel");
        loseScreen = loseScreen != null ? loseScreen : GameObject.Find("LosePanel");
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null && panel.activeSelf != isActive)
        {
            panel.SetActive(isActive);
        }
    }
}
