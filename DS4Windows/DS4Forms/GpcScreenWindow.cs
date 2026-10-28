using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DS4Windows.DS4Control.GpcScripting;

namespace DS4WinWPF.DS4Forms
{
    /// <summary>Shows a controller's GPC script screen (the virtual 128x64 OLED), live.</summary>
    public sealed class GpcScreenWindow : Window
    {
        const int W = global::Gpc.Engine.Runtime.Oled.Width;
        const int H = global::Gpc.Engine.Runtime.Oled.Height;
        const int Zoom = 4;

        readonly int slot;
        readonly WriteableBitmap bitmap = new WriteableBitmap(W, H, 96, 96, PixelFormats.Gray8, null);
        readonly byte[] frame = new byte[W * H];
        readonly TextBlock status = new TextBlock { Margin = new Thickness(6, 4, 6, 6) };
        readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        GpcScriptHost lastHost;
        int lastVersion = -1;

        public GpcScreenWindow(int slot)
        {
            this.slot = slot;
            Title = $"GPC screen - controller {slot + 1}";
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.CanMinimize;
            Topmost = true;

            var image = new Image { Source = bitmap, Width = W * Zoom, Height = H * Zoom, Margin = new Thickness(6) };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            var onTop = new CheckBox { Content = "Always on top", IsChecked = true, Margin = new Thickness(6, 6, 6, 0) };
            onTop.Checked += (_, __) => Topmost = true;
            onTop.Unchecked += (_, __) => Topmost = false;

            var panel = new DockPanel { Background = Brushes.Black };
            var top = new Border { Background = SystemColors.ControlBrush, Child = onTop };
            var bottom = new Border { Background = SystemColors.ControlBrush, Child = status };
            DockPanel.SetDock(top, Dock.Top);
            DockPanel.SetDock(bottom, Dock.Bottom);
            panel.Children.Add(top);
            panel.Children.Add(bottom);
            panel.Children.Add(image);
            Content = panel;

            timer.Tick += (_, __) => Refresh();
            Loaded += (_, __) => { Refresh(); timer.Start(); };
            Closed += (_, __) => timer.Stop();
        }

        void Refresh()
        {
            GpcScriptHost host = GpcScripts.HostFor(slot);
            if (host == null)
            {
                status.Text = $"No GPC script running on controller {slot + 1}.";
                if (lastHost != null)
                {
                    Array.Clear(frame, 0, frame.Length);
                    bitmap.WritePixels(new Int32Rect(0, 0, W, H), frame, W, 0);
                    lastHost = null;
                }
                return;
            }

            if (!ReferenceEquals(host, lastHost))
            {
                status.Text = $"{GpcScripts.DisplayNameFor(host.ScriptPath)} on controller {slot + 1}";
                lastHost = host;
                lastVersion = -1;
            }
            if (!host.CopyScreen(frame, ref lastVersion))
                return;

            for (int i = 0; i < frame.Length; i++)
                frame[i] = frame[i] != 0 ? (byte)255 : (byte)0;
            bitmap.WritePixels(new Int32Rect(0, 0, W, H), frame, W, 0);
        }
    }
}
