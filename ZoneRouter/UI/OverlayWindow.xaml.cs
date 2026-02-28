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
    private List<SplitPoint> _points = new();
    private List<double> _extraV = new();
    private List<double> _extraH = new();

    // 드래그 상태
    private bool _isDragging = false;
    private string _dragType = ""; // "point", "vline", "hline"
    private int _dragIndex = -1;

    private const double BarHeight = 36;
    private const double HitSize = 40; // 핸들 히트 영역 (크게)
    private const double DotSize = 18;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        AppState.ModeChanged += OnModeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Width  = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
        Left = 0; Top = 0;

        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget != null)
        {
            _dpiX = src.CompositionTarget.TransformToDevice.M11;
            _dpiY = src.CompositionTarget.TransformToDevice.M22;
        }

        // 저장된 레이아웃 불러오기
        if (ConfigStore.Current.SplitPoints.Count > 0)
        {
            _points = ConfigStore.Current.SplitPoints.Select(p => new SplitPoint { X = p.X, Y = p.Y }).ToList();
        }
        else
        {
            // 기본: 화면 중앙 교차점 1개
            _points = new List<SplitPoint>
            {
                new() { X = SystemParameters.PrimaryScreenWidth / 2, Y = SystemParameters.PrimaryScreenHeight / 2 }
            };
        }
        _extraV = new List<double>(ConfigStore.Current.ExtraVLines);
        _extraH = new List<double>(ConfigStore.Current.ExtraHLines);

        OpacitySlider.Value = ConfigStore.Current.OverlayOpacity;
        ApplyMode(AppState.Mode);
        RefreshUI();
    }

    private void OnModeChanged(ZoneViewMode mode) => ApplyMode(mode);

    private void ApplyMode(ZoneViewMode mode)
    {
        switch (mode)
        {
            case ZoneViewMode.Desktop:
                DimBackground.Visibility = Visibility.Collapsed;
                ZoneCanvas.Visibility    = Visibility.Collapsed;
                LineCanvas.Visibility    = Visibility.Collapsed;
                ZoneBarCanvas.Visibility = Visibility.Collapsed;
                HandleCanvas.Visibility  = Visibility.Collapsed;
                ZoneModeBar.Visibility   = Visibility.Visible;
                EditModeBar.Visibility   = Visibility.Collapsed;
                BtnToggleMode.Content    = "바탕화면 모드";
                BtnToggleMode.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                BtnEnterEdit.Visibility  = Visibility.Collapsed;
                break;

            case ZoneViewMode.Zone:
                // 흰색 반투명 오버레이 (앱 보이게)
                DimBackground.Fill       = Brushes.White;
                DimBackground.Opacity    = ConfigStore.Current.OverlayOpacity;
                DimBackground.Visibility = Visibility.Visible;
                ZoneCanvas.Visibility    = Visibility.Collapsed; // 색 블록 없음
                LineCanvas.Visibility    = Visibility.Visible;
                ZoneBarCanvas.Visibility = Visibility.Visible;
                HandleCanvas.Visibility  = Visibility.Collapsed; // 핸들 없음
                ZoneModeBar.Visibility   = Visibility.Visible;
                EditModeBar.Visibility   = Visibility.Collapsed;
                BtnToggleMode.Content    = "존 모드 ON";
                BtnToggleMode.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                BtnEnterEdit.Visibility  = Visibility.Visible;
                RefreshUI();
                break;

            case ZoneViewMode.Edit:
                // 바탕화면 완전 가림
                DimBackground.Fill       = new SolidColorBrush(Color.FromRgb(15, 15, 30));
                DimBackground.Opacity    = 0.97;
                DimBackground.Visibility = Visibility.Visible;
                ZoneCanvas.Visibility    = Visibility.Visible;
                LineCanvas.Visibility    = Visibility.Visible;
                ZoneBarCanvas.Visibility = Visibility.Collapsed;
                HandleCanvas.Visibility  = Visibility.Visible;
                ZoneModeBar.Visibility   = Visibility.Collapsed;
                EditModeBar.Visibility   = Visibility.Visible;
                RefreshUI();
                break;
        }
        UpdateControlBarWidth();
    }

    private void UpdateControlBarWidth()
    {
        ControlBar.Width = double.NaN; // Auto
    }

    public void ApplyOpacity()
    {
        if (AppState.Mode == ZoneViewMode.Zone)
        {
            DimBackground.Opacity = ConfigStore.Current.OverlayOpacity;
        }
    }

    public void RefreshUI()
    {
        var screen = GetScreenRect();
        var zones = ZoneEngine.CalcZones(_points, _extraV, _extraH, screen);

        Router.UpdateZones(zones.ToDictionary(
            kv => kv.Key,
            kv => WindowManager.DipToPixel(kv.Value, _dpiX, _dpiY)));

        DrawLines(screen);
        if (AppState.Mode == ZoneViewMode.Edit)
        {
            DrawZoneLabels(zones);
            DrawHandles(screen);
        }
        if (AppState.Mode == ZoneViewMode.Zone)
            DrawZoneBars(zones);
    }

    private Rect GetScreenRect()
        => new(0, 0, SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);

    // ─── 선 그리기 ─────────────────────────────────────────────

    private void DrawLines(Rect screen)
    {
        LineCanvas.Children.Clear();
        bool isEdit = AppState.Mode == ZoneViewMode.Edit;
        var pen = isEdit
            ? new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
            : new SolidColorBrush(Color.FromArgb(80, 100, 100, 200));
        double thick = isEdit ? 2 : 1;
        var dash = new DoubleCollection { 6, 4 };

        var vLines = _points.Select(p => p.X).Concat(_extraV).OrderBy(x => x);
        var hLines = _points.Select(p => p.Y).Concat(_extraH).OrderBy(y => y);

        foreach (var x in vLines)
            LineCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = screen.Height,
                Stroke = pen, StrokeThickness = thick, StrokeDashArray = dash
            });

        foreach (var y in hLines)
            LineCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = screen.Width, Y2 = y,
                Stroke = pen, StrokeThickness = thick, StrokeDashArray = dash
            });
    }

    // ─── Zone 레이블 (편집 모드) ───────────────────────────────

    private void DrawZoneLabels(Dictionary<int, Rect> zones)
    {
        ZoneCanvas.Children.Clear();
        foreach (var kv in zones)
        {
            var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == kv.Key);
            string name = def?.DisplayName ?? $"Zone {kv.Key}";
            string apps = def?.ProcessNames.Count > 0
                ? string.Join(", ", def.ProcessNames) : "앱 없음";

            var nameLabel = new TextBlock
            {
                Text = name, Foreground = Brushes.White,
                FontSize = 22, FontWeight = FontWeights.Bold, Opacity = 0.75
            };
            Canvas.SetLeft(nameLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(nameLabel, kv.Value.Top + kv.Value.Height / 2 - 24);
            ZoneCanvas.Children.Add(nameLabel);

            var appLabel = new TextBlock
            {
                Text = apps, FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 160, 200, 255))
            };
            Canvas.SetLeft(appLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(appLabel, kv.Value.Top + kv.Value.Height / 2 + 6);
            ZoneCanvas.Children.Add(appLabel);
        }
    }

    // ─── 핸들 그리기 (편집 모드) ───────────────────────────────

    private void DrawHandles(Rect screen)
    {
        HandleCanvas.Children.Clear();

        // 교차점 핸들 (점)
        for (int i = 0; i < _points.Count; i++)
            AddPointHandle(_points[i].X, _points[i].Y, "point", i, screen);

        // 단독 세로선 핸들
        for (int i = 0; i < _extraV.Count; i++)
            AddLineHandle(_extraV[i], screen.Height * 0.25, "vline", i, Cursors.SizeWE);

        // 단독 가로선 핸들
        for (int i = 0; i < _extraH.Count; i++)
            AddLineHandle(screen.Width * 0.25, _extraH[i], "hline", i, Cursors.SizeNS);
    }

    private void AddPointHandle(double cx, double cy, string type, int index, Rect screen)
    {
        // 십자 모양 + 원 핸들
        var outer = new Ellipse
        {
            Width = HitSize, Height = HitSize,
            Fill = Brushes.Transparent,
            Cursor = Cursors.SizeAll,
            Tag = (type, index)
        };
        var dot = new Ellipse
        {
            Width = DotSize, Height = DotSize,
            Fill = new SolidColorBrush(Color.FromArgb(220, 33, 150, 243)),
            Stroke = Brushes.White, StrokeThickness = 2.5,
            IsHitTestVisible = false
        };
        var center = new Ellipse
        {
            Width = 8, Height = 8,
            Fill = Brushes.White,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(outer, cx - HitSize / 2);
        Canvas.SetTop(outer, cy - HitSize / 2);
        Canvas.SetLeft(dot, cx - DotSize / 2);
        Canvas.SetTop(dot, cy - DotSize / 2);
        Canvas.SetLeft(center, cx - 4);
        Canvas.SetTop(center, cy - 4);

        outer.MouseLeftButtonDown += Handle_Down;
        outer.MouseLeftButtonUp   += Handle_Up;
        outer.MouseMove           += Handle_Move;

        var ctx = new ContextMenu();
        var del = new MenuItem { Header = "이 점(교차선) 삭제" };
        int idx = index;
        del.Click += (_, _) => { _points.RemoveAt(idx); SaveAndRefresh(); };
        ctx.Items.Add(del);
        outer.ContextMenu = ctx;

        HandleCanvas.Children.Add(dot);
        HandleCanvas.Children.Add(center);
        HandleCanvas.Children.Add(outer);
    }

    private void AddLineHandle(double cx, double cy, string type, int index, Cursor cursor)
    {
        var outer = new Rectangle
        {
            Width = type == "vline" ? 20 : HitSize,
            Height = type == "hline" ? 20 : HitSize,
            Fill = Brushes.Transparent,
            Cursor = cursor,
            Tag = (type, index)
        };
        var dot = new Ellipse
        {
            Width = DotSize - 4, Height = DotSize - 4,
            Fill = new SolidColorBrush(Color.FromArgb(200, 0, 150, 136)),
            Stroke = Brushes.White, StrokeThickness = 2,
            IsHitTestVisible = false
        };

        Canvas.SetLeft(outer, cx - outer.Width / 2);
        Canvas.SetTop(outer, cy - outer.Height / 2);
        Canvas.SetLeft(dot, cx - (DotSize - 4) / 2);
        Canvas.SetTop(dot, cy - (DotSize - 4) / 2);

        outer.MouseLeftButtonDown += Handle_Down;
        outer.MouseLeftButtonUp   += Handle_Up;
        outer.MouseMove           += Handle_Move;

        var ctx = new ContextMenu();
        var del = new MenuItem { Header = "이 선 삭제" };
        string t = type; int idx = index;
        del.Click += (_, _) =>
        {
            if (t == "vline") _extraV.RemoveAt(idx);
            else              _extraH.RemoveAt(idx);
            SaveAndRefresh();
        };
        ctx.Items.Add(del);
        outer.ContextMenu = ctx;

        HandleCanvas.Children.Add(dot);
        HandleCanvas.Children.Add(outer);
    }

    // ─── 드래그 ────────────────────────────────────────────────

    private void Handle_Down(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is (string type, int idx))
        {
            _isDragging = true;
            _dragType = type;
            _dragIndex = idx;
            el.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Handle_Up(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            (sender as FrameworkElement)?.ReleaseMouseCapture();
            SaveAndRefresh();
            e.Handled = true;
        }
    }

    private void Handle_Move(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        var pos = e.GetPosition(this);

        switch (_dragType)
        {
            case "point" when _dragIndex < _points.Count:
                _points[_dragIndex].X = pos.X;
                _points[_dragIndex].Y = pos.Y;
                break;
            case "vline" when _dragIndex < _extraV.Count:
                _extraV[_dragIndex] = pos.X;
                break;
            case "hline" when _dragIndex < _extraH.Count:
                _extraH[_dragIndex] = pos.Y;
                break;
        }
        RefreshUI();
        e.Handled = true;
    }

    private void SaveAndRefresh()
    {
        ConfigStore.Current.SplitPoints = _points.Select(p => new SplitPoint { X = p.X, Y = p.Y }).ToList();
        ConfigStore.Current.ExtraVLines = new List<double>(_extraV);
        ConfigStore.Current.ExtraHLines = new List<double>(_extraH);
        ConfigStore.SaveLayout();
        RefreshUI();
    }

    // ─── Zone 바 (Zone 모드) ────────────────────────────────────

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

    // ─── 버튼 핸들러 ───────────────────────────────────────────

    private void BtnToggleMode_Click(object sender, RoutedEventArgs e)
        => AppState.ToggleDesktopZone();

    private void BtnEnterEdit_Click(object sender, RoutedEventArgs e)
        => AppState.SetMode(ZoneViewMode.Edit);

    private void BtnDone_Click(object sender, RoutedEventArgs e)
        => AppState.SetMode(ZoneViewMode.Zone);

    private void BtnAddPoint_Click(object sender, RoutedEventArgs e)
    {
        // 화면 빈 공간에 새 교차점 추가
        double w = SystemParameters.PrimaryScreenWidth;
        double h = SystemParameters.PrimaryScreenHeight;
        _points.Add(new SplitPoint { X = w * 0.75, Y = h * 0.75 });
        SaveAndRefresh();
    }

    private void BtnAddHLine_Click(object sender, RoutedEventArgs e)
    {
        _extraH.Add(SystemParameters.PrimaryScreenHeight * 0.75);
        SaveAndRefresh();
    }

    private void BtnAddVLine_Click(object sender, RoutedEventArgs e)
    {
        _extraV.Add(SystemParameters.PrimaryScreenWidth * 0.75);
        SaveAndRefresh();
    }

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
