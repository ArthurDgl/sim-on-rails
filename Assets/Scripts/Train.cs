using System.Collections.Generic;
using UnityEngine;

public class SplineHistory
{
    public List<int> splineIndices = new List<int>();
    public List<bool> isReversed = new List<bool>();

    public void pruneOldest()
    {
        if (splineIndices.Count > 0 && isReversed.Count > 0)
        {
            splineIndices.RemoveAt(0);
            isReversed.RemoveAt(0);
        }
    }
}

public class Train : MonoBehaviour
{
    private const int BogeyPositionIterations = 10;

    public SplineManager splineManager;

    public bool attached = false;

    public SplineHistory history;

    public int attachedTo
    {
        get { return getSplineIndex(); }
        set { history.splineIndices.Add(value); }
    }

    public int getSplineIndex(int historyDepth = 0)
    {
        return history.splineIndices[history.splineIndices.Count - 1 - historyDepth];
    }

    public bool isReversed
    {
        get { return getIsReversed(); }
        set { history.isReversed.Add(value); }
    }

    public bool getIsReversed(int historyDepth = 0)
    {
        return history.isReversed[history.isReversed.Count - 1 - historyDepth];
    }

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
    
    protected int maxUsedHistoryDepth = 0;

    void Awake()
    {
        Time.fixedDeltaTime = 0.02f;
    }

    void Start()
    {
        history = new SplineHistory();

        if (splineManager == null)
        {
            Debug.LogWarning("SplineManager not assigned.");
            return;
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying) return;

        if (attached && history.splineIndices.Count == 0)
        {
            InitializeHistory(0, false);
        }
    }

    void InitializeHistory(int currentSplineIndex, bool reversed = false)
    {
        history.splineIndices.Clear();
        history.isReversed.Clear();
        
        List<int> defaultHistory = GetDefaultHistoryFor(currentSplineIndex, reversed);
        defaultHistory.Reverse();
        foreach (int entry in defaultHistory)
        {
            if (entry < 0)
            {
                history.splineIndices.Add(-entry);
                history.isReversed.Add(true);
            }
            else
            {
                history.splineIndices.Add(entry);
                history.isReversed.Add(false);
            }
        }
        
    }

    public virtual List<int> GetDefaultHistoryFor(int currentSplineIndex, bool reversed = false)
    {
        return new List<int> { reversed ? -currentSplineIndex : currentSplineIndex };
    }

    void FixedUpdate()
    {
        if (splineManager == null) return;
        if (!attached) return;

        maxUsedHistoryDepth = 0;

        ApplyForces();

        if (maxUsedHistoryDepth < history.splineIndices.Count - 1)
        {
            history.pruneOldest();
        }

        velocity += acceleration * Time.fixedDeltaTime;
        velocity *= Mathf.Exp(-damping * Time.fixedDeltaTime);

        StepResult step = splineManager.StepDistance(attachedTo, currentT, velocity * Time.fixedDeltaTime * (isReversed ? -1f : 1f));

        currentT = step.newT;
        transform.position = step.position;
        if (step.reachedEnd)
        {
            if (step.newSplineIndex >= 0)
            {
                attachedTo = step.newSplineIndex;
                isReversed = step.reverseDirection ? !isReversed : isReversed;
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

    protected virtual void OnDrawGizmos()
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
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t, isReversed);
        acceleration += Vector3.Dot(force, tangent) / GetEffectiveMass();
    }

    public void ApplyGravity(int splineIndex, float t)
    {
        ApplyForce(new Vector3(0f, -9.81f * mass, 0f), splineIndex, t);
    }

    public void ApplyThrottle(int splineIndex, float t) {
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t, isReversed);
        float force = throttle * maxThrottle;
        ApplyForce(tangent * force, splineIndex, t);
    }

    public void ApplyBrake(int splineIndex, float t) {
        Vector3 tangent = splineManager.ComputeTangent(splineIndex, t, isReversed);
        float force = velocity * brake * maxBrake;
        ApplyForce(-tangent * force, splineIndex, t);
    }
    public virtual void ApplyForces()
    {
        ApplyGravity(attachedTo, currentT);
        ApplyThrottle(attachedTo, currentT);
        ApplyBrake(attachedTo, currentT);
    }

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
