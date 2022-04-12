using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        AvailableWindowInfo taskManInfo;

        public ObservableCollection<KeyValuePair<string, AvailableWindowInfo>> AvailableWindows { get; } = new ObservableCollection<KeyValuePair<string, AvailableWindowInfo>>();

        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = this;

            _wih = new WindowInteropHelper(this);

            settings = Newtonsoft.Json.JsonConvert.DeserializeObject<Settings>(File.ReadAllText(@"E:\ThumbSettings.json"));
            this.ThumbsWindow.Topmost = settings.onTop;
            this.LeftPanel.Width = settings.thumbnailWidth + 10;
            this.RightPanel.Width = settings.thumbnailWidth + 10;

            Loaded += (s, e) => RefreshWindows();
            SizeChanged += (s, e) => UpdateThumb();

            CommandBindings.Add(new CommandBinding(NavigationCommands.Refresh, (s, e) =>
                {
                    RefreshWindows();
                }));
        }

        void RefreshWindows()
        {
            for (int i = 0; i < AvailableWindows.Count; i++)
            {
                if (AvailableWindows[i].Value.thumbHandle != IntPtr.Zero)
                    DWMApi.DwmUnregisterThumbnail(AvailableWindows[i].Value.thumbHandle);
            }

            AvailableWindows.Clear();

            User32.EnumWindows((hwnd, e) =>
                {
                    if (_wih.Handle != hwnd && (User32.GetWindowLongA(hwnd, User32.GWL_STYLE) & User32.TARGETWINDOW) == User32.TARGETWINDOW)
                    {
                        var sb = new StringBuilder(100);
                        User32.GetWindowText(hwnd, sb, sb.Capacity);
                        System.Diagnostics.Debug.WriteLine(sb.ToString());

                        if (!settings.ignore.Contains(sb.ToString().Trim()))
                        {
                            IntPtr _thumbHandle;
                            if (DWMApi.DwmRegisterThumbnail(_wih.Handle, hwnd, out _thumbHandle) == 0)
                            {
                                if (settings.taskMan.enabled && sb.ToString().Trim().Equals("Task Manager"))
                                    taskManInfo = new AvailableWindowInfo(hwnd, _thumbHandle);
                                else
                                    AvailableWindows.Add(new KeyValuePair<string, AvailableWindowInfo>(sb.ToString(), new AvailableWindowInfo(hwnd, _thumbHandle)));
                            }
                        }
                    }

                    return true;
                }, 0);

            UpdateThumb();
        }

        void UpdateThumb()
        {
            if (AvailableWindows.Count == 0)
                return;

            int limitNum = (int)this.LeftPanel.ActualHeight / (settings.thumbnailWidth + 10);
            if (AvailableWindows.Count > limitNum || settings.taskMan.enabled)
                this.RightPanel.Visibility = Visibility.Visible;
            else
                this.RightPanel.Visibility = Visibility.Collapsed;

            for (int i = 0; i < AvailableWindows.Count; i++)
            {
                KeyValuePair<string, AvailableWindowInfo> awi = AvailableWindows[i];
                IntPtr _thumbHandle = awi.Value.thumbHandle;

                int leftPos = i >= limitNum ? (int)this.ActualWidth - (settings.thumbnailWidth + 10) : 0;
                int topPos = (i % limitNum) * (settings.thumbnailWidth + 10);
                int usedWidth = settings.thumbnailWidth;
                int usedHeight = settings.thumbnailWidth;

                PSIZE size;
                DWMApi.DwmQueryThumbnailSourceSize(_thumbHandle, out size);
                /*if (size.x > size.y)
                    usedHeight = size.y;
                else
                    usedWidth = size.x;*/

                var props = new DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = DWMApi.DWM_TNP_VISIBLE | DWMApi.DWM_TNP_RECTDESTINATION | DWMApi.DWM_TNP_OPACITY,
                    opacity = (byte)255,
                    rcDestination = new Rect(leftPos + 5, topPos + 5, leftPos + 5 + settings.thumbnailWidth, topPos + 5 + settings.thumbnailWidth)
                };

                DWMApi.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
            }

            if (settings.taskMan.enabled)
            {
                IntPtr _thumbHandle = taskManInfo.thumbHandle;

                int leftPos = (int)this.ActualWidth - (settings.taskMan.width + 5);
                int topPos = (int)this.ActualHeight - (settings.taskMan.height + 5);
                int usedWidth = settings.taskMan.width;
                int usedHeight = settings.taskMan.height;

                var props = new DWM_THUMBNAIL_PROPERTIES
                {
                    fVisible = true,
                    dwFlags = DWMApi.DWM_TNP_VISIBLE | DWMApi.DWM_TNP_RECTDESTINATION | DWMApi.DWM_TNP_RECTSOURCE | DWMApi.DWM_TNP_OPACITY,
                    opacity = (byte)255,
                    rcDestination = new Rect(leftPos, topPos, leftPos + usedWidth, topPos + usedHeight),
                    rcSource = new Rect(5, 88, 5 + usedWidth, 88 + usedHeight)
                };

                DWMApi.DwmUpdateThumbnailProperties(_thumbHandle, ref props);
            }
        }
    }
}
