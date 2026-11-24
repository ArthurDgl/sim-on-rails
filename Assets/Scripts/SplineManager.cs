using System.Collections.Generic;
using UnityEngine;

public struct Spline
{
    public List<Vector3> points;
    public int startSwitchIndex;
    public bool startIsSource;
    public int endSwitchIndex;
    public bool endIsSource;
}

public struct Switch
{
    public int sourceSplineIndex;
    public bool sourceIsStart;
    public int defaultDestinationIndex;
    public bool defaultIsStart;
    public int alternateDestinationIndex;
    public bool alternateIsStart;
    public bool useAlternate;
}

public struct StepResult
{
    public Vector3 position;
    public float newT;
    public bool reachedEnd;
    public int newSplineIndex;
    public bool reverseDirection;
}

[System.Serializable]
public struct PathPosition
{
    public int splineIndex;
    public bool isGoingForward;

    public PathPosition(int splineIndex, bool isGoingForward)
    {
        this.splineIndex = splineIndex;
        this.isGoingForward = isGoingForward;
    }

    public PathPosition Default()
    {
        return new PathPosition(-1, true);
    }
}

public class SplineManager : MonoBehaviour
{
    private List<Spline> splines;
    private List<Switch> switches;

    public int FromPoints(Vector3[] points)
    {
        Spline spline = new Spline { points = new List<Vector3>(points), startSwitchIndex = -1, endSwitchIndex = -1 };
        int index = AddSpline(spline);
        return index;
    }

    public int AddSpline(Spline spline)
    {
        if (splines == null)
        {
            splines = new List<Spline>();
        }
        splines.Add(spline);
        return splines.Count - 1;
    }

    public int ConnectSplines(int sourceSplineIndex, bool sourceIsStart, int destinationSplineIndex, bool destinationIsStart, int alternateDestinationIndex = -1, bool alternateIsStart = false)
    {
        Switch sw = new Switch
        {
            sourceSplineIndex = sourceSplineIndex,
            sourceIsStart = sourceIsStart,
            defaultDestinationIndex = destinationSplineIndex,
            defaultIsStart = destinationIsStart,
            alternateDestinationIndex = alternateDestinationIndex,
            alternateIsStart = alternateIsStart,
            useAlternate = false
        };
        return AddSwitch(sw);
    }

    public int AddSwitch(Switch sw)
    {
        if (switches == null)
        {
            switches = new List<Switch>();
        }
        switches.Add(sw);
        int index = switches.Count - 1;
        ApplySwitch(sw);
        return index;
    }

    public void ApplySwitch(Switch sw)
    {
        int index = switches.IndexOf(sw);
        int sourceSplineIndex = sw.sourceSplineIndex;
        bool sourceIsStart = sw.sourceIsStart;
        int destinationSplineIndex = sw.defaultDestinationIndex;
        bool destinationIsStart = sw.defaultIsStart;
        int alternateDestinationIndex = sw.alternateDestinationIndex;
        bool alternateIsStart = sw.alternateIsStart;

        if (sourceIsStart)
        {
            Spline sourceSpline = splines[sourceSplineIndex];
            sourceSpline.startSwitchIndex = index;
            sourceSpline.startIsSource = true;
            splines[sourceSplineIndex] = sourceSpline;
        }
        else
        {
            Spline sourceSpline = splines[sourceSplineIndex];
            sourceSpline.endSwitchIndex = index;
            sourceSpline.endIsSource = true;
            splines[sourceSplineIndex] = sourceSpline;
        }

        if (destinationIsStart)
        {
            Spline destinationSpline = splines[destinationSplineIndex];
            destinationSpline.startSwitchIndex = index;
            destinationSpline.startIsSource = false;
            splines[destinationSplineIndex] = destinationSpline;
        }
        else
        {
            Spline destinationSpline = splines[destinationSplineIndex];
            destinationSpline.endSwitchIndex = index;
            destinationSpline.endIsSource = false;
            splines[destinationSplineIndex] = destinationSpline;
        }

        if (alternateDestinationIndex != -1)
        {
            Spline alternateSpline = splines[alternateDestinationIndex];
            if (alternateIsStart)
            {
                alternateSpline.startSwitchIndex = index;
                alternateSpline.startIsSource = false;
            }
            else
            {
                alternateSpline.endSwitchIndex = index;
                alternateSpline.endIsSource = false;
            }
            splines[alternateDestinationIndex] = alternateSpline;
        }
    }

    public void RemoveSpline(int splineIndex)
    {
        if (splines == null || splineIndex < 0 || splineIndex >= splines.Count)
        {
            return;
        }
        splines.RemoveAt(splineIndex);
    }

    public void ClearSplines()
    {
        if (splines != null)
        {
            splines.Clear();
        }
    }

    public int SplineCount()
    {
        return splines != null ? splines.Count : 0;
    }

    public Spline GetSpline(int splineIndex)
    {
        return splines[splineIndex];
    }

    public Vector3 ComputeTangent(int splineIndex, float t, bool isReversed = false)
    {
        Spline spline = splines[splineIndex];
        GetSegment(spline, t, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3, out float localT);

        Vector3 tangent = ComputeTangentOnSegment(p0, p1, p2, p3, localT).normalized;
        return isReversed ? -tangent : tangent;
    }

    public Vector3 ComputeNormal(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t, false);
        return Vector3.Cross(tangent, Vector3.up).normalized;
    }

    public Vector3 ComputeLocalUp(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t, false);
        Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
        return Vector3.Cross(normal, tangent).normalized;
    }

    public float GetDistanceAlongPath(List<PathPosition> path, float currentT = 0f, float lastT = 1f)
    {
        float distance = 0f;
        for (int i = 0; i < path.Count; i++)
        {
            float startT = i == 0 ? currentT : 0f;
            float endT = i == path.Count - 1 ? lastT : 1f;
            distance += ApproximateSplineLength(path[i].splineIndex, startT: startT, endT: endT);
        }
        return distance;
    }

    public float Heuristic(PathPosition spline, PathPosition endSpline)
    {
        Vector3 startPoint = ComputePoint(spline.splineIndex, 0.5f);
        Vector3 endPoint = ComputePoint(endSpline.splineIndex, 0.5f);
        return Vector3.Distance(startPoint, endPoint);
    }

    public List<PathPosition> GetNeighbors(PathPosition current)
    {
        List<PathPosition> neighbors = new List<PathPosition>();

        Spline spline = splines[current.splineIndex];
        int switchIndex = current.isGoingForward ? spline.endSwitchIndex : spline.startSwitchIndex;
        if (switchIndex < 0 || switchIndex >= switches.Count)
        {
            return neighbors;
        }
        Switch sw = switches[switchIndex];

        if (current.isGoingForward ? spline.endIsSource : spline.startIsSource)
        {
            if (sw.defaultDestinationIndex >= 0)
            {
                neighbors.Add(new PathPosition { splineIndex = sw.defaultDestinationIndex, isGoingForward = sw.defaultIsStart });
            }
            if (sw.alternateDestinationIndex >= 0)
            {
                neighbors.Add(new PathPosition { splineIndex = sw.alternateDestinationIndex, isGoingForward = sw.alternateIsStart });
            }
        }
        else
        {
            if (sw.sourceSplineIndex >= 0)
            {
                neighbors.Add(new PathPosition { splineIndex = sw.sourceSplineIndex, isGoingForward = sw.sourceIsStart });
            }
        }
        return neighbors;
    }

    public List<PathPosition> ReconstructPath(Dictionary<PathPosition, PathPosition> cameFrom, PathPosition current)
    {
        List<PathPosition> totalPath = new List<PathPosition> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }
        return totalPath;
    }

    public List<PathPosition> Pathfind(PathPosition startSpline, PathPosition endSpline)
    {
        HashSet<PathPosition> known = new HashSet<PathPosition>();
        known.Add(startSpline);

        Dictionary<PathPosition, PathPosition> cameFrom = new Dictionary<PathPosition, PathPosition>();

        Dictionary<PathPosition, float> gScore = new Dictionary<PathPosition, float>();
        for (int i = 0; i < splines.Count; i++)
        {
            gScore[new PathPosition { splineIndex = i, isGoingForward = true }] = float.PositiveInfinity;
            gScore[new PathPosition { splineIndex = i, isGoingForward = false }] = float.PositiveInfinity;
        }
        gScore[startSpline] = 0f;

        Dictionary<PathPosition, float> fScore = new Dictionary<PathPosition, float>();
        for (int i = 0; i < splines.Count; i++)
        {
            fScore[new PathPosition { splineIndex = i, isGoingForward = true }] = float.PositiveInfinity;
            fScore[new PathPosition { splineIndex = i, isGoingForward = false }] = float.PositiveInfinity;
        }
        fScore[startSpline] = Heuristic(startSpline, endSpline);

        while (known.Count > 0)
        {
            PathPosition current = new PathPosition().Default();
            float minFScore = float.PositiveInfinity;
            foreach (PathPosition node in known)
            {
                if (fScore[node] < minFScore)
                {
                    minFScore = fScore[node];
                    current = node;
                }
            }

            if (current.Equals(endSpline))
            {
                return ReconstructPath(cameFrom, current);
            }

            known.Remove(current);
            float currentLength = ApproximateSplineLength(current.splineIndex);
            foreach (PathPosition neighbor in GetNeighbors(current))
            {
                float tentativeGScore = gScore[current] + currentLength;
                if (tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, endSpline);
                    known.Add(neighbor);
                }
            }
        }
        return null;
    }

    public float ApproximateSplineLength(int splineIndex, int resolution = 100, float startT = 0f, float endT = 1f)
    {
        Spline spline = splines[splineIndex];
        List<Vector3> points = spline.points;

        float length = 0f;
        Vector3 previousPoint = ComputePoint(splineIndex, startT);
        for (int i = 1; i <= resolution; i++)
        {
            float t = startT + (endT - startT) * (i / (float)resolution);
            Vector3 currentPoint = ComputePoint(splineIndex, t);
            length += Vector3.Distance(currentPoint, previousPoint);
            previousPoint = currentPoint;
        }
        return length;
    }

    public float StepDistanceApprox(int splineIndex, float t, float distance)
    {
        Spline spline = splines[splineIndex];
        List<Vector3> points = spline.points;

        int segmentCount = points.Count - 1;
        float scaledT = t * segmentCount;
        int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);

        float approximatedSegmentLength = (points[segmentIndex + 1] - points[segmentIndex]).magnitude;

        float localStepT = distance / approximatedSegmentLength;
        float globalStepT = localStepT / segmentCount;

        return globalStepT;
    }

    public (Vector3, float) GridSearch(int splineIndex, float t, float distance, float start, float end, int resolution, bool useStraightLineDistance = false)
    {
        float accumulatedDistance = 0f;

        float stepT = start;
        Vector3 previousPoint = ComputePoint(splineIndex, t);
        Vector3 startPoint = previousPoint;
        for (int i = 0; i < resolution; i++)
        {
            float tStep = start + (end - start) * (i / (float)(resolution - 1));
            Vector3 currentPoint = ComputePoint(splineIndex, t + tStep);
            accumulatedDistance = useStraightLineDistance
                ? Vector3.Distance(currentPoint, startPoint)
                : accumulatedDistance + Vector3.Distance(currentPoint, previousPoint);
            previousPoint = currentPoint;
            if (accumulatedDistance >= distance)
            {
                return (currentPoint, tStep);
            }
        }
        return (ComputePoint(splineIndex, t + end), end);
    }

    public (Vector3, float) BinarySearch(
        int splineIndex,
        float t,
        float distance,
        float start,
        float end,
        int resolution,
        bool useStraightLineDistance = false)
    {
        float targetDistance = Mathf.Abs(distance);
        float direction = Mathf.Sign(distance);

        if (targetDistance < 0.000001f)
            return (ComputePoint(splineIndex, t), 0f);

        float low = 0f;
        float high = Mathf.Max(Mathf.Abs(start), Mathf.Abs(end));

        float ArcLength(float deltaT)
        {
            Vector3 startPoint = ComputePoint(splineIndex, t);
            if (useStraightLineDistance)
            {
                Vector3 endPoint = ComputePoint(splineIndex, t + direction * deltaT);
                return Vector3.Distance(startPoint, endPoint);
            }

            float length = 0f;
            Vector3 previous = startPoint;

            for (int i = 1; i <= resolution; i++)
            {
                float sampleT = t + direction * deltaT
                    * (i / (float)resolution);

                Vector3 current = ComputePoint(splineIndex, sampleT);
                length += Vector3.Distance(previous, current);
                previous = current;
            }

            return length;
        }

        for (int i = 0; i < 20; i++)
        {
            float mid = (low + high) * 0.5f;

            if (ArcLength(mid) < targetDistance)
                low = mid;
            else
                high = mid;
        }

        float stepT = direction * (low + high) * 0.5f;
        return (ComputePoint(splineIndex, t + stepT), stepT);
    }

    public StepResult StepDistance(int splineIndex, float t, float distance, int resolution = 10, bool useDichotomy = true, bool useStraightLineDistance = false, int preferSplineIndex = -1, int preferDirection = -1)
    {
        float globalStepT = StepDistanceApprox(splineIndex, t, distance);
        
        float start = globalStepT * 0.6f;
        float end = globalStepT * 1.4f;

        (Vector3 position, float stepT) = useDichotomy
            ? BinarySearch(splineIndex, t, distance, start, end, resolution, useStraightLineDistance)
            : GridSearch(splineIndex, t, distance, start, end, resolution, useStraightLineDistance);

        float newT = t + stepT;
        if (newT >= 0f && newT <= 1f)
        {
            return new StepResult { position = position, newT = newT, reachedEnd = false, newSplineIndex = splineIndex, reverseDirection = false };
        }

        Spline spline = splines[splineIndex];
        bool forward = newT > 1f;
        float boundaryT = forward ? 1f : 0f;
        int neighbor;
        bool neighborIsReversed;
        if (forward)
        {
            Switch sw = switches[spline.endSwitchIndex];
            if (spline.endIsSource)
            {
                bool useAlternate = sw.useAlternate && sw.defaultDestinationIndex != preferSplineIndex || sw.alternateDestinationIndex == preferSplineIndex;
                if (preferDirection != -1 && sw.defaultDestinationIndex == sw.alternateDestinationIndex)
                {
                    bool preferForward = preferDirection == 1;
                    useAlternate = preferForward ^ sw.defaultIsStart;
                }
                neighbor = useAlternate ? sw.alternateDestinationIndex : sw.defaultDestinationIndex;
                neighborIsReversed = useAlternate ? !sw.alternateIsStart : !sw.defaultIsStart;
            }
            else
            {
                neighbor = sw.sourceSplineIndex;
                neighborIsReversed = !sw.sourceIsStart;
            }
        }
        else
        {
            Switch sw = switches[spline.startSwitchIndex];
            if (spline.startIsSource)
            {
                bool useAlternate = sw.useAlternate && sw.defaultDestinationIndex != preferSplineIndex || sw.alternateDestinationIndex == preferSplineIndex;
                if (preferDirection != -1 && sw.defaultDestinationIndex == sw.alternateDestinationIndex)
                {
                    bool preferForward = preferDirection == 1;
                    useAlternate = preferForward ^ sw.defaultIsStart;
                }
                neighbor = useAlternate ? sw.alternateDestinationIndex : sw.defaultDestinationIndex;
                neighborIsReversed = useAlternate ? sw.alternateIsStart : sw.defaultIsStart;
            }
            else
            {
                neighbor = sw.sourceSplineIndex;
                neighborIsReversed = sw.sourceIsStart;
            }
        }

        if (neighbor < 0 || neighbor >= splines.Count)
        {
            return new StepResult { position = ComputePoint(splineIndex, boundaryT), newT = boundaryT, reachedEnd = true, newSplineIndex = -1, reverseDirection = false };
        }

        float toBoundary = 0f;
        Vector3 startPoint = ComputePoint(splineIndex, t);
        Vector3 previous = startPoint;
        for (int i = 1; i <= resolution; i++)
        {
            Vector3 current = ComputePoint(splineIndex, Mathf.Lerp(t, boundaryT, i / (float)resolution));
            toBoundary = useStraightLineDistance
                ? Vector3.Distance(startPoint, current)
                : toBoundary + Vector3.Distance(previous, current);
            previous = current;
        }

        float remaining = Mathf.Max(Mathf.Abs(distance) - toBoundary, 0f) * Mathf.Sign(distance);
        if (neighborIsReversed)
        {
            remaining = -remaining;
        }
        float neighborT = forward != neighborIsReversed ? 0f : 1f;
        if (Mathf.Abs(remaining) < 0.000001f)
        {
            return new StepResult { position = previous, newT = neighborT, reachedEnd = true, newSplineIndex = neighbor, reverseDirection = neighborIsReversed };
        }

        StepResult result = StepDistance(neighbor, neighborT, remaining, resolution, useDichotomy, useStraightLineDistance);
        result.reachedEnd = true;
        result.reverseDirection ^= neighborIsReversed;
        return result;
    }

    public Vector3[] SamplePoints(int splineIndex, int sampleCount)
    {
        Vector3[] samples = new Vector3[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            samples[i] = ComputePoint(splineIndex, t);
        }
        return samples;
    }

    public Vector3 ComputePoint(int splineIndex, float t)
    {
        t = Mathf.Clamp01(t);
        Spline spline = splines[splineIndex];
        GetSegment(spline, t, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3, out float localT);

        return ComputeOnSegment(p0, p1, p2, p3, localT);
    }

    // Source tangent at the junction, oriented away from the source.
    private Vector3 SwitchDirection(Switch sw)
    {
        Vector3 tangent = ComputeTangent(sw.sourceSplineIndex, sw.sourceIsStart ? 0f : 1f);
        return sw.sourceIsStart ? -tangent : tangent;
    }

    private void GetSegment(Spline spline, float t, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3, out float localT)
    {
        List<Vector3> points = spline.points;

        int segmentCount = points.Count - 1;
        float scaledT = t * segmentCount;
        int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
        localT = scaledT - segmentIndex;

        if (segmentIndex > 0)
        {
            p0 = points[segmentIndex - 1];
        }
        else if (spline.startSwitchIndex >= 0 && spline.startSwitchIndex < switches.Count && !spline.startIsSource)
        {
            Vector3 d = SwitchDirection(switches[spline.startSwitchIndex]);
            p0 = points[1] - 2f * (points[1] - points[0]).magnitude * d;
        }
        else
        {
            p0 = 2f * points[0] - points[1];
        }

        p1 = points[segmentIndex];
        p2 = points[segmentIndex + 1];

        if (segmentIndex + 2 < points.Count)
        {
            p3 = points[segmentIndex + 2];
        }
        else if (spline.endSwitchIndex >= 0 && spline.endSwitchIndex < switches.Count && !spline.endIsSource)
        {
            Vector3 d = SwitchDirection(switches[spline.endSwitchIndex]);
            p3 = points[^2] - 2f * (points[^1] - points[^2]).magnitude * d;
        }
        else
        {
            p3 = 2f * points[^1] - points[^2];
        }
    }

    private Vector3 ComputeOnSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float localT)
    {
        float t2 = localT * localT;
        float t3 = t2 * localT;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * localT +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private Vector3 ComputeTangentOnSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float localT)
    {
        float t2 = localT * localT;
        return 0.5f * ((-p0 + p2) +
                       2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT +
                       3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
    }
}
