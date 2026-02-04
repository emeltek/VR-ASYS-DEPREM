using UnityEngine;
using DG.Tweening;

public class ShakeGroundObject : MonoBehaviour
{
    public float duration = 10f;
    public float magnitude = 0f;
    public float maxMagnitude = 0.1f;
    public float noiseScale = 1f;

    public bool shakeActive = false;

    // 🔥 player.cs uyumluluğu için
    public bool isShaking => shakeActive;

    private float elapsedTime = 0f;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    // 🔘 Button burayı çağıracak
    public void StartShake()
    {
        if (shakeActive) return;

        shakeActive = true;
        elapsedTime = 0f;

        DOTween.Kill(this);

        DOTween.To(() => magnitude, x => magnitude = x, maxMagnitude, duration / 2)
            .SetTarget(this)
            .OnComplete(() =>
            {
                DOTween.To(() => magnitude, x => magnitude = x, 0f, duration / 2)
                    .SetTarget(this);
            });
    }

    void LateUpdate()
    {
        if (!shakeActive) return;

        if (elapsedTime < duration)
        {
            float x = Mathf.PerlinNoise(Time.time * noiseScale, 0) * 2 - 1;
            float z = Mathf.PerlinNoise(0, Time.time * noiseScale) * 2 - 1;

            Vector3 newPos =
                originalLocalPosition + new Vector3(x, 0, z) * magnitude;

            float tiltX = Mathf.PerlinNoise(Time.time * noiseScale, 1) * 2 - 1;
            float tiltZ = Mathf.PerlinNoise(1, Time.time * noiseScale) * 2 - 1;

            Quaternion newRot =
                Quaternion.Euler(tiltX * magnitude * 10, 0, tiltZ * magnitude * 10);

            transform.localPosition = newPos;
            transform.localRotation = originalLocalRotation * newRot;

            elapsedTime += Time.deltaTime;
        }
        else
        {
            StopShake();
        }
    }

    public void StopShake()
    {
        shakeActive = false;
        magnitude = 0f;

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
    }
}
