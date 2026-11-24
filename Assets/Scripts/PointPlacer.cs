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
    [SerializeField] private bool selectNewPointAfterLink = true;   // lets you keep extending a chain by clicking

    [Header("Dragging")]
    [SerializeField] private float dragThresholdPixels = 5f;   // movement needed before a press becomes a drag

    [Header("Spline")]
    [SerializeField] private SplineManager splineManager;
    [SerializeField] private int samplesPerSegment = 20;
    [SerializeField] private float lineWidth = 0.3f;
    [SerializeField] private Color lineColor = Color.yellow;

    public List<TerrainPoint> Points { get; } = new List<TerrainPoint>();   // every point, all chains
    public List<PointChain> Chains { get; } = new List<PointChain>();
    public TerrainPoint Selected { get; private set; }

    private TerrainPoint dragged;
    private Vector2 pressScreenPos;
    private bool dragMoved;

    private int nextChainId;
    private Material lineMaterial;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in
    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 screenPos = mouse.position.ReadValue();

        if (mouse.rightButton.wasPressedThisFrame)
            Select(null);

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // Alt + left mouse is used by the camera to rotate
            if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed)
                return;

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

            bool linkToSelected = Selected != null;
            PointChain chain;
            int insertAt;

            if (linkToSelected)
            {
                // Extend the selected point's chain
                chain = Selected.chain;
                int i = chain.points.IndexOf(Selected);

                // First point of a longer chain: extend backwards. Otherwise insert right after it.
                insertAt = (i == 0 && chain.points.Count > 1) ? 0 : i + 1;
            }
            else
            {
                // Standalone point: starts its own chain, no spline yet
                chain = CreateChain();
                insertAt = 0;
            }

            TerrainPoint newPoint = CreatePoint(pos, chain, insertAt);
            RefreshChain(chain);

            if (linkToSelected && selectNewPointAfterLink)
                Select(newPoint);
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

    private TerrainPoint CreatePoint(Vector3 pos, PointChain chain, int insertAt)
    {
        GameObject marker = Instantiate(pointPrefab, pos, Quaternion.identity, transform);

        TerrainPoint point = new TerrainPoint(0, pos, marker) { chain = chain };
        chain.points.Insert(insertAt, point);
        Points.Add(point);

        Reindex(chain);
        return point;
    }

    private PointChain CreateChain()
    {
        PointChain chain = new PointChain(nextChainId++);
        chain.line = CreateLineRenderer(chain.id);
        Chains.Add(chain);
        return chain;
    }

    private LineRenderer CreateLineRenderer(int chainId)
    {
        if (lineMaterial == null)
            lineMaterial = new Material(Shader.Find("Sprites/Default"));

        GameObject go = new GameObject($"SplineLine_{chainId}");
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.widthMultiplier = lineWidth;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.sharedMaterial = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        return lr;
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

    // ---------- Edit ----------

    public void DeleteSelected()
    {
        if (Selected == null) return;

        dragged = null;

        TerrainPoint p = Selected;
        Select(null);

        PointChain chain = p.chain;

        if (p.marker != null) Destroy(p.marker);
        chain.points.Remove(p);
        Points.Remove(p);

        if (chain.points.Count == 0)
        {
            RemoveChain(chain);
        }
        else
        {
            Reindex(chain);
            RefreshChain(chain);
        }
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

        RefreshChain(p.chain);                  // live update of this chain's spline
    }

    /// <summary>Keeps index values contiguous (0..n-1) inside a chain and in sync with its order.</summary>
    private void Reindex(PointChain chain)
    {
        for (int i = 0; i < chain.points.Count; i++)
        {
            chain.points[i].index = i;
            if (chain.points[i].marker != null)
                chain.points[i].marker.name = $"Chain{chain.id}_Point{i}";
        }
    }

    // ---------- Splines ----------

    /// <summary>Creates, updates or removes the spline of a chain, and redraws its line.</summary>
    private void RefreshChain(PointChain chain)
    {
        if (splineManager == null)
        {
            Debug.LogError("PointPlacer: no SplineManager assigned.");
            return;
        }

        // A spline needs at least 2 points
        if (chain.points.Count < 2)
        {
            RemoveChainSpline(chain);
            if (chain.line != null) chain.line.positionCount = 0;
            return;
        }

        Vector3[] positions = chain.points.Select(p => p.position).ToArray();

        if (chain.splineIndex < 0)
        {
            chain.splineIndex = splineManager.FromPoints(positions);
        }
        else
        {
            // Spline is a struct, but its points list is a reference: editing it updates the real spline.
            // This keeps the spline index (and any switch attached to it) unchanged.
            List<Vector3> splinePoints = splineManager.GetSpline(chain.splineIndex).points;
            splinePoints.Clear();
            splinePoints.AddRange(positions);
        }

        if (chain.line != null)
        {
            int count = (chain.points.Count - 1) * samplesPerSegment + 1;
            Vector3[] samples = splineManager.SamplePoints(chain.splineIndex, count);
            chain.line.positionCount = samples.Length;
            chain.line.SetPositions(samples);
        }
    }

    /// <summary>Removes a chain's spline from the SplineManager and fixes the indices of the other chains.</summary>
    private void RemoveChainSpline(PointChain chain)
    {
        if (chain.splineIndex < 0 || splineManager == null) return;

        int removed = chain.splineIndex;
        splineManager.RemoveSpline(removed);
        chain.splineIndex = -1;

        // RemoveSpline shifts every later spline down by one
        foreach (PointChain other in Chains)
            if (other.splineIndex > removed) other.splineIndex--;
    }

    private void RemoveChain(PointChain chain)
    {
        RemoveChainSpline(chain);
        if (chain.line != null) Destroy(chain.line.gameObject);
        Chains.Remove(chain);
    }

    /// <summary>Deletes every point and every spline created by this placer (other splines are left alone).</summary>
    public void ClearAll()
    {
        Select(null);
        dragged = null;

        foreach (TerrainPoint p in Points)
            if (p.marker != null) Destroy(p.marker);

        foreach (PointChain chain in Chains)
        {
            RemoveChainSpline(chain);
            if (chain.line != null) Destroy(chain.line.gameObject);
        }

        Points.Clear();
        Chains.Clear();
    }
}