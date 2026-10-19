using System.Collections.Generic;
using NUnit.Framework;
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

    public Vector3 ComputePoint(int splineIndex, float t)
    {
        Spline spline = splines[splineIndex];
        List<Vector3> points = spline.points;

        Vector3 implicitStart = points[0] + points[1] - points[0];
        Vector3 implicitEnd = points[^1] + points[^2] - points[^1];

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

    private Vector3 ComputeOnSegment(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float localT)
    {
        float t2 = localT * localT;
        float t3 = t2 * localT;
        return 0.5f * ((2f * p1) +
                       (-p0 + p2) * localT +
                       (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
}
