#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class SplineTests : MonoBehaviour
{
    [HideInInspector]
    public SplineManager splineManager;

    public int splinePoints = 4;
    public int resolution = 10;

    public bool hideOtherTests = true;

    [Range(0f, 1f)]
    public float currentT = 0f;

    public bool autoPlay = false;

    public int distanceTests = 1000;
    public float minDistance = 0.001f;
    public float maxDistance = 0.1f;

    public float stepDistance = 0.01f;
    public int stepResolution = 10;
    public bool useDichotomy = false;

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

        if (hideOtherTests) return;

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

        // Draw expected and actual step distance point
        Gizmos.color = Color.magenta;
        Vector3 expectedStepPoint = point + tangent.normalized * stepDistance;
        Gizmos.DrawSphere(expectedStepPoint, 0.03f);
        Gizmos.color = Color.cyan;
        (Vector3 stepPoint, float stepT) = splineManager.StepDistance(0, currentT, stepDistance, stepResolution, useDichotomy);
        Gizmos.DrawSphere(stepPoint, 0.03f);
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

    public void Create101Spline()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[3];
        points[0] = new Vector3(-1f, 1f, 0f);
        points[1] = new Vector3(0f, 0f, 0f);
        points[2] = new Vector3(1f, 1f, 0f);
        splineManager.FromPoints(points);
    }

    public void TestStepDistance()
    {
        float[] requested = new float[distanceTests];
        float[] actual = new float[distanceTests];
        float[] errors = new float[distanceTests];
        float minError = float.MaxValue;
        float maxError = float.MinValue;
        float sumError = 0f;
        for (int i = 0; i < distanceTests; i++)
        {
            float distance = Random.Range(minDistance, maxDistance);
            Vector3 point = splineManager.ComputePoint(0, currentT);
            (Vector3 steppedPoint, float stepT) = splineManager.StepDistance(0, currentT, distance, stepResolution, useDichotomy);
            requested[i] = distance;
            actual[i] = (steppedPoint - point).magnitude;
            errors[i] = Mathf.Abs(requested[i] - actual[i]) / distance;
            minError = Mathf.Min(minError, errors[i]);
            maxError = Mathf.Max(maxError, errors[i]);
            sumError += errors[i];
        }

        float avgError = sumError / distanceTests;
        Debug.Log($"Step Distance Test Results: Min Error = {minError*100}%, Max Error = {maxError*100}%, Avg Error = {avgError*100}%");
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

        GUI.enabled = splineTests.splineManager != null;

        if (GUILayout.Button("Random Spline"))
        {
            splineTests.RandomSpline();
        }

        if (GUILayout.Button("Create 101 Spline"))
        {
            splineTests.Create101Spline();
        }

        if (GUILayout.Button("Test Step Distance"))
        {
            splineTests.TestStepDistance();
        }

        GUI.enabled = true;
    }
}
#endif