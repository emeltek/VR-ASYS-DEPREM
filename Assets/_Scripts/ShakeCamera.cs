using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShakeCamera : MonoBehaviour
{
     // Kamera sallama süresi
    public float duration = 0.5f;
    // Sallama yoğunluğu
    public float strength = 0.005f;
    // Sallama sayısı
    public int vibrato = 1;
    // Rastgelelik miktarı
    public float randomness = 1;

    void Start()
    {
        // Örneğin oyun başladığında kamerayı sallamak için bu satırı kullanabilirsiniz
    Shake();
    }

    public void Shake()
    {
        // DoTween kullanarak kamerayı sallama
        transform.DOShakePosition(duration, strength, vibrato, randomness);
    }

}
