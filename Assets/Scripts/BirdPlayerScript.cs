
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;


public class BirdPlayerScript : MonoBehaviour
{
    [Header("Ground Navigation")]
    public float groundSpeed = 4f;
    private NavMeshAgent agent; 

    [Header("Flight Steering")]
    public float flightSpeed = 8f;
    public float flightAcceleration = 12f;
    public float flightHeight = 3f;

    private Vector3 flightVelocity;
    private Vector3 flightTarget;
    private bool isFlying = false;
    private bool hasFlightTarget = false;

    [Header("Wander Steering")]
    public float wanderRadius = 3f;
    public float wanderDistance = 5f;
    public float wanderChangeRate = 2f;

    private float wanderAngle = 0f;
    private float wanderTimer = 0f;

    [Header("References")]
    public Camera mainCamera;

    [Header("Animation")]
    public Animator wingAnimator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (wingAnimator != null)
            wingAnimator.enabled = false;
        if (mainCamera == null)
            mainCamera = Camera.main;

        agent.speed = groundSpeed;
        agent.updateRotation = false;
    }

    void Update()
    {
        if (Keyboard.current == null ||
            Mouse.current == null)
            return;

        // F switches between ground and flight.
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (isFlying)
                Land();
            else
                TakeOff();
        }

        // Left-click selects a destination.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetTarget();
        }

        if (isFlying)
            Fly();
        else
            Walk();
    }

    void Walk()
    {
        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    10f * Time.deltaTime
                );
            }
        }
    }

    void TakeOff()
    {
        agent.isStopped = true;
        agent.enabled = false;

        isFlying = true;
        flightVelocity = Vector3.zero;
        hasFlightTarget = false;
        if (wingAnimator != null)
            wingAnimator.enabled = true;
        wanderAngle = Random.Range(0f, 360f);
        wanderTimer = 0f;

        transform.position += Vector3.up * flightHeight;
    }

    void Fly()
    {
        Vector3 desiredVelocity;

        if (hasFlightTarget)
        {
            // SEEK: steer toward the selected target.
            Vector3 direction = flightTarget - transform.position;

            if (direction.magnitude < 0.5f)
            {
                flightVelocity = Vector3.zero;
                hasFlightTarget = false;
                return;
            }

            desiredVelocity = direction.normalized * flightSpeed;
        }
        else
        {
            // WANDER: periodically change flight direction.
            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f)
            {
                wanderAngle += Random.Range(-90f, 90f);
                wanderTimer = 1f / Mathf.Max(
                    0.1f,
                    wanderChangeRate
                );
            }

            Vector3 forward = flightVelocity.sqrMagnitude > 0.1f
                ? flightVelocity.normalized
                : transform.forward;

            // Centre of the imaginary circle ahead.
            Vector3 circleCenter = forward * wanderDistance;

            float angleRadians = wanderAngle * Mathf.Deg2Rad;

            Vector3 circleOffset = new Vector3(
                Mathf.Cos(angleRadians),
                0f,
                Mathf.Sin(angleRadians)
            ) * wanderRadius;

            Vector3 wanderDirection =
                (circleCenter + circleOffset).normalized;

            desiredVelocity = wanderDirection * flightSpeed;
        }

        // Steering force: desired velocity - current velocity.
        Vector3 steering = desiredVelocity - flightVelocity;

        flightVelocity += Vector3.ClampMagnitude(
            steering,
            flightAcceleration
        ) * Time.deltaTime;

        flightVelocity = Vector3.ClampMagnitude(
            flightVelocity,
            flightSpeed
        );

        transform.position += flightVelocity * Time.deltaTime;

        if (flightVelocity.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(flightVelocity),
                5f * Time.deltaTime
            );
        }
    }

    void SetTarget()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        if (isFlying)
        {
            // Flight targets lie on a horizontal plane.
            Plane plane = new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    flightHeight,
                    0f
                )
            );

            if (plane.Raycast(ray, out float distance))
            {
                flightTarget = ray.GetPoint(distance);
                hasFlightTarget = true;
            }
        }
        else
        {
            // Ground targets must lie on the NavMesh.
            if (Physics.Raycast(
                ray,
                out RaycastHit hit,
                200f))
            {
                if (NavMesh.SamplePosition(
                    hit.point,
                    out NavMeshHit navHit,
                    2f,
                    NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                    agent.isStopped = false;
                }
            }
        }
    }

    void Land()
    {
        // Search for walkable ground below the bird.
        Vector3 groundPosition = transform.position;
        groundPosition.y -= flightHeight;

        if (NavMesh.SamplePosition(
            groundPosition,
            out NavMeshHit hit,
            3f,
            NavMesh.AllAreas))
        {
            transform.position = hit.position;

            flightVelocity = Vector3.zero;
            hasFlightTarget = false;
            isFlying = false;
            if (wingAnimator != null)
                wingAnimator.enabled = false;
            agent.enabled = true;
            agent.Warp(hit.position);
            agent.isStopped = false;
            agent.ResetPath();
        }
        else
        {
            Debug.LogWarning(
                "No NavMesh nearby. Move closer to walkable ground."
            );
        }
    }
}
