using System.Windows;
using System.Windows.Controls;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class ZoneBarControl : UserControl
{
    private readonly int _zoneId;

    public ZoneBarControl(int zoneId)
    {
        InitializeComponent();
        _zoneId = zoneId;
        Refresh();
    }

    public void Refresh()
    {
        WindowList.Children.Clear();

        if (!Router.ZoneWindows.TryGetValue(_zoneId, out var handles)) return;

        var allWindows = WindowManager.GetVisibleWindows();
        var dict = allWindows.ToDictionary(w => w.Handle);

        foreach (var hWnd in handles.ToList())
        {
            if (!dict.TryGetValue(hWnd, out var win)) continue;

            string title = win.Title.Length > 20
                ? win.Title[..20] + "…"
                : win.Title;

            var btn = new Button
            {
                Content = title,
                Height = 22,
                Padding = new Thickness(6, 0, 6, 0),
                Margin = new Thickness(2, 0, 2, 0),
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = hWnd
            };
            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is IntPtr h)
                    WindowManager.BringToFront(h);
            };
            WindowList.Children.Add(btn);
        }
    }
}
