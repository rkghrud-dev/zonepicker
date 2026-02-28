using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class OverlayWindow : Window
{
    private double _dpiX = 1.0, _dpiY = 1.0;

    // 다중 선
    private List<double> _vLines = new(); // 세로선 x
    private List<double> _hLines = new(); // 가로선 y

    // 드래그 상태
    private bool _isDragging = false;
    private bool _isDraggingV = false; // true=세로선, false=가로선
    private int _draggingIndex = -1;
    private Point _lastMousePos;

    private const double BarHeight = 36;
    private const double HandleSize = 16;
    private const double HandleHitSize = 20;

    private static readonly Brush[] ZoneBrushes =
    [
        new SolidColorBrush(Color.FromArgb(40, 33, 150, 243)),
        new SolidColorBrush(Color.FromArgb(40, 76, 175, 80)),
        new SolidColorBrush(Color.FromArgb(40, 255, 152, 0)),
        new SolidColorBrush(Color.FromArgb(40, 156, 39, 176)),
        new SolidColorBrush(Color.FromArgb(40, 233, 30, 99)),
        new SolidColorBrush(Color.FromArgb(40, 0, 188, 212)),
        new SolidColorBrush(Color.FromArgb(40, 139, 195, 74)),
        new SolidColorBrush(Color.FromArgb(40, 255, 87, 34)),
        new SolidColorBrush(Color.FromArgb(40, 103, 58, 183)),
    ];

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        AppState.ModeChanged += OnModeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left = 0;
        Top = 0;

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget != null)
        {
            _dpiX = source.CompositionTarget.TransformToDevice.M11;
            _dpiY = source.CompositionTarget.TransformToDevice.M22;
        }

        // 저장된 선 불러오기 (없으면 화면 중앙 기본값)
        _vLines = ConfigStore.Current.VerticalLines.Count > 0
            ? new List<double>(ConfigStore.Current.VerticalLines)
            : new List<double> { SystemParameters.PrimaryScreenWidth / 2 };

        _hLines = ConfigStore.Current.HorizontalLines.Count > 0
            ? new List<double>(ConfigStore.Current.HorizontalLines)
            : new List<double> { SystemParameters.PrimaryScreenHeight / 2 };

        OpacitySlider.Value = ConfigStore.Current.OverlayOpacity;
        ApplyOpacity();
        RefreshUI();
    }

    private void OnModeChanged(bool isZoneMode)
    {
        var vis = isZoneMode ? Visibility.Visible : Visibility.Collapsed;
        DimBackground.Visibility = vis;
        ZoneCanvas.Visibility    = vis;
        LineCanvas.Visibility    = vis;
        HandleCanvas.Visibility  = vis;
        ZoneBarCanvas.Visibility = vis;

        BtnToggle.Content    = isZoneMode ? "존 모드 ON" : "바탕화면 모드";
        BtnToggle.Background = isZoneMode
            ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
            : new SolidColorBrush(Color.FromRgb(76, 175, 80));

        if (isZoneMode) RefreshUI();
    }

    public void ApplyOpacity() => DimBackground.Opacity = ConfigStore.Current.OverlayOpacity;

    public void RefreshUI()
    {
        var screen = GetScreenRect();
        var zones = ZoneEngine.CalcZones(_vLines, _hLines, screen);

        Router.UpdateZones(zones.ToDictionary(
            kv => kv.Key,
            kv => WindowManager.DipToPixel(kv.Value, _dpiX, _dpiY)));

        DrawZones(zones);
        DrawLines(screen);
        DrawHandles(screen);
        DrawZoneBars(zones);
    }

    private Rect GetScreenRect()
        => new(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);

    // ─── 그리기 ───────────────────────────────────────────────

    private void DrawZones(Dictionary<int, Rect> zones)
    {
        ZoneCanvas.Children.Clear();
        int idx = 0;
        foreach (var kv in zones)
        {
            var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == kv.Key);
            string zoneName = def?.DisplayName ?? $"Zone {kv.Key}";
            string apps = def?.ProcessNames.Count > 0 ? string.Join(", ", def.ProcessNames) : "앱 없음";

            var rect = new Rectangle
            {
                Width = kv.Value.Width, Height = kv.Value.Height,
                Fill = ZoneBrushes[idx % ZoneBrushes.Length],
                Stroke = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(rect, kv.Value.Left);
            Canvas.SetTop(rect, kv.Value.Top);
            ZoneCanvas.Children.Add(rect);

            var nameLabel = new TextBlock
            {
                Text = zoneName,
                Foreground = Brushes.White, FontSize = 18,
                FontWeight = FontWeights.Bold, Opacity = 0.6
            };
            Canvas.SetLeft(nameLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(nameLabel, kv.Value.Top + kv.Value.Height / 2 - 22);
            ZoneCanvas.Children.Add(nameLabel);

            var appLabel = new TextBlock
            {
                Text = apps, FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(170, 180, 200, 255))
            };
            Canvas.SetLeft(appLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(appLabel, kv.Value.Top + kv.Value.Height / 2 + 4);
            ZoneCanvas.Children.Add(appLabel);

            idx++;
        }
    }

    private void DrawLines(Rect screen)
    {
        LineCanvas.Children.Clear();
        var pen = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255));
        var dash = new DoubleCollection { 6, 4 };

        foreach (var x in _vLines)
            LineCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = screen.Height,
                Stroke = pen, StrokeThickness = 1.5, StrokeDashArray = dash
            });

        foreach (var y in _hLines)
            LineCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = screen.Width, Y2 = y,
                Stroke = pen, StrokeThickness = 1.5, StrokeDashArray = dash
            });
    }

    private void DrawHandles(Rect screen)
    {
        HandleCanvas.Children.Clear();

        // 세로선 핸들 (선의 수직 중앙)
        for (int i = 0; i < _vLines.Count; i++)
        {
            double x = _vLines[i];
            double y = screen.Height / 2;
            AddHandle(x, y, true, i);
        }

        // 가로선 핸들 (선의 수평 중앙)
        for (int i = 0; i < _hLines.Count; i++)
        {
            double x = screen.Width / 2;
            double y = _hLines[i];
            AddHandle(x, y, false, i);
        }
    }

    private void AddHandle(double cx, double cy, bool isVertical, int index)
    {
        // 히트 영역 (투명 큰 원)
        var hitArea = new Ellipse
        {
            Width = HandleHitSize, Height = HandleHitSize,
            Fill = Brushes.Transparent,
            Cursor = isVertical ? Cursors.SizeWE : Cursors.SizeNS,
            Tag = (isVertical, index)
        };
        Canvas.SetLeft(hitArea, cx - HandleHitSize / 2);
        Canvas.SetTop(hitArea, cy - HandleHitSize / 2);

        // 보이는 핸들
        var handle = new Ellipse
        {
            Width = HandleSize, Height = HandleSize,
            Fill = new SolidColorBrush(Color.FromArgb(200, 33, 150, 243)),
            Stroke = Brushes.White, StrokeThickness = 2,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(handle, cx - HandleSize / 2);
        Canvas.SetTop(handle, cy - HandleSize / 2);

        // 중앙 점
        var dot = new Ellipse
        {
            Width = 6, Height = 6,
            Fill = Brushes.White,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(dot, cx - 3);
        Canvas.SetTop(dot, cy - 3);

        hitArea.MouseLeftButtonDown += Handle_MouseDown;
        hitArea.MouseLeftButtonUp   += Handle_MouseUp;
        hitArea.MouseMove           += Handle_MouseMove;

        // 우클릭 → 삭제 메뉴
        var ctxMenu = new ContextMenu();
        var deleteItem = new MenuItem { Header = "이 선 삭제" };
        bool iv = isVertical; int idx = index;
        deleteItem.Click += (_, _) =>
        {
            if (iv) _vLines.RemoveAt(idx);
            else    _hLines.RemoveAt(idx);
            ConfigStore.SaveLines(_vLines, _hLines);
            RefreshUI();
        };
        ctxMenu.Items.Add(deleteItem);
        hitArea.ContextMenu = ctxMenu;

        HandleCanvas.Children.Add(handle);
        HandleCanvas.Children.Add(dot);
        HandleCanvas.Children.Add(hitArea);
    }

    // ─── 드래그 ───────────────────────────────────────────────

    private void Handle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is (bool isV, int idx))
        {
            _isDragging = true;
            _isDraggingV = isV;
            _draggingIndex = idx;
            _lastMousePos = e.GetPosition(this);
            el.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Handle_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            (sender as Ellipse)?.ReleaseMouseCapture();
            ConfigStore.SaveLines(_vLines, _hLines);
            e.Handled = true;
        }
    }

    private void Handle_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(this);

        if (_isDraggingV && _draggingIndex < _vLines.Count)
            _vLines[_draggingIndex] = pos.X;
        else if (!_isDraggingV && _draggingIndex < _hLines.Count)
            _hLines[_draggingIndex] = pos.Y;

        RefreshUI();
        e.Handled = true;
    }

    // ─── 선 추가 (우클릭 컨텍스트 메뉴) ──────────────────────

    private void AddVLine_Click(object sender, RoutedEventArgs e)
    {
        // 클릭한 x 위치에 세로선 추가 (컨텍스트 메뉴 위치 기준)
        double x = SystemParameters.PrimaryScreenWidth / 2;
        _vLines.Add(x);
        ConfigStore.SaveLines(_vLines, _hLines);
        RefreshUI();
    }

    private void AddHLine_Click(object sender, RoutedEventArgs e)
    {
        double y = SystemParameters.PrimaryScreenHeight / 2;
        _hLines.Add(y);
        ConfigStore.SaveLines(_vLines, _hLines);
        RefreshUI();
    }

    // ─── Zone Bar (하단 작업표시줄) ───────────────────────────

    private void DrawZoneBars(Dictionary<int, Rect> zones)
    {
        ZoneBarCanvas.Children.Clear();
        foreach (var kv in zones)
        {
            var bar = new ZoneBarControl(kv.Key);
            Canvas.SetLeft(bar, kv.Value.Left);
            Canvas.SetTop(bar, kv.Value.Bottom - BarHeight);
            bar.Width = kv.Value.Width;
            ZoneBarCanvas.Children.Add(bar);
        }
    }

    // ─── 버튼 ─────────────────────────────────────────────────

    private void BtnToggle_Click(object sender, RoutedEventArgs e) => AppState.Toggle();

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DimBackground == null) return;
        ConfigStore.Current.OverlayOpacity = e.NewValue;
        ApplyOpacity();
        ConfigStore.Save();
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        AppState.ModeChanged -= OnModeChanged;
        base.OnClosed(e);
    }
}
