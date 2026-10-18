using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Deprem puanlaması. Alan bilgisi artık BoxCollider trigger'larından değil,
/// eğitilmiş GNN modelinin (GnnRiskPredictor) risk haritasından gelir.
/// Tehlike cezası, modelin o noktadaki risk skoruyla ölçeklenir:
/// daha tehlikeli nokta → daha yüksek ceza.
/// </summary>
public class CalculateScore : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;

    [Header("Game References")]
    public ShakeGroundObject shakeGroundObject;
    [Tooltip("Boşsa sahnedeki GnnRiskPredictor kullanılır, o da yoksa otomatik oluşturulur")]
    [SerializeField] private GnnRiskPredictor riskPredictor;
    [Tooltip("Konumu puanlanan obje. Boşsa bu obje (PlayerController)")]
    [SerializeField] private Transform trackedTarget;

    [Header("Score Settings")]
    [SerializeField] private int startingScore = 50;
    [SerializeField] private int minScore = 0;
    [SerializeField] private int maxScore = 100;
    [SerializeField] private float updateInterval = 2f;

    [Header("Puan Değerleri - Güvenli Alan")]
    [Tooltip("Deprem boyunca güvenli alana ilk girişte bir kez verilir")]
    [SerializeField] private int safeAreaEnterBonus = 15;
    [SerializeField] private int safeAreaStayBonus = 8;

    [Header("Puan Değerleri - Tehlikeli Alan (risk skoruyla ölçeklenir)")]
    [SerializeField] private int dangerAreaEnterPenaltyMin = -6;
    [SerializeField] private int dangerAreaEnterPenaltyMax = -15;
    [SerializeField] private int dangerAreaStayPenaltyMin = -3;
    [SerializeField] private int dangerAreaStayPenaltyMax = -10;
    [Tooltip("Bu risk skorunda minimum, 1.0'da maksimum ceza uygulanır")]
    [SerializeField, Range(0f, 1f)] private float dangerRiskFloor = 0.5f;

    [Header("Puan Değerleri - Açıkta Kalma")]
    [SerializeField] private int exposedAreaBasePenalty = -8;
    [SerializeField] private int exposedAreaPenaltyIncrement = -3;
    [SerializeField] private int exposedAreaMaxPenalty = -20;

    [Header("Performans Değerlendirmesi")]
    [SerializeField] private int excellentThreshold = 80;
    [SerializeField] private int goodThreshold = 60;
    [SerializeField] private int failThreshold = 30;

    [Header("Risk Etiketi (deprem bitince puana göre gösterilir)")]
    [Tooltip("Bu puan ve üzeri: Güvenli")]
    [SerializeField] private int safeThreshold = 75;
    [Tooltip("Bu puan ve üzeri: Orta Riskli")]
    [SerializeField] private int mediumRiskThreshold = 50;
    [Tooltip("Bu puan ve üzeri: Riskli. Altı: Yüksek Riskli")]
    [SerializeField] private int riskyThreshold = 25;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private int currentScore;
    private GnnRiskClass currentArea = GnnRiskClass.Exposed;
    private int currentExposedPenalty;
    private bool safeBonusGiven;
    private bool wasShaking;
    private bool quakeRunning;
    private bool showRiskLabel;

    private void Start()
    {
        if (trackedTarget == null) trackedTarget = transform;
        if (riskPredictor == null) riskPredictor = GnnRiskPredictor.Instance;
        if (riskPredictor == null) riskPredictor = FindObjectOfType<GnnRiskPredictor>();
        if (riskPredictor == null) riskPredictor = new GameObject("GNN Risk Predictor").AddComponent<GnnRiskPredictor>();

        currentScore = startingScore;
        currentExposedPenalty = exposedAreaBasePenalty;
        UpdateScoreDisplay();
        StartCoroutine(UpdateScoreRoutine());
    }

    private void Update()
    {
        // Deprem başlangıç/bitişini anında yakala: etiket, puan ekranı açılır açılmaz görünsün
        bool shaking = shakeGroundObject != null && shakeGroundObject.isShaking;
        if (shaking == quakeRunning) return;
        quakeRunning = shaking;

        showRiskLabel = !shaking;
        UpdateScoreDisplay();
        if (!shaking) Log("cyan", $"🏁 DEPREM BİTTİ: {GetRiskLabel()}");
    }

    private IEnumerator UpdateScoreRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            bool shaking = shakeGroundObject != null && shakeGroundObject.isShaking;
            if (!shaking)
            {
                wasShaking = false;
                currentExposedPenalty = exposedAreaBasePenalty;
                continue;
            }

            if (!wasShaking)
            {
                // Yeni deprem: alan durumunu sıfırla
                wasShaking = true;
                safeBonusGiven = false;
                currentArea = GnnRiskClass.Exposed;
            }

            if (!riskPredictor.IsReady) riskPredictor.PredictNow();
            GnnPrediction prediction = riskPredictor.GetPrediction(trackedTarget.position);
            if (!prediction.IsValid) continue;

            ApplyScore(prediction);
        }
    }

    private void ApplyScore(GnnPrediction prediction)
    {
        bool entered = prediction.RiskClass != currentArea;
        currentArea = prediction.RiskClass;

        switch (currentArea)
        {
            case GnnRiskClass.Safe:
                currentExposedPenalty = exposedAreaBasePenalty;
                int bonus = safeAreaStayBonus;
                if (entered && !safeBonusGiven)
                {
                    bonus += safeAreaEnterBonus;
                    safeBonusGiven = true;
                }
                UpdateScore(bonus);
                Log("green", $"✅ GÜVENLİ ALAN (p={prediction.SafeProbability:F2}): +{bonus}");
                break;

            case GnnRiskClass.Danger:
                currentExposedPenalty = exposedAreaBasePenalty;
                float severity = Mathf.InverseLerp(dangerRiskFloor, 1f, prediction.RiskScore);
                int penalty = Mathf.RoundToInt(Mathf.Lerp(dangerAreaStayPenaltyMin, dangerAreaStayPenaltyMax, severity));
                if (entered)
                    penalty += Mathf.RoundToInt(Mathf.Lerp(dangerAreaEnterPenaltyMin, dangerAreaEnterPenaltyMax, severity));
                UpdateScore(penalty);
                Log("red", $"⛔ TEHLİKELİ ALAN (risk={prediction.RiskScore:F2}, şiddet={severity:F2}): {penalty}");
                break;

            case GnnRiskClass.Exposed:
                UpdateScore(currentExposedPenalty);
                Log("orange", $"🚨 AÇIKTA KALMA (risk={prediction.RiskScore:F2}): {currentExposedPenalty}");
                currentExposedPenalty = Mathf.Max(currentExposedPenalty + exposedAreaPenaltyIncrement, exposedAreaMaxPenalty);
                break;
        }
    }

    private void Log(string color, string message)
    {
        if (showDebugLogs)
            Debug.Log($"<color={color}>[CalculateScore] {message} → Puan: {currentScore}/{maxScore}</color>");
    }

    private void UpdateScore(int change)
    {
        currentScore = Mathf.Clamp(currentScore + change, minScore, maxScore);
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText == null) return;

        string text = "Puan: " + currentScore.ToString() + "/100";
        if (showRiskLabel)
            text += $"\n<size=75%><color={GetRiskColor()}>{GetRiskLabel()}</color></size>";
        scoreText.text = text;
    }

    public int GetCurrentScore() => currentScore;

    /// <summary>Puana göre risk etiketi: yüksek puan = doğru davranış = düşük risk.</summary>
    public string GetRiskLabel()
    {
        if (currentScore >= safeThreshold) return "Güvenli";
        if (currentScore >= mediumRiskThreshold) return "Orta Riskli";
        if (currentScore >= riskyThreshold) return "Riskli";
        return "Yüksek Riskli";
    }

    private string GetRiskColor()
    {
        if (currentScore >= safeThreshold) return "#3CC864";
        if (currentScore >= mediumRiskThreshold) return "#F0B43C";
        if (currentScore >= riskyThreshold) return "#F07832";
        return "#E04646";
    }

    public string GetPerformanceRating()
    {
        if (currentScore >= excellentThreshold) return "Mükemmel! 🌟";
        if (currentScore >= goodThreshold) return "İyi! 👍";
        if (currentScore >= failThreshold) return "Orta 😐";
        return "Başarısız ❌";
    }

    public void ResetScore()
    {
        currentScore = startingScore;
        currentExposedPenalty = exposedAreaBasePenalty;
        currentArea = GnnRiskClass.Exposed;
        safeBonusGiven = false;
        showRiskLabel = false;
        UpdateScoreDisplay();
    }
}
