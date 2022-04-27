using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Thumbs
{
    #region WinApi
    [StructLayout(LayoutKind.Sequential)]
    struct DWM_THUMBNAIL_PROPERTIES
    {
        public int dwFlags;
        public Rect rcDestination;
        public Rect rcSource;
        public byte opacity;
        public bool fVisible;
        public bool fSourceClientAreaOnly;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct Rect
    {
        public Rect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PSIZE
    {
        public int x;
        public int y;
    }
    #endregion

    #region Application
    class AvailableWindowInfo
    {
        public AvailableWindowInfo(string _title, IntPtr _hwnd, IntPtr _thumbHandle)
        {
            title = _title;
            hwnd = _hwnd;
            thumbHandle = _thumbHandle;
        }

        public string title;
        public IntPtr hwnd;
        public IntPtr thumbHandle;
        public Rect boundRect;
    }

    class TaskManSettings
    {
        public bool enabled;
        public int width;
        public int height;
    }

    class Settings
    {
        public List<string> ignore;
        public bool onTop;
        public int thumbnailWidth;
        public bool correctRatio;
        public TaskManSettings taskMan;
    }
    #endregion
}