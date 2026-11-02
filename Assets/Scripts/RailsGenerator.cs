using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class RailsGenerator : MonoBehaviour
{
    private SplineManager splineManager;
    private SplineTests splineTests;
    public float railGauge = 1.5f;
    public float railWidth = 1.0f;

    void OnDrawGizmos()
    {
        splineManager = GetComponent<SplineManager>();
        splineTests = GetComponent<SplineTests>();

        if(splineManager.SplineCount() == 0) return;
        if(splineTests.stepDistance <= 0f) return;

        Gizmos.color = Color.grey;

        float t = 0f;

        while(t < 1f)
        {
            float deltaT = splineManager.StepDistance(0, t, splineTests.stepDistance).Item2;
            if(deltaT <= 0f) break;
            float nextT = Math.Min(t + deltaT, 1f);

            Vector3 p1 = splineManager.ComputePoint(0, t);
            Vector3 p2 = splineManager.ComputePoint(0, nextT);

            Vector3 n1 = splineManager.ComputeNormal(0, t);
            Vector3 n2 = splineManager.ComputeNormal(0, nextT);

            float offset = railGauge / 2f;

            Gizmos.DrawLine(p1 - n1 * offset, p2 - n2 * offset);

            Gizmos.DrawLine(p1 + n1 * offset, p2 + n2 * offset);

            t = nextT;

        }
    }

    [ContextMenu("Generate Rail")]
    void GenerateRail()
    {
        splineManager = GetComponent<SplineManager>();
        splineTests = GetComponent<SplineTests>();

        if (splineManager.SplineCount() == 0) return;
        if (splineTests.stepDistance <= 0f) return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float t = 0f;

        while (true)
        {
            Vector3 point = splineManager.ComputePoint(0, t);
            Vector3 normal = splineManager.ComputeNormal(0, t);

            // Centre du rail gauche
            Vector3 center = point - normal * railGauge / 2f;

            // Deux vertices de chaque section
            vertices.Add(transform.InverseTransformPoint(
                center - normal * railWidth / 2f));

            vertices.Add(transform.InverseTransformPoint(
                center + normal * railWidth / 2f));

            // Connecter avec la section précédente
            int i = vertices.Count - 4;

            if (i >= 0)
            {
                triangles.AddRange(new int[]
                {
                    i, i + 1, i + 2,
                    i + 1, i + 3, i + 2
                });
            }

            if (t >= 1f) break;

            float deltaT = splineManager.StepDistance(
                0, t, splineTests.stepDistance
            ).Item2;

            if (deltaT <= 0f) break;

            t = Mathf.Min(t + deltaT, 1f);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
