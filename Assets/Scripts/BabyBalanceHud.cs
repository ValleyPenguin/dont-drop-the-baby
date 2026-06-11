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

    [SerializeField, HideInInspector] private BabyBalanceGame game;

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

    public void UpdateText(BabyBalanceGame sourceGame)
    {
        game = sourceGame;

        if (sourceGame == null)
        {
            return;
        }

        SetTimerText(sourceGame.TimeRemainingWholeSeconds.ToString());
        UpdatePanels(sourceGame);

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
        SetPanelActive(winPanel, sourceGame.IsGameWon);
        SetPanelActive(losePanel, sourceGame.IsGameOver);
    }

    private static void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null && panel.activeSelf != isActive)
        {
            panel.SetActive(isActive);
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
