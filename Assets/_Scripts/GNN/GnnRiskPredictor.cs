using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GnnPrediction
{
    public bool IsValid;
    public float SafeProbability;
    public float ExposedProbability;
    public float DangerProbability;
    public GnnRiskClass RiskClass;

    /// <summary>0 (tamamen güvenli) – 1 (tamamen tehlikeli) arası sürekli risk skoru.</summary>
    public float RiskScore => Mathf.Clamp01(DangerProbability + 0.5f * ExposedProbability);
}

/// <summary>
/// Sahne açılınca zemin grafını arka planda (kareye yayarak) kurar, deprem başladığında
/// eğitilmiş GNN'i bir kez çalıştırıp her düğümün risk sınıfını önbelleğe alır.
/// Puanlama sırasında oyuncunun konumuna en yakın düğümün tahmini döndürülür.
/// </summary>
public class GnnRiskPredictor : MonoBehaviour
{
    public const string DefaultWeightsResource = "GNN/gnn_weights";

    public static GnnRiskPredictor Instance { get; private set; }

    [Header("Model")]
    [Tooltip("Boşsa Resources/GNN/gnn_weights.json yüklenir")]
    [SerializeField] private TextAsset weightsJson;

    [Header("Sahne Referansları (boşsa otomatik bulunur)")]
    [SerializeField] private ShakeGroundObject shakeGroundObject;
    [Tooltip("Graf bu çerçeveye göre tutulur; deprem evi sallasa da konum eşleşmesi bozulmaz")]
    [SerializeField] private Transform referenceFrame;
    [SerializeField] private Renderer[] floorRenderers;
    [SerializeField] private string floorNamePrefix = "Floor_Interior";
    [Tooltip("Bu objelerin collider'ları özellik hesabında yok sayılır (oyuncu, eller vb.)")]
    [SerializeField] private Transform[] ignoreRoots;

    [Header("Graf Ayarları (eğitimle aynı olmalı)")]
    [SerializeField] private GnnGraphSettings graphSettings = new GnnGraphSettings();
    [Tooltip("Graf kurulurken kare başına işlenecek düğüm sayısı")]
    [SerializeField] private int nodesPerFrame = 150;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    private GnnModel model;
    private GnnGraph graph;
    private IEnumerator buildRoutine;
    private Coroutine buildCoroutine;
    private bool graphReady;
    private float[] probabilities;
    private int[] previewLabels;
    private bool wasShaking;

    public bool IsReady => graphReady && probabilities != null;
    public GnnGraph Graph => graph;
    public GnnGraphSettings GraphSettings => graphSettings;

    private void Awake()
    {
        Instance = this;
        LoadModel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        ResolveReferences();
        GnnGraphBuilder builder = CreateBuilder(graphSettings);
        if (!builder.HasFloor)
        {
            Debug.LogError($"[GNN] '{floorNamePrefix}' isimli zemin renderer'ı bulunamadı, graf kurulamadı.");
            return;
        }

        graph = builder.Graph;
        buildRoutine = builder.Build(Mathf.Max(1, nodesPerFrame));
        buildCoroutine = StartCoroutine(RunBuild());
    }

    private void Update()
    {
        bool shaking = shakeGroundObject != null && shakeGroundObject.isShaking;
        if (shaking && !wasShaking) PredictNow();
        wasShaking = shaking;
    }

    private IEnumerator RunBuild()
    {
        float t0 = Time.realtimeSinceStartup;
        while (buildRoutine.MoveNext()) yield return null;
        OnGraphBuilt(t0);
    }

    private void OnGraphBuilt(float startTime)
    {
        graphReady = true;
        buildCoroutine = null;
        if (showDebugLogs)
            Debug.Log($"[GNN] Graf hazır: {graph.NodeCount} düğüm, {graph.EdgeCount / 2} kenar ({(Time.realtimeSinceStartup - startTime) * 1000f:F0} ms, kareye yayılmış).");
    }

    /// <summary>Deprem başında çağrılır: grafı tamamlar ve GNN çıkarımını bir kez yapar.</summary>
    public void PredictNow()
    {
        if (model == null || graph == null) return;

        if (!graphReady)
        {
            if (buildCoroutine != null) StopCoroutine(buildCoroutine);
            float t0 = Time.realtimeSinceStartup;
            while (buildRoutine.MoveNext()) { }
            OnGraphBuilt(t0);
        }

        float start = Time.realtimeSinceStartup;
        probabilities = model.Predict(graph);

        if (showDebugLogs)
        {
            int[] counts = new int[3];
            for (int i = 0; i < graph.NodeCount; i++) counts[(int)ArgMax(i)]++;
            Debug.Log($"<color=cyan>[GNN] Risk haritası hesaplandı ({(Time.realtimeSinceStartup - start) * 1000f:F1} ms): " +
                      $"Güvenli {counts[0]}, Açık {counts[1]}, Tehlikeli {counts[2]} düğüm.</color>");
        }
    }

    public GnnPrediction GetPrediction(Vector3 worldPosition)
    {
        var result = new GnnPrediction();
        if (!IsReady) return result;

        int node = FindNearestNode(worldPosition);
        if (node < 0)
        {
            // Grafın dışında (ör. hücre eşya ile dolu): açık alan kabul et
            result.IsValid = true;
            result.ExposedProbability = 1f;
            result.RiskClass = GnnRiskClass.Exposed;
            return result;
        }

        result.IsValid = true;
        result.SafeProbability = probabilities[node * 3 + 0];
        result.ExposedProbability = probabilities[node * 3 + 1];
        result.DangerProbability = probabilities[node * 3 + 2];
        result.RiskClass = ArgMax(node);
        return result;
    }

    private GnnRiskClass ArgMax(int node)
    {
        int best = 0;
        for (int c = 1; c < 3; c++)
        {
            if (probabilities[node * 3 + c] > probabilities[node * 3 + best]) best = c;
        }
        return (GnnRiskClass)best;
    }

    private int FindNearestNode(Vector3 worldPosition)
    {
        Vector3 p = ToBuildSpace(worldPosition);
        float cs = graph.CellSize;
        int cx = Mathf.FloorToInt((p.x - graph.GridOrigin.x) / cs - graphSettings.gridOffset.x);
        int cz = Mathf.FloorToInt((p.z - graph.GridOrigin.z) / cs - graphSettings.gridOffset.y);

        const int searchRadius = 2;
        int best = -1;
        float bestDist = float.MaxValue;
        for (int dx = -searchRadius; dx <= searchRadius; dx++)
        {
            for (int dz = -searchRadius; dz <= searchRadius; dz++)
            {
                if (!graph.CellToNode.TryGetValue(new Vector2Int(cx + dx, cz + dz), out int node)) continue;
                Vector3 q = graph.Positions[node];
                float d = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z);
                if (d < bestDist) { bestDist = d; best = node; }
            }
        }
        return best;
    }

    /// <summary>Güncel dünya konumunu, graf kurulduğu andaki dünya konumuna çevirir.</summary>
    private Vector3 ToBuildSpace(Vector3 worldPosition)
    {
        if (referenceFrame == null) return worldPosition;
        Vector3 local = referenceFrame.InverseTransformPoint(worldPosition);
        return graph.FrameWorldToLocal.inverse.MultiplyPoint3x4(local);
    }

    private Vector3 FromBuildSpace(Vector3 buildPosition)
    {
        if (referenceFrame == null) return buildPosition;
        return referenceFrame.TransformPoint(graph.FrameWorldToLocal.MultiplyPoint3x4(buildPosition));
    }

    private void LoadModel()
    {
        TextAsset asset = weightsJson != null ? weightsJson : Resources.Load<TextAsset>(DefaultWeightsResource);
        if (asset == null)
        {
            Debug.LogError($"[GNN] Model ağırlıkları bulunamadı (Resources/{DefaultWeightsResource}.json). Önce GNN/train_gnn.py ile modeli eğitin.");
            return;
        }

        model = GnnModel.FromJson(asset.text, out string error);
        if (model == null) Debug.LogError("[GNN] " + error);
    }

    public void ResolveReferences()
    {
        if (shakeGroundObject == null) shakeGroundObject = FindObjectOfType<ShakeGroundObject>();
        if (referenceFrame == null && shakeGroundObject != null) referenceFrame = shakeGroundObject.transform;

        if (floorRenderers == null || floorRenderers.Length == 0)
        {
            var found = new List<Renderer>();
            foreach (Renderer r in FindObjectsOfType<Renderer>())
            {
                if (!r.name.StartsWith(floorNamePrefix)) continue;
                if (referenceFrame != null && !r.transform.IsChildOf(referenceFrame)) continue;
                found.Add(r);
            }
            floorRenderers = found.ToArray();
        }

        if (ignoreRoots == null || ignoreRoots.Length == 0)
        {
            var roots = new List<Transform>();
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player")) roots.Add(go.transform);
            ignoreRoots = roots.ToArray();
        }
    }

    /// <summary>Editor dışa aktarıcısı da aynı builder'ı kullanır.</summary>
    public GnnGraphBuilder CreateBuilder(GnnGraphSettings settings)
    {
        return new GnnGraphBuilder(settings, floorRenderers, ignoreRoots, referenceFrame);
    }

    [ContextMenu("GNN: Grafı kur ve etiketleri göster (Editor)")]
    private void PreviewLabels()
    {
        ResolveReferences();
        graph = CreateBuilder(graphSettings).BuildImmediate();
        graphReady = true;
        previewLabels = GnnGraphBuilder.ComputeLabels(graph, graphSettings.worldScale);
        probabilities = null;
        Debug.Log($"[GNN] Önizleme: {graph.NodeCount} düğüm, {graph.EdgeCount / 2} kenar.");
    }

    [ContextMenu("GNN: Grafı kur ve modeli çalıştır (Editor)")]
    private void PreviewPrediction()
    {
        LoadModel();
        PreviewLabels();
        previewLabels = null;
        PredictNow();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || graph == null || !graphReady) return;

        float size = graph.CellSize * 0.6f;
        Vector3 cube = new Vector3(size, size * 0.15f, size);
        for (int i = 0; i < graph.NodeCount; i++)
        {
            Color color;
            if (probabilities != null)
            {
                float safe = probabilities[i * 3], danger = probabilities[i * 3 + 2];
                color = new Color(danger, safe, 0.15f, 0.85f) + new Color(0.5f, 0.5f, 0f, 0f) * probabilities[i * 3 + 1];
            }
            else if (previewLabels != null)
            {
                color = previewLabels[i] == 0 ? Color.green : previewLabels[i] == 2 ? Color.red : Color.yellow;
            }
            else continue;

            Gizmos.color = color;
            Gizmos.DrawCube(FromBuildSpace(graph.Positions[i]) + Vector3.up * size * 0.1f, cube);
        }
    }
}
