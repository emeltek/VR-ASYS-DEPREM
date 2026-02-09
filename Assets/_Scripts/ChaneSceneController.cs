using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ChaneSceneController : MonoBehaviour
{

    public void LoadExamScene()
    {
        SceneManager.LoadScene("ExamScene");
    }

    public void ExitApplication()
    {
        // Uygulamayı kapat
        #if UNITY_EDITOR
                EditorApplication.isPlaying = false; // Unity Editor'de test için
        #else
                    Application.Quit(); // Build edilmiş uygulamada çalışır
        #endif
    }
    public void RefreshScene()
    {
        // Aktif sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
