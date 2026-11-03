using UnityEngine;

public class Train : MonoBehaviour
{
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

    void OnDrawGizmos()
    {
        // show position of the train
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 2f);

        // Gizmos.color = Color.red;
        // Gizmos.DrawLine(transform.position, transform.position + shownForce);

        // Show velocity along the tangent
        // if (splineManager != null && attached)
        // {
        //     Gizmos.color = Color.blue;
        //     Vector3 tangent = splineManager.ComputeTangent(attachedTo, currentT);
        //     Gizmos.DrawLine(transform.position, transform.position + tangent * velocity);
        // }
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
}
