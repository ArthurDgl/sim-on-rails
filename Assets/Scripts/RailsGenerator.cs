using UnityEngine;

[RequireComponent(typeof(SplineManager))]
public class RailsGenerator : MonoBehaviour
{
    private SplineManager splineManager;
    private SplineTests splineTests;
    public float railGauge = 1.5f;

    void Awake()
    {
        splineManager = GetComponent<SplineManager>();
        splineTests = GetComponent<SplineTests>();
    }
}
