using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BabyBalanceHud : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Optional UI Text for control/status messages. If left empty, this component uses a simple built-in game view label.")]
    [SerializeField] private Text statusText;

    [Tooltip("Optional UI Text for the survival timer. If left empty, this component uses a simple built-in game view label.")]
    [SerializeField] private Text timerText;

    [Tooltip("Optional TextMeshPro text for the survival timer. If left empty, this component looks for an object named TimerText.")]
    [SerializeField] private TMP_Text timerTmpText;

    [Header("End Panels")]
    [Tooltip("Panel shown only after the player wins. If empty, this component looks for an object named WinPanel.")]
    [SerializeField] private GameObject winPanel;

    [Tooltip("Panel shown only after the player drops the baby. If empty, this component looks for an object named LosePanel.")]
    [SerializeField] private GameObject losePanel;

    [Tooltip("If true, text inside the win and lose panels blinks while that panel is visible.")]
    [SerializeField] private bool flashEndPanelText = true;

    [Tooltip("How often win and lose panel text switches between visible and hidden.")]
    [SerializeField, Min(0.05f)] private float endPanelTextFlashInterval = 0.35f;

    [SerializeField, HideInInspector] private BabyBalanceGame game;

    private GameObject currentEndPanel;
    private float endPanelFlashTimer;
    private bool isEndPanelTextVisible = true;

    private void Reset()
    {
        AutoFindTextReferences();
        AutoFindEndPanels();
    }

    private void Awake()
    {
        if (game == null)
        {
            game = GetComponent<BabyBalanceGame>();
        }

        AutoFindTextReferences();
        AutoFindEndPanels();
        HideEndPanels();
    }

    private void Update()
    {
        if (game == null || (!game.IsGameWon && !game.IsGameOver))
        {
            return;
        }

        UpdateEndPanelTextFlash(Time.deltaTime);
    }

    public void UpdateText(BabyBalanceGame sourceGame)
    {
        game = sourceGame;

        if (sourceGame == null)
        {
            return;
        }

        SetTimerText(sourceGame.TimeRemainingWholeSeconds.ToString());
        UpdatePanels(sourceGame);
        UpdateEndPanelTextFlash(0f);

        if (statusText == null)
        {
            return;
        }

        if (sourceGame.IsGameWon)
        {
            statusText.text = "You win!";
            return;
        }

        if (sourceGame.IsGameOver)
        {
            statusText.text = "Dropped! Hold Space or Left Mouse to retry";
            return;
        }

        statusText.text = sourceGame.IsTargetChaseMode
            ? "Stay on the moving target"
            : "Hold Space or Left Mouse";
    }

    public void HideEndPanels()
    {
        SetPanelActive(winPanel, false);
        SetPanelActive(losePanel, false);
        SetPanelTextVisible(winPanel, true);
        SetPanelTextVisible(losePanel, true);
        currentEndPanel = null;
        endPanelFlashTimer = 0f;
        isEndPanelTextVisible = true;
    }

    private void AutoFindTextReferences()
    {
        if (timerText != null && timerTmpText != null)
        {
            return;
        }

        GameObject timerObject = GameObject.Find("TimerText");
        if (timerObject == null)
        {
            return;
        }

        if (timerText == null)
        {
            timerText = timerObject.GetComponent<Text>();
        }

        if (timerTmpText == null)
        {
            timerTmpText = timerObject.GetComponent<TMP_Text>();
        }
    }

    private void AutoFindEndPanels()
    {
        if (winPanel == null)
        {
            winPanel = GameObject.Find("WinPanel");
        }

        if (losePanel == null)
        {
            losePanel = GameObject.Find("LosePanel");
        }
    }

    private void UpdatePanels(BabyBalanceGame sourceGame)
    {
        GameObject activeEndPanel = null;

        if (sourceGame.IsGameWon)
        {
            activeEndPanel = winPanel;
        }
        else if (sourceGame.IsGameOver)
        {
            activeEndPanel = losePanel;
        }

        if (activeEndPanel != currentEndPanel)
        {
            SetPanelTextVisible(currentEndPanel, true);
            currentEndPanel = activeEndPanel;
            endPanelFlashTimer = 0f;
            isEndPanelTextVisible = true;
            SetPanelTextVisible(currentEndPanel, true);
        }

        SetPanelActive(winPanel, sourceGame.IsGameWon);
        SetPanelActive(losePanel, sourceGame.IsGameOver);
    }

    private void UpdateEndPanelTextFlash(float deltaTime)
    {
        if (currentEndPanel == null)
        {
            return;
        }

        if (!flashEndPanelText)
        {
            SetPanelTextVisible(currentEndPanel, true);
            return;
        }

        endPanelFlashTimer += deltaTime;
        if (endPanelFlashTimer < endPanelTextFlashInterval)
        {
            return;
        }

        endPanelFlashTimer = 0f;
        isEndPanelTextVisible = !isEndPanelTextVisible;
        SetPanelTextVisible(currentEndPanel, isEndPanelTextVisible);
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null && panel.activeSelf != isActive)
        {
            panel.SetActive(isActive);
        }
    }

    private static void SetPanelTextVisible(GameObject panel, bool isVisible)
    {
        if (panel == null)
        {
            return;
        }

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            text.enabled = isVisible;
        }

        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            text.enabled = isVisible;
        }
    }

    private void SetTimerText(string text)
    {
        if (timerText != null)
        {
            timerText.text = text;
        }

        if (timerTmpText != null)
        {
            timerTmpText.text = text;
        }
    }
}
