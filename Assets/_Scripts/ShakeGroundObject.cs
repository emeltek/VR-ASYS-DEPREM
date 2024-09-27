using System;
using UnityEngine;
using DG.Tweening;  // DoTween kütüphanesini kullanmak için gerekli

public class GroundShake : MonoBehaviour
{
    public float duration = 10f;  // Deprem süresi
    public float magnitude = 0f;  // Sarsıntı şiddeti (başlangıçta 0)
    public float maxMagnitude = 0.1f;  // Maksimum sarsıntı şiddeti
    public float noiseScale = 1f;  // Perlin gürültüsü ölçeği

    private float elapsedTime = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        // İlk yarıda magnitude'u maxMagnitude değerine kadar artır
        DOTween.To(() => magnitude, x => magnitude = x, maxMagnitude, duration / 2).OnComplete(() =>
        {
            // İkinci yarıda magnitude'u tekrar 0'a düşür
            DOTween.To(() => magnitude, x => magnitude = x, 0f, duration / 2);
        });
    }

    void Update()
    {
        Debug.Log("deprem sallanma şiddeti: " + magnitude);
        if (elapsedTime < duration)
        {
            // Pozisyon değişimi
            float x = Mathf.PerlinNoise(Time.time * noiseScale, 0) * 2 - 1;
            float z = Mathf.PerlinNoise(0, Time.time * noiseScale) * 2 - 1;
            Vector3 newPosition = originalPosition + new Vector3(x, 0, z) * magnitude;

            // Rotasyon değişimi
            float tiltX = Mathf.PerlinNoise(Time.time * noiseScale, 1) * 2 - 1;
            float tiltZ = Mathf.PerlinNoise(1, Time.time * noiseScale) * 2 - 1;
            Quaternion newRotation = Quaternion.Euler(tiltX * magnitude * 10, 0, tiltZ * magnitude * 10) * originalRotation;

            // Pozisyon ve rotasyonu uygula
            transform.position = newPosition;
            transform.rotation = newRotation;

            elapsedTime += Time.deltaTime;
        }
        else
        {
            // Deprem bittiğinde orijinal pozisyon ve rotasyona dön
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
}
