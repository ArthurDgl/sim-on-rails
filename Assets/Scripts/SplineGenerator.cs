#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class SplineGenerator : MonoBehaviour
{
    [HideInInspector]
    public SplineManager splineManager;
    public int splinePoints = 4;
    public float scale = 100f;
    
    void Start()
    {
        splineManager = GetComponent<SplineManager>();
    }

    public void RandomSpline()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[splinePoints];
        for (int i = 0; i < splinePoints; i++)
        {
            points[i] = new Vector3(Random.value * 2f - 1f, Random.value * 0.2f, Random.value * 2f - 1f) * scale;
        }
        splineManager.FromPoints(points);
    }

    public void Create101Spline()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[3];
        points[0] = new Vector3(-1f, 1f, 0f) * scale;
        points[1] = new Vector3(0f, 0f, 0f) * scale;
        points[2] = new Vector3(1f, 1f, 0f) * scale;
        splineManager.FromPoints(points);
    }

    public void CreateTestTrack()
    {
        splineManager.ClearSplines();
        Vector3[] points = new Vector3[9];
        points[0] = new Vector3(0f, 0f, 1f) * scale;
        points[1] = new Vector3(1f, 0f, 1f) * scale;
        points[2] = new Vector3(2f, 0f, 0f) * scale;
        points[3] = new Vector3(1f, 0f, -1f) * scale;
        points[4] = new Vector3(0f, 0.2f, -1f) * scale;
        points[5] = new Vector3(-1f, 0f, -1f) * scale;
        points[6] = new Vector3(-2f, 0f, 0f) * scale;
        points[7] = new Vector3(-1f, 0f, 1f) * scale;
        points[8] = new Vector3(0f, 0f, 1f) * scale;
        splineManager.FromPoints(points, 0, 0, true);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SplineGenerator))]
public class SplineGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SplineGenerator splineGenerator = (SplineGenerator)target;

        GUI.enabled = splineGenerator.splineManager != null;

        if (GUILayout.Button("Random Spline"))
        {
            splineGenerator.RandomSpline();
        }

        if (GUILayout.Button("Create 101 Spline"))
        {
            splineGenerator.Create101Spline();
        }

        if (GUILayout.Button("Create Test Track"))
        {
            splineGenerator.CreateTestTrack();
        }

        GUI.enabled = true;
    }
}
#endif