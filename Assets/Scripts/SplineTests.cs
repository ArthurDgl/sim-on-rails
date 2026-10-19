#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class SplineTests : MonoBehaviour
{
    private SplineManager splineManager;

    public int splinePoints = 4;
    public int resolution = 10;

    [Range(0f, 1f)]
    public float currentT = 0f;

    public bool autoPlay = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        splineManager = GetComponent<SplineManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (autoPlay)
        {
            currentT += Time.deltaTime * 0.2f;
            if (currentT > 1f)
            {
                currentT = 0f;
            }
        }
    }

    void OnValidate()
    {
        if (resolution < 2)
        {
            resolution = 2;
        }
        if (splinePoints < 2)
        {
            splinePoints = 2;
        }
    }

    void OnDrawGizmos()
    {
        if (splineManager == null) return;
        if (splineManager.SplineCount() == 0) return;

        // Draw control points
        Spline spline = splineManager.GetSpline(0);
        List<Vector3> points = spline.points;
        Gizmos.color = Color.red;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawWireSphere(points[i], 0.03f);
        }
        
        // Draw spline curve
        Gizmos.color = Color.green;
        Vector3[] samples = splineManager.SamplePoints(0, resolution);
        for (int i = 0; i < samples.Length - 1; i++)
        {
            Gizmos.DrawLine(samples[i], samples[i + 1]);
        }

        // Draw current point at current t
        Gizmos.color = Color.blue;
        Vector3 point = splineManager.ComputePoint(0, currentT);
        Gizmos.DrawSphere(point, 0.03f);

        // Draw coordinate frame at current point
        Gizmos.color = Color.red;
        Vector3 tangent = splineManager.ComputeTangent(0, currentT);
        Gizmos.DrawLine(point, point + tangent * 0.1f);
        Gizmos.color = Color.green;
        Vector3 normal = splineManager.ComputeNormal(0, currentT);
        Gizmos.DrawLine(point, point + normal * 0.1f);
        Gizmos.color = Color.yellow;
        Vector3 localUp = splineManager.ComputeLocalUp(0, currentT);
        Gizmos.DrawLine(point, point + localUp * 0.1f);
    }

    public void RandomSpline()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[splinePoints];
        for (int i = 0; i < splinePoints; i++)
        {
            points[i] = new Vector3(Random.value * 2f - 1f, Random.value * 0.2f, Random.value * 2f - 1f);
        }
        splineManager.FromPoints(points);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplineTests))]
public class SplineTestsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SplineTests splineTests = (SplineTests)target;
        if (GUILayout.Button("Random Spline"))
        {
            splineTests.RandomSpline();
        }
    }
}
#endif