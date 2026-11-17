using UnityEngine;

public class Train : MonoBehaviour
{
    private const int BogeyPositionIterations = 10;

    public SplineManager splineManager;

    public bool attached = false;

    public int attachedTo = 0;

    public float currentT = 0f;

    public float velocity = 0f;

    public float acceleration = 0f;

    public float mass = 1000f;

    public float damping = 0.05f;

    [Range(0f, 1f)]
    public float throttle = 0f;

    public float maxThrottle = 1000f;
    
    [Range(0f, 1f)]
    public float brake = 0f;

    public float maxBrake = 5000f;

    public float carLength = 8f;

    void Awake()
    {
        Time.fixedDeltaTime = 0.02f;
    }

    void Start()
    {
        if (splineManager == null)
        {
            Debug.LogWarning("SplineManager not assigned.");
            return;
        }
        if (splineManager.SplineCount() > 0)
        {
            attached = true;
            attachedTo = 0;
        }
    }

    void FixedUpdate()
    {
        if (splineManager == null) return;
        if (!attached) return;

        ApplyForces();

        velocity += acceleration * Time.fixedDeltaTime;
        velocity *= Mathf.Exp(-damping * Time.fixedDeltaTime);

        StepResult step = splineManager.StepDistance(attachedTo, currentT, velocity * Time.fixedDeltaTime);

        currentT = step.newT;
        transform.position = step.position;
        if (step.reachedEnd)
        {
            if (step.newSplineIndex >= 0)
            {
                attachedTo = step.newSplineIndex;
            }
            else
            {
                velocity = 0f;
            }
        }
        acceleration = 0f;
    }

    public void DrawBogey(int splineIndex, float t)
    {
        (Vector3 frontBogey, Vector3 rearBogey) = GetBogeyPositions(splineIndex, t);
        Gizmos.DrawSphere(frontBogey, 1f);
        Gizmos.DrawSphere(rearBogey, 1f);
        Gizmos.DrawLine(frontBogey, rearBogey);
    }

    void OnDrawGizmos()
    {
        // show bogey positions with a straight line between them
        if (splineManager != null && attached)
        {
            Gizmos.color = Color.green;
            DrawBogey(attachedTo, currentT);
        }
    }

    public virtual float GetEffectiveMass()
    {
        return mass;
    }

    public void ApplyForce(Vector3 force, int splineIndex, float t)
    {
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t);
        acceleration += Vector3.Dot(force, tangent) / GetEffectiveMass();
    }

    public void ApplyGravity(int splineIndex, float t)
    {
        ApplyForce(new Vector3(0f, -9.81f * mass, 0f), splineIndex, t);
    }

    public void ApplyThrottle(int splineIndex, float t) {
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t);
        float force = throttle * maxThrottle;
        ApplyForce(tangent * force, splineIndex, t);
    }

    public void ApplyBrake(int splineIndex, float t) {
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t);
        float force = velocity * brake * maxBrake;
        ApplyForce(-tangent * force, splineIndex, t);
    }
    public virtual void ApplyForces()
    {
        ApplyGravity(attachedTo, currentT);
        ApplyThrottle(attachedTo, currentT);
        ApplyBrake(attachedTo, currentT);
    }

    /// <summary>
    /// Gets the positions of the front and rear bogeys of the car. The abstract position along the spline is the projected center of the train.
    /// Following this logic, this function needs to find 2 points equidistant from the center, such that the straight line distance between the 2 points is equal to the car length.
    /// </summary>
    /// <returns></returns>
    public (Vector3, Vector3) GetBogeyPositions(int splineIndex, float t)
    {
        float optimalDistance = carLength / 2f;
        StepResult frontStep = splineManager.StepDistance(splineIndex, t, optimalDistance, useStraightLineDistance: true);
        StepResult rearStep = splineManager.StepDistance(splineIndex, t, -optimalDistance, useStraightLineDistance: true);
        for (int i = 0; i < BogeyPositionIterations; i++)
        {
            float currentDistance = (frontStep.position - rearStep.position).magnitude;
            float error = currentDistance - carLength;
            if (Mathf.Abs(error) < 0.01f) break;
            float adjustment = error / 2f;
            frontStep = splineManager.StepDistance(splineIndex, t, optimalDistance - adjustment, useStraightLineDistance: true);
            rearStep = splineManager.StepDistance(splineIndex, t, -optimalDistance - adjustment, useStraightLineDistance: true);
        }
        return (frontStep.position, rearStep.position);
    }
}
