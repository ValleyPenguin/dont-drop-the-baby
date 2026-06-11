using UnityEngine;

public class BabyBalanceVisuals : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The left arm/cast object. If left empty, this component looks for an object named Placeholder_Arm_Left.")]
    [SerializeField] private Transform leftArm;

    [Tooltip("The right arm/cast object. If left empty, this component looks for an object named Placeholder_Arm_Right.")]
    [SerializeField] private Transform rightArm;

    [Tooltip("The baby object that slides, wiggles, and tilts as balance gets worse. If left empty, this component looks for Placeholder_Baby.")]
    [SerializeField] private Transform baby;

    [Header("Arm Visuals")]
    [Tooltip("How far the arms move up and down as the balance marker moves. Higher values make the arm height change more dramatic.")]
    [SerializeField] private float armHeightOffset = 0.9f;

    [Tooltip("How many degrees the arms rotate at full imbalance. Higher values make the casts tilt more.")]
    [SerializeField] private float armTiltDegrees = 14f;

    [Tooltip("How quickly the arms catch up to their target positions. Higher values are snappier; lower values are floatier.")]
    [SerializeField, Min(0f)] private float armFollowSpeed = 12f;

    [Header("Baby Visuals")]
    [Tooltip("How close to an edge the marker must be before danger visuals start in Classic Edge Balance mode. 0.72 means danger starts at 72% of the way to either edge.")]
    [SerializeField, Range(0f, 1f)] private float classicDangerZoneStartsAt = 0.72f;

    [Tooltip("How far the baby slides left/right with the balance marker before danger extra sliding is added.")]
    [SerializeField] private float babySlideDistance = 1.15f;

    [Tooltip("Extra sideways slide added only in the danger zone, making the baby look close to falling out.")]
    [SerializeField] private float babyDangerSlideDistance = 0.8f;

    [Tooltip("How far down the baby drops in the danger zone, making the baby look less supported by the arms.")]
    [SerializeField] private float babyDangerDropDistance = 0.65f;

    [Tooltip("How many degrees the baby tilts at full imbalance. Higher values make the almost-falling pose stronger.")]
    [SerializeField] private float babyTiltDegrees = 38f;

    [Tooltip("If true, the baby leans farther in the current target travel direction shortly before the target changes direction.")]
    [SerializeField] private bool babyAnticipatesTargetTurns = true;

    [Tooltip("How many seconds before a target direction change the baby starts leaning into the upcoming turn warning.")]
    [SerializeField, Min(0f)] private float babyTargetTurnWarningTime = 0.85f;

    [Tooltip("Optional distance-to-edge warning. Keep at 0 for timer-only turn anticipation.")]
    [SerializeField, Range(0f, 1f)] private float babyTargetTurnWarningDistance = 0f;

    [Tooltip("Extra sideways baby slide at full target-turn warning.")]
    [SerializeField] private float babyTargetTurnWarningSlideDistance = 1.65f;

    [Tooltip("Extra baby tilt at full target-turn warning.")]
    [SerializeField] private float babyTargetTurnWarningTiltDegrees = 55f;

    [Tooltip("Small up/down wiggle amount from the baby's squirming.")]
    [SerializeField] private float babySquirmPosition = 0.14f;

    [Tooltip("Small rotation wiggle amount from the baby's squirming.")]
    [SerializeField] private float babySquirmRotation = 7f;

    [Tooltip("How quickly the baby catches up to its target pose. Higher values are snappier; lower values are floatier.")]
    [SerializeField, Min(0f)] private float babyFollowSpeed = 10f;

    [Header("Baby Expression Sprites")]
    [Tooltip("Sprite used when the marker is on the target. If empty, this uses the baby's starting sprite.")]
    [SerializeField] private Sprite normieBabySprite;

    [Tooltip("Sprite used when the marker is not on the target.")]
    [SerializeField] private Sprite sadBabySprite;

    [Tooltip("Sprite used when the player is close to losing.")]
    [SerializeField] private Sprite cryingBabySprite;

    [Tooltip("Danger amount where the baby switches to crying. This matches the marker shifting toward orange/red.")]
    [SerializeField, Range(0f, 1f)] private float cryingDangerStartsAt = 0.55f;

    private Vector3 leftArmStartPosition;
    private Vector3 rightArmStartPosition;
    private Vector3 babyStartPosition;
    private Quaternion leftArmStartRotation;
    private Quaternion rightArmStartRotation;
    private Quaternion babyStartRotation;
    private SpriteRenderer babyRenderer;

    private void Reset()
    {
        AutoFindSceneReferences();
        CacheBabyRenderer();
    }

    public void Initialize()
    {
        AutoFindSceneReferences();
        CacheBabyRenderer();
        CacheStartingPoses();
    }

    public void UpdateVisuals(BabyBalanceGame game, BabyBalanceTargetChase targetChase, BabyBalanceMeter meter, float deltaTime)
    {
        if (game == null)
        {
            return;
        }

        float visualStep = 1f - Mathf.Exp(-armFollowSpeed * deltaTime);
        float babyStep = 1f - Mathf.Exp(-babyFollowSpeed * deltaTime);
        float dangerAmount = GetDangerAmount(game);
        float fallDirection = Mathf.Sign(game.Balance);
        float targetTurnWarningAmount = targetChase != null && game.IsTargetChaseMode
            ? targetChase.GetTurnWarningAmount(babyAnticipatesTargetTurns, babyTargetTurnWarningTime, babyTargetTurnWarningDistance)
            : 0f;
        float targetTurnWarningDirection = targetChase != null && game.IsTargetChaseMode
            ? targetChase.TargetMoveDirection
            : 0f;

        UpdateArmVisual(leftArm, leftArmStartPosition, leftArmStartRotation, game.Balance, game.Balance, visualStep);
        UpdateArmVisual(rightArm, rightArmStartPosition, rightArmStartRotation, -game.Balance, game.Balance, visualStep);
        UpdateBabyVisual(game, babyStep, dangerAmount, fallDirection, targetTurnWarningAmount, targetTurnWarningDirection);
        UpdateBabyExpression(game, dangerAmount);

        meter?.SetMarkerPosition(game.Balance);
        meter?.UpdateMarkerColor(dangerAmount);
    }

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

    private void CacheBabyRenderer()
    {
        babyRenderer = baby != null ? baby.GetComponent<SpriteRenderer>() : null;

        if (normieBabySprite == null && babyRenderer != null)
        {
            normieBabySprite = babyRenderer.sprite;
        }
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

    private void UpdateArmVisual(
        Transform arm,
        Vector3 startPosition,
        Quaternion startRotation,
        float heightBalance,
        float rotationBalance,
        float visualStep)
    {
        if (arm == null)
        {
            return;
        }

        Vector3 targetPosition = startPosition + Vector3.up * (heightBalance * armHeightOffset);
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, -rotationBalance * armTiltDegrees);
        arm.localPosition = Vector3.Lerp(arm.localPosition, targetPosition, visualStep);
        arm.localRotation = Quaternion.Slerp(arm.localRotation, targetRotation, visualStep);
    }

    private void UpdateBabyVisual(
        BabyBalanceGame game,
        float babyStep,
        float dangerAmount,
        float fallDirection,
        float targetTurnWarningAmount,
        float targetTurnWarningDirection)
    {
        if (baby == null)
        {
            return;
        }

        Vector3 targetPosition = babyStartPosition;
        targetPosition.x += game.Balance * babySlideDistance + fallDirection * dangerAmount * babyDangerSlideDistance;
        targetPosition.x += targetTurnWarningDirection * targetTurnWarningAmount * babyTargetTurnWarningSlideDistance;
        targetPosition.y += Mathf.Sin(Time.time * 14f) * game.Squirm * babySquirmPosition;
        targetPosition.y -= dangerAmount * babyDangerDropDistance;

        float targetAngle = -game.Balance * babyTiltDegrees + Mathf.Sin(Time.time * 18f) * game.Squirm * babySquirmRotation;
        targetAngle -= targetTurnWarningDirection * targetTurnWarningAmount * babyTargetTurnWarningTiltDegrees;
        Quaternion targetRotation = babyStartRotation * Quaternion.Euler(0f, 0f, targetAngle);

        baby.localPosition = Vector3.Lerp(baby.localPosition, targetPosition, babyStep);
        baby.localRotation = Quaternion.Slerp(baby.localRotation, targetRotation, babyStep);
    }

    private void UpdateBabyExpression(BabyBalanceGame game, float dangerAmount)
    {
        if (babyRenderer == null)
        {
            return;
        }

        Sprite expressionSprite = GetBabyExpressionSprite(game, dangerAmount);
        if (expressionSprite != null && babyRenderer.sprite != expressionSprite)
        {
            babyRenderer.sprite = expressionSprite;
        }
    }

    private Sprite GetBabyExpressionSprite(BabyBalanceGame game, float dangerAmount)
    {
        if (dangerAmount >= cryingDangerStartsAt)
        {
            return cryingBabySprite != null ? cryingBabySprite : sadBabySprite;
        }

        if (game.IsTargetChaseMode && !game.IsMarkerOnTarget)
        {
            return sadBabySprite != null ? sadBabySprite : normieBabySprite;
        }

        return normieBabySprite;
    }

    private float GetDangerAmount(BabyBalanceGame game)
    {
        if (game.IsTargetChaseMode)
        {
            return 1f - game.Stability;
        }

        float dangerRange = Mathf.Max(0.01f, 1f - classicDangerZoneStartsAt);
        return Mathf.Clamp01((Mathf.Abs(game.Balance) - classicDangerZoneStartsAt) / dangerRange);
    }

    private static Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }
}
