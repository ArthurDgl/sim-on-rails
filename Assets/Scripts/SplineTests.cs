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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        splineManager = GetComponent<SplineManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
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

        Spline spline = splineManager.GetSpline(0);
        List<Vector3> points = spline.points;
        Gizmos.color = Color.red;
        for (int i = 0; i < points.Count; i++)
        {
            Gizmos.DrawWireSphere(points[i], 0.03f);
        }

        Gizmos.color = Color.green;
        Vector3[] samples = splineManager.SamplePoints(0, resolution);
        for (int i = 0; i < samples.Length - 1; i++)
        {
            Gizmos.DrawLine(samples[i], samples[i + 1]);
        }
    }

    public void RandomSpline()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[splinePoints];
        for (int i = 0; i < splinePoints; i++)
        {
            points[i] = new Vector3(Random.value, Random.value, Random.value);
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