using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BabyBalanceGame : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private Transform baby;
    [SerializeField] private Transform meterMarker;
    [SerializeField] private Transform meterBar;
    [SerializeField] private Text statusText;
    [SerializeField] private Text timerText;

    [Header("Input")]
    [Tooltip("Optional. If empty, the script creates a new Input System action using Space and Left Mouse Button.")]
    [SerializeField] private InputActionReference holdButton;

    [Header("Balance")]
    [SerializeField, Min(0f)] private float startingSpeed = 0.55f;
    [SerializeField, Min(0f)] private float speedIncreasePerSecond = 0.025f;
    [SerializeField, Min(0f)] private float buttonInfluence = 1f;
    [SerializeField, Min(0f)] private float squirmInfluence = 0.42f;
    [SerializeField, Min(0.01f)] private float squirmChangeInterval = 0.55f;
    [SerializeField, Min(0f)] private float squirmResponseSpeed = 2.2f;
    [SerializeField, Range(0f, 1f)] private float dangerZoneStartsAt = 0.72f;

    [Header("Arm Visuals")]
    [SerializeField] private float armHeightOffset = 0.9f;
    [SerializeField] private float armTiltDegrees = 14f;
    [SerializeField, Min(0f)] private float armFollowSpeed = 12f;

    [Header("Baby Visuals")]
    [SerializeField] private float babySlideDistance = 1.15f;
    [SerializeField] private float babyDangerSlideDistance = 0.8f;
    [SerializeField] private float babyDangerDropDistance = 0.65f;
    [SerializeField] private float babyTiltDegrees = 38f;
    [SerializeField] private float babySquirmPosition = 0.14f;
    [SerializeField] private float babySquirmRotation = 7f;
    [SerializeField, Min(0f)] private float babyFollowSpeed = 10f;

    [Header("Meter Visuals")]
    [SerializeField] private bool createMeterIfMissing = true;
    [SerializeField] private Vector2 generatedMeterPosition = new Vector2(0f, 4.1f);
    [SerializeField, Min(0.1f)] private float meterHalfWidth = 3f;
    [SerializeField] private Color safeMeterColor = new Color(0.15f, 0.9f, 0.45f);
    [SerializeField] private Color dangerMeterColor = new Color(1f, 0.28f, 0.2f);

    [Header("Restart")]
    [SerializeField] private bool allowButtonToRestart = true;
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
    private float balance;
    private float elapsedTime;
    private float gameOverTime;
    private float squirm;
    private float squirmTarget;
    private float squirmTimer;
    private bool isGameOver;

    public float Balance => balance;
    public float ElapsedTime => elapsedTime;
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

        meterBarRenderer = meterBar != null ? meterBar.GetComponent<SpriteRenderer>() : null;
        meterMarkerRenderer = meterMarker != null ? meterMarker.GetComponent<SpriteRenderer>() : null;
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
        UpdateBalance(isHolding, Time.deltaTime);
        UpdateVisuals(Time.deltaTime);
        UpdateText();

        if (Mathf.Abs(balance) >= 1f)
        {
            LoseGame();
        }
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

    private void UpdateBalance(bool isHolding, float deltaTime)
    {
        float currentSpeed = startingSpeed + elapsedTime * speedIncreasePerSecond;
        float buttonDirection = isHolding ? -1f : 1f;
        float drift = buttonDirection * buttonInfluence + squirm * squirmInfluence;

        balance = Mathf.Clamp(balance + drift * currentSpeed * deltaTime, -1f, 1f);
    }

    private void UpdateVisuals(float deltaTime)
    {
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
            markerRect.anchoredPosition = new Vector2(balance * meterHalfWidth, markerRect.anchoredPosition.y);
            return;
        }

        Vector3 markerPosition = meterMarker.localPosition;
        markerPosition.x = balance * meterHalfWidth;
        meterMarker.localPosition = markerPosition;
    }

    private float GetDangerAmount()
    {
        float dangerRange = Mathf.Max(0.01f, 1f - dangerZoneStartsAt);
        return Mathf.Clamp01((Mathf.Abs(balance) - dangerZoneStartsAt) / dangerRange);
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
        squirm = 0f;
        squirmTarget = 0f;
        squirmTimer = 0f;
        isGameOver = false;
        UpdateText();
    }

    private void UpdateText()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {elapsedTime:0.0}";
        }

        if (statusText == null)
        {
            return;
        }

        statusText.text = isGameOver
            ? "Dropped! Hold Space or Left Mouse to retry"
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
            fontSize = 26,
            normal = { textColor = Color.white }
        };

        string message = isGameOver
            ? $"Dropped!\nTime: {elapsedTime:0.0}\nHold Space or Left Mouse to retry"
            : $"Time: {elapsedTime:0.0}";

        GUI.Label(new Rect(0f, 16f, Screen.width, 100f), message, style);
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
