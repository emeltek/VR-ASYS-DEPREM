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
    [SerializeField] private int startingScore = 50; // ⭐ 50'den başla
    [SerializeField] private int minScore = 0;
    [SerializeField] private int maxScore = 100;
    [SerializeField] private float updateInterval = 2f;
    
    [Header("Puan Değerleri - Safe Area (En İyi)")]
    [Tooltip("SafeArea'ya ilk girişte kazanılan puan")]
    [SerializeField] private int safeAreaEnterBonus = 15; // ⭐ Artırıldı
    [Tooltip("SafeArea'da her 2 saniyede kazanılan puan")]
    [SerializeField] private int safeAreaStayBonus = 8; // ⭐ Artırıldı
    
    [Header("Puan Değerleri - Warning Area (Orta)")]
    [Tooltip("WarningArea'ya ilk girişte kaybedilen puan")]
    [SerializeField] private int warningAreaEnterPenalty = -3; // ⭐ Hafif ceza
    [Tooltip("WarningArea'da her 2 saniyede kaybedilen puan")]
    [SerializeField] private int warningAreaStayPenalty = -2; // ⭐ Hafif ceza
    
    [Header("Puan Değerleri - Danger Area (Kötü)")]
    [Tooltip("DangerArea'ya ilk girişte kaybedilen puan")]
    [SerializeField] private int dangerAreaEnterPenalty = -12; // ⭐ Artırıldı
    [Tooltip("DangerArea'da her 2 saniyede kaybedilen puan")]
    [SerializeField] private int dangerAreaStayPenalty = -6; // ⭐ Artırıldı
    
    [Header("Puan Değerleri - Açıkta Kalma (En Kötü)")]
    [Tooltip("Açıkta ilk 2 saniyedeki ceza")]
    [SerializeField] private int exposedAreaBasePenalty = -8; // ⭐ Artırıldı
    [Tooltip("Her 2 saniyede ceza artışı")]
    [SerializeField] private int exposedAreaPenaltyIncrement = -3; // ⭐ Artırıldı
    [Tooltip("Açıkta kalma maksimum ceza")]
    [SerializeField] private int exposedAreaMaxPenalty = -20; // ⭐ Artırıldı
    
    [Header("Performans Değerlendirmesi")]
    [Tooltip("Bu puanın üstü mükemmel performans")]
    [SerializeField] private int excellentThreshold = 80;
    [Tooltip("Bu puanın üstü iyi performans")]
    [SerializeField] private int goodThreshold = 60;
    [Tooltip("Bu puanın altı başarısız")]
    [SerializeField] private int failThreshold = 30;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private int currentScore;
    private AreaType currentArea = AreaType.None;
    private int currentExposedPenalty;
    
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
        
        if (showDebugLogs)
        {
            Debug.Log($"[CalculateScore] 🎮 Script başlatıldı. Başlangıç puanı: {currentScore}/100");
            Debug.Log($"[CalculateScore] Performans Seviyeleri: Mükemmel >{excellentThreshold}, İyi >{goodThreshold}, Başarısız <{failThreshold}");
            Debug.Log($"[CalculateScore] Açıkta kalma cezası: Başlangıç {exposedAreaBasePenalty}, Artış {exposedAreaPenaltyIncrement}, Maksimum {exposedAreaMaxPenalty}");
            
            if (shakeGroundObject == null)
                Debug.LogError("[CalculateScore] ShakeGroundObject referansı atanmamış!");
            
            if (scoreText == null)
                Debug.LogError("[CalculateScore] ScoreText referansı atanmamış!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Trigger'a giriş yapıldı! Obje: {other.gameObject.name}, Tag: {other.tag}");
        
        if (shakeGroundObject == null)
        {
            Debug.LogError("[CalculateScore] ShakeGroundObject null! Referansı kontrol et.");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Deprem durumu: {shakeGroundObject.isShaking}");
        
        if (!shakeGroundObject.isShaking)
        {
            if (showDebugLogs)
                Debug.LogWarning("[CalculateScore] Deprem aktif değil, puan değişmedi.");
            return;
        }
        
        AreaType newArea = GetAreaType(other.tag);
        
        if (newArea == AreaType.None)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[CalculateScore] Bilinmeyen tag: '{other.tag}'. SafeArea, WarningArea veya DangerArea olmalı.");
            return;
        }
        
        currentArea = newArea;
        
        // Açıkta kalma cezasını sıfırla (güvenli alana girdiyse)
        currentExposedPenalty = exposedAreaBasePenalty;
        
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Alan değişti: {currentArea}. Açıkta kalma cezası sıfırlandı.");
        
        // İlk giriş bonusu/cezası
        switch (currentArea)
        {
            case AreaType.SafeArea:
                UpdateScore(safeAreaEnterBonus);
                if (showDebugLogs)
                    Debug.Log($"<color=green>[CalculateScore] ✅ SafeArea'ya girildi! +{safeAreaEnterBonus} puan. Yeni puan: {currentScore}/100</color>");
                break;
            case AreaType.WarningArea:
                UpdateScore(warningAreaEnterPenalty);
                if (showDebugLogs)
                    Debug.Log($"<color=yellow>[CalculateScore] ⚠️ WarningArea'ya girildi! {warningAreaEnterPenalty} puan. Yeni puan: {currentScore}/100</color>");
                break;
            case AreaType.DangerArea:
                UpdateScore(dangerAreaEnterPenalty);
                if (showDebugLogs)
                    Debug.Log($"<color=red>[CalculateScore] ❌ DangerArea'ya girildi! {dangerAreaEnterPenalty} puan. Yeni puan: {currentScore}/100</color>");
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Trigger'dan çıkış yapıldı! Obje: {other.gameObject.name}, Tag: {other.tag}");
        
        AreaType exitedArea = GetAreaType(other.tag);
        if (exitedArea == currentArea)
        {
            currentArea = AreaType.None;
            
            // Açıkta kalma cezasını sıfırla
            currentExposedPenalty = exposedAreaBasePenalty;
            
            if (showDebugLogs)
                Debug.Log($"<color=orange>[CalculateScore] ⚠️ Alan temizlendi: {exitedArea} → None (Açıkta kaldı! Ceza başlıyor...)</color>");
        }
    }

    private IEnumerator UpdateScoreRoutine()
    {
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Puan güncelleme coroutine başlatıldı. Güncelleme aralığı: {updateInterval} saniye");
        
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            
            if (!shakeGroundObject.isShaking)
            {
                // Deprem bittiğinde açıkta kalma cezasını sıfırla
                currentExposedPenalty = exposedAreaBasePenalty;
                
                if (showDebugLogs && currentArea != AreaType.None)
                    Debug.Log($"[CalculateScore] Deprem aktif değil, sürekli puan güncellemesi atlandı.");
                continue;
            }
            
            // Sürekli kalma bonusu/cezası
            switch (currentArea)
            {
                case AreaType.SafeArea:
                    // Güvenli alanda, açıkta kalma cezasını sıfırla
                    currentExposedPenalty = exposedAreaBasePenalty;
                    
                    UpdateScore(safeAreaStayBonus);
                    if (showDebugLogs)
                        Debug.Log($"<color=green>[CalculateScore] ✅ SafeArea'da kalma bonusu: +{safeAreaStayBonus} puan. Yeni puan: {currentScore}/100</color>");
                    break;
                    
                case AreaType.WarningArea:
                    // Uyarı alanında, açıkta kalma cezasını sıfırla
                    currentExposedPenalty = exposedAreaBasePenalty;
                    
                    UpdateScore(warningAreaStayPenalty);
                    if (showDebugLogs)
                        Debug.Log($"<color=yellow>[CalculateScore] ⚠️ WarningArea'da kalma cezası: {warningAreaStayPenalty} puan. Yeni puan: {currentScore}/100</color>");
                    break;
                    
                case AreaType.DangerArea:
                    // Tehlikeli alanda, açıkta kalma cezasını sıfırla
                    currentExposedPenalty = exposedAreaBasePenalty;
                    
                    UpdateScore(dangerAreaStayPenalty);
                    if (showDebugLogs)
                        Debug.Log($"<color=red>[CalculateScore] ❌ DangerArea'da kalma cezası: {dangerAreaStayPenalty} puan. Yeni puan: {currentScore}/100</color>");
                    break;
                    
                case AreaType.None:
                    // ⚠️ AÇIKTA KALMA - ARTAN CEZA SİSTEMİ
                    UpdateScore(currentExposedPenalty);
                    
                    if (showDebugLogs)
                        Debug.Log($"<color=red><b>[CalculateScore] 🚨 AÇIKTA KALMA CEZASI: {currentExposedPenalty} puan. Yeni puan: {currentScore}/100</b></color>");
                    
                    // Cezayı artır (max'a kadar)
                    int previousPenalty = currentExposedPenalty;
                    currentExposedPenalty = Mathf.Max(currentExposedPenalty + exposedAreaPenaltyIncrement, exposedAreaMaxPenalty);
                    
                    if (showDebugLogs && previousPenalty != currentExposedPenalty)
                        Debug.Log($"<color=orange>[CalculateScore] 📈 Açıkta kalma cezası arttı: {previousPenalty} → {currentExposedPenalty}</color>");
                    else if (showDebugLogs && currentExposedPenalty == exposedAreaMaxPenalty)
                        Debug.Log($"<color=red>[CalculateScore] 💀 MAKSİMUM CEZAYA ULAŞILDI: {exposedAreaMaxPenalty}</color>");
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
        int oldScore = currentScore;
        currentScore = Mathf.Clamp(currentScore + change, minScore, maxScore);
        
        if (showDebugLogs && oldScore != currentScore)
            Debug.Log($"[CalculateScore] 📊 Puan güncellendi: {oldScore} → {currentScore}/100 (Değişim: {change})");
        
        UpdateScoreDisplay();
        
        // Kritik puan uyarıları
        if (currentScore <= failThreshold && oldScore > failThreshold && showDebugLogs)
            Debug.LogWarning($"<color=red>[CalculateScore] ⚠️ DÜŞÜK PUAN! Başarısız eşiğinin altına düştü: {currentScore}/{failThreshold}</color>");
        
        if (currentScore >= excellentThreshold && oldScore < excellentThreshold && showDebugLogs)
            Debug.Log($"<color=cyan>[CalculateScore] 🌟 MÜKEMMEL PERFORMANS! {currentScore}/{excellentThreshold}</color>");
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Puan: " + currentScore.ToString() + "/100";
        }
        else if (showDebugLogs)
        {
            Debug.LogError("[CalculateScore] ScoreText null! UI güncellenemedi.");
        }
    }
    
    // Dışarıdan erişim için
    public int GetCurrentScore() => currentScore;
    
    // Performans değerlendirmesi
    public string GetPerformanceRating()
    {
        if (currentScore >= excellentThreshold)
            return "Mükemmel! 🌟";
        else if (currentScore >= goodThreshold)
            return "İyi! 👍";
        else if (currentScore >= failThreshold)
            return "Orta 😐";
        else
            return "Başarısız ❌";
    }
    
    // Puanı sıfırlamak için
    public void ResetScore()
    {
        currentScore = startingScore;
        currentExposedPenalty = exposedAreaBasePenalty;
        currentArea = AreaType.None;
        UpdateScoreDisplay();
        
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] 🔄 Puan sıfırlandı: {currentScore}/100. Açıkta kalma cezası sıfırlandı.");
    }
}