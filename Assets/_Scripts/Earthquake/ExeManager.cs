using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class ExeManager : MonoBehaviour
{
    // Dosya yolunu ve çalıştırılacak dosyanın adını buraya ekleyin
    public string path = @"C:\Users\emin0\OneDrive\Masaüstü\basketballExe\dd.txt";
    public string workingDirectory = @"C:\Users\emin0\OneDrive\Masaüstü\basketballExe\";

    void Start()
    {
        Debug.Log("Path: " + workingDirectory+"dd.txt");
        Debug.Log("path: "+path);
        Debug.Log("Working Directory: " + workingDirectory);
    }

    void Update()
    {

    }

    public void Manager()
    {
         string sonPath = workingDirectory + "DunkHit.exe";

        try
        {
            Process p = new Process();
            p.StartInfo.FileName = sonPath;
            p.StartInfo.WorkingDirectory = workingDirectory;
            p.Start();
            Debug.Log("buton çalıştı.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Dosya bulunamadı veya çalıştırılamadı: " + ex.Message);
        }
    }
}