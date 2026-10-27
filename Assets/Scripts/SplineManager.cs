using System.Collections.Generic;
using UnityEngine;

public struct Spline
{
    public List<Vector3> points;
}

public class SplineManager : MonoBehaviour
{
    private List<Spline> splines; // Catmull Rom Splines with implicit end points

    public int FromPoints(Vector3[] points)
    {
        Spline spline = new Spline { points = new List<Vector3>(points) };
        return AddSpline(spline);
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

    public Vector3 ComputeTangent(int splineIndex, float t)
    {
        Spline spline = splines[splineIndex];
        List<Vector3> points = spline.points;

        Vector3 implicitStart = 2f * points[0] - points[1];
        Vector3 implicitEnd = 2f * points[^1] - points[^2];

        int segmentCount = points.Count - 1;
        float scaledT = t * segmentCount;
        int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
        float localT = scaledT - segmentIndex;

        Vector3 p0 = segmentIndex == 0 ? implicitStart : points[segmentIndex - 1];
        Vector3 p1 = points[segmentIndex];
        Vector3 p2 = points[segmentIndex + 1];
        Vector3 p3 = segmentIndex + 2 < points.Count ? points[segmentIndex + 2] : implicitEnd;

        return ComputeTangentOnSegment(p0, p1, p2, p3, localT).normalized;
    }

    public Vector3 ComputeNormal(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t);
        return Vector3.Cross(tangent, Vector3.up).normalized;
    }

    public Vector3 ComputeLocalUp(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t);
        Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
        return Vector3.Cross(normal, tangent).normalized;
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

    public (Vector3, float) GridSearch(int splineIndex, float t, float distance, float start, float end, int resolution)
    {
        float accumulatedDistance = 0f;

        float stepT = start;
        Vector3 previousPoint = ComputePoint(splineIndex, t);
        for (int i = 0; i < resolution; i++)
        {
            float tStep = start + (end - start) * (i / (float)(resolution - 1));
            Vector3 currentPoint = ComputePoint(splineIndex, t + tStep);
            accumulatedDistance += (currentPoint - previousPoint).magnitude;
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
        int resolution)
    {
        float targetDistance = Mathf.Abs(distance);
        float direction = Mathf.Sign(distance);

        if (targetDistance < 0.000001f)
            return (ComputePoint(splineIndex, t), 0f);

        // Search only positive step magnitudes.
        float low = 0f;
        float high = Mathf.Max(Mathf.Abs(start), Mathf.Abs(end));

        // Find the approximate arc length of a parameter step.
        float ArcLength(float deltaT)
        {
            float length = 0f;
            Vector3 previous = ComputePoint(splineIndex, t);

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

    public (Vector3, float) StepDistance(int splineIndex, float t, float distance, int resolution = 10, bool useDichotomy = true)
    {
        float globalStepT = StepDistanceApprox(splineIndex, t, distance);
        
        float start = globalStepT * 0.6f;
        float end = globalStepT * 1.4f;

        if (useDichotomy)
        {
            return BinarySearch(splineIndex, t, distance, start, end, resolution);
        }
        else
        {
            return GridSearch(splineIndex, t, distance, start, end, resolution);
        }
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
        Spline spline = splines[splineIndex];
        List<Vector3> points = spline.points;

        Vector3 implicitStart = 2f * points[0] - points[1];
        Vector3 implicitEnd = 2f * points[^1] - points[^2];

        int segmentCount = points.Count - 1;
        float scaledT = t * segmentCount;
        int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
        float localT = scaledT - segmentIndex;

        Vector3 p0 = segmentIndex == 0 ? implicitStart : points[segmentIndex - 1];
        Vector3 p1 = points[segmentIndex];
        Vector3 p2 = points[segmentIndex + 1];
        Vector3 p3 = segmentIndex + 2 < points.Count ? points[segmentIndex + 2] : implicitEnd;

        return ComputeOnSegment(p0, p1, p2, p3, localT);
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
