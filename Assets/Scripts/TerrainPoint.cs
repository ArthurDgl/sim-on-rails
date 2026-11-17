using UnityEngine;

[System.Serializable]
public class TerrainPoint
{
    public int index;
    public Vector3 position;
    public GameObject marker;   // the visual in the scene

    public TerrainPoint(int index, Vector3 position, GameObject marker)
    {
        this.index = index;
        this.position = position;
        this.marker = marker;
    }
}