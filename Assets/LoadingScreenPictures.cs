using System;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

public class LoadingScreenPictures : MonoBehaviour {

    public Unity_Overlay overlay;
    public float waitTime;
    private string pictures_path = @"C:\Users\Mark\Pictures\VRChat";
    private float wait = 0.0f;
    private StreamReader reader;
    private Transform hmd;

    void Start() {
        string log_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat";
        string latestLog = new DirectoryInfo(log_path).GetFiles().OrderByDescending(f => f.LastWriteTime).First().ToString();
        watchLog(latestLog);
    }

    void Update() {
        if (hmd == null) hmd = GameObject.Find("HMD").transform;

        
        if (reader == null) return;
        string text = reader.ReadLine();
        if (text != null) parseText(text);
        

        if (!overlay.isVisible) return;
        if (Time.time > wait) {
            wait += waitTime;
            changeImage();
        }
    }

    private void watchLog(string path) {
        string fileName = path.Substring(path.LastIndexOf(@"\") + 1, path.Length - path.LastIndexOf(@"\") - 1);
        Debug.Log(fileName);
        var wh = new AutoResetEvent(false);
        var fsw = new FileSystemWatcher(".");
        fsw.Filter = fileName;
        fsw.EnableRaisingEvents = true;
        fsw.Changed += (s, e) => wh.Set();

        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        reader = new StreamReader(fs);
        //wh.Close();
    }

    private void parseText(String text) {
        if (text.Length == 0) return;

        if (text.Contains("Unloading scenes")) {
            overlay.isVisible = true;

            //fix rotation
            Vector3 newpos = hmd.position + hmd.forward * 2;
            newpos.y = 1.45f;
            overlay.transform.position = newpos;
            overlay.transform.rotation = new Quaternion(hmd.rotation.x, hmd.rotation.y, 0, hmd.rotation.w);

        } else if (text.Contains("Waiting for world metadata load to finish.. ")) {
            overlay.isVisible = false;
        }
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
