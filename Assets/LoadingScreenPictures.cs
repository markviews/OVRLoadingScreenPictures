using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

public class LoadingScreenPictures : MonoBehaviour {

    public Unity_Overlay overlay;
    public float waitTime;
    private string pictures_path = @"C:\Users\Mark\Pictures\VRChat";
    private float wait = 0.0f;
    private StreamReader reader;
    private Transform hmd;
    private bool gameRunning = false;

    void Update() {
        if (hmd == null) hmd = GameObject.Find("HMD").transform;

        if (Time.time > wait) {
            wait += waitTime;

            if (!gameRunning)
                if (vrcIsOpen()) gameRunning = true;

            if (overlay.isVisible)
                changeImage();

        }

        //parse the log when new info comes in
        if (reader != null) {
            string text = reader.ReadLine();
            if (text != null) parseText(text);
        }

    }

    private bool vrcIsOpen() {
        System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName("vrchat");
        if (process.Length == 0)
            return false;

        process[0].EnableRaisingEvents = true;
        process[0].Exited += (sender, e) => {
            gameRunning = false;
            Debug.Log("VRChat closed.");
            reader = null;
            overlay.isVisible = false;
        };

        Debug.Log("VRChat Detected.");
        StartCoroutine(watchLog());

        return true;
    }

    IEnumerator watchLog() {

        fixRotation();
        overlay.isVisible = true;

        //wait 5 seconds for log to be created..
        yield return new WaitForSeconds(5);

        string log_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat";
        string latestLog = new DirectoryInfo(log_path).GetFiles().OrderByDescending(f => f.LastWriteTime).First().ToString();
        string fileName = latestLog.Substring(latestLog.LastIndexOf(@"\") + 1, latestLog.Length - latestLog.LastIndexOf(@"\") - 1);

        var wh = new AutoResetEvent(false);
        var fsw = new FileSystemWatcher(".");
        fsw.Filter = fileName;
        fsw.EnableRaisingEvents = true;
        fsw.Changed += (s, e) => wh.Set();

        var fs = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        reader = new StreamReader(fs);
        Debug.Log("Found log: " + fileName);
    }

    private void parseText(String text) {
        if (text.Length == 0) return;

        if (text.Contains("Unloading scenes")) {//entering loading screen
            overlay.isVisible = true;
            fixRotation();
        } else if (text.Contains("Waiting for world metadata load to finish.. ")) {//exiting loading screen
            overlay.isVisible = false;
        }
    }

    private void fixRotation() {
        Vector3 newpos = hmd.position + hmd.forward * 2;
        newpos.y = 1.47f;
        overlay.transform.position = newpos;
        overlay.transform.rotation = new Quaternion(hmd.rotation.x, hmd.rotation.y, 0, hmd.rotation.w);
    }

    private void changeImage() {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(randImage()));
        if (tex.width > tex.height) {
            overlay.widthInMeters = 2.15f;
        } else {
            overlay.widthInMeters = 1;
        }
        overlay.overlayTexture = tex;
    }

    private String randImage() {
        if (!Directory.Exists(pictures_path)) return null;
        string[] pics = Directory.GetFiles(pictures_path, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".png") || s.EndsWith(".jpeg")).ToArray();
        if (pics.Length == 0) return null;
        int randPic = new System.Random().Next(0, pics.Length);
        return pics[randPic].ToString();
    }

}
