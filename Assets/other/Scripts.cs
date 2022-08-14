using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using static LoadingScreenPictures;

public class Scripts : MonoBehaviour {

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern uint GetWindowModuleFileName(IntPtr hwnd, StringBuilder lpszFileName, uint cchFileNameMax);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);


    public bool vrcIsOpen() {
        IEnumerable<IntPtr> windows = FindWindowsWithText("VRChat");

        foreach (IntPtr w in windows) {
            StringBuilder fileName = new StringBuilder(2000);
            GetWindowModuleFileName(w, fileName, 2000);

            //fileName comes back empty for the actual VRChat process becasue EAC hides it.
            if (fileName.Length == 0) return true;
        }

        return false;
    }

    //source https://www.codeproject.com/Answers/1096768/Is-there-a-way-to-check-if-a-file-is-in-use-opened#answer1
    public bool IsFileLocked(FileInfo file) {
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
        return FindWindows(delegate (IntPtr wnd, IntPtr param) {
            return GetWindowText(wnd).Contains(titleText);
        });
    }


}
