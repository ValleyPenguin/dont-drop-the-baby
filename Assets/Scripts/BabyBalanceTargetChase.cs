using UnityEngine;

public class BabyBalanceTargetChase : MonoBehaviour
{
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

    [Tooltip("If true, target changes can prefer reversals, but still occasionally continue in the same direction for less predictable movement.")]
    [SerializeField] private bool targetSwitchDirectionsOnInterval = true;

    [Tooltip("Chance that a scheduled target change reverses direction. Use 1 for a guaranteed slow-down, turn, speed-up rhythm.")]
    [SerializeField, Range(0f, 1f)] private float targetDirectionReverseChance = 1f;

    [Tooltip("Minimum normalized distance the target tries to travel when switching directions. Higher values make reversals more noticeable.")]
    [SerializeField, Range(0f, 1f)] private float targetDirectionSwitchMinDistance = 0.25f;

    [Tooltip("Maximum normalized distance the target tries to travel when switching directions. Lower values prevent long edge-to-edge sweeps as speed increases.")]
    [SerializeField, Range(0f, 1f)] private float targetDirectionSwitchMaxDistance = 0.65f;

    [Tooltip("Keeps random target destinations this far away from the meter edges when possible.")]
    [SerializeField, Range(0f, 0.45f)] private float targetEdgePadding = 0.08f;

    [Tooltip("If the target gets this close to its destination before the timer ends, it immediately chooses a new destination instead of stopping.")]
    [SerializeField, Range(0.001f, 0.2f)] private float targetArrivalRetargetDistance = 0.025f;

    [Tooltip("Adds extra random variation to each target change interval. 0.35 means intervals range from 65% to 135% of the base interval.")]
    [SerializeField, Range(0f, 0.9f)] private float targetChangeIntervalJitter = 0.45f;

    [Tooltip("If true, the target eases down shortly before choosing its next direction.")]
    [SerializeField] private bool slowTargetBeforeDirectionChange = true;

    [Tooltip("How many seconds before a direction change the target begins slowing down.")]
    [SerializeField, Min(0f)] private float targetPreTurnSlowdownTime = 0.85f;

    [Tooltip("Unused by default. Keep at 0 for timer-only slowdown, or raise it if you want distance-to-edge to also affect the warning.")]
    [SerializeField, Range(0f, 1f)] private float targetPreTurnSlowdownDistance = 0f;

    [Tooltip("Target speed multiplier at the end of the slowdown window. 0.35 means the target keeps moving, but very visibly slows before turning.")]
    [SerializeField, Range(0.05f, 1f)] private float targetPreTurnSpeedMultiplier = 0.25f;

    [Tooltip("Shape of the pre-turn slowdown. Higher values keep speed normal longer, then slow more sharply at the end.")]
    [SerializeField, Min(0.1f)] private float targetPreTurnSlowdownCurve = 0.65f;

    [Tooltip("How quickly the target eases down to its pre-turn speed. Higher values make the slowdown happen sooner and more visibly.")]
    [SerializeField, Min(0.1f)] private float targetSlowdownResponseSpeed = 8f;

    [Tooltip("How quickly the target eases back up to full speed after choosing a new direction.")]
    [SerializeField, Min(0.1f)] private float targetSpeedUpResponseSpeed = 4f;

    [Tooltip("How quickly stability drains while the marker is not touching the moving target.")]
    [SerializeField, Min(0f)] private float offTargetDrainPerSecond = 0.35f;

    [Tooltip("How quickly stability recovers while the marker is touching the moving target.")]
    [SerializeField, Min(0f)] private float onTargetRecoverPerSecond = 0.18f;

    [Tooltip("The color of the generated target sprite in Target Chase Balance mode.")]
    [SerializeField] private Color targetColor = new Color(1f, 0.86f, 0.18f);

    private float targetBalance;
    private float targetDestination;
    private float targetTimer;
    private float targetMoveDirection;
    private float currentTargetSpeedMultiplier = 1f;
    private bool isMarkerOnTarget;

    public bool IsMarkerOnTarget => isMarkerOnTarget;
    public float TargetMoveDirection => !Mathf.Approximately(targetMoveDirection, 0f)
        ? Mathf.Sign(targetMoveDirection)
        : Mathf.Sign(targetDestination - targetBalance);

    public void Initialize(BabyBalanceMeter meter)
    {
        meter?.EnsureTargetIfMissing(targetColor, targetHalfWidth);
    }

    public void UpdateTargetChase(BabyBalanceGame game, BabyBalanceMeter meter, bool isHolding, float deltaTime)
    {
        if (game == null)
        {
            return;
        }

        float buttonDirection = isHolding ? 1f : -1f;
        game.SetBalance(game.Balance + buttonDirection * game.ButtonInfluence * game.CurrentBalanceSpeed * deltaTime);

        UpdateTargetMovement(game.ElapsedTime, deltaTime);
        meter?.SetMarkerPosition(game.Balance);
        meter?.SetTargetPosition(targetBalance);

        isMarkerOnTarget = meter != null && meter.IsMarkerOverTargetEnough(requiredTargetOverlap, targetHalfWidth);
        float stabilityChange = isMarkerOnTarget ? onTargetRecoverPerSecond : -offTargetDrainPerSecond;
        game.SetStability(game.Stability + stabilityChange * deltaTime);
    }

    public float GetTurnWarningAmount(bool enabled, float warningTime, float warningDistance)
    {
        if (!enabled)
        {
            return 0f;
        }

        float warningAmount = GetPreTurnAmount(warningTime, warningDistance);
        return Mathf.SmoothStep(0f, 1f, warningAmount);
    }

    public void ResetState()
    {
        targetBalance = 0f;
        targetDestination = 0f;
        targetTimer = 0f;
        targetMoveDirection = 0f;
        currentTargetSpeedMultiplier = 1f;
        isMarkerOnTarget = false;
    }

    private void UpdateTargetMovement(float elapsedTime, float deltaTime)
    {
        targetTimer -= deltaTime;

        if (Mathf.Approximately(targetMoveDirection, 0f))
        {
            PickNextTargetDestination();
            ResetTargetTimer();
        }

        float currentTargetSpeed = targetStartingSpeed + elapsedTime * targetSpeedIncreasePerSecond;
        currentTargetSpeed *= GetSmoothedTargetSpeedMultiplier(deltaTime);
        targetBalance += targetMoveDirection * currentTargetSpeed * deltaTime;

        if (HasHitPaddedEdge())
        {
            targetBalance = ClampToPaddedRange(targetBalance);
            targetMoveDirection = -Mathf.Sign(targetMoveDirection);
            targetDestination = PickBoundedTargetDestination(targetMoveDirection);
            ResetTargetTimer();
        }

        if (targetTimer <= 0f)
        {
            PickNextTargetDestination();
            ResetTargetTimer();
        }
    }

    private void PickNextTargetDestination()
    {
        float previousDirection = Mathf.Approximately(targetMoveDirection, 0f)
            ? Mathf.Sign(targetDestination - targetBalance)
            : targetMoveDirection;

        if (!targetSwitchDirectionsOnInterval)
        {
            targetDestination = PickRandomDestinationAwayFromCurrent();
            targetMoveDirection = Mathf.Sign(targetDestination - targetBalance);
            return;
        }

        float randomDirection = Random.value < 0.5f ? -1f : 1f;
        float nextDirection = PickNextDirection(previousDirection, randomDirection);

        targetDestination = PickBoundedTargetDestination(nextDirection);
        targetMoveDirection = Mathf.Sign(targetDestination - targetBalance);
    }

    private float PickNextDirection(float previousDirection, float randomDirection)
    {
        if (Mathf.Approximately(previousDirection, 0f))
        {
            return randomDirection;
        }

        float direction = Random.value <= targetDirectionReverseChance
            ? -Mathf.Sign(previousDirection)
            : Mathf.Sign(previousDirection);

        if (WouldHitEdge(direction))
        {
            direction = -direction;
        }

        return direction;
    }

    private float PickBoundedTargetDestination(float direction)
    {
        if (Mathf.Approximately(direction, 0f))
        {
            return PickRandomDestinationAwayFromCurrent();
        }

        float minimumDistance = Mathf.Clamp01(targetDirectionSwitchMinDistance);
        float maximumDistance = Mathf.Clamp01(Mathf.Max(minimumDistance, targetDirectionSwitchMaxDistance));
        float availableDistance = GetAvailableDistance(direction);

        if (availableDistance <= targetArrivalRetargetDistance)
        {
            direction = -Mathf.Sign(direction);
            availableDistance = GetAvailableDistance(direction);
        }

        if (availableDistance <= targetArrivalRetargetDistance)
        {
            return ClampToPaddedRange(0f);
        }

        float distance = Random.Range(Mathf.Min(minimumDistance, availableDistance), Mathf.Min(maximumDistance, availableDistance));
        float destination = targetBalance + Mathf.Sign(direction) * distance;

        return ClampToPaddedRange(destination);
    }

    private float GetSmoothedTargetSpeedMultiplier(float deltaTime)
    {
        float targetSpeedMultiplier = GetTargetPreTurnSpeedMultiplier();
        float responseSpeed = targetSpeedMultiplier < currentTargetSpeedMultiplier
            ? targetSlowdownResponseSpeed
            : targetSpeedUpResponseSpeed;
        float interpolation = 1f - Mathf.Exp(-responseSpeed * deltaTime);

        currentTargetSpeedMultiplier = Mathf.Lerp(currentTargetSpeedMultiplier, targetSpeedMultiplier, interpolation);
        return currentTargetSpeedMultiplier;
    }

    private float GetTargetPreTurnSpeedMultiplier()
    {
        if (!slowTargetBeforeDirectionChange)
        {
            return 1f;
        }

        float slowdownAmount = GetPreTurnAmount(targetPreTurnSlowdownTime, targetPreTurnSlowdownDistance);
        float curvedSlowdown = Mathf.Pow(slowdownAmount, targetPreTurnSlowdownCurve);
        return Mathf.Lerp(1f, targetPreTurnSpeedMultiplier, curvedSlowdown);
    }

    private float GetPreTurnAmount(float warningTime, float warningDistance)
    {
        float timeAmount = warningTime <= 0f
            ? 0f
            : Mathf.Clamp01(1f - targetTimer / warningTime);
        float distanceAmount = 0f;

        if (warningDistance > 0f)
        {
            float distanceToEdge = targetMoveDirection < 0f
                ? targetBalance - (-1f + targetEdgePadding)
                : (1f - targetEdgePadding) - targetBalance;
            distanceAmount = Mathf.Clamp01(1f - distanceToEdge / warningDistance);
        }

        return Mathf.Max(timeAmount, distanceAmount);
    }

    private void ResetTargetTimer()
    {
        float jitter = Mathf.Clamp01(targetChangeIntervalJitter);
        targetTimer = Random.Range(targetChangeInterval * (1f - jitter), targetChangeInterval * (1f + jitter));
    }

    private bool HasHitPaddedEdge()
    {
        float paddedMin = -1f + targetEdgePadding;
        float paddedMax = 1f - targetEdgePadding;
        return targetBalance <= paddedMin || targetBalance >= paddedMax;
    }

    private bool WouldHitEdge(float direction)
    {
        return GetAvailableDistance(direction) <= targetDirectionSwitchMinDistance;
    }

    private float GetAvailableDistance(float direction)
    {
        float paddedMin = -1f + targetEdgePadding;
        float paddedMax = 1f - targetEdgePadding;
        return direction < 0f
            ? Mathf.Max(0f, targetBalance - paddedMin)
            : Mathf.Max(0f, paddedMax - targetBalance);
    }

    private float PickRandomDestinationAwayFromCurrent()
    {
        float paddedMin = -1f + targetEdgePadding;
        float paddedMax = 1f - targetEdgePadding;
        float destination = Random.Range(paddedMin, paddedMax);

        if (Mathf.Abs(destination - targetBalance) >= targetArrivalRetargetDistance)
        {
            return destination;
        }

        float direction = Random.value < 0.5f ? -1f : 1f;
        return PickBoundedTargetDestination(direction);
    }

    private float ClampToPaddedRange(float value)
    {
        return Mathf.Clamp(value, -1f + targetEdgePadding, 1f - targetEdgePadding);
    }
}
