using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Thumbs
{
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

    public struct AvailableWindowInfo
    {
        public AvailableWindowInfo(IntPtr _hwnd, IntPtr _thumbHandle)
        {
            hwnd = _hwnd;
            thumbHandle = _thumbHandle;
        }

        public IntPtr hwnd;
        public IntPtr thumbHandle;
    }

    struct TaskManSettings
    {
        public bool enabled;
        public int width;
        public int height;
    }

    struct Settings
    {
        public List<string> ignore;
        public bool onTop;
        public int thumbnailWidth;
        public bool correctRatio;
        public TaskManSettings taskMan;
    }
}