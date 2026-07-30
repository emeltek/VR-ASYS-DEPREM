using System.Collections;
using UnityEngine;
using TMPro;

public class CalculateScore : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI scoreText;
    
    [Header("Game References")]
    public ShakeGroundObject shakeGroundObject;
    
    [Header("Score Settings")]
    [SerializeField] private int startingScore = 50;
    [SerializeField] private int minScore = 0;
    [SerializeField] private int maxScore = 100;
    [SerializeField] private float updateInterval = 2f;
    
    [Header("Puan Değerleri - Safe Area (En İyi)")]
    [SerializeField] private int safeAreaEnterBonus = 15;
    [SerializeField] private int safeAreaStayBonus = 8;
    
    [Header("Puan Değerleri - Warning Area (Orta)")]
    [SerializeField] private int warningAreaEnterPenalty = -3;
    [SerializeField] private int warningAreaStayPenalty = -2;
    
    [Header("Puan Değerleri - Danger Area (Kötü)")]
    [SerializeField] private int dangerAreaEnterPenalty = -12;
    [SerializeField] private int dangerAreaStayPenalty = -6;
    
    [Header("Puan Değerleri - Açıkta Kalma (En Kötü)")]
    [SerializeField] private int exposedAreaBasePenalty = -8;
    [SerializeField] private int exposedAreaPenaltyIncrement = -3;
    [SerializeField] private int exposedAreaMaxPenalty = -20;
    
    [Header("Performans Değerlendirmesi")]
    [SerializeField] private int excellentThreshold = 80;
    [SerializeField] private int goodThreshold = 60;
    [SerializeField] private int failThreshold = 30;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private int currentScore;
    private AreaType currentArea = AreaType.None;
    private int currentExposedPenalty;
    
    // ⭐ YENİ: Oyuncunun anlık bir alan içinde olup olmadığını takip eder
    private bool isInAnyArea = false;

    private enum AreaType
    {
        None,
        SafeArea,
        WarningArea,
        DangerArea
    }

    private void Start()
    {
        currentScore = startingScore;
        currentExposedPenalty = exposedAreaBasePenalty;
        UpdateScoreDisplay();
        StartCoroutine(UpdateScoreRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        AreaType newArea = GetAreaType(other.tag);
        if (newArea == AreaType.None) return;

        currentArea = newArea;
        isInAnyArea = true; // ⭐ Alana girdiğini doğrula
        currentExposedPenalty = exposedAreaBasePenalty; // Ceza sıfırlandı
        
        if (shakeGroundObject != null && !shakeGroundObject.isShaking) return;

        switch (currentArea)
        {
            case AreaType.SafeArea:
                UpdateScore(safeAreaEnterBonus);
                break;
            case AreaType.WarningArea:
                UpdateScore(warningAreaEnterPenalty);
                break;
            case AreaType.DangerArea:
                UpdateScore(dangerAreaEnterPenalty);
                break;
        }
    }

    // ⭐ YENİ: Alanın içinde durduğu sürece alan durumunu aktif tutar
    private void OnTriggerStay(Collider other)
    {
        AreaType area = GetAreaType(other.tag);
        if (area != AreaType.None)
        {
            isInAnyArea = true;
            currentArea = area;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        AreaType exitedArea = GetAreaType(other.tag);
        if (exitedArea != AreaType.None)
        {
            isInAnyArea = false; // ⭐ Alandan çıktığını doğrula
            currentArea = AreaType.None;
            currentExposedPenalty = exposedAreaBasePenalty;
            
            if (showDebugLogs)
                Debug.Log("<color=orange>[CalculateScore] ⚠️ Karakter alandan çıktı, açıkta kaldı!</color>");
        }
    }

    private IEnumerator UpdateScoreRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            // Deprem yoksa puan hesabı yapma
            if (shakeGroundObject == null || !shakeGroundObject.isShaking)
            {
                currentExposedPenalty = exposedAreaBasePenalty;
                continue;
            }

            // ⭐ KONTROL: Eğer bir alanın içinde değilse otomatik None (Açıkta) sayılır
            if (!isInAnyArea)
            {
                currentArea = AreaType.None;
            }

            switch (currentArea)
            {
                case AreaType.SafeArea:
                    currentExposedPenalty = exposedAreaBasePenalty;
                    UpdateScore(safeAreaStayBonus);
                    break;
                    
                case AreaType.WarningArea:
                    currentExposedPenalty = exposedAreaBasePenalty;
                    UpdateScore(warningAreaStayPenalty);
                    break;
                    
                case AreaType.DangerArea:
                    currentExposedPenalty = exposedAreaBasePenalty;
                    UpdateScore(dangerAreaStayPenalty);
                    break;
                    
                case AreaType.None:
                    // 🚨 AÇIKTA KALMA CEZASI
                    UpdateScore(currentExposedPenalty);
                    
                    if (showDebugLogs)
                        Debug.Log($"<color=red><b>[CalculateScore] 🚨 AÇIKTA KALMA CEZASI: {currentExposedPenalty} puan. Yeni puan: {currentScore}/100</b></color>");
                    
                    // Cezayı kademeli artır
                    currentExposedPenalty = Mathf.Max(currentExposedPenalty + exposedAreaPenaltyIncrement, exposedAreaMaxPenalty);
                    break;
            }
        }
    }

    private AreaType GetAreaType(string tag)
    {
        switch (tag)
        {
            case "SafeArea": return AreaType.SafeArea;
            case "WarningArea": return AreaType.WarningArea;
            case "DangerArea": return AreaType.DangerArea;
            default: return AreaType.None;
        }
    }

    private void UpdateScore(int change)
    {
        currentScore = Mathf.Clamp(currentScore + change, minScore, maxScore);
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Puan: " + currentScore.ToString() + "/100";
    }
    
    public int GetCurrentScore() => currentScore;
    
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
        currentArea = AreaType.None;
        isInAnyArea = false;
        UpdateScoreDisplay();
    }
}