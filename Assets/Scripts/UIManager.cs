using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _winScreen;
    [SerializeField] private GameObject _loseScreen;
  
    
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        Instance = this;
    }
    
    public void ShowPauseMenu(bool show)
    {
        _pauseMenu.SetActive(show);
    }

    public void ShowWinScreen()
    {
        _winScreen.SetActive(true);
    }

    public void ShowLoseScreen()
    {
        _loseScreen.SetActive(true);
    }
}