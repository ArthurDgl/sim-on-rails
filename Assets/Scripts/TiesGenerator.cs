using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class TiesGenerator : MonoBehaviour
{
    public SplineManager splineManager;

    public float tieSpacing = 0.5f;
    public float tieHeight = 0.178f;
    public float tieLength = 2.438f;
    public float tieWidth = 0.229f;
    public MeshFilter railsMeshFilter;

    private void Reset()
    {
        splineManager = GetComponent<SplineManager>();
    }

    public void GenerateTies()
    {
        splineManager = GetComponent<SplineManager>();

        if (splineManager == null || splineManager.SplineCount() == 0)
            return;

        if (tieSpacing <= 0f || tieHeight <= 0f || tieLength <= 0f || tieWidth <= 0f)
        {
            Debug.LogWarning("Tie spacing and dimensions must be greater than zero.", this);

            return;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float t = 0f;
        int safetyCounter = 0;

        while (t <= 1f && safetyCounter++ < 100000)
        {
            Vector3 point = splineManager.ComputePoint(0, t);
            Vector3 tangent = splineManager.ComputeTangent(0, t).normalized;
            Vector3 lateral = splineManager.ComputeNormal(0, t).normalized;
            Vector3 up = splineManager.ComputeLocalUp(0, t).normalized;

            lateral = Vector3.ProjectOnPlane(lateral, tangent).normalized;
            up = Vector3.Cross(tangent, lateral).normalized;

            if (tangent.sqrMagnitude > 0.0001f && lateral.sqrMagnitude > 0.0001f && up.sqrMagnitude > 0.0001f)
            {
                Vector3 halfLength = lateral * (tieLength * 0.5f);
                Vector3 halfWidth = tangent * (tieWidth * 0.5f);
                Vector3 height = up * tieHeight;

                Vector3 a = point - halfLength - halfWidth;
                Vector3 b = point + halfLength - halfWidth;
                Vector3 c = point - halfLength + halfWidth;
                Vector3 d = point + halfLength + halfWidth;

                Vector3 A = a + height;
                Vector3 B = b + height;
                Vector3 C = c + height;
                Vector3 D = d + height;

                AddQuad(vertices, triangles, uvs, A, C, B, D); // Top
                AddQuad(vertices, triangles, uvs, b, d, a, c); // Bottom
                AddQuad(vertices, triangles, uvs, a, c, A, C); // Left end
                AddQuad(vertices, triangles, uvs, B, D, b, d); // Right end
                AddQuad(vertices, triangles, uvs, b, a, B, A); // Front
                AddQuad(vertices, triangles, uvs, C, c, D, d); // Back
            }

            if (t >= 1f)
                break;

            StepResult result = splineManager.StepDistance(0, t, tieSpacing);
            float nextT = result.newT;

            if (nextT <= t + 0.000001f)
                break;

            if (result.reachedEnd)
                break;

            t = Mathf.Min(nextT, 1f);
        }

        Mesh mesh = new Mesh { name = "Generated Ties" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<RailwayMeshManager>().SetTies(mesh);

    }

    private void AddQuad(
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = vertices.Count;

        vertices.Add(transform.InverseTransformPoint(a));
        vertices.Add(transform.InverseTransformPoint(b));
        vertices.Add(transform.InverseTransformPoint(c));
        vertices.Add(transform.InverseTransformPoint(d));

        triangles.Add(i);
        triangles.Add(i + 1);
        triangles.Add(i + 2);
        triangles.Add(i + 3);
        triangles.Add(i + 2);
        triangles.Add(i + 1);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(1f, 0f));
        uvs.Add(new Vector2(0f, 1f));
        uvs.Add(new Vector2(1f, 1f));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TiesGenerator))]
public class TiesGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TiesGenerator generator = (TiesGenerator)target;
        bool canGenerate = generator.GetComponent<SplineManager>() != null &&
                           generator.GetComponent<SplineManager>().SplineCount() > 0;

        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("Generate Ties"))
            {
                generator.GenerateTies();
            }
        }
    }
}
#endif
