using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShakeObject : MonoBehaviour
{private Vector3 originalPosition;
    public float speed = 5.0f;
    public float intensity = 0.1f;

    void Start()
    {
        // Objenin başlangıç pozisyonunu kaydediyoruz
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        // Objenin orijinal pozisyonuna küçük bir sallanma hareketi ekliyoruz
        float yMovement = Mathf.PerlinNoise(speed * Time.time, 1);
        transform.localPosition = originalPosition + intensity * new Vector3(
            0,
            yMovement,
            yMovement / 5);
    }
}