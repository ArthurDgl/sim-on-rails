using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RailwayMeshManager : MonoBehaviour
{
    private Mesh railsMesh;

    private Mesh tiesMesh;

    public Material railsMaterial;
    public Material tiesMaterial;

    public void SetRails(Mesh mesh)
    {
        railsMesh = mesh;
        UpdateMesh();
    }

    public void SetTies(Mesh mesh)
    {
        tiesMesh = mesh;
        UpdateMesh();
    }

    void UpdateMesh()
    {
        List<CombineInstance> combine = new List<CombineInstance>();
        List<Material> materials = new List<Material>();

        if (railsMesh != null)
        {
            CombineInstance rails = new CombineInstance();
            rails.mesh = railsMesh;
            rails.transform = Matrix4x4.identity;

            combine.Add(rails);
            materials.Add(railsMaterial);
        }

        if (tiesMesh != null)
        {
            CombineInstance ties = new CombineInstance();
            ties.mesh = tiesMesh;
            ties.transform = Matrix4x4.identity;

            combine.Add(ties);
            materials.Add(tiesMaterial);
        }

        if (combine.Count == 0)
            return;

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = "Railway Mesh";
        combinedMesh.indexFormat = IndexFormat.UInt32;

        combinedMesh.CombineMeshes(
            combine.ToArray(),
            false,
            false
        );

        combinedMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.sharedMesh = combinedMesh;
        meshRenderer.sharedMaterials = materials.ToArray();
    }
}