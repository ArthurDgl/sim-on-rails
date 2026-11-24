using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PointPlacer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject pointPrefab;      // a small sphere, for example
    [SerializeField] private LayerMask terrainLayer;      // set to the layer of your terrain

    [Header("Settings")]
    [SerializeField] private float heightOffset = 0.3f;   // how far above the terrain
    [SerializeField] private float maxRayDistance = 2000f;

    [Header("Spline")]
    [SerializeField] private SplineManager splineManager;
    [SerializeField] private LineRenderer lineRenderer;   // optional, to see the spline in the Game view
    [SerializeField] private int samplesPerSegment = 20;

    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private Color lineColor = Color.yellow;

    public List<TerrainPoint> Points { get; } = new List<TerrainPoint>();

    private int currentSplineIndex = -1;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (lineRenderer == null) lineRenderer = CreateLineRenderer();
    }

    private LineRenderer CreateLineRenderer()
    {
        GameObject go = new GameObject("SplineLine");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.widthMultiplier = lineWidth;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        return lr;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Ignore clicks on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TryPlacePoint(mouse.position.ReadValue());
        }
    }

    private void LateUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame) BuildSpline();
        if (kb.backspaceKey.wasPressedThisFrame) ClearAll();
    }

    private void TryPlacePoint(Vector2 screenPos)
    {
        if (cam == null) { Debug.LogError("PointPlacer: no camera"); return; }
        if (pointPrefab == null) { Debug.LogError("PointPlacer: no point prefab"); return; }

        Ray ray = cam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, terrainLayer))
        {
            // Offset along the surface normal, so it also works on steep slopes
            Vector3 pos = hit.point + hit.normal * heightOffset;

            GameObject marker = Instantiate(pointPrefab, pos, Quaternion.identity, transform);
            int index = Points.Count;
            marker.name = $"Point_{index}";

            Points.Add(new TerrainPoint(index, pos, marker));
        }
        else
        {
            // Diagnostic: tells you what is under the cursor, ignoring the layer mask
            if (Physics.Raycast(ray, out RaycastHit anyHit, maxRayDistance))
                Debug.LogWarning($"PointPlacer: ray hit '{anyHit.collider.name}' on layer '{LayerMask.LayerToName(anyHit.collider.gameObject.layer)}', which is not in the Terrain Layer mask.");
            else
                Debug.LogWarning("PointPlacer: ray hit no collider at all. Check the terrain has a Terrain Collider with its Terrain Data assigned.");
        }
    }

    public void BuildSpline()
    {
        if (splineManager == null)
        {
            Debug.LogError("PointPlacer: no SplineManager assigned.");
            return;
        }

        if (Points.Count < 2)
        {
            Debug.LogWarning("PointPlacer: need at least 2 points to build a spline.");
            return;
        }

        Debug.Log($"PointPlacer: building spline from {Points.Count} points.");
        splineManager.ClearSplines();
        Vector3[] positions = Points.Select(p => p.position).ToArray();
        currentSplineIndex = splineManager.FromPoints(positions);

        if (lineRenderer != null)
        {
            Debug.Log($"PointPlacer: sampling spline for LineRenderer with {samplesPerSegment} samples per segment.");
            int count = (Points.Count - 1) * samplesPerSegment + 1;
            Vector3[] samples = splineManager.SamplePoints(currentSplineIndex, count);
            lineRenderer.positionCount = samples.Length;
            lineRenderer.SetPositions(samples);
        }
    }

    public void ClearAll()
    {
        foreach (TerrainPoint p in Points)
            if (p.marker != null) Destroy(p.marker);

        Points.Clear();

        if (splineManager != null) splineManager.ClearSplines();
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        currentSplineIndex = -1;
    }
}