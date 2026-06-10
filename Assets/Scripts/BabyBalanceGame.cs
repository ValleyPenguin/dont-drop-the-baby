using UnityEngine;
using UnityEngine.InputSystem;

public class BabyBalanceGame : MonoBehaviour
{
    public enum GameMode
    {
        ClassicEdgeBalance,
        TargetChaseBalance
    }

    [Header("Mode")]
    [Tooltip("Classic Edge Balance is the original version: avoid either end of the bar. Target Chase Balance is the Stardew-like version: keep your marker on the moving target.")]
    [SerializeField] private GameMode gameMode = GameMode.ClassicEdgeBalance;

    [Header("Manager Components")]
    [Tooltip("Component that owns target movement, overlap checks, and stability tuning.")]
    [SerializeField] private BabyBalanceTargetChase targetChase;

    [Tooltip("Component that owns baby, arm, and danger visual tuning.")]
    [SerializeField] private BabyBalanceVisuals visuals;

    [Tooltip("Component that owns the balance meter references and generated meter options.")]
    [SerializeField] private BabyBalanceMeter meter;

    [Tooltip("Component that owns UI text references and fallback on-screen labels.")]
    [SerializeField] private BabyBalanceHud hud;

    [Header("Input")]
    [Tooltip("Optional. If empty, the script creates a new Input System action using Space and Left Mouse Button.")]
    [SerializeField] private InputActionReference holdButton;

    [Header("Balance")]
    [Tooltip("How fast the balance marker moves at the start of the game. Higher values make the game harder right away.")]
    [SerializeField, Min(0f)] private float startingSpeed = 0.55f;

    [Tooltip("How much extra speed is added every second. Higher values make the game ramp up faster over time.")]
    [SerializeField, Min(0f)] private float speedIncreasePerSecond = 0.025f;

    [Tooltip("How strongly the held/released button pushes the balance left or right. Usually keep this near 1.")]
    [SerializeField, Min(0f)] private float buttonInfluence = 1f;

    [Tooltip("How strongly the baby's random squirming pushes the balance around. Higher values make the baby feel wilder.")]
    [SerializeField, Min(0f)] private float squirmInfluence = 0.42f;

    [Tooltip("How often the baby chooses a new random squirm direction, in seconds. Lower values change direction more often.")]
    [SerializeField, Min(0.01f)] private float squirmChangeInterval = 0.55f;

    [Tooltip("How quickly the current squirm force moves toward its new random target. Higher values feel jerkier and more reactive.")]
    [SerializeField, Min(0f)] private float squirmResponseSpeed = 2.2f;

    [Header("Restart")]
    [Tooltip("If true, holding the balance button after losing restarts the game.")]
    [SerializeField] private bool allowButtonToRestart = true;

    [Tooltip("How long after losing the player must wait before the button can restart the game.")]
    [SerializeField, Min(0f)] private float restartDelay = 0.35f;

    private InputAction fallbackHoldAction;
    private InputAction ActiveHoldAction => holdButton != null && holdButton.action != null ? holdButton.action : fallbackHoldAction;

    private float balance;
    private float elapsedTime;
    private float gameOverTime;
    private float stability = 1f;
    private float squirm;
    private float squirmTarget;
    private float squirmTimer;
    private bool isGameOver;

    public float Balance => balance;
    public float ElapsedTime => elapsedTime;
    public float Stability => stability;
    public float Squirm => squirm;
    public bool IsMarkerOnTarget => targetChase != null && targetChase.IsMarkerOnTarget;
    public bool IsGameOver => isGameOver;
    public bool IsTargetChaseMode => gameMode == GameMode.TargetChaseBalance;

    internal float ButtonInfluence => buttonInfluence;
    internal float CurrentBalanceSpeed => startingSpeed + elapsedTime * speedIncreasePerSecond;

    internal void SetBalance(float value)
    {
        balance = Mathf.Clamp(value, -1f, 1f);
    }

    internal void SetStability(float value)
    {
        stability = Mathf.Clamp01(value);
    }

    private void Reset()
    {
        FindManagerComponents();
    }

    private void Awake()
    {
        FindManagerComponents();

        meter?.Initialize();
        targetChase?.Initialize(meter);
        visuals?.Initialize();
        UpdateTargetVisibility();
        hud?.UpdateText(this);
    }

    private void OnEnable()
    {
        if (holdButton != null && holdButton.action != null)
        {
            holdButton.action.Enable();
            return;
        }

        fallbackHoldAction = new InputAction("Hold Balance", InputActionType.Button);
        fallbackHoldAction.AddBinding("<Keyboard>/space");
        fallbackHoldAction.AddBinding("<Mouse>/leftButton");
        fallbackHoldAction.Enable();
    }

    private void OnDisable()
    {
        if (holdButton != null && holdButton.action != null)
        {
            holdButton.action.Disable();
            return;
        }

        fallbackHoldAction?.Disable();
        fallbackHoldAction?.Dispose();
        fallbackHoldAction = null;
    }

    private void Update()
    {
        FindManagerComponents();
        targetChase?.Initialize(meter);
        UpdateTargetVisibility();

        bool isHolding = ActiveHoldAction != null && ActiveHoldAction.IsPressed();

        if (isGameOver)
        {
            if (allowButtonToRestart && Time.time >= gameOverTime + restartDelay && isHolding)
            {
                RestartGame();
            }

            visuals?.UpdateVisuals(this, targetChase, meter, Time.deltaTime);
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateSquirm(Time.deltaTime);

        if (IsTargetChaseMode && targetChase != null)
        {
            targetChase.UpdateTargetChase(this, meter, isHolding, Time.deltaTime);
        }
        else
        {
            UpdateClassicBalance(isHolding, Time.deltaTime);
        }

        visuals?.UpdateVisuals(this, targetChase, meter, Time.deltaTime);
        hud?.UpdateText(this);

        if (ShouldLose())
        {
            LoseGame();
        }
    }

    private void FindManagerComponents()
    {
        if (targetChase == null)
        {
            targetChase = GetComponent<BabyBalanceTargetChase>();
        }

        if (visuals == null)
        {
            visuals = GetComponent<BabyBalanceVisuals>();
        }

        if (meter == null)
        {
            meter = GetComponent<BabyBalanceMeter>();
        }

        if (hud == null)
        {
            hud = GetComponent<BabyBalanceHud>();
        }
    }

    private void UpdateTargetVisibility()
    {
        meter?.SetTargetVisible(IsTargetChaseMode);
    }

    private void UpdateSquirm(float deltaTime)
    {
        squirmTimer -= deltaTime;

        if (squirmTimer <= 0f)
        {
            squirmTarget = Random.Range(-1f, 1f);
            squirmTimer = squirmChangeInterval;
        }

        squirm = Mathf.MoveTowards(squirm, squirmTarget, squirmResponseSpeed * deltaTime);
    }

    private void UpdateClassicBalance(bool isHolding, float deltaTime)
    {
        float buttonDirection = isHolding ? 1f : -1f;
        float drift = buttonDirection * buttonInfluence + squirm * squirmInfluence;
        SetBalance(balance + drift * CurrentBalanceSpeed * deltaTime);
    }

    private bool ShouldLose()
    {
        if (IsTargetChaseMode)
        {
            return stability <= 0f;
        }

        return Mathf.Abs(balance) >= 1f;
    }

    private void LoseGame()
    {
        isGameOver = true;
        gameOverTime = Time.time;
        SetBalance(balance);
        hud?.UpdateText(this);
    }

    [ContextMenu("Restart Game")]
    public void RestartGame()
    {
        balance = 0f;
        elapsedTime = 0f;
        gameOverTime = 0f;
        stability = 1f;
        squirm = 0f;
        squirmTarget = 0f;
        squirmTimer = 0f;
        targetChase?.ResetState();
        isGameOver = false;
        hud?.UpdateText(this);
    }
}
