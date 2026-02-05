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
    [SerializeField] private int startingScore = 100;
    [SerializeField] private int minScore = 0;
    [SerializeField] private int maxScore = 100;
    [SerializeField] private float updateInterval = 2f;
    
    [Header("Puan Değerleri")]
    [Tooltip("SafeArea'ya ilk girişte kazanılan puan")]
    [SerializeField] private int safeAreaEnterBonus = 10;
    [Tooltip("SafeArea'da her 2 saniyede kazanılan puan")]
    [SerializeField] private int safeAreaStayBonus = 5;
    
    [Tooltip("WarningArea'ya ilk girişte kaybedilen puan")]
    [SerializeField] private int warningAreaEnterPenalty = -5;
    [Tooltip("WarningArea'da her 2 saniyede kaybedilen puan")]
    [SerializeField] private int warningAreaStayPenalty = -3;
    
    [Tooltip("DangerArea'ya ilk girişte kaybedilen puan")]
    [SerializeField] private int dangerAreaEnterPenalty = -10;
    [Tooltip("DangerArea'da her 2 saniyede kaybedilen puan")]
    [SerializeField] private int dangerAreaStayPenalty = -5;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private int currentScore;
    private AreaType currentArea = AreaType.None;
    
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
        UpdateScoreDisplay();
        StartCoroutine(UpdateScoreRoutine());
        
        if (showDebugLogs)
        {
            Debug.Log($"[CalculateScore] Script başlatıldı. Başlangıç puanı: {currentScore}");
            
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
        
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Alan değişti: {currentArea}");
        
        // İlk giriş bonusu/cezası
        switch (currentArea)
        {
            case AreaType.SafeArea:
                UpdateScore(safeAreaEnterBonus);
                if (showDebugLogs)
                    Debug.Log($"<color=green>[CalculateScore] SafeArea'ya girildi! +{safeAreaEnterBonus} puan. Yeni puan: {currentScore}</color>");
                break;
            case AreaType.WarningArea:
                UpdateScore(warningAreaEnterPenalty);
                if (showDebugLogs)
                    Debug.Log($"<color=yellow>[CalculateScore] WarningArea'ya girildi! {warningAreaEnterPenalty} puan. Yeni puan: {currentScore}</color>");
                break;
            case AreaType.DangerArea:
                UpdateScore(dangerAreaEnterPenalty);
                if (showDebugLogs)
                    Debug.Log($"<color=red>[CalculateScore] DangerArea'ya girildi! {dangerAreaEnterPenalty} puan. Yeni puan: {currentScore}</color>");
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
            if (showDebugLogs)
                Debug.Log($"[CalculateScore] Alan temizlendi: {exitedArea} → None");
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
                if (showDebugLogs && currentArea != AreaType.None)
                    Debug.Log($"[CalculateScore] Deprem aktif değil, sürekli puan güncellemesi atlandı.");
                continue;
            }
            
            // Sürekli kalma bonusu/cezası
            switch (currentArea)
            {
                case AreaType.SafeArea:
                    UpdateScore(safeAreaStayBonus);
                    if (showDebugLogs)
                        Debug.Log($"<color=green>[CalculateScore] SafeArea'da kalma bonusu: +{safeAreaStayBonus} puan. Yeni puan: {currentScore}</color>");
                    break;
                case AreaType.WarningArea:
                    UpdateScore(warningAreaStayPenalty);
                    if (showDebugLogs)
                        Debug.Log($"<color=yellow>[CalculateScore] WarningArea'da kalma cezası: {warningAreaStayPenalty} puan. Yeni puan: {currentScore}</color>");
                    break;
                case AreaType.DangerArea:
                    UpdateScore(dangerAreaStayPenalty);
                    if (showDebugLogs)
                        Debug.Log($"<color=red>[CalculateScore] DangerArea'da kalma cezası: {dangerAreaStayPenalty} puan. Yeni puan: {currentScore}</color>");
                    break;
                case AreaType.None:
                    if (showDebugLogs)
                        Debug.Log($"[CalculateScore] Hiçbir alanda değil, puan değişmedi.");
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
            Debug.Log($"[CalculateScore] Puan güncellendi: {oldScore} → {currentScore} (Değişim: {change})");
        
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Puan: " + currentScore.ToString();
        }
        else if (showDebugLogs)
        {
            Debug.LogError("[CalculateScore] ScoreText null! UI güncellenemedi.");
        }
    }
    
    // Dışarıdan erişim için (isteğe bağlı)
    public int GetCurrentScore() => currentScore;
    
    // Puanı sıfırlamak için (isteğe bağlı)
    public void ResetScore()
    {
        currentScore = startingScore;
        UpdateScoreDisplay();
        
        if (showDebugLogs)
            Debug.Log($"[CalculateScore] Puan sıfırlandı: {currentScore}");
    }
}