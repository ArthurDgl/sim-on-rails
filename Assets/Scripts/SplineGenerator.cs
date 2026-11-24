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
        int index = splineManager.FromPoints(points);
        splineManager.ConnectSplines(index, true, index, false);
    }

    public void Create2ParallelTracks()
    {
        splineManager.ClearSplines();
        Vector3[] points1 = new Vector3[2];
        points1[0] = new Vector3(0f, 0f, 0f) * scale;
        points1[1] = new Vector3(1f, 0f, 0f) * scale;

        Vector3[] points2 = new Vector3[2];
        points2[0] = new Vector3(0f, 0f, 1f) * scale;
        points2[1] = new Vector3(1f, 0f, 1f) * scale;

        splineManager.FromPoints(points1);
        splineManager.FromPoints(points2);
    }

    public void CreateMultiTestTrack()
    {
        splineManager.ClearSplines();
        Vector3[] points1 = new Vector3[5];
        points1[0] = new Vector3(-1f, 0f, 0f) * scale;
        points1[1] = new Vector3(-2f, 0f, 1f) * scale;
        points1[2] = new Vector3(-3f, 0f, 0f) * scale;
        points1[3] = new Vector3(-2f, 0f, -1f) * scale;
        points1[4] = new Vector3(-1f, 0f, 0f) * scale;
        int left = splineManager.FromPoints(points1);

        Vector3[] points2 = new Vector3[2];
        points2[0] = new Vector3(-1f, 0f, 0f) * scale;
        points2[1] = new Vector3(1f, 0f, 0f) * scale;
        int middle = splineManager.FromPoints(points2);

        Vector3[] points3 = new Vector3[5];
        points3[0] = new Vector3(1f, 0f, 0f) * scale;
        points3[1] = new Vector3(2f, 0f, 1f) * scale;
        points3[2] = new Vector3(3f, 0f, 0f) * scale;
        points3[3] = new Vector3(2f, 0f, -1f) * scale;
        points3[4] = new Vector3(1f, 0f, 0f) * scale;
        int right = splineManager.FromPoints(points3);

        splineManager.ConnectSplines(middle, true, left, true, left, false);
        splineManager.ConnectSplines(middle, false, right, true, right, false);
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

        if (GUILayout.Button("Create 2 Parallel Tracks"))
        {
            splineGenerator.Create2ParallelTracks();
        }
        
        if (GUILayout.Button("Create Multi Test Track"))
        {
            splineGenerator.CreateMultiTestTrack();
        }

        GUI.enabled = true;
    }
}
#endif