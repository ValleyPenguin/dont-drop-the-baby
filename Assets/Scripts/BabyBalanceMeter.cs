using UnityEngine;

public class BabyBalanceMeter : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The moving line/marker on the balance meter. Its local X position represents the current balance value.")]
    [SerializeField] private Transform meterMarker;

    [Tooltip("The moving target for Target Chase Balance mode. If left empty, this component can create a simple target sprite at runtime.")]
    [SerializeField] private Transform balanceTarget;

    [Tooltip("The background bar for the balance meter. This is optional unless you want to use your own meter art.")]
    [SerializeField] private Transform meterBar;

    [Header("Generated Meter")]
    [Tooltip("If true, this component creates a simple bar and marker at runtime when Meter Bar or Meter Marker is empty.")]
    [SerializeField] private bool createMeterIfMissing = true;

    [Tooltip("World position for the generated meter, relative to this GameObject.")]
    [SerializeField] private Vector2 generatedMeterPosition = new Vector2(0f, 4.1f);

    [Tooltip("If true, the component measures the bar, marker, and target sizes so their edges line up correctly. Turn off only if you want to tune Meter Half Width manually.")]
    [SerializeField] private bool autoCalculateMeterBounds = true;

    [Tooltip("Fallback half-width of the meter, used when Auto Calculate Meter Bounds is off or the component cannot measure the bar.")]
    [SerializeField, Min(0.1f)] private float meterHalfWidth = 3f;

    [Header("Meter Colors")]
    [Tooltip("Marker color when the baby is safely balanced near the middle.")]
    [SerializeField] private Color safeMeterColor = new Color(0.15f, 0.9f, 0.45f);

    [Tooltip("Marker color when the baby is close to being dropped.")]
    [SerializeField] private Color dangerMeterColor = new Color(1f, 0.28f, 0.2f);

    private SpriteRenderer meterBarRenderer;
    private SpriteRenderer meterMarkerRenderer;
    private SpriteRenderer balanceTargetRenderer;
    private float targetHalfWidthFallback = 0.16f;

    public void Reset()
    {
        AutoFindSceneReferences();
    }

    public void Initialize()
    {
        AutoFindSceneReferences();

        if (createMeterIfMissing && (meterBar == null || meterMarker == null))
        {
            CreateSimpleMeter();
        }

        CacheRenderers();
    }

    public void EnsureTargetIfMissing(Color targetColor, float targetHalfWidth)
    {
        targetHalfWidthFallback = targetHalfWidth;

        if (createMeterIfMissing && balanceTarget == null)
        {
            CreateSimpleTarget(targetColor, targetHalfWidth);
        }

        if (balanceTargetRenderer == null && balanceTarget != null)
        {
            balanceTargetRenderer = balanceTarget.GetComponent<SpriteRenderer>();
        }
    }

    public void SetTargetVisible(bool shouldShowTarget)
    {
        if (balanceTarget == null)
        {
            return;
        }

        if (balanceTarget.gameObject.activeSelf != shouldShowTarget)
        {
            balanceTarget.gameObject.SetActive(shouldShowTarget);
        }
    }

    public void SetMarkerPosition(float balance)
    {
        if (meterMarker == null)
        {
            return;
        }

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

    public void SetTargetPosition(float targetBalance)
    {
        if (balanceTarget == null)
        {
            return;
        }

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

    public void UpdateMarkerColor(float dangerAmount)
    {
        if (meterMarkerRenderer != null)
        {
            meterMarkerRenderer.color = Color.Lerp(safeMeterColor, dangerMeterColor, dangerAmount);
        }
    }

    public bool IsMarkerOverTargetEnough(float requiredTargetOverlap, float targetHalfWidth)
    {
        if (meterMarker == null || balanceTarget == null)
        {
            return false;
        }

        Transform measurementParent = GetMarkerTravelParent();
        float markerCenter = measurementParent.InverseTransformPoint(meterMarker.position).x;
        float targetCenter = measurementParent.InverseTransformPoint(balanceTarget.position).x;
        float markerHalfWidth = GetObjectHalfWidth(meterMarker, measurementParent, 0.01f);
        float targetHalfWidthWorld = GetObjectHalfWidth(balanceTarget, measurementParent, targetHalfWidth * meterHalfWidth);

        float markerLeft = markerCenter - markerHalfWidth;
        float markerRight = markerCenter + markerHalfWidth;
        float targetLeft = targetCenter - targetHalfWidthWorld;
        float targetRight = targetCenter + targetHalfWidthWorld;
        float overlapWidth = Mathf.Min(markerRight, targetRight) - Mathf.Max(markerLeft, targetLeft);

        if (overlapWidth <= 0f)
        {
            return false;
        }

        float smallerWidth = Mathf.Max(0.001f, Mathf.Min(markerHalfWidth * 2f, targetHalfWidthWorld * 2f));
        return overlapWidth / smallerWidth >= requiredTargetOverlap;
    }

    private void AutoFindSceneReferences()
    {
        if (meterMarker == null)
        {
            meterMarker = FindTransformByName("Meter_Marker");
        }

        if (balanceTarget == null)
        {
            balanceTarget = FindTransformByName("Balance_Target");
        }

        if (meterBar == null)
        {
            meterBar = FindTransformByName("Meter_Bar");
        }
    }

    private void CacheRenderers()
    {
        meterBarRenderer = meterBar != null ? meterBar.GetComponent<SpriteRenderer>() : null;
        meterMarkerRenderer = meterMarker != null ? meterMarker.GetComponent<SpriteRenderer>() : null;
        balanceTargetRenderer = balanceTarget != null ? balanceTarget.GetComponent<SpriteRenderer>() : null;
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

    private void CreateSimpleTarget(Color targetColor, float targetHalfWidth)
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
    }

    private float GetMarkerTravelHalfWidth()
    {
        return GetTravelHalfWidth(meterMarker, GetMarkerTravelParent(), 0.01f);
    }

    private float GetTargetTravelHalfWidth()
    {
        return GetTravelHalfWidth(balanceTarget, GetTargetTravelParent(), targetHalfWidthFallback * meterHalfWidth);
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

    private static Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
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
