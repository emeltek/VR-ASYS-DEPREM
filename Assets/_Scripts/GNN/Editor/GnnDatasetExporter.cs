using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Açık sahneden GNN eğitim graflarını GNN/data klasörüne JSON olarak yazar.
/// Etiketler mevcut SafeArea / DangerArea collider'larından gelir.
/// Veri çoğaltma: grid yarım hücre kaydırılarak aynı sahneden 4 farklı graf üretilir.
/// </summary>
public static class GnnDatasetExporter
{
    private const string ExamScenePath = "Assets/Scenes/ExamScene.unity";

    private static readonly Vector2[] GridOffsets =
    {
        new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)
    };

    public static string DataDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "GNN", "data");

    [MenuItem("Tools/GNN/Eğitim Verisini Dışa Aktar (Açık Sahne)")]
    public static void ExportOpenScene()
    {
        string summary = Export();
        EditorUtility.DisplayDialog("GNN Dışa Aktarım", summary, "Tamam");
    }

    /// <summary>
    /// Komut satırı: Unity -batchmode -quit -projectPath . -executeMethod GnnDatasetExporter.ExportFromCommandLine
    /// </summary>
    public static void ExportFromCommandLine()
    {
        EditorSceneManager.OpenScene(ExamScenePath, OpenSceneMode.Single);
        Debug.Log(Export());
    }

    private static string Export()
    {
        GnnRiskPredictor predictor = Object.FindObjectOfType<GnnRiskPredictor>();
        GameObject temp = null;
        if (predictor == null)
        {
            temp = new GameObject("GNN Exporter (temp)") { hideFlags = HideFlags.HideAndDontSave };
            predictor = temp.AddComponent<GnnRiskPredictor>();
        }

        try
        {
            predictor.ResolveReferences();
            Directory.CreateDirectory(DataDirectory);
            foreach (string old in Directory.GetFiles(DataDirectory, "*_graph_*.json")) File.Delete(old);

            string sceneName = EditorSceneManager.GetActiveScene().name;
            var log = new System.Text.StringBuilder();

            for (int k = 0; k < GridOffsets.Length; k++)
            {
                GnnGraphSettings settings = predictor.GraphSettings.Clone();
                settings.gridOffset = GridOffsets[k];

                GnnGraphBuilder builder = predictor.CreateBuilder(settings);
                if (!builder.HasFloor) return "Zemin renderer'ı bulunamadı (Floor_Interior*).";

                GnnGraph graph = builder.BuildImmediate();
                graph.Labels = GnnGraphBuilder.ComputeLabels(graph, settings.worldScale);

                string path = Path.Combine(DataDirectory, $"{sceneName}_graph_{k}.json");
                File.WriteAllText(path, JsonUtility.ToJson(ToExport(graph, settings, sceneName)));

                int[] counts = new int[3];
                foreach (int l in graph.Labels) counts[l]++;
                log.AppendLine($"graph_{k}: {graph.NodeCount} düğüm, {graph.EdgeCount / 2} kenar | Güvenli {counts[0]}, Açık {counts[1]}, Tehlikeli {counts[2]}");
            }

            log.AppendLine("Klasör: " + DataDirectory);
            return log.ToString();
        }
        finally
        {
            if (temp != null) Object.DestroyImmediate(temp);
        }
    }

    private static GnnGraphExport ToExport(GnnGraph graph, GnnGraphSettings settings, string sceneName)
    {
        int n = graph.NodeCount;
        var export = new GnnGraphExport
        {
            sceneName = sceneName,
            featureNames = GnnGraphBuilder.FeatureNames,
            classNames = GnnGraphBuilder.ClassNames,
            worldScale = settings.worldScale,
            cellSize = graph.CellSize,
            numNodes = n,
            numFeatures = graph.FeatureCount,
            positions = new float[n * 3],
            features = graph.Features,
            labels = graph.Labels,
            edgeSrc = new int[graph.EdgeCount],
            edgeDst = new int[graph.EdgeCount]
        };

        for (int i = 0; i < n; i++)
        {
            Vector3 p = graph.Positions[i];
            export.positions[i * 3] = p.x;
            export.positions[i * 3 + 1] = p.y;
            export.positions[i * 3 + 2] = p.z;

            for (int e = graph.NeighborOffsets[i]; e < graph.NeighborOffsets[i + 1]; e++)
            {
                export.edgeSrc[e] = i;
                export.edgeDst[e] = graph.NeighborIndices[e];
            }
        }

        return export;
    }
}
