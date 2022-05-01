using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Thumbs
{
    public partial class MainWindow
    {
        readonly WindowInteropHelper _wih;
        Settings settings;
        List<AvailableWindowInfo> availableWindows = new List<AvailableWindowInfo>();
        AvailableWindowInfo taskManInfo = null;

        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = this;

            _wih = new WindowInteropHelper(this);

            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"ThumbSettings.json"));
            this.ThumbsWindow.Topmost = settings.onTop;
            this.LeftPanel.Width = settings.thumbnailWidth + 10;
            this.RightPanel.Width = settings.thumbnailWidth + 10;

            Loaded += (s, e) => RefreshWindows();
            SizeChanged += (s, e) => UpdateThumb();
            this.FullWindow.MouseUp += (s, e) => MouseClick(Mouse.GetPosition(this.FullWindow));

            CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, (s, e) =>
                {
                    RefreshWindows();
                }));
        }

        void RefreshWindows()
        {
            for (int i = 0; i < availableWindows.Count; i++)
            {
                if (availableWindows[i].thumbHandle != IntPtr.Zero)
                    DWMApi.DwmUnregisterThumbnail(availableWindows[i].thumbHandle);
            }

            availableWindows.Clear();

            User32.EnumWindows((hwnd, e) =>
                {
                    if (_wih.Handle != hwnd && (User32.GetWindowLongA(hwnd, User32.GWL_STYLE) & User32.TARGETWINDOW) == User32.TARGETWINDOW)
                    {
                        var sb = new StringBuilder(100);
                        User32.GetWindowText(hwnd, sb, sb.Capacity);
                        System.Diagnostics.Debug.WriteLine($"Window found: {sb.ToString()}" );

                        if (!settings.ignore.Contains(sb.ToString().Trim()))
                        {
                            IntPtr _thumbHandle;
                            if (DWMApi.DwmRegisterThumbnail(_wih.Handle, hwnd, out _thumbHandle) == 0)
                            {
                                if (settings.taskMan.enabled && sb.ToString().Trim().Equals("Task Manager"))
                                    taskManInfo = new AvailableWindowInfo(sb.ToString(), hwnd, _thumbHandle);
                                else
                                    availableWindows.Add(new AvailableWindowInfo(sb.ToString(), hwnd, _thumbHandle));
                            }
                        }
                    }

                    return true;
                }, 0);

            UpdateThumb();
        }

        void UpdateThumb()
        {
            if (availableWindows.Count == 0)
                return;

            this.RightPanel.Visibility = Visibility.Collapsed;

            int leftPos = 0;
            int topPos = 0;
            for (int i = 0; i < availableWindows.Count; i++)
            {
                AvailableWindowInfo awi = availableWindows[i];
                IntPtr _thumbHandle = awi.thumbHandle;

                int usedWidth = settings.thumbnailWidth;
                int usedHeight = settings.thumbnailWidth;

                if (settings.correctRatio)
                {
                    PSIZE size;
                    DWMApi.DwmQueryThumbnailSourceSize(_thumbHandle, out size);
                    if (size.x > size.y)
                        usedHeight = (int)(((double)settings.thumbnailWidth / size.x) * size.y);
                    else
                        usedWidth = (int)(((double)settings.thumbnailWidth / size.y) * size.x);
                }

                if (topPos + usedHeight + 10 > (int)this.ActualHeight)
                {
                    topPos = 0;
                    leftPos = (int)this.ActualWidth - (settings.thumbnailWidth + 10);
                    this.RightPanel.Visibility = Visibility.Visible;
                }

                Rect boundRect = new Rect(leftPos + 5, topPos + 5, leftPos + 5 + usedWidth, topPos + 5 + usedHeight);
                var props = new DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = DWMApi.DWM_TNP_VISIBLE | DWMApi.DWM_TNP_RECTDESTINATION | DWMApi.DWM_TNP_OPACITY,
                    opacity = (byte)255,
                    rcDestination = boundRect
                };

                System.Diagnostics.Debug.WriteLine($"Bounding rectangle of {awi.title}: {boundRect.Left}x{boundRect.Top} - {boundRect.Right}x{boundRect.Bottom}");
                DWMApi.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
                awi.boundRect = boundRect;

                topPos = topPos + usedHeight + 10;
            }

            if (settings.taskMan.enabled && taskManInfo != null)
            {
                this.RightPanel.Visibility = Visibility.Visible;
                IntPtr _thumbHandle = taskManInfo.thumbHandle;

                int tmLeftPos = (int)this.ActualWidth - (settings.taskMan.width + 5);
                int tmTopPos = (int)this.ActualHeight - (settings.taskMan.height + 5);
                int usedWidth = settings.taskMan.width;
                int usedHeight = settings.taskMan.height;

                Rect boundRect = new Rect(tmLeftPos, tmTopPos, tmLeftPos + usedWidth, tmTopPos + usedHeight);
                var props = new DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = DWMApi.DWM_TNP_VISIBLE | DWMApi.DWM_TNP_RECTDESTINATION | DWMApi.DWM_TNP_RECTSOURCE | DWMApi.DWM_TNP_OPACITY,
                    opacity = (byte)255,
                    rcDestination = boundRect,
                    rcSource = new Rect(5, 88, 5 + usedWidth, 88 + usedHeight)
                };

                System.Diagnostics.Debug.WriteLine($"Bounding rectangle of {taskManInfo.title}: {boundRect.Left}x{boundRect.Top} - {boundRect.Right}x{boundRect.Bottom}");
                DWMApi.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
                taskManInfo.boundRect = boundRect;
            }
        }

        void MouseClick(Point coordinates)
        {
            System.Diagnostics.Debug.WriteLine($"Mouse click at: {coordinates.X}x{coordinates.Y}");

            AvailableWindowInfo clickedAwi = availableWindows.FirstOrDefault(awi => coordinates.X > awi.boundRect.Left && coordinates.X < awi.boundRect.Right &&
                                                                                    coordinates.Y > awi.boundRect.Top  && coordinates.Y < awi.boundRect.Bottom);
            if (settings.taskMan.enabled && taskManInfo != null)
            {
                if (coordinates.X > taskManInfo.boundRect.Left && coordinates.X < taskManInfo.boundRect.Right &&
                    coordinates.Y > taskManInfo.boundRect.Top  && coordinates.Y < taskManInfo.boundRect.Bottom)
                {
                    clickedAwi = taskManInfo;
                }
            }

            if (clickedAwi != null)
            {
                System.Diagnostics.Debug.WriteLine($"Clicked window: {clickedAwi.title}");
                User32.ShowWindow(clickedAwi.hwnd, User32.SW_RESTORE);
                User32.SetForegroundWindow(clickedAwi.hwnd);
            }
        }
    }
}
