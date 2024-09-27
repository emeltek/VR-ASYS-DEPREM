using UnityEngine;

public class GroundShake : MonoBehaviour
{
    public float duration = 10f;  // Deprem süresi
    public float magnitude = 0.1f;  // Sarsıntı şiddeti
    public float noiseScale = 1f;  // Perlin gürültüsü ölçeği

    private float elapsedTime = 0f;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Update()
    {
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