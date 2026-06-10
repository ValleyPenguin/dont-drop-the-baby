using UnityEngine;
using UnityEngine.UI;

public class BabyBalanceHud : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Optional UI Text for control/status messages. If left empty, this component uses a simple built-in game view label.")]
    [SerializeField] private Text statusText;

    [Tooltip("Optional UI Text for the survival timer. If left empty, this component uses a simple built-in game view label.")]
    [SerializeField] private Text timerText;

    [SerializeField, HideInInspector] private BabyBalanceGame game;

    private void Awake()
    {
        if (game == null)
        {
            game = GetComponent<BabyBalanceGame>();
        }
    }

    public void UpdateText(BabyBalanceGame sourceGame)
    {
        game = sourceGame;

        if (sourceGame == null)
        {
            return;
        }

        if (timerText != null)
        {
            timerText.text = sourceGame.IsTargetChaseMode
                ? $"Time: {sourceGame.ElapsedTime:0.0}  Stability: {sourceGame.Stability:P0}"
                : $"Time: {sourceGame.ElapsedTime:0.0}";
        }

        if (statusText == null)
        {
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

    private void OnGUI()
    {
        if (game == null)
        {
            game = GetComponent<BabyBalanceGame>();
        }

        if (game == null || (!game.IsGameOver && timerText != null && statusText != null))
        {
            return;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.032f), 18, 28),
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        string modeDetails = game.IsTargetChaseMode
            ? $"\nStability: {game.Stability:P0}"
            : "";

        string message = game.IsGameOver
            ? $"Dropped!\nTime: {game.ElapsedTime:0.0}{modeDetails}\nHold Space or Left Mouse to retry"
            : $"Time: {game.ElapsedTime:0.0}{modeDetails}";

        GUI.Label(new Rect(20f, 24f, Screen.width - 40f, 180f), message, style);
    }
}
