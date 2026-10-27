using UnityEngine;

public class Train : MonoBehaviour
{
    public SplineManager splineManager;

    public bool attached = false;

    public int attachedTo = 0;

    public float currentT = 0f;

    public float velocity = 0f;

    public float acceleration = 0f;

    public float mass = 1f;

    public float damping = 0.01f;

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

        if (currentT < 0f || currentT > 1f)
        {
            currentT = Mathf.Clamp01(currentT);
        }

        ApplyForce(new Vector3(0f, -9.81f * mass, 0f));

        velocity += acceleration * Time.fixedDeltaTime;
        velocity *= Mathf.Exp(-damping * Time.fixedDeltaTime);

        (Vector3 newPosition, float stepT) = splineManager.StepDistance(attachedTo, currentT, velocity * Time.fixedDeltaTime);

        currentT += stepT;
        transform.position = newPosition;
        acceleration = 0f;
    }

    void OnDrawGizmos()
    {
        // Gizmos.color = Color.red;
        // Gizmos.DrawLine(transform.position, transform.position + shownForce);

        // Show velocity along the tangent
        if (splineManager != null && attached)
        {
            Gizmos.color = Color.blue;
            Vector3 tangent = splineManager.ComputeTangent(attachedTo, currentT);
            Gizmos.DrawLine(transform.position, transform.position + tangent * velocity);
        }
    }

    public void ApplyForce(Vector3 force)
    {
        Vector3 tangent = splineManager.ComputeTangent(attachedTo, currentT);
        acceleration += Vector3.Dot(force, tangent) / mass;
    }
}
