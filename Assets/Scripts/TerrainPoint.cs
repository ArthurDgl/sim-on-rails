using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainPoint
{
    public int index;            // position inside its chain (0..n-1)
    public Vector3 position;
    public GameObject marker;    // the visual in the scene

    [System.NonSerialized] public PointChain chain;   // the spline this point belongs to

    public TerrainPoint(int index, Vector3 position, GameObject marker)
    {
        this.index = index;
        this.position = position;
        this.marker = marker;
    }
}

/// <summary>An ordered list of points that forms one spline.</summary>
public class PointChain
{
    public readonly int id;
    public readonly List<TerrainPoint> points = new List<TerrainPoint>();
    public int splineIndex = -1;     // index in SplineManager, -1 while the chain has fewer than 2 points
    public LineRenderer line;

    public PointChain(int id)
    {
        this.id = id;
    }
}