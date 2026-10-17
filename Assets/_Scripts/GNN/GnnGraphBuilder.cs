using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sahne geometrisinden GNN grafı kurar. Hem Editor'de (eğitim verisi) hem de
/// runtime'da (tahmin) aynı kod çalışır; böylece özellikler birebir aynı hesaplanır.
///
/// ÖNEMLİ: SafeArea / DangerArea collider'ları özellik olarak KULLANILMAZ (trigger'lar
/// sorgulardan hariç tutulur). Onlar yalnızca eğitimde etiket kaynağıdır (ComputeLabels).
/// </summary>
public sealed class GnnGraphBuilder
{
    public static readonly string[] FeatureNames =
    {
        "overhead_clearance",     // 0: üstteki ilk yüzeye uzaklık (masa altı → küçük)
        "overhead_is_furniture",  // 1: üstteki yüzey eşya mı (1) yoksa tavan mı (0)
        "glass_distance",         // 2: en yakın cama uzaklık
        "min_clearance",          // 3: 8 yönde en yakın engel
        "mean_clearance",         // 4: 8 yönde ortalama engel uzaklığı
        "tall_object_height",     // 5: yakındaki en yüksek eşya (devrilme riski)
        "elevated_loose_objects", // 6: yakında, yüksekte duran serbest (düşebilecek) eşya sayısı
        "sturdy_cover_distance",  // 7: en yakın sağlam alçak eşyaya (masa/koltuk) uzaklık
        "openness"                // 8: açık yön oranı
    };

    public static readonly string[] ClassNames = { "Safe", "Exposed", "Danger" };
    public static int FeatureCount => FeatureNames.Length;

    private static readonly Vector3[] Dirs8 =
    {
        new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
        new Vector3(0.7071f, 0, 0.7071f), new Vector3(-0.7071f, 0, 0.7071f),
        new Vector3(0.7071f, 0, -0.7071f), new Vector3(-0.7071f, 0, -0.7071f)
    };

    private static readonly Vector2Int[] ForwardNeighbors =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1)
    };

    private readonly GnnGraphSettings settings;
    private readonly float S;
    private readonly HashSet<Collider> ignored = new HashSet<Collider>();
    private readonly List<Bounds> floorBounds = new List<Bounds>();
    private readonly List<Bounds> glassBounds = new List<Bounds>();
    private readonly HashSet<Rigidbody> looseSet = new HashSet<Rigidbody>();
    private readonly RaycastHit[] hitBuffer = new RaycastHit[64];
    private readonly Collider[] colliderBuffer = new Collider[512];
    private float floorTop;
    private Bounds area;

    public GnnGraph Graph { get; } = new GnnGraph();

    public GnnGraphBuilder(GnnGraphSettings settings, IEnumerable<Renderer> floorRenderers,
        IEnumerable<Transform> ignoreRoots, Transform referenceFrame)
    {
        this.settings = settings;
        S = settings.worldScale;

        foreach (Transform root in ignoreRoots)
        {
            if (root == null) continue;
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
                ignored.Add(c);
        }

        bool hasArea = false;
        floorTop = float.MinValue;
        foreach (Renderer r in floorRenderers)
        {
            if (r == null) continue;
            floorBounds.Add(r.bounds);
            floorTop = Mathf.Max(floorTop, r.bounds.max.y);
            if (!hasArea) { area = r.bounds; hasArea = true; }
            else area.Encapsulate(r.bounds);
        }

        if (hasArea)
            CollectGlass(ignoreRoots);

        Graph.FeatureCount = FeatureCount;
        Graph.CellSize = settings.cellSize * S;
        Graph.GridOrigin = hasArea ? new Vector3(area.min.x, floorTop, area.min.z) : Vector3.zero;
        Graph.FrameWorldToLocal = referenceFrame != null ? referenceFrame.worldToLocalMatrix : Matrix4x4.identity;
    }

    public bool HasFloor => floorBounds.Count > 0;

    /// <summary>Grafı birkaç kareye yayarak kurar (Quest'te takılmayı önler).</summary>
    public IEnumerator Build(int nodesPerStep)
    {
        if (!HasFloor) yield break;
        Physics.SyncTransforms();

        float cs = Graph.CellSize;
        int countX = Mathf.CeilToInt(area.size.x / cs);
        int countZ = Mathf.CeilToInt(area.size.z / cs);
        int budget = 0;

        // 1) Düğümler: yürünebilir ve boş hücreler
        for (int ix = 0; ix < countX; ix++)
        {
            for (int iz = 0; iz < countZ; iz++)
            {
                Vector3 center = CellCenter(new Vector2Int(ix, iz));
                if (TrySampleFloor(center, out Vector3 floorPoint))
                {
                    var cell = new Vector2Int(ix, iz);
                    Graph.CellToNode[cell] = Graph.Positions.Count;
                    Graph.Positions.Add(floorPoint);
                    Graph.Cells.Add(cell);
                }

                if (++budget >= nodesPerStep * 2) { budget = 0; yield return null; }
            }
        }

        // 2) Düğüm özellikleri
        int n = Graph.NodeCount;
        int f = FeatureCount;
        Graph.Features = new float[n * f];
        for (int i = 0; i < n; i++)
        {
            ComputeFeatures(Graph.Positions[i], Graph.Features, i * f);
            if (++budget >= nodesPerStep) { budget = 0; yield return null; }
        }

        // 3) Kenarlar: arada engel olmayan 8-komşular
        var adjacency = new List<int>[n];
        for (int i = 0; i < n; i++) adjacency[i] = new List<int>(8);
        for (int i = 0; i < n; i++)
        {
            Vector2Int cell = Graph.Cells[i];
            foreach (Vector2Int d in ForwardNeighbors)
            {
                if (!Graph.CellToNode.TryGetValue(cell + d, out int j)) continue;
                if (!IsConnected(Graph.Positions[i], Graph.Positions[j])) continue;
                adjacency[i].Add(j);
                adjacency[j].Add(i);
            }
            if (++budget >= nodesPerStep * 2) { budget = 0; yield return null; }
        }

        Graph.NeighborOffsets = new int[n + 1];
        for (int i = 0; i < n; i++) Graph.NeighborOffsets[i + 1] = Graph.NeighborOffsets[i] + adjacency[i].Count;
        Graph.NeighborIndices = new int[Graph.NeighborOffsets[n]];
        for (int i = 0; i < n; i++) adjacency[i].CopyTo(Graph.NeighborIndices, Graph.NeighborOffsets[i]);
    }

    public GnnGraph BuildImmediate()
    {
        IEnumerator routine = Build(int.MaxValue);
        while (routine.MoveNext()) { }
        return Graph;
    }

    private Vector3 CellCenter(Vector2Int cell)
    {
        float cs = Graph.CellSize;
        return new Vector3(
            Graph.GridOrigin.x + (cell.x + 0.5f + settings.gridOffset.x) * cs,
            floorTop,
            Graph.GridOrigin.z + (cell.y + 0.5f + settings.gridOffset.y) * cs);
    }

    private bool TrySampleFloor(Vector3 center, out Vector3 floorPoint)
    {
        floorPoint = center;
        if (!InsideFloorXZ(center)) return false;

        Vector3 origin = center + Vector3.up * (0.4f * S);
        if (NearestHit(origin, Vector3.down, 0.8f * S, out RaycastHit hit) &&
            Mathf.Abs(hit.point.y - floorTop) <= 0.25f * S)
        {
            floorPoint = hit.point;
        }

        // Hücre bir eşya / duvar tarafından dolduruluyorsa düğüm değil
        float r = settings.bodyRadius * S;
        Vector3 probe = floorPoint + Vector3.up * (0.2f * S + r);
        return Overlap(probe, r) == 0;
    }

    private bool InsideFloorXZ(Vector3 p)
    {
        foreach (Bounds b in floorBounds)
        {
            if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z)
                return true;
        }
        return false;
    }

    private void ComputeFeatures(Vector3 fp, float[] output, int offset)
    {
        Vector3 up = Vector3.up;
        float probe = settings.maxProbeDistance * S;
        float near = settings.neighborRadius * S;

        // 0-1: Üst koruma
        float upLength = settings.maxOverheadDistance * S;
        float overhead = 1f, overheadIsFurniture = 0f;
        if (NearestHit(fp + up * (0.3f * S), up, upLength, out RaycastHit upHit))
        {
            overhead = upHit.distance / upLength;
            overheadIsFurniture = IsStructure(upHit.collider) ? 0f : 1f;
        }

        // 2: Cama uzaklık
        Vector3 chest = fp + up * (1.0f * S);
        float glass = probe;
        foreach (Bounds gb in glassBounds)
            glass = Mathf.Min(glass, Mathf.Sqrt(gb.SqrDistance(chest)));

        // 3, 4, 8: Çevre açıklığı
        Vector3 waist = fp + up * (0.5f * S);
        float minClear = probe, sumClear = 0f;
        int open = 0;
        foreach (Vector3 dir in Dirs8)
        {
            float d = NearestHit(waist, dir, probe, out RaycastHit h) ? h.distance : probe;
            minClear = Mathf.Min(minClear, d);
            sumClear += d;
            if (d >= near) open++;
        }

        // 5, 6, 7: Eşya analizi
        float tall = 0f, sturdy = probe;
        looseSet.Clear();
        int count = Overlap(waist, probe);
        for (int k = 0; k < count; k++)
        {
            Collider c = colliderBuffer[k];
            if (IsStructure(c)) continue;

            Bounds b = c.bounds;
            float height = b.max.y - fp.y;
            float dist = Mathf.Sqrt(b.SqrDistance(waist));

            if (dist <= near)
            {
                tall = Mathf.Max(tall, height / (2.5f * S));
                Rigidbody rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic && b.center.y - fp.y > 0.8f * S)
                    looseSet.Add(rb);
            }

            float footprint = b.size.x * b.size.z;
            if (height >= 0.35f * S && height <= 1.2f * S && footprint >= 0.25f * S * S)
            {
                float dx = Mathf.Max(b.min.x - fp.x, 0f, fp.x - b.max.x);
                float dz = Mathf.Max(b.min.z - fp.z, 0f, fp.z - b.max.z);
                sturdy = Mathf.Min(sturdy, Mathf.Sqrt(dx * dx + dz * dz));
            }
        }

        output[offset + 0] = overhead;
        output[offset + 1] = overheadIsFurniture;
        output[offset + 2] = glass / probe;
        output[offset + 3] = minClear / probe;
        output[offset + 4] = sumClear / Dirs8.Length / probe;
        output[offset + 5] = Mathf.Clamp01(tall);
        output[offset + 6] = Mathf.Clamp01(looseSet.Count / 5f);
        output[offset + 7] = sturdy / probe;
        output[offset + 8] = open / (float)Dirs8.Length;
    }

    private bool IsConnected(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.y - b.y) > 0.2f * S) return false;
        Vector3 from = a + Vector3.up * (0.5f * S);
        Vector3 delta = b - a;
        return !NearestHit(from, delta.normalized, delta.magnitude, out _);
    }

    private bool IsStructure(Collider c)
    {
        Vector3 size = c.bounds.size;
        return Mathf.Max(size.x, Mathf.Max(size.y, size.z)) > settings.structureMinSize * S;
    }

    private bool NearestHit(Vector3 origin, Vector3 dir, float length, out RaycastHit best)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitBuffer, length, settings.layerMask, QueryTriggerInteraction.Ignore);
        best = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (ignored.Contains(h.collider)) continue;
            if (!found || h.distance < best.distance) { best = h; found = true; }
        }
        return found;
    }

    /// <summary>Yoksayılmayan collider'ları buffer'ın başına toplar.</summary>
    private int Overlap(Vector3 center, float radius)
    {
        int count = Physics.OverlapSphereNonAlloc(center, radius, colliderBuffer, settings.layerMask, QueryTriggerInteraction.Ignore);
        int kept = 0;
        for (int i = 0; i < count; i++)
        {
            if (ignored.Contains(colliderBuffer[i])) continue;
            colliderBuffer[kept++] = colliderBuffer[i];
        }
        return kept;
    }

    private void CollectGlass(IEnumerable<Transform> ignoreRoots)
    {
        Bounds searchArea = area;
        searchArea.Expand(new Vector3(2f, 4f, 2f) * S);

        foreach (Renderer r in Object.FindObjectsOfType<Renderer>())
        {
            if (!r.enabled || !searchArea.Intersects(r.bounds) || IsUnder(r.transform, ignoreRoots)) continue;
            if (MatchesGlass(r)) glassBounds.Add(r.bounds);
        }
    }

    private bool MatchesGlass(Renderer r)
    {
        if (ContainsKeyword(r.name)) return true;
        foreach (Material m in r.sharedMaterials)
        {
            if (m != null && ContainsKeyword(m.name)) return true;
        }
        return false;
    }

    private bool ContainsKeyword(string text)
    {
        string lower = text.ToLowerInvariant();
        foreach (string k in settings.glassKeywords)
        {
            if (!string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant())) return true;
        }
        return false;
    }

    private static bool IsUnder(Transform t, IEnumerable<Transform> roots)
    {
        foreach (Transform root in roots)
        {
            if (root != null && t.IsChildOf(root)) return true;
        }
        return false;
    }

    /// <summary>
    /// Eğitim etiketleri: mevcut SafeArea / WarningArea / DangerArea collider'larından.
    /// Çakışmada tehlike önceliklidir.
    /// </summary>
    public static int[] ComputeLabels(GnnGraph graph, float worldScale)
    {
        var safe = new List<Collider>();
        var danger = new List<Collider>();
        foreach (Collider c in Object.FindObjectsOfType<Collider>())
        {
            string tag = c.gameObject.tag;
            if (tag == "SafeArea") safe.Add(c);
            else if (tag == "DangerArea") danger.Add(c);
            // WarningArea etiketli alanlar "Exposed" sınıfında kalır
        }

        var labels = new int[graph.NodeCount];
        for (int i = 0; i < graph.NodeCount; i++)
        {
            Vector3 p = graph.Positions[i] + Vector3.up * (0.5f * worldScale);
            if (Inside(danger, p)) labels[i] = (int)GnnRiskClass.Danger;
            else if (Inside(safe, p)) labels[i] = (int)GnnRiskClass.Safe;
            else labels[i] = (int)GnnRiskClass.Exposed;
        }
        return labels;
    }

    private static bool Inside(List<Collider> colliders, Vector3 p)
    {
        foreach (Collider c in colliders)
        {
            if (c.enabled && (c.ClosestPoint(p) - p).sqrMagnitude < 1e-6f) return true;
        }
        return false;
    }
}
