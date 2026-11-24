using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
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

public struct TrainGraphics
{
    public Vector3 frontOffset;
    public Vector3 rearOffset;
    public Vector3 centerOffset; // Real center offset from logical position on spline
    public Vector3 tangent;
    public Vector3 normal;
    public Vector3 localUp;
}

[System.Serializable]
public class PathFollowing
{
    public List<PathPosition> path = new List<PathPosition>();

    public PathPosition CurrentPosition
    {
        get { return path.Count > 0 ? path[0] : new PathPosition().Default(); }
    }

    public PathPosition NextPosition
    {
        get { return path.Count > 1 ? path[1] : new PathPosition().Default(); }
    }

    public PathFollowing()
    {
        path = new List<PathPosition>();
    }

    public PathFollowing(List<PathPosition> initialPath)
    {
        path = initialPath ?? new List<PathPosition>();
    }

    public bool AdvancePathIsDone()
    {
        if (path.Count > 0)
        {
            path.RemoveAt(0);

            if (path.Count <= 1)
            {
                Debug.Log("Path has been fully advanced.");
                return true;
            }
        }
        return false;
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

    public PathFollowing pathFollowing = new PathFollowing();

    public PathPosition goal;

    void Awake()
    {
        Time.fixedDeltaTime = 0.02f;
    }

    void Start()
    {
        history = new SplineHistory();

        pathFollowing = null;

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

    PathPosition GetCurrentPathPosition()
    {
        return new PathPosition
        {
            splineIndex = attachedTo,
            isGoingForward = !isReversed
        };
    }

    void SetPathFollowing(PathFollowing newPathFollowing)
    {
        if (newPathFollowing == null)
        {
            Debug.LogWarning("Attempted to set a null PathFollowing instance.");
            return;
        }

        if (!newPathFollowing.CurrentPosition.Equals(GetCurrentPathPosition()))
        {
            Debug.LogWarning("New PathFollowing instance does not match the current path position.");
            return;
        }

        pathFollowing = newPathFollowing;
    }

    public void PathfindToGoal()
    {
        if (splineManager == null) return;
        if (!attached) return;

        List<PathPosition> pathToGoal = splineManager.Pathfind(GetCurrentPathPosition(), goal);

        if (pathToGoal == null || pathToGoal.Count == 0)
        {
            Debug.LogWarning("No path found to goal.");
            return;
        }
        else
        {
            SetPathFollowing(new PathFollowing(pathToGoal));
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

        int preferedSplineIndex = pathFollowing != null ? pathFollowing.NextPosition.splineIndex : -1;
        int preferedDirection = pathFollowing != null ? (pathFollowing.NextPosition.isGoingForward ? 1 : 0) : -1;
        StepResult step = splineManager.StepDistance(attachedTo, currentT, velocity * Time.fixedDeltaTime * (isReversed ? -1f : 1f), preferSplineIndex: preferedSplineIndex, preferDirection: preferedDirection);

        currentT = step.newT;
        transform.position = step.position;
        if (step.reachedEnd)
        {
            if (step.newSplineIndex >= 0)
            {
                attachedTo = step.newSplineIndex;
                isReversed = step.reverseDirection ? !isReversed : isReversed;
                if (pathFollowing != null) {
                    if (pathFollowing.AdvancePathIsDone())
                    {
                        pathFollowing = null;
                    }
                }
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

    public (Vector3, Vector3, Vector3) GetGraphicsPosition(int splineIndex, float t)
    {
        (Vector3 front, Vector3 rear) = GetBogeyPositions(splineIndex, t);
        return (front, rear, (front + rear) / 2f);
    }

    public TrainGraphics ComputeTrainGraphics(int attachedTo = -1, float currentT = 0f, bool isReversed = false)
    {
        if (attachedTo == -1)
        {
            attachedTo = this.attachedTo;
            currentT = this.currentT;
            isReversed = this.isReversed;
        }
        (Vector3 front, Vector3 rear, Vector3 center) = GetGraphicsPosition(attachedTo, currentT);
        Vector3 tangent = splineManager.ComputeTangent(attachedTo, currentT, isReversed);
        Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
        TrainGraphics graphics = new TrainGraphics
        {
            frontOffset = front - center,
            rearOffset = rear - center,
            centerOffset = center - transform.position,
            tangent = tangent,
            normal = normal,
            localUp = Vector3.up
        };
        return graphics;
    }

    public virtual TrainGraphics[] GetTrainGraphics()
    {
        return new TrainGraphics[] { ComputeTrainGraphics() };
    }
}
