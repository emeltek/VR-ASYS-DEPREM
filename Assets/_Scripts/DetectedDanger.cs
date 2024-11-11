using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectedDanger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("DangerArea"))
        {
            Debug.Log("Karakter tehlike bölgesine girdi!");
        }
    }
}
