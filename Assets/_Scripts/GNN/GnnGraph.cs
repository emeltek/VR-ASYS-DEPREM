using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sınıf indeksleri. Python eğitim betiğiyle (GNN/train_gnn.py) aynı sırada olmalı.
/// </summary>
public enum GnnRiskClass
{
    Safe = 0,     // Güvenli alan (masa altı, sağlam eşya yanı)
    Exposed = 1,  // Açık / nötr alan
    Danger = 2    // Tehlikeli alan (cam, devrilebilecek / düşebilecek eşya)
}

/// <summary>
/// Grafın sahneden nasıl çıkarılacağını belirleyen ayarlar.
/// Mesafeler "gerçek metre" cinsindendir, worldScale ile dünya birimine çevrilir
/// (ExamScene'de 3D HOUSE ölçeği 3 olduğu için varsayılan 3).
/// </summary>
[Serializable]
public class GnnGraphSettings
{
    [Tooltip("Sahne ölçeği: 1 gerçek metre kaç dünya birimi (3D HOUSE scale = 3)")]
    public float worldScale = 3f;

    [Tooltip("Grid hücre boyu (m). Her hücre grafta bir düğüm olur.")]
    public float cellSize = 0.25f;

    [Tooltip("Yakın çevre yarıçapı (m): devrilebilecek / düşebilecek eşya araması")]
    public float neighborRadius = 1.5f;

    [Tooltip("Yatay tarama mesafesi (m): cam, duvar, sağlam eşya uzaklığı")]
    public float maxProbeDistance = 3f;

    [Tooltip("Yukarı tarama mesafesi (m): üstte koruyucu bir yüzey var mı")]
    public float maxOverheadDistance = 2f;

    [Tooltip("Hücrenin dolu sayılması için gövde yarıçapı (m)")]
    public float bodyRadius = 0.12f;

    [Tooltip("Bu boyuttan (m) büyük collider'lar bina yapısı (duvar/zemin/tavan) sayılır")]
    public float structureMinSize = 4f;

    [Tooltip("Cam olarak kabul edilecek renderer / materyal isim anahtar kelimeleri")]
    public string[] glassKeywords = { "glass", "cam", "window", "pencere" };

    public LayerMask layerMask = ~0;

    [Tooltip("Grid başlangıç kaydırması (hücre cinsinden). Eğitim verisi çoğaltmak için kullanılır.")]
    public Vector2 gridOffset = Vector2.zero;

    public GnnGraphSettings Clone() => (GnnGraphSettings)MemberwiseClone();
}

/// <summary>
/// Sahnenin zemininden çıkarılmış graf: düğüm = yürünebilir grid hücresi,
/// kenar = engelsiz komşu hücreler. Komşuluk CSR formatında tutulur.
/// </summary>
public class GnnGraph
{
    public int FeatureCount;
    public float CellSize;               // dünya birimi
    public Vector3 GridOrigin;           // dünya birimi, kuruluş anındaki
    public Matrix4x4 FrameWorldToLocal;  // kuruluş anında referans çerçevesi

    public readonly List<Vector3> Positions = new List<Vector3>(); // kuruluş anındaki dünya konumu (zemin noktası)
    public readonly List<Vector2Int> Cells = new List<Vector2Int>();
    public readonly Dictionary<Vector2Int, int> CellToNode = new Dictionary<Vector2Int, int>();

    public float[] Features;   // N * FeatureCount
    public int[] Labels;       // N (yalnızca eğitim verisi dışa aktarılırken dolu)

    public int[] NeighborOffsets; // N + 1
    public int[] NeighborIndices; // toplam kenar

    public int NodeCount => Positions.Count;
    public int EdgeCount => NeighborIndices?.Length ?? 0;
}

/// <summary>
/// Eğitim için dışa aktarılan JSON. Python tarafı bu formatı okur.
/// </summary>
[Serializable]
public class GnnGraphExport
{
    public string sceneName;
    public string[] featureNames;
    public string[] classNames;
    public float worldScale;
    public float cellSize;
    public int numNodes;
    public int numFeatures;
    public float[] positions; // N * 3
    public float[] features;  // N * F
    public int[] labels;      // N
    public int[] edgeSrc;
    public int[] edgeDst;
}
