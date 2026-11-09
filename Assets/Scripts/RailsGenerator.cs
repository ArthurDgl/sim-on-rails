using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class RailsGenerator : MonoBehaviour
{
    public SplineManager splineManager;
    public float railGauge = 1.435f;
    public float railWidth = 0.13f;
    public float railHeight = 0.143f;
    public MeshFilter railsMeshFilter;

    public void GenerateRails()
    {
        splineManager = GetComponent<SplineManager>();

        if (splineManager.SplineCount() == 0) return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float t = 0f;
        float distance = 0f;
        bool reachedEnd = false;
        while (t < 1f && !reachedEnd)
        {
            StepResult result = splineManager.StepDistance(0, t, 0.5f);
            reachedEnd = result.reachedEnd;
            float nextT = result.newT;

            Vector3 p1 = splineManager.ComputePoint(0, t);
            Vector3 p2 = splineManager.ComputePoint(0, nextT);

            float segmentLength = Vector3.Distance(p1, p2);
            float endDistance = distance + segmentLength;

            Vector3 n1 = splineManager.ComputeNormal(0, t).normalized;
            Vector3 n2 = splineManager.ComputeNormal(0, nextT).normalized;

            Vector3 up1 = splineManager.ComputeLocalUp(0, t);
            Vector3 up2 = splineManager.ComputeLocalUp(0, nextT);

            Vector3 centerLeft1 = p1 - n1 * railGauge / 2f;
            Vector3 centerLeft2 = p2 - n2 * railGauge / 2f;

            Vector3 leftEdge1 = centerLeft1 - n1 * railWidth / 2f;
            Vector3 rightEdge1 = centerLeft1 + n1 * railWidth / 2f;
            Vector3 topLeftEdge1 = leftEdge1 + up1 * railHeight;
            Vector3 topRightEdge1 = rightEdge1 + up1 * railHeight;
            Vector3 leftEdge2 = centerLeft2 - n2 * railWidth / 2f;
            Vector3 rightEdge2 = centerLeft2 + n2 * railWidth / 2f;
            Vector3 topLeftEdge2 = leftEdge2 + up2 * railHeight;
            Vector3 topRightEdge2 = rightEdge2 + up2 * railHeight;

            Vector3 centerRight1 = p1 + n1 * railGauge / 2f;
            Vector3 centerRight2 = p2 + n2 * railGauge / 2f;

            Vector3 rLeftEdge1 = centerRight1 - n1 * railWidth / 2f;
            Vector3 rRightEdge1 = centerRight1 + n1 * railWidth / 2f;
            Vector3 rTopLeftEdge1 = rLeftEdge1 + up1 * railHeight;
            Vector3 rTopRightEdge1 = rRightEdge1 + up1 * railHeight;
            Vector3 rLeftEdge2 = centerRight2 - n2 * railWidth / 2f;
            Vector3 rRightEdge2 = centerRight2 + n2 * railWidth / 2f;
            Vector3 rTopLeftEdge2 = rLeftEdge2 + up2 * railHeight;
            Vector3 rTopRightEdge2 = rRightEdge2 + up2 * railHeight;

            AddQuad(vertices, triangles, uvs, topLeftEdge1, topRightEdge1, topLeftEdge2, topRightEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, topRightEdge1, rightEdge1, topRightEdge2, rightEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, rightEdge1, leftEdge1, rightEdge2, leftEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, leftEdge1, topLeftEdge1, leftEdge2, topLeftEdge2, distance, endDistance);

            AddQuad(vertices, triangles, uvs, rTopLeftEdge1, rTopRightEdge1, rTopLeftEdge2, rTopRightEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, rTopRightEdge1, rRightEdge1, rTopRightEdge2, rRightEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, rRightEdge1, rLeftEdge1, rRightEdge2, rLeftEdge2, distance, endDistance);
            AddQuad(vertices, triangles, uvs, rLeftEdge1, rTopLeftEdge1, rLeftEdge2, rTopLeftEdge2, distance, endDistance);

            distance += segmentLength;
            t = nextT;
        }



        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        railsMeshFilter.sharedMesh = mesh;
    }

    void Start()
    {
        splineManager = GetComponent<SplineManager>();
    }

    void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float distance, float endDistance)
    {
        int i = vertices.Count;

        vertices.Add(transform.InverseTransformPoint(a));
        vertices.Add(transform.InverseTransformPoint(b));
        vertices.Add(transform.InverseTransformPoint(c));
        vertices.Add(transform.InverseTransformPoint(d));

        triangles.Add(i);
        triangles.Add(i+1);
        triangles.Add(i+2);

        triangles.Add(i+3);
        triangles.Add(i+2);
        triangles.Add(i+1);

        uvs.Add(new Vector2(0f, distance));
        uvs.Add(new Vector2(1f, distance));
        uvs.Add(new Vector2(0f, endDistance));
        uvs.Add(new Vector2(1f, endDistance));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RailsGenerator))]
public class RailsGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RailsGenerator railsGenerator = (RailsGenerator)target;

        GUI.enabled = railsGenerator.splineManager != null && railsGenerator.splineManager.SplineCount() > 0;

        if (GUILayout.Button("Generate Rail"))
        {
            railsGenerator.GenerateRails();
        }

        GUI.enabled = true;
    }
}
#endif