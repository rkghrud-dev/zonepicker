using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class OverlayWindow : Window
{
    private bool _isDragging;
    private Point _dragOffset;
    private Point _splitPoint;
    private bool _overlayVisible = true;
    private double _dpiX = 1.0, _dpiY = 1.0;

    // Zone 색상
    private static readonly Brush[] ZoneBrushes =
    [
        new SolidColorBrush(Color.FromArgb(40, 33, 150, 243)),   // 파랑
        new SolidColorBrush(Color.FromArgb(40, 76, 175, 80)),    // 초록
        new SolidColorBrush(Color.FromArgb(40, 255, 152, 0)),    // 주황
        new SolidColorBrush(Color.FromArgb(40, 156, 39, 176)),   // 보라
    ];

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // DPI 스케일 가져오기
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiX = source.CompositionTarget.TransformToDevice.M11;
            _dpiY = source.CompositionTarget.TransformToDevice.M22;
        }

        // 저장된 분할점 불러오기 (없으면 화면 중앙)
        var screen = GetScreenRect();
        if (ConfigStore.Current.SplitX > 0 && ConfigStore.Current.SplitY > 0)
            _splitPoint = new Point(ConfigStore.Current.SplitX, ConfigStore.Current.SplitY);
        else
            _splitPoint = ZoneEngine.DefaultSplitPoint(screen);

        RefreshUI();
    }

    private Rect GetScreenRect()
        => new Rect(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);

    private void RefreshUI()
    {
        var screen = GetScreenRect();
        var zones = ZoneEngine.CalcZones(_splitPoint, screen);

        // 픽셀 변환 후 Router에 업데이트
        var pixelZones = zones.ToDictionary(
            kv => kv.Key,
            kv => WindowManager.DipToPixel(kv.Value, _dpiX, _dpiY));
        Router.UpdateZones(pixelZones);

        DrawZones(zones);
        DrawLines(zones);
        PlaceHandle();
        DrawZoneBars(zones);
    }

    private void DrawZones(Dictionary<int, Rect> zones)
    {
        ZoneCanvas.Children.Clear();
        int idx = 0;
        foreach (var kv in zones)
        {
            var rect = new Rectangle
            {
                Width = kv.Value.Width,
                Height = kv.Value.Height,
                Fill = ZoneBrushes[idx % ZoneBrushes.Length],
                Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, kv.Value.Left);
            Canvas.SetTop(rect, kv.Value.Top);
            ZoneCanvas.Children.Add(rect);

            // Zone 번호 레이블
            string zoneName = ConfigStore.Current.ZoneNames.TryGetValue(kv.Key, out var n) ? n : $"Zone {kv.Key}";
            var label = new TextBlock
            {
                Text = zoneName,
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Opacity = 0.6
            };
            Canvas.SetLeft(label, kv.Value.Left + kv.Value.Width / 2 - 40);
            Canvas.SetTop(label, kv.Value.Top + kv.Value.Height / 2 - 15);
            ZoneCanvas.Children.Add(label);

            idx++;
        }
    }

    private void DrawLines(Dictionary<int, Rect> zones)
    {
        LineCanvas.Children.Clear();
        var screen = GetScreenRect();

        // 세로선
        var vLine = new Line
        {
            X1 = _splitPoint.X, Y1 = 0,
            X2 = _splitPoint.X, Y2 = screen.Height,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 6, 4 }
        };
        LineCanvas.Children.Add(vLine);

        // 가로선
        var hLine = new Line
        {
            X1 = 0, Y1 = _splitPoint.Y,
            X2 = screen.Width, Y2 = _splitPoint.Y,
            Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 6, 4 }
        };
        LineCanvas.Children.Add(hLine);
    }

    private void PlaceHandle()
    {
        Canvas.SetLeft(SplitHandle, _splitPoint.X - SplitHandle.Width / 2);
        Canvas.SetTop(SplitHandle, _splitPoint.Y - SplitHandle.Height / 2);
    }

    private void DrawZoneBars(Dictionary<int, Rect> zones)
    {
        ZoneBarCanvas.Children.Clear();
        foreach (var kv in zones)
        {
            var bar = new ZoneBarControl(kv.Key);
            Canvas.SetLeft(bar, kv.Value.Left + 2);
            Canvas.SetTop(bar, kv.Value.Top + 2);
            bar.Width = kv.Value.Width - 4;
            ZoneBarCanvas.Children.Add(bar);
        }
    }

    #region 드래그 핸들

    private void SplitHandle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragOffset = e.GetPosition(SplitHandle);
        SplitHandle.CaptureMouse();
    }

    private void SplitHandle_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        SplitHandle.ReleaseMouseCapture();
        ConfigStore.SaveSplitPoint(_splitPoint);
    }

    private void SplitHandle_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(this);
        _splitPoint = new Point(pos.X, pos.Y);
        RefreshUI();
    }

    #endregion

    #region 버튼 이벤트

    private void BtnConfirm_Click(object sender, RoutedEventArgs e)
    {
        ConfigStore.SaveSplitPoint(_splitPoint);
        HideOverlay();
    }

    private void BtnSendActive_Click(object sender, RoutedEventArgs e)
    {
        // 활성 창을 Zone으로 보내기
        var picker = new ZonePickerWindow();
        picker.ZoneSelected += (zoneId) =>
        {
            var activeHwnd = WindowManager.GetForeground();
            if (activeHwnd == IntPtr.Zero) return;

            var windows = WindowManager.GetVisibleWindows();
            var win = windows.FirstOrDefault(w => w.Handle == activeHwnd);
            if (win == null) return;

            Router.AssignWindowToZone(activeHwnd, zoneId);
            ConfigStore.AddOrUpdateRule(win.ProcessName, zoneId);
            DrawZoneBars(ZoneEngine.CalcZones(_splitPoint, GetScreenRect()));
        };
        picker.ShowDialog();
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_overlayVisible) HideOverlay();
        else ShowOverlay();
    }

    private void HideOverlay()
    {
        ZoneCanvas.Visibility = Visibility.Collapsed;
        LineCanvas.Visibility = Visibility.Collapsed;
        HandleCanvas.Visibility = Visibility.Collapsed;
        BtnConfirm.Visibility = Visibility.Collapsed;
        BtnSendActive.Visibility = Visibility.Collapsed;
        BtnToggle.Content = "오버레이 보기";
        _overlayVisible = false;
    }

    private void ShowOverlay()
    {
        ZoneCanvas.Visibility = Visibility.Visible;
        LineCanvas.Visibility = Visibility.Visible;
        HandleCanvas.Visibility = Visibility.Visible;
        BtnConfirm.Visibility = Visibility.Visible;
        BtnSendActive.Visibility = Visibility.Visible;
        BtnToggle.Content = "오버레이 숨기기";
        _overlayVisible = true;
        RefreshUI();
    }

    #endregion
}
