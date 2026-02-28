using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class ZoneBarControl : UserControl
{
    private readonly int _zoneId;
    private DateTime _lastClick = DateTime.MinValue;

    public ZoneBarControl(int zoneId)
    {
        InitializeComponent();
        _zoneId = zoneId;
        Loaded += (_, _) => Refresh();
    }

    public void Refresh()
    {
        // Zone 이름 갱신
        var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == _zoneId);
        ZoneNameLabel.Text = def?.DisplayName ?? $"Zone {_zoneId}";

        // 창 목록 갱신
        WindowList.Children.Clear();
        if (!Router.ZoneWindows.TryGetValue(_zoneId, out var handles)) return;

        var allWindows = WindowManager.GetVisibleWindows();
        var dict = allWindows.ToDictionary(w => w.Handle);

        foreach (var hWnd in handles.ToList())
        {
            if (!dict.TryGetValue(hWnd, out var win)) continue;

            string title = win.Title.Length > 24 ? win.Title[..24] + "…" : win.Title;

            var btn = new Button
            {
                Content = title,
                Height = 26,
                Padding = new Thickness(10, 0, 10, 0),
                Margin = new Thickness(2, 0, 2, 0),
                Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 11,
                Cursor = Cursors.Hand,
                Tag = hWnd,
                VerticalAlignment = VerticalAlignment.Center
            };

            btn.MouseEnter += (_, _) => btn.Background = new SolidColorBrush(Color.FromArgb(160, 33, 150, 243));
            btn.MouseLeave += (_, _) => btn.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));

            btn.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is IntPtr h)
                    // 모드 전환 없이 바로 창 전면화 (오버레이는 투명이라 그대로 사용 가능)
                    WindowManager.BringToFront(h);
            };

            WindowList.Children.Add(btn);
        }
    }

    #region Zone 이름 더블클릭 편집

    private void ZoneName_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        if ((now - _lastClick).TotalMilliseconds < 400)
            StartEditing();
        _lastClick = now;
    }

    private void StartEditing()
    {
        ZoneNameEdit.Text = ZoneNameLabel.Text;
        ZoneNameLabel.Visibility = Visibility.Collapsed;
        ZoneNameEdit.Visibility = Visibility.Visible;
        ZoneNameEdit.Focus();
        ZoneNameEdit.SelectAll();
    }

    private void FinishEditing()
    {
        string newName = ZoneNameEdit.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == _zoneId);
            if (def != null)
            {
                def.DisplayName = newName;
                ConfigStore.Save();
                ZoneNameLabel.Text = newName;
            }
        }
        ZoneNameEdit.Visibility = Visibility.Collapsed;
        ZoneNameLabel.Visibility = Visibility.Visible;
    }

    private void ZoneNameEdit_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) FinishEditing();
        else if (e.Key == Key.Escape)
        {
            ZoneNameEdit.Visibility = Visibility.Collapsed;
            ZoneNameLabel.Visibility = Visibility.Visible;
        }
    }

    private void ZoneNameEdit_LostFocus(object sender, RoutedEventArgs e)
        => FinishEditing();

    #endregion
}
