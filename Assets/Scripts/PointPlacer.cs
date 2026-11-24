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

    [Header("Selection")]
    [SerializeField] private float pickRadiusPixels = 25f;   // click this close to a point to select it
    [SerializeField] private Color selectedColor = Color.red;
    [SerializeField] private float selectedScale = 1.5f;
    
    [Header("Dragging")]
    [SerializeField] private float dragThresholdPixels = 5f;   // movement needed before a press becomes a drag

    private TerrainPoint dragged;
    private Vector2 pressScreenPos;
    private bool dragMoved;

    [Header("Spline")]
    [SerializeField] private SplineManager splineManager;
    [SerializeField] private LineRenderer lineRenderer;   // optional, created automatically if empty
    [SerializeField] private int samplesPerSegment = 20;
    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private Color lineColor = Color.yellow;

    public List<TerrainPoint> Points { get; } = new List<TerrainPoint>();
    public TerrainPoint Selected { get; private set; }

    private int currentSplineIndex = -1;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in
    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

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

        Vector2 screenPos = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Ignore clicks on UI elements
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            TerrainPoint picked = PickPoint(screenPos);
            if (picked != null)
            {
                Select(picked);
                dragged = picked;
                pressScreenPos = screenPos;
                dragMoved = false;
            }
            else
            {
                dragged = null;
                Select(null);
                TryPlacePoint(screenPos);
            }
        }
        else if (dragged != null && mouse.leftButton.isPressed)
        {
            if (dragMoved || Vector2.Distance(screenPos, pressScreenPos) >= dragThresholdPixels)
            {
                dragMoved = true;
                DragPoint(dragged, screenPos);
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            dragged = null;
    }

    private void LateUpdate()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.enterKey.wasPressedThisFrame) BuildSpline();
        if (kb.backspaceKey.wasPressedThisFrame) ClearAll();
        if (kb.deleteKey.wasPressedThisFrame) DeleteSelected();
        if (kb.escapeKey.wasPressedThisFrame) Select(null);
    }

    // ---------- Placement ----------

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

    // ---------- Selection ----------

    /// <summary>Returns the point closest to the cursor (in screen pixels), or null if none is close enough.</summary>
    private TerrainPoint PickPoint(Vector2 screenPos)
    {
        if (cam == null) return null;

        TerrainPoint best = null;
        float bestDist = pickRadiusPixels;

        foreach (TerrainPoint p in Points)
        {
            if (p.marker == null) continue;

            Vector3 sp = cam.WorldToScreenPoint(p.marker.transform.position);
            if (sp.z < 0f) continue; // behind the camera

            float d = Vector2.Distance(screenPos, new Vector2(sp.x, sp.y));
            if (d < bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    public void Select(TerrainPoint point)
    {
        if (Selected == point) return;

        ApplyVisual(Selected, false);
        Selected = point;
        ApplyVisual(Selected, true);
    }

    private void ApplyVisual(TerrainPoint p, bool selected)
    {
        if (p == null || p.marker == null) return;

        p.marker.transform.localScale = pointPrefab.transform.localScale * (selected ? selectedScale : 1f);

        Renderer r = p.marker.GetComponentInChildren<Renderer>();
        if (r == null) return;

        if (selected)
        {
            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, selectedColor);
            propertyBlock.SetColor(ColorId, selectedColor);
            r.SetPropertyBlock(propertyBlock);
        }
        else
        {
            r.SetPropertyBlock(null); // back to the prefab's own material
        }
    }

    public void DeleteSelected()
    {
        if (Selected == null) return;

        dragged = null;

        TerrainPoint p = Selected;
        Select(null);

        if (p.marker != null) Destroy(p.marker);
        Points.Remove(p);

        Reindex();
        RefreshSpline();
    }

    private void DragPoint(TerrainPoint p, Vector2 screenPos)
    {
        if (p.marker == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, terrainLayer))
            return; // cursor is off the terrain: keep the last valid position

        Vector3 pos = hit.point + hit.normal * heightOffset;

        p.position = pos;                       // data used by the spline
        p.marker.transform.position = pos;      // visual

        RefreshSpline();                        // live update if a spline already exists
    }

    /// <summary>Keeps index values contiguous (0..n-1) and in sync with the list order.</summary>
    private void Reindex()
    {
        for (int i = 0; i < Points.Count; i++)
        {
            Points[i].index = i;
            if (Points[i].marker != null) Points[i].marker.name = $"Point_{i}";
        }
    }

    // ---------- Spline ----------

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

        splineManager.ClearSplines();
        Vector3[] positions = Points.Select(p => p.position).ToArray();
        currentSplineIndex = splineManager.FromPoints(positions);

        if (lineRenderer != null)
        {
            int count = (Points.Count - 1) * samplesPerSegment + 1;
            Vector3[] samples = splineManager.SamplePoints(currentSplineIndex, count);
            lineRenderer.positionCount = samples.Length;
            lineRenderer.SetPositions(samples);
        }
    }

    /// <summary>Called after the point list changes. Rebuilds the spline only if one was already built.</summary>
    private void RefreshSpline()
    {
        if (currentSplineIndex < 0) return;

        if (Points.Count >= 2)
        {
            BuildSpline();
        }
        else
        {
            if (splineManager != null) splineManager.ClearSplines();
            if (lineRenderer != null) lineRenderer.positionCount = 0;
            currentSplineIndex = -1;
        }
    }

    public void ClearAll()
    {
        Select(null);

        dragged = null;

        foreach (TerrainPoint p in Points)
            if (p.marker != null) Destroy(p.marker);

        Points.Clear();

        if (splineManager != null) splineManager.ClearSplines();
        if (lineRenderer != null) lineRenderer.positionCount = 0;
        currentSplineIndex = -1;
    }
}