using UnityEngine;

public class SteeringAgent : MonoBehaviour
{
    public enum BehaviorState
    {
        Arrive,
        Wander,
        Flee,
        Avoiding
    }

    [Header("Target")]

    [SerializeField]
    private Transform target;

    [SerializeField]
    private bool useTarget = true;

    [Header("Movement")]

    [SerializeField]
    private float maxSpeed = 4f;

    [SerializeField]
    private float maxAcceleration = 8f;

    [SerializeField]
    private float turnSpeed = 8f;

    [Header("Arrive")]

    [SerializeField]
    private float slowRadius = 4f;

    [SerializeField]
    private float stopRadius = 1.5f;

    [Header("Wander")]

    [SerializeField]
    private float wanderSpeed = 2.5f;

    [SerializeField]
    private float wanderChangeInterval = 1.5f;

    [SerializeField]
    private float wanderAngleChange = 45f;

    [Header("Flee")]

    [SerializeField]
    private bool fleeEnabled = false;

    [SerializeField]
    private Transform fleeTarget;

    [SerializeField]
    private float fleeRadius = 3f;

    [Header("Obstacle Avoidance")]

    [SerializeField]
    private SteeringSensor sensor;

    [SerializeField]
    private float avoidanceWeight = 2.5f;

    [Header("Behavior Visualization")]

    [SerializeField]
    private Renderer bodyRenderer;

    [SerializeField]
    private Color arriveColor = Color.green;

    [SerializeField]
    private Color wanderColor = Color.cyan;

    [SerializeField]
    private Color fleeColor = Color.yellow;

    [SerializeField]
    private Color avoidingColor = Color.red;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private Vector3 velocity;

    private Vector3 wanderDirection;

    private float wanderTimer;

    private BehaviorState currentBehavior;

    private MaterialPropertyBlock propertyBlock;

    public Vector3 Velocity => velocity;

    public BehaviorState CurrentBehavior => currentBehavior;

    private void Start()
    {
        wanderDirection = transform.forward;
        wanderTimer = wanderChangeInterval;

        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        Vector3 desiredVelocity;

        if (IsFleeTriggered())
        {
            currentBehavior = BehaviorState.Flee;
            desiredVelocity = CalculateFlee();
        }
        else if (useTarget && target != null)
        {
            currentBehavior = BehaviorState.Arrive;
            desiredVelocity = CalculateArrive();
        }
        else
        {
            currentBehavior = BehaviorState.Wander;
            desiredVelocity = CalculateWander();
        }

        desiredVelocity =
            ApplyObstacleAvoidance(desiredVelocity);

        if (sensor != null && sensor.ObstacleDetected)
        {
            currentBehavior = BehaviorState.Avoiding;
        }

        UpdateBehaviorColor();

        velocity =
            Vector3.MoveTowards(
                velocity,
                desiredVelocity,
                maxAcceleration * Time.deltaTime
            );

        velocity =
            Vector3.ClampMagnitude(
                velocity,
                maxSpeed
            );

        ApplyMovement();

        UpdateRotation();
    }

    private Vector3 CalculateArrive()
    {
        Vector3 toTarget =
            target.position - transform.position;

        toTarget.y = 0f;

        float distance = toTarget.magnitude;

        if (distance <= stopRadius)
        {
            return Vector3.zero;
        }

        float desiredSpeed = maxSpeed;

        if (distance < slowRadius)
        {
            float range =
                Mathf.Max(
                    slowRadius - stopRadius,
                    0.001f
                );

            float normalizedDistance =
                (distance - stopRadius) / range;

            desiredSpeed =
                maxSpeed *
                Mathf.Clamp01(normalizedDistance);
        }

        return toTarget.normalized * desiredSpeed;
    }

    private bool IsFleeTriggered()
    {
        if (!fleeEnabled || fleeTarget == null)
        {
            return false;
        }

        Vector3 fromTarget =
            transform.position - fleeTarget.position;

        fromTarget.y = 0f;

        return fromTarget.sqrMagnitude <=
            fleeRadius * fleeRadius;
    }

    private Vector3 CalculateFlee()
    {
        Vector3 fromTarget =
            transform.position - fleeTarget.position;

        fromTarget.y = 0f;

        if (fromTarget.sqrMagnitude < 0.001f)
        {
            fromTarget = transform.forward;
        }

        return fromTarget.normalized * maxSpeed;
    }

    private Vector3 CalculateWander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            float randomAngle =
                Random.Range(
                    -wanderAngleChange,
                    wanderAngleChange
                );

            wanderDirection =
                Quaternion.Euler(
                    0f,
                    randomAngle,
                    0f
                ) * transform.forward;

            wanderDirection.y = 0f;
            wanderDirection.Normalize();

            wanderTimer = wanderChangeInterval;
        }

        return wanderDirection * wanderSpeed;
    }

    private Vector3 ApplyObstacleAvoidance(
        Vector3 desiredVelocity)
    {
        if (sensor == null)
        {
            return desiredVelocity;
        }

        Vector3 checkDirection =
            desiredVelocity.sqrMagnitude > 0.001f
                ? desiredVelocity.normalized
                : transform.forward;

        Vector3 avoidanceDirection =
            sensor.GetAvoidanceDirection(
                checkDirection
            );

        if (avoidanceDirection.sqrMagnitude > 0.001f)
        {
            Vector3 combinedDirection =
                checkDirection +
                avoidanceDirection *
                avoidanceWeight;

            combinedDirection.y = 0f;

            if (combinedDirection.sqrMagnitude > 0.001f)
            {
                combinedDirection.Normalize();
            }

            float desiredSpeed =
                Mathf.Max(
                    desiredVelocity.magnitude,
                    wanderSpeed
                );

            return combinedDirection * desiredSpeed;
        }

        return desiredVelocity;
    }

    private void UpdateBehaviorColor()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        Color color = arriveColor;

        switch (currentBehavior)
        {
            case BehaviorState.Wander:
                color = wanderColor;
                break;
            case BehaviorState.Flee:
                color = fleeColor;
                break;
            case BehaviorState.Avoiding:
                color = avoidingColor;
                break;
        }

        bodyRenderer.GetPropertyBlock(propertyBlock);

        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);

        bodyRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyMovement()
    {
        transform.position +=
            velocity * Time.deltaTime;
    }

    private void UpdateRotation()
    {
        Vector3 horizontalVelocity = velocity;
        horizontalVelocity.y = 0f;

        if (horizontalVelocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                horizontalVelocity.normalized
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            stopRadius
        );

        Gizmos.DrawWireSphere(
            transform.position,
            slowRadius
        );

        if (target != null)
        {
            Gizmos.DrawLine(
                transform.position,
                target.position
            );
        }

        if (fleeEnabled)
        {
            Gizmos.color = Color.yellow;

            Gizmos.DrawWireSphere(
                transform.position,
                fleeRadius
            );

            Gizmos.color = Color.white;

            if (fleeTarget != null)
            {
                Gizmos.DrawLine(
                    transform.position,
                    fleeTarget.position
                );
            }
        }
    }
}