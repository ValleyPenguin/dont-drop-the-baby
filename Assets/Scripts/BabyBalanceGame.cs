using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BabyBalanceGame : MonoBehaviour
{
    private enum GameMode
    {
        ClassicEdgeBalance,
        TargetChaseBalance
    }

    [Header("Mode")]
    [Tooltip("Classic Edge Balance is the original version: avoid either end of the bar. Target Chase Balance is the Stardew-like version: keep your marker on the moving target.")]
    [SerializeField] private GameMode gameMode = GameMode.ClassicEdgeBalance;

    [Header("Scene References")]
    [Tooltip("The left arm/cast object. If left empty, the script looks for an object named Placeholder_Arm_Left.")]
    [SerializeField] private Transform leftArm;

    [Tooltip("The right arm/cast object. If left empty, the script looks for an object named Placeholder_Arm_Right.")]
    [SerializeField] private Transform rightArm;

    [Tooltip("The baby object that slides, wiggles, and tilts as balance gets worse. If left empty, the script looks for Placeholder_Baby.")]
    [SerializeField] private Transform baby;

    [Tooltip("The moving line/marker on the balance meter. Its local X position represents the current balance value.")]
    [SerializeField] private Transform meterMarker;

    [Tooltip("The moving target for Target Chase Balance mode. If left empty, the script can create a simple target sprite at runtime.")]
    [SerializeField] private Transform balanceTarget;

    [Tooltip("The background bar for the balance meter. This is optional unless you want to use your own meter art.")]
    [SerializeField] private Transform meterBar;

    [Tooltip("Optional UI Text for control/status messages. If left empty, the script uses a simple built-in game view label.")]
    [SerializeField] private Text statusText;

    [Tooltip("Optional UI Text for the survival timer. If left empty, the script uses a simple built-in game view label.")]
    [SerializeField] private Text timerText;

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

    [Tooltip("How close to an edge the marker must be before danger visuals start. 0.72 means danger starts at 72% of the way to either edge.")]
    [SerializeField, Range(0f, 1f)] private float dangerZoneStartsAt = 0.72f;

    [Header("Target Chase Balance")]
    [Tooltip("Width of the generated target sprite as a normalized bar amount. Also used as a fallback if bounds cannot be measured.")]
    [SerializeField, Range(0.02f, 0.75f)] private float targetHalfWidth = 0.16f;

    [Tooltip("How much the marker and target must overlap to count as balanced. 0.5 means at least half of the smaller object must overlap the other.")]
    [SerializeField, Range(0.01f, 1f)] private float requiredTargetOverlap = 0.5f;

    [Tooltip("How fast the moving target travels at the start of Target Chase Balance mode.")]
    [SerializeField, Min(0f)] private float targetStartingSpeed = 0.7f;

    [Tooltip("How much faster the moving target gets every second in Target Chase Balance mode.")]
    [SerializeField, Min(0f)] private float targetSpeedIncreasePerSecond = 0.025f;

    [Tooltip("How often the moving target chooses a new random destination, in seconds.")]
    [SerializeField, Min(0.01f)] private float targetChangeInterval = 0.85f;

    [Tooltip("How quickly stability drains while the marker is not touching the moving target.")]
    [SerializeField, Min(0f)] private float offTargetDrainPerSecond = 0.35f;

    [Tooltip("How quickly stability recovers while the marker is touching the moving target.")]
    [SerializeField, Min(0f)] private float onTargetRecoverPerSecond = 0.18f;

    [Tooltip("The color of the generated target sprite in Target Chase Balance mode.")]
    [SerializeField] private Color targetColor = new Color(1f, 0.86f, 0.18f);

    [Header("Arm Visuals")]
    [Tooltip("How far the arms move up and down as the balance marker moves. Higher values make the arm height change more dramatic.")]
    [SerializeField] private float armHeightOffset = 0.9f;

    [Tooltip("How many degrees the arms rotate at full imbalance. Higher values make the casts tilt more.")]
    [SerializeField] private float armTiltDegrees = 14f;

    [Tooltip("How quickly the arms catch up to their target positions. Higher values are snappier; lower values are floatier.")]
    [SerializeField, Min(0f)] private float armFollowSpeed = 12f;

    [Header("Baby Visuals")]
    [Tooltip("How far the baby slides left/right with the balance marker before danger extra sliding is added.")]
    [SerializeField] private float babySlideDistance = 1.15f;

    [Tooltip("Extra sideways slide added only in the danger zone, making the baby look close to falling out.")]
    [SerializeField] private float babyDangerSlideDistance = 0.8f;

    [Tooltip("How far down the baby drops in the danger zone, making the baby look less supported by the arms.")]
    [SerializeField] private float babyDangerDropDistance = 0.65f;

    [Tooltip("How many degrees the baby tilts at full imbalance. Higher values make the almost-falling pose stronger.")]
    [SerializeField] private float babyTiltDegrees = 38f;

    [Tooltip("Small up/down wiggle amount from the baby's squirming.")]
    [SerializeField] private float babySquirmPosition = 0.14f;

    [Tooltip("Small rotation wiggle amount from the baby's squirming.")]
    [SerializeField] private float babySquirmRotation = 7f;

    [Tooltip("How quickly the baby catches up to its target pose. Higher values are snappier; lower values are floatier.")]
    [SerializeField, Min(0f)] private float babyFollowSpeed = 10f;

    [Header("Meter Visuals")]
    [Tooltip("If true, the script creates a simple bar and marker at runtime when Meter Bar or Meter Marker is empty.")]
    [SerializeField] private bool createMeterIfMissing = true;

    [Tooltip("World position for the generated meter, relative to this GameObject.")]
    [SerializeField] private Vector2 generatedMeterPosition = new Vector2(0f, 4.1f);

    [Tooltip("If true, the script measures the bar, marker, and target sizes so their edges line up correctly. Turn off only if you want to tune Meter Half Width manually.")]
    [SerializeField] private bool autoCalculateMeterBounds = true;

    [Tooltip("Fallback half-width of the meter, used when Auto Calculate Meter Bounds is off or the script cannot measure the bar.")]
    [SerializeField, Min(0.1f)] private float meterHalfWidth = 3f;

    [Tooltip("Marker color when the baby is safely balanced near the middle.")]
    [SerializeField] private Color safeMeterColor = new Color(0.15f, 0.9f, 0.45f);

    [Tooltip("Marker color when the baby is close to being dropped.")]
    [SerializeField] private Color dangerMeterColor = new Color(1f, 0.28f, 0.2f);

    [Header("Restart")]
    [Tooltip("If true, holding the balance button after losing restarts the game.")]
    [SerializeField] private bool allowButtonToRestart = true;

    [Tooltip("How long after losing the player must wait before the button can restart the game.")]
    [SerializeField, Min(0f)] private float restartDelay = 0.35f;

    private InputAction fallbackHoldAction;
    private InputAction ActiveHoldAction => holdButton != null && holdButton.action != null ? holdButton.action : fallbackHoldAction;

    private Vector3 leftArmStartPosition;
    private Vector3 rightArmStartPosition;
    private Vector3 babyStartPosition;
    private Quaternion leftArmStartRotation;
    private Quaternion rightArmStartRotation;
    private Quaternion babyStartRotation;

    private SpriteRenderer meterBarRenderer;
    private SpriteRenderer meterMarkerRenderer;
    private SpriteRenderer balanceTargetRenderer;

    // Balance is the main game state: -1 means fully left, 0 means safe center, 1 means fully right.
    private float balance;
    private float elapsedTime;
    private float gameOverTime;
    private float stability = 1f;
    private float squirm;
    private float squirmTarget;
    private float squirmTimer;
    private float targetBalance;
    private float targetDestination;
    private float targetTimer;
    private bool isMarkerOnTarget;
    private bool isGameOver;

    public float Balance => balance;
    public float ElapsedTime => elapsedTime;
    public float Stability => stability;
    public bool IsMarkerOnTarget => isMarkerOnTarget;
    public bool IsGameOver => isGameOver;

    private void Reset()
    {
        AutoFindSceneReferences();
    }

    private void Awake()
    {
        AutoFindSceneReferences();
        CacheStartingPoses();

        if (createMeterIfMissing && (meterBar == null || meterMarker == null))
        {
            CreateSimpleMeter();
        }

        if (createMeterIfMissing && balanceTarget == null)
        {
            CreateSimpleTarget();
        }

        meterBarRenderer = meterBar != null ? meterBar.GetComponent<SpriteRenderer>() : null;
        meterMarkerRenderer = meterMarker != null ? meterMarker.GetComponent<SpriteRenderer>() : null;
        balanceTargetRenderer = balanceTarget != null ? balanceTarget.GetComponent<SpriteRenderer>() : null;
        UpdateModeVisibility();
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
        if (createMeterIfMissing && gameMode == GameMode.TargetChaseBalance && balanceTarget == null)
        {
            CreateSimpleTarget();
        }

        UpdateModeVisibility();

        bool isHolding = ActiveHoldAction != null && ActiveHoldAction.IsPressed();

        if (isGameOver)
        {
            if (allowButtonToRestart && Time.time >= gameOverTime + restartDelay && isHolding)
            {
                RestartGame();
            }

            UpdateVisuals(Time.deltaTime);
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateSquirm(Time.deltaTime);

        if (gameMode == GameMode.TargetChaseBalance)
        {
            UpdateTargetChaseBalance(isHolding, Time.deltaTime);
        }
        else
        {
            UpdateClassicBalance(isHolding, Time.deltaTime);
        }

        UpdateVisuals(Time.deltaTime);
        UpdateText();

        if (ShouldLose())
        {
            LoseGame();
        }
    }

    private void UpdateSquirm(float deltaTime)
    {
        squirmTimer -= deltaTime;

        if (squirmTimer <= 0f)
        {
            // Pick a new random force so the player cannot simply memorize one perfect rhythm.
            squirmTarget = Random.Range(-1f, 1f);
            squirmTimer = squirmChangeInterval;
        }

        squirm = Mathf.MoveTowards(squirm, squirmTarget, squirmResponseSpeed * deltaTime);
    }

    private void UpdateClassicBalance(bool isHolding, float deltaTime)
    {
        float currentSpeed = startingSpeed + elapsedTime * speedIncreasePerSecond;

        // Holding the button pushes right; releasing pushes left.
        float buttonDirection = isHolding ? 1f : -1f;
        float drift = buttonDirection * buttonInfluence + squirm * squirmInfluence;

        balance = Mathf.Clamp(balance + drift * currentSpeed * deltaTime, -1f, 1f);
    }

    private void UpdateTargetChaseBalance(bool isHolding, float deltaTime)
    {
        float currentSpeed = startingSpeed + elapsedTime * speedIncreasePerSecond;
        float buttonDirection = isHolding ? 1f : -1f;

        balance = Mathf.Clamp(balance + buttonDirection * buttonInfluence * currentSpeed * deltaTime, -1f, 1f);
        UpdateTargetMovement(deltaTime);

        if (meterMarker != null)
        {
            SetMeterMarkerPosition();
        }

        if (balanceTarget != null)
        {
            SetBalanceTargetPosition();
        }

        isMarkerOnTarget = IsMarkerOverTargetEnough();
        float stabilityChange = isMarkerOnTarget ? onTargetRecoverPerSecond : -offTargetDrainPerSecond;
        stability = Mathf.Clamp01(stability + stabilityChange * deltaTime);
    }

    private void UpdateTargetMovement(float deltaTime)
    {
        targetTimer -= deltaTime;

        if (targetTimer <= 0f || Mathf.Approximately(targetBalance, targetDestination))
        {
            targetDestination = Random.Range(-1f, 1f);
            targetTimer = Random.Range(targetChangeInterval * 0.65f, targetChangeInterval * 1.35f);
        }

        float currentTargetSpeed = targetStartingSpeed + elapsedTime * targetSpeedIncreasePerSecond;
        targetBalance = Mathf.MoveTowards(targetBalance, targetDestination, currentTargetSpeed * deltaTime);
    }

    private void UpdateVisuals(float deltaTime)
    {
        // Exponential smoothing makes the visuals catch up smoothly without depending on frame rate.
        float visualStep = 1f - Mathf.Exp(-armFollowSpeed * deltaTime);
        float babyStep = 1f - Mathf.Exp(-babyFollowSpeed * deltaTime);
        float dangerAmount = GetDangerAmount();
        float fallDirection = Mathf.Sign(balance);

        if (leftArm != null)
        {
            Vector3 targetPosition = leftArmStartPosition + Vector3.up * (balance * armHeightOffset);
            Quaternion targetRotation = leftArmStartRotation * Quaternion.Euler(0f, 0f, -balance * armTiltDegrees);
            leftArm.localPosition = Vector3.Lerp(leftArm.localPosition, targetPosition, visualStep);
            leftArm.localRotation = Quaternion.Slerp(leftArm.localRotation, targetRotation, visualStep);
        }

        if (rightArm != null)
        {
            Vector3 targetPosition = rightArmStartPosition + Vector3.up * (-balance * armHeightOffset);
            Quaternion targetRotation = rightArmStartRotation * Quaternion.Euler(0f, 0f, -balance * armTiltDegrees);
            rightArm.localPosition = Vector3.Lerp(rightArm.localPosition, targetPosition, visualStep);
            rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, targetRotation, visualStep);
        }

        if (baby != null)
        {
            Vector3 targetPosition = babyStartPosition;
            targetPosition.x += balance * babySlideDistance + fallDirection * dangerAmount * babyDangerSlideDistance;
            targetPosition.y += Mathf.Sin(Time.time * 14f) * squirm * babySquirmPosition;
            targetPosition.y -= dangerAmount * babyDangerDropDistance;

            float targetAngle = -balance * babyTiltDegrees + Mathf.Sin(Time.time * 18f) * squirm * babySquirmRotation;
            Quaternion targetRotation = babyStartRotation * Quaternion.Euler(0f, 0f, targetAngle);

            baby.localPosition = Vector3.Lerp(baby.localPosition, targetPosition, babyStep);
            baby.localRotation = Quaternion.Slerp(baby.localRotation, targetRotation, babyStep);
        }

        if (meterMarker != null)
        {
            SetMeterMarkerPosition();
        }

        if (balanceTarget != null)
        {
            SetBalanceTargetPosition();
        }

        if (meterMarkerRenderer != null)
        {
            meterMarkerRenderer.color = Color.Lerp(safeMeterColor, dangerMeterColor, dangerAmount);
        }
    }

    private void SetMeterMarkerPosition()
    {
        RectTransform markerRect = meterMarker as RectTransform;

        if (markerRect != null)
        {
            markerRect.anchoredPosition = new Vector2(balance * GetMarkerTravelHalfWidth(), markerRect.anchoredPosition.y);
            return;
        }

        Vector3 markerPosition = meterMarker.localPosition;
        markerPosition.x = balance * GetMarkerTravelHalfWidth();
        meterMarker.localPosition = markerPosition;
    }

    private void SetBalanceTargetPosition()
    {
        RectTransform targetRect = balanceTarget as RectTransform;

        if (targetRect != null)
        {
            targetRect.anchoredPosition = new Vector2(targetBalance * GetTargetTravelHalfWidth(), targetRect.anchoredPosition.y);
            return;
        }

        Vector3 targetPosition = balanceTarget.localPosition;
        targetPosition.x = targetBalance * GetTargetTravelHalfWidth();
        balanceTarget.localPosition = targetPosition;
    }

    private bool IsMarkerOverTargetEnough()
    {
        if (meterMarker == null || balanceTarget == null)
        {
            return false;
        }

        Transform measurementParent = GetMarkerTravelParent();
        float markerCenter = measurementParent.InverseTransformPoint(meterMarker.position).x;
        float targetCenter = measurementParent.InverseTransformPoint(balanceTarget.position).x;
        float markerHalfWidth = GetObjectHalfWidth(meterMarker, measurementParent, 0.01f);
        float targetHalfWidth = GetObjectHalfWidth(balanceTarget, measurementParent, this.targetHalfWidth * meterHalfWidth);

        float markerLeft = markerCenter - markerHalfWidth;
        float markerRight = markerCenter + markerHalfWidth;
        float targetLeft = targetCenter - targetHalfWidth;
        float targetRight = targetCenter + targetHalfWidth;
        float overlapWidth = Mathf.Min(markerRight, targetRight) - Mathf.Max(markerLeft, targetLeft);

        if (overlapWidth <= 0f)
        {
            return false;
        }

        float smallerWidth = Mathf.Max(0.001f, Mathf.Min(markerHalfWidth * 2f, targetHalfWidth * 2f));
        return overlapWidth / smallerWidth >= requiredTargetOverlap;
    }

    private float GetMarkerTravelHalfWidth()
    {
        return GetTravelHalfWidth(meterMarker, GetMarkerTravelParent(), 0.01f);
    }

    private float GetTargetTravelHalfWidth()
    {
        return GetTravelHalfWidth(balanceTarget, GetTargetTravelParent(), targetHalfWidth * meterHalfWidth);
    }

    private float GetTravelHalfWidth(Transform movingObject, Transform travelParent, float fallbackMovingHalfWidth)
    {
        float barHalfWidth = GetBarHalfWidth(travelParent);
        float movingHalfWidth = GetObjectHalfWidth(movingObject, travelParent, fallbackMovingHalfWidth);
        return Mathf.Max(0f, barHalfWidth - movingHalfWidth);
    }

    private float GetBarHalfWidth(Transform travelParent)
    {
        if (!autoCalculateMeterBounds || meterBar == null)
        {
            return meterHalfWidth;
        }

        return GetObjectHalfWidth(meterBar, travelParent, meterHalfWidth);
    }

    private Transform GetMarkerTravelParent()
    {
        return meterMarker != null && meterMarker.parent != null ? meterMarker.parent : transform;
    }

    private Transform GetTargetTravelParent()
    {
        return balanceTarget != null && balanceTarget.parent != null ? balanceTarget.parent : transform;
    }

    private float GetObjectHalfWidth(Transform objectTransform, Transform measurementParent, float fallbackHalfWidth)
    {
        if (!autoCalculateMeterBounds || objectTransform == null)
        {
            return Mathf.Max(0f, fallbackHalfWidth);
        }

        RectTransform rectTransform = objectTransform as RectTransform;
        if (rectTransform != null)
        {
            return Mathf.Abs(rectTransform.rect.width * rectTransform.localScale.x) * 0.5f;
        }

        Renderer renderer = objectTransform.GetComponent<Renderer>();
        if (renderer == null)
        {
            return Mathf.Max(0f, fallbackHalfWidth);
        }

        Bounds bounds = renderer.bounds;
        Transform parent = measurementParent != null ? measurementParent : transform;
        Vector3 localMin = parent.InverseTransformPoint(bounds.min);
        Vector3 localMax = parent.InverseTransformPoint(bounds.max);
        return Mathf.Abs(localMax.x - localMin.x) * 0.5f;
    }

    private float GetDangerAmount()
    {
        if (gameMode == GameMode.TargetChaseBalance)
        {
            return 1f - stability;
        }

        // Converts the balance value into 0 = safe, 1 = at the edge.
        float dangerRange = Mathf.Max(0.01f, 1f - dangerZoneStartsAt);
        return Mathf.Clamp01((Mathf.Abs(balance) - dangerZoneStartsAt) / dangerRange);
    }

    private bool ShouldLose()
    {
        if (gameMode == GameMode.TargetChaseBalance)
        {
            return stability <= 0f;
        }

        return Mathf.Abs(balance) >= 1f;
    }

    private void LoseGame()
    {
        isGameOver = true;
        gameOverTime = Time.time;
        balance = Mathf.Clamp(balance, -1f, 1f);
        UpdateText();
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
        targetBalance = 0f;
        targetDestination = 0f;
        targetTimer = 0f;
        isMarkerOnTarget = false;
        isGameOver = false;
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText != null)
        {
            timerText.text = gameMode == GameMode.TargetChaseBalance
                ? $"Time: {elapsedTime:0.0}  Stability: {stability:P0}"
                : $"Time: {elapsedTime:0.0}";
        }

        if (statusText == null)
        {
            return;
        }

        if (isGameOver)
        {
            statusText.text = "Dropped! Hold Space or Left Mouse to retry";
            return;
        }

        statusText.text = gameMode == GameMode.TargetChaseBalance
            ? "Stay on the moving target"
            : "Hold Space or Left Mouse";
    }

    private void OnGUI()
    {
        if (!isGameOver && timerText != null && statusText != null)
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

        string modeDetails = gameMode == GameMode.TargetChaseBalance
            ? $"\nStability: {stability:P0}"
            : "";

        string message = isGameOver
            ? $"Dropped!\nTime: {elapsedTime:0.0}{modeDetails}\nHold Space or Left Mouse to retry"
            : $"Time: {elapsedTime:0.0}{modeDetails}";

        GUI.Label(new Rect(20f, 24f, Screen.width - 40f, 180f), message, style);
    }

    [ContextMenu("Auto Find Scene References")]
    private void AutoFindSceneReferences()
    {
        if (leftArm == null)
        {
            leftArm = FindTransformByName("Placeholder_Arm_Left");
        }

        if (rightArm == null)
        {
            rightArm = FindTransformByName("Placeholder_Arm_Right");
        }

        if (baby == null)
        {
            baby = FindTransformByName("Placeholder_Baby");
        }

        if (balanceTarget == null)
        {
            balanceTarget = FindTransformByName("Balance_Target");
        }
    }

    private static Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private void CacheStartingPoses()
    {
        if (leftArm != null)
        {
            leftArmStartPosition = leftArm.localPosition;
            leftArmStartRotation = leftArm.localRotation;
        }

        if (rightArm != null)
        {
            rightArmStartPosition = rightArm.localPosition;
            rightArmStartRotation = rightArm.localRotation;
        }

        if (baby != null)
        {
            babyStartPosition = baby.localPosition;
            babyStartRotation = baby.localRotation;
        }
    }

    private void CreateSimpleMeter()
    {
        Sprite whitePixel = CreateWhitePixelSprite();

        GameObject meterRoot = new GameObject("Generated_BalanceMeter");
        meterRoot.transform.SetParent(transform);
        meterRoot.transform.localPosition = generatedMeterPosition;

        GameObject barObject = new GameObject("Meter_Bar");
        barObject.transform.SetParent(meterRoot.transform);
        barObject.transform.localPosition = Vector3.zero;
        barObject.transform.localScale = new Vector3(meterHalfWidth * 2f, 0.18f, 1f);
        meterBarRenderer = barObject.AddComponent<SpriteRenderer>();
        meterBarRenderer.sprite = whitePixel;
        meterBarRenderer.color = safeMeterColor;
        meterBarRenderer.sortingOrder = 20;
        meterBar = barObject.transform;

        GameObject centerObject = new GameObject("Meter_CenterLine");
        centerObject.transform.SetParent(meterRoot.transform);
        centerObject.transform.localPosition = Vector3.zero;
        centerObject.transform.localScale = new Vector3(0.04f, 0.42f, 1f);
        SpriteRenderer centerRenderer = centerObject.AddComponent<SpriteRenderer>();
        centerRenderer.sprite = whitePixel;
        centerRenderer.color = Color.white;
        centerRenderer.sortingOrder = 21;

        GameObject markerObject = new GameObject("Meter_Marker");
        markerObject.transform.SetParent(meterRoot.transform);
        markerObject.transform.localPosition = Vector3.zero;
        markerObject.transform.localScale = new Vector3(0.12f, 0.65f, 1f);
        meterMarkerRenderer = markerObject.AddComponent<SpriteRenderer>();
        meterMarkerRenderer.sprite = whitePixel;
        meterMarkerRenderer.color = safeMeterColor;
        meterMarkerRenderer.sortingOrder = 22;
        meterMarker = markerObject.transform;
    }

    private void CreateSimpleTarget()
    {
        Sprite whitePixel = CreateWhitePixelSprite();
        Transform targetParent = meterMarker != null ? meterMarker.parent : transform;

        GameObject targetObject = new GameObject("Balance_Target");
        targetObject.transform.SetParent(targetParent);
        targetObject.transform.localPosition = Vector3.zero;
        targetObject.transform.localScale = new Vector3(targetHalfWidth * meterHalfWidth * 2f, 0.5f, 1f);

        balanceTargetRenderer = targetObject.AddComponent<SpriteRenderer>();
        balanceTargetRenderer.sprite = whitePixel;
        balanceTargetRenderer.color = targetColor;
        balanceTargetRenderer.sortingOrder = 21;
        balanceTarget = targetObject.transform;
        UpdateModeVisibility();
    }

    private void UpdateModeVisibility()
    {
        if (balanceTarget == null)
        {
            return;
        }

        bool shouldShowTarget = gameMode == GameMode.TargetChaseBalance;
        if (balanceTarget.gameObject.activeSelf != shouldShowTarget)
        {
            balanceTarget.gameObject.SetActive(shouldShowTarget);
        }
    }

    private static Sprite CreateWhitePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
