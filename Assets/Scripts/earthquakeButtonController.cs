using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class earthquakeButtonController : MonoBehaviour
{
    [SerializeField] private GameObject button;
    [SerializeField] private ShakeGroundObject _shakeGroundObject;
    private float time = 0f;
    private bool earthquakeStarted = false;

    public void OnClickEarthquakeButton()
    {
        // Depremi başlat
        earthquakeStarted = true;
        time = 0f;
        
        // Butonu deaktif et
        button.SetActive(false);
    }

    void Update()
    {
        // Deprem başladıysa süreyi say
        if (earthquakeStarted && _shakeGroundObject.shakeActive)
        {
            time += Time.deltaTime;
            
            // Süre dolduğunda
            if (time >= _shakeGroundObject.duration)
            {
                earthquakeStarted = false;
                time = 0f;
                
                // Butonu tekrar aktif et
                button.SetActive(true);
            }
        }
    }
}