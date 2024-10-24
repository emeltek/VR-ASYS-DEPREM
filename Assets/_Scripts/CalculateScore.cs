using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro için

public class CalculateScore : MonoBehaviour
{
    public TextMeshProUGUI scoreText; // Text yerine TextMeshProUGUI kullanıyoruz
    private int score = 100; // Başlangıç puanı
    private bool isInDangerArea = false;
    public ShakeGroundObject shakeGroundObject;
    private bool isInSafeArea = false;
    private const float updateInterval = 2f; // Puan güncelleme aralığı
    private const int minScore = 0;
    private const int maxScore = 100;

    private void Start()
    {
        
        UpdateScoreDisplay();
        StartCoroutine(UpdateScoreRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
         if (!shakeGroundObject.isShaking) return; // Deprem yoksa işlem yapma
        if (other.CompareTag("DangerArea"))
        {
            isInDangerArea = true;
            UpdateScore(-10);
        }
        else if (other.CompareTag("SafeArea"))
        {
            isInSafeArea = true;
            UpdateScore(10);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DangerArea"))
        {
            isInDangerArea = false;
        }
        else if (other.CompareTag("SafeArea"))
        {
            isInSafeArea = false;
        }
    }

    private IEnumerator UpdateScoreRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
  if (!shakeGroundObject.isShaking) continue; // Deprem yoksa döngüyü atla

            if (isInDangerArea)
            {
                UpdateScore(-5);
            }
            else if (isInSafeArea)
            {
                UpdateScore(5);
            }
        }
    }

    private void UpdateScore(int change)
    {
        score = Mathf.Clamp(score + change, minScore, maxScore);
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        scoreText.text = "Puan: " + score.ToString();
    }
}
