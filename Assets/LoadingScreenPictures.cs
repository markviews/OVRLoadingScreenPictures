using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;


public class LoadingScreenPictures : MonoBehaviour {

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    private static extern IntPtr FindWindow(string sClass, string sWindow);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern uint GetWindowModuleFileName(IntPtr hwnd, StringBuilder lpszFileName, uint cchFileNameMax);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

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

            bool checkRunning = vrcIsOpen();
            if (checkRunning != gameRunning) {
                gameRunning = checkRunning;

                if (gameRunning) {
                    Debug.Log("VRChat opened.");
                    StartCoroutine(watchLog());

                } else {
                    Debug.Log("VRChat closed.");
                    reader = null;
                    overlay.isVisible = false;
                }
            }

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
        IEnumerable<IntPtr> windows = FindWindowsWithText("VRChat");

        foreach (IntPtr w in windows) {
            StringBuilder fileName = new StringBuilder(2000);
            GetWindowModuleFileName(w, fileName, 2000);

            //fileName comes back empty for the actual VRChat process becasue EAC hides it.
            if (fileName.Length == 0) return true;
        }

        return false;
    }


    //source https://stackoverflow.com/a/20276701/3184295
    public static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter) {
        IntPtr found = IntPtr.Zero;
        List<IntPtr> windows = new List<IntPtr>();

        EnumWindows(delegate (IntPtr wnd, IntPtr param) {
            if (filter(wnd, param)) windows.Add(wnd);

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static string GetWindowText(IntPtr hWnd) {
        int size = GetWindowTextLength(hWnd);
        if (size > 0) {
            var builder = new StringBuilder(size + 1);
            GetWindowText(hWnd, builder, builder.Capacity);
            return builder.ToString();
        }

        return String.Empty;
    }

    public static IEnumerable<IntPtr> FindWindowsWithText(string titleText) {
        return FindWindows(delegate (IntPtr wnd, IntPtr param)
        {
            return GetWindowText(wnd).Contains(titleText);
        });
    }




    IEnumerator watchLog() {
        string log_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat";
        string latestLog = new DirectoryInfo(log_path).GetFiles().OrderByDescending(f => f.LastWriteTime).First().ToString();
        string fileName = latestLog.Substring(latestLog.LastIndexOf(@"\") + 1, latestLog.Length - latestLog.LastIndexOf(@"\") - 1);

        if (!IsFileLocked(new FileInfo(fileName))) {
            yield return new WaitForSeconds(1);
            watchLog();
            yield return true;
        }

        fixRotation();
        overlay.isVisible = true;

        var wh = new AutoResetEvent(false);
        var fsw = new FileSystemWatcher(".");
        fsw.Filter = fileName;
        fsw.EnableRaisingEvents = true;
        fsw.Changed += (s, e) => wh.Set();

        var fs = new FileStream(latestLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        reader = new StreamReader(fs);
        Debug.Log("Found log: " + fileName);
    }

    //source https://www.codeproject.com/Answers/1096768/Is-there-a-way-to-check-if-a-file-is-in-use-opened#answer1
    protected virtual bool IsFileLocked(FileInfo file) {
        FileStream stream = null;
        try {
            stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
        } catch (IOException) {
            return true;
        } finally {
            if (stream != null) stream.Close();
        }
        return false;
    }

    private void parseText(String text) {
        if (text.Length == 0) return;

        if (text.Contains("Unloading scenes")) {//entering loading screen
            Debug.Log("enabling overlay");
            overlay.isVisible = true;
            fixRotation();
        } else if (text.Contains("Instantiating VRC_OBJECTS")) {//exiting loading screen (could also try 'Loading asset bundle: ' which is a bit earlier)
            Debug.Log("disabling overlay");
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
