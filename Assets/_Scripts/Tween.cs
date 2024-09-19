using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

public class Tween : MonoBehaviour
{

    //devam et
    public Vector3 hedef = new Vector3(-59.5600014f, 166.244324f, -40.3479996f);
    public Vector3 hedefRotate = new Vector3(0, 161.688934f, 0);

    // Hareket süresi
    public float duration = 5.0f;

    void Start()
    {
        MoveToTarget();
    }

    void MoveToTarget()
    {
        transform.DOMove(hedef, 2).OnStart(() =>
        {
            transform.DORotate(hedefRotate,2);
        });

    }
}
