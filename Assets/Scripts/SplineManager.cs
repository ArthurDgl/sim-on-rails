using System.Collections.Generic;
using UnityEngine;

public struct Spline
{
    public List<Vector3> points;
    public int previousSplineIndex;
    public int nextSplineIndex;
}

public struct StepResult
{
    public Vector3 position;
    public float newT;
    public bool reachedEnd;
    public int newSplineIndex;
}

public class SplineManager : MonoBehaviour
{
    private List<Spline> splines; // Catmull Rom Splines with implicit end points or using neighbor splines for continuity

    /// <summary>
    /// Creates a new spline from the given points and adds it to the spline manager.
    /// </summary>
    /// <param name="points">The control points of the spline.</param>
    /// <param name="previousSplineIndex">The index of the previous spline for continuity.</param>
    /// <param name="nextSplineIndex">The index of the next spline for continuity.</param>
    /// <param name="circular">Whether the spline is circular.</param>
    /// <returns>The index of the newly added spline.</returns>
    public int FromPoints(Vector3[] points, int previousSplineIndex = -1, int nextSplineIndex = -1, bool circular = false)
    {
        Spline spline = new Spline { points = new List<Vector3>(points), nextSplineIndex = nextSplineIndex, previousSplineIndex = previousSplineIndex };
        int index = AddSpline(spline);
        if (circular)
        {
            spline.previousSplineIndex = index;
            spline.nextSplineIndex = index;
            splines[index] = spline;
        }
        return index;
    }

    /// <summary>
    /// Adds an existing spline to the spline manager.
    /// </summary>
    /// <param name="spline">The spline to add.</param>
    /// <returns>The index of the newly added spline.</returns>
    public int AddSpline(Spline spline)
    {
        if (splines == null)
        {
            splines = new List<Spline>();
        }
        splines.Add(spline);
        return splines.Count - 1;
    }

    /// <summary>
    /// Removes the spline at the specified index from the spline manager.
    /// </summary>
    /// <param name="splineIndex">The index of the spline to remove.</param>
    public void RemoveSpline(int splineIndex)
    {
        if (splines == null || splineIndex < 0 || splineIndex >= splines.Count)
        {
            return;
        }
        splines.RemoveAt(splineIndex);
    }

    /// <summary>
    /// Clears all splines from the spline manager.
    /// </summary>
    public void ClearSplines()
    {
        if (splines != null)
        {
            splines.Clear();
        }
    }

    /// <summary>
    /// Returns the number of splines in the spline manager.
    /// </summary>
    /// <returns>The number of splines in the spline manager.</returns>
    public int SplineCount()
    {
        return splines != null ? splines.Count : 0;
    }

    /// <summary>
    /// Returns the spline at the specified index.
    /// </summary>
    /// <param name="splineIndex">The index of the spline to retrieve.</param>
    /// <returns>The spline at the specified index.</returns>
    public Spline GetSpline(int splineIndex)
    {
        return splines[splineIndex];
    }

    /// <summary>
    /// Computes the tangent vector at the specified parameter t on the given spline.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <returns>The tangent vector at the specified parameter.</returns>
    public Vector3 ComputeTangent(int splineIndex, float t)
    {
        Spline spline = splines[splineIndex];
        GetSegment(spline, t, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3, out float localT);

        return ComputeTangentOnSegment(p0, p1, p2, p3, localT).normalized;
    }

    /// <summary>
    /// Computes the normal vector at the specified parameter t on the given spline.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <returns>The normal vector at the specified parameter.</returns>
    public Vector3 ComputeNormal(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t);
        return Vector3.Cross(tangent, Vector3.up).normalized;
    }

    /// <summary>
    /// Computes the local up vector at the specified parameter t on the given spline.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <returns>The local up vector at the specified parameter.</returns>
    public Vector3 ComputeLocalUp(int splineIndex, float t)
    {
        Vector3 tangent = ComputeTangent(splineIndex, t);
        Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
        return Vector3.Cross(normal, tangent).normalized;
    }

    /// <summary>
    /// Approximates the step distance along the spline.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <param name="distance">The distance to step.</param>
    /// <returns>The approximate step parameter along the spline.</returns>
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

    /// <summary>
    /// Performs a grid search to find the point along the spline at the specified distance.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <param name="distance">The distance to step.</param>
    /// <param name="start">The start parameter for the search.</param>
    /// <param name="end">The end parameter for the search.</param>
    /// <param name="resolution">The number of steps in the grid search.</param>
    /// <param name="useStraightLineDistance">Whether to measure direct displacement instead of distance along the curve.</param>
    /// <returns>A tuple containing the point and the parameter at the specified distance.</returns>
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

    /// <summary>
    /// Performs a binary search to find the point along the spline at the specified distance.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <param name="distance">The distance to step.</param>
    /// <param name="start">The start parameter for the search.</param>
    /// <param name="end">The end parameter for the search.</param>
    /// <param name="tolerance">The tolerance for the binary search.</param>
    /// <param name="useStraightLineDistance">Whether to measure direct displacement instead of distance along the curve.</param>
    /// <returns>A tuple containing the point and the parameter at the specified distance.</returns>
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

    /// <summary>
    /// Steps along the spline by the specified distance.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <param name="distance">The distance to step.</param>
    /// <param name="resolution">The resolution for the search.</param>
    /// <param name="useDichotomy">Whether to use dichotomy (binary search) for refinement.</param>
    /// <param name="useStraightLineDistance">Whether to measure direct displacement instead of distance along the curve.</param>
    /// <returns>A StepResult containing the new position and parameter along the spline.</returns>
    /// <remarks>
    /// If the step reaches the end of the spline, the StepResult will indicate whether the end was reached and provide the new spline index if applicable.
    /// </remarks>
    public StepResult StepDistance(int splineIndex, float t, float distance, int resolution = 10, bool useDichotomy = true, bool useStraightLineDistance = false)
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
            return new StepResult { position = position, newT = newT, reachedEnd = false, newSplineIndex = splineIndex };
        }

        Spline spline = splines[splineIndex];
        bool forward = newT > 1f;
        float boundaryT = forward ? 1f : 0f;
        int neighbor = forward ? spline.nextSplineIndex : spline.previousSplineIndex;

        if (neighbor < 0 || neighbor >= splines.Count)
        {
            return new StepResult { position = ComputePoint(splineIndex, boundaryT), newT = boundaryT, reachedEnd = true, newSplineIndex = -1 };
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
        float neighborT = forward ? 0f : 1f;
        if (Mathf.Abs(remaining) < 0.000001f)
        {
            return new StepResult { position = previous, newT = neighborT, reachedEnd = true, newSplineIndex = neighbor };
        }

        StepResult result = StepDistance(neighbor, neighborT, remaining, resolution, useDichotomy, useStraightLineDistance);
        result.reachedEnd = true;
        return result;
    }

    /// <summary>
    /// Samples points along the specified spline.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="sampleCount">The number of points to sample.</param>
    /// <returns>An array of sampled points along the spline.</returns>
    /// <remarks>
    /// The points are evenly distributed along the parameter t of the spline, not necessarily along the arc length.
    /// </remarks>
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

    /// <summary>
    /// Computes the point on the specified spline at the given parameter t.
    /// </summary>
    /// <param name="splineIndex">The index of the spline.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <returns>The computed point on the spline.</returns>
    /// <remarks>
    /// The parameter t is clamped between 0 and 1. The point is computed using the Catmull-Rom spline formula.
    /// </remarks>
    public Vector3 ComputePoint(int splineIndex, float t)
    {
        t = Mathf.Clamp01(t);
        Spline spline = splines[splineIndex];
        GetSegment(spline, t, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3, out float localT);

        return ComputeOnSegment(p0, p1, p2, p3, localT);
    }

    /// <summary>
    /// Retrieves the segment of the spline corresponding to the given parameter t.
    /// </summary>
    /// <param name="spline">The spline to evaluate.</param>
    /// <param name="t">The parameter along the spline (0 to 1).</param>
    /// <param name="p0">The first control point of the segment.</param>
    /// <param name="p1">The second control point of the segment.</param>
    /// <param name="p2">The third control point of the segment.</param>
    /// <param name="p3">The fourth control point of the segment.</param>
    /// <param name="localT">The local parameter within the segment (0 to 1).</param>
    /// <remarks>
    /// The method determines the appropriate segment of the spline and calculates the local parameter within that segment.
    /// </remarks>
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
        else if (spline.previousSplineIndex >= 0 && spline.previousSplineIndex < splines.Count)
        {
            p0 = splines[spline.previousSplineIndex].points[^2];
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
        else if (spline.nextSplineIndex >= 0 && spline.nextSplineIndex < splines.Count)
        {
            p3 = splines[spline.nextSplineIndex].points[1];
        }
        else
        {
            p3 = 2f * points[^1] - points[^2];
        }
    }

    /// <summary>
    /// Computes the point on a Catmull-Rom spline segment given the control points and local parameter.
    /// </summary>
    /// <param name="p0">The first control point of the segment.</param>
    /// <param name="p1">The second control point of the segment.</param>
    /// <param name="p2">The third control point of the segment.</param>
    /// <param name="p3">The fourth control point of the segment.</param>
    /// <param name="localT">The local parameter within the segment (0 to 1).</param>
    /// <returns>The computed point on the segment.</returns>
    /// <remarks>
    /// The method uses the Catmull-Rom spline formula to interpolate the point based on the local parameter.
    /// </remarks>
    private Vector3 ComputeOnSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float localT)
    {
        float t2 = localT * localT;
        float t3 = t2 * localT;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * localT +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    /// <summary>
    /// Computes the tangent vector on a Catmull-Rom spline segment given the control points and local parameter.
    /// </summary>
    /// <param name="p0">The first control point of the segment.</param>
    /// <param name="p1">The second control point of the segment.</param>
    /// <param name="p2">The third control point of the segment.</param>
    /// <param name="p3">The fourth control point of the segment.</param>
    /// <param name="localT">The local parameter within the segment (0 to 1).</param>
    /// <returns>The computed tangent vector on the segment.</returns>
    /// <remarks>
    /// The method uses the derivative of the Catmull-Rom spline formula to compute the tangent based on the local parameter.
    /// </remarks>
    private Vector3 ComputeTangentOnSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float localT)
    {
        float t2 = localT * localT;
        return 0.5f * ((-p0 + p2) +
                       2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT +
                       3f * (-p0 + 3f * p1 - 3f * p2 + p3) * t2);
    }
}
