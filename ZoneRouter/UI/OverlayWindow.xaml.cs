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
    private string _dragType = "";
    private int _dragIndex = -1;

    // 배치 모드 (선/점 클릭 배치)
    private string _placingType = "none"; // "none" | "point" | "vline" | "hline"

    private const double BarHeight = 36;
    private const double HitSize   = 44;
    private const double DotSize   = 20;

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
        Left = 0; Top = 0;

        var src = PresentationSource.FromVisual(this);
        if (src?.CompositionTarget != null)
        {
            _dpiX = src.CompositionTarget.TransformToDevice.M11;
            _dpiY = src.CompositionTarget.TransformToDevice.M22;
        }

        _points = ConfigStore.Current.SplitPoints.Count > 0
            ? ConfigStore.Current.SplitPoints.Select(p => new SplitPoint { X = p.X, Y = p.Y }).ToList()
            : new List<SplitPoint> { new() { X = SystemParameters.PrimaryScreenWidth / 2, Y = SystemParameters.PrimaryScreenHeight / 2 } };

        _extraV = new List<double>(ConfigStore.Current.ExtraVLines);
        _extraH = new List<double>(ConfigStore.Current.ExtraHLines);

        OpacitySlider.Value = ConfigStore.Current.OverlayOpacity;
        ApplyMode(AppState.Mode);
        RefreshUI();
    }

    private void OnModeChanged(ZoneViewMode mode) => ApplyMode(mode);

    private void ApplyMode(ZoneViewMode mode)
    {
        CancelPlacing(); // 모드 바뀌면 배치 취소

        switch (mode)
        {
            case ZoneViewMode.Desktop:
                DimBackground.Visibility = Visibility.Collapsed;
                ZoneCanvas.Visibility    = Visibility.Collapsed;
                LineCanvas.Visibility    = Visibility.Collapsed;
                PreviewCanvas.Visibility = Visibility.Collapsed;
                ZoneBarCanvas.Visibility = Visibility.Collapsed;
                HandleCanvas.Visibility  = Visibility.Collapsed;
                ZoneModeBar.Visibility   = Visibility.Visible;
                EditModeBar.Visibility   = Visibility.Collapsed;
                BtnToggleMode.Content    = "바탕화면 모드";
                BtnToggleMode.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                BtnEnterEdit.Visibility  = Visibility.Collapsed;
                break;

            case ZoneViewMode.Zone:
                DimBackground.Fill       = Brushes.White;
                DimBackground.Opacity    = ConfigStore.Current.OverlayOpacity;
                DimBackground.Visibility = Visibility.Visible;
                ZoneCanvas.Visibility    = Visibility.Collapsed;
                LineCanvas.Visibility    = Visibility.Visible;
                PreviewCanvas.Visibility = Visibility.Collapsed;
                ZoneBarCanvas.Visibility = Visibility.Visible;
                HandleCanvas.Visibility  = Visibility.Collapsed;
                ZoneModeBar.Visibility   = Visibility.Visible;
                EditModeBar.Visibility   = Visibility.Collapsed;
                BtnToggleMode.Content    = "존 모드 ON";
                BtnToggleMode.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                BtnEnterEdit.Visibility  = Visibility.Visible;
                RefreshUI();
                break;

            case ZoneViewMode.Edit:
                DimBackground.Fill       = new SolidColorBrush(Color.FromRgb(15, 15, 30));
                DimBackground.Opacity    = 0.97;
                DimBackground.Visibility = Visibility.Visible;
                ZoneCanvas.Visibility    = Visibility.Visible;
                LineCanvas.Visibility    = Visibility.Visible;
                PreviewCanvas.Visibility = Visibility.Visible;
                ZoneBarCanvas.Visibility = Visibility.Collapsed;
                HandleCanvas.Visibility  = Visibility.Visible;
                ZoneModeBar.Visibility   = Visibility.Collapsed;
                EditModeBar.Visibility   = Visibility.Visible;
                RefreshUI();
                break;
        }
    }

    public void ApplyOpacity()
    {
        if (AppState.Mode == ZoneViewMode.Zone)
            DimBackground.Opacity = ConfigStore.Current.OverlayOpacity;
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
            DrawHandles();
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
            : new SolidColorBrush(Color.FromArgb(80, 80, 100, 200));
        double thick = isEdit ? 2 : 1;
        var dash = new DoubleCollection { 6, 4 };

        foreach (var x in _points.Select(p => p.X).Concat(_extraV).OrderBy(v => v))
            LineCanvas.Children.Add(new Line { X1 = x, Y1 = 0, X2 = x, Y2 = screen.Height, Stroke = pen, StrokeThickness = thick, StrokeDashArray = dash });

        foreach (var y in _points.Select(p => p.Y).Concat(_extraH).OrderBy(v => v))
            LineCanvas.Children.Add(new Line { X1 = 0, Y1 = y, X2 = screen.Width, Y2 = y, Stroke = pen, StrokeThickness = thick, StrokeDashArray = dash });
    }

    // ─── Zone 레이블 ───────────────────────────────────────────

    private void DrawZoneLabels(Dictionary<int, Rect> zones)
    {
        ZoneCanvas.Children.Clear();
        foreach (var kv in zones)
        {
            var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == kv.Key);
            string name = def?.DisplayName ?? $"Zone {kv.Key}";
            string apps = def?.ProcessNames.Count > 0 ? string.Join(", ", def.ProcessNames) : "앱 없음";

            var nameLabel = new TextBlock { Text = name, Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeights.Bold, Opacity = 0.75 };
            Canvas.SetLeft(nameLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(nameLabel,  kv.Value.Top  + kv.Value.Height / 2 - 24);
            ZoneCanvas.Children.Add(nameLabel);

            var appLabel = new TextBlock { Text = apps, FontSize = 13, Foreground = new SolidColorBrush(Color.FromArgb(160, 160, 200, 255)) };
            Canvas.SetLeft(appLabel, kv.Value.Left + kv.Value.Width / 2 - 55);
            Canvas.SetTop(appLabel,  kv.Value.Top  + kv.Value.Height / 2 + 6);
            ZoneCanvas.Children.Add(appLabel);
        }
    }

    // ─── 핸들 그리기 (편집 모드) ───────────────────────────────

    private void DrawHandles()
    {
        HandleCanvas.Children.Clear();
        double scrW = SystemParameters.PrimaryScreenWidth;
        double scrH = SystemParameters.PrimaryScreenHeight;

        // 교차점 핸들
        for (int i = 0; i < _points.Count; i++)
            AddHandle(_points[i].X, _points[i].Y, "point", i, Cursors.SizeAll, Colors.DodgerBlue);

        // 단독 세로선 핸들 (Y는 1/4 지점)
        for (int i = 0; i < _extraV.Count; i++)
            AddHandle(_extraV[i], scrH * 0.25, "vline", i, Cursors.SizeWE, Colors.Teal);

        // 단독 가로선 핸들 (X는 1/4 지점)
        for (int i = 0; i < _extraH.Count; i++)
            AddHandle(scrW * 0.25, _extraH[i], "hline", i, Cursors.SizeNS, Colors.Teal);
    }

    private void AddHandle(double cx, double cy, string type, int index, Cursor cursor, Color color)
    {
        // 핸들 동그라미
        var dot = new Ellipse
        {
            Width = DotSize, Height = DotSize,
            Fill = new SolidColorBrush(Color.FromArgb(220, color.R, color.G, color.B)),
            Stroke = Brushes.White, StrokeThickness = 2.5,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(dot, cx - DotSize / 2);
        Canvas.SetTop(dot,  cy - DotSize / 2);

        // 중심점
        var center = new Ellipse { Width = 7, Height = 7, Fill = Brushes.White, IsHitTestVisible = false };
        Canvas.SetLeft(center, cx - 3.5);
        Canvas.SetTop(center,  cy - 3.5);

        // 히트 영역 (투명, 클릭/드래그용)
        var hit = new Ellipse
        {
            Width = HitSize, Height = HitSize,
            Fill = Brushes.Transparent,
            Cursor = cursor,
            Tag = (type, index)
        };
        Canvas.SetLeft(hit, cx - HitSize / 2);
        Canvas.SetTop(hit,  cy - HitSize / 2);
        hit.MouseLeftButtonDown += Handle_Down;
        hit.MouseLeftButtonUp   += Handle_Up;
        hit.MouseMove           += Handle_Move;

        // X 삭제 버튼 (핸들 우상단)
        var xBtn = new Border
        {
            Width = 18, Height = 18,
            Background = new SolidColorBrush(Color.FromRgb(220, 50, 50)),
            CornerRadius = new CornerRadius(9),
            Cursor = Cursors.Hand,
            Child = new TextBlock { Text = "✕", Foreground = Brushes.White, FontSize = 10, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        Canvas.SetLeft(xBtn, cx + DotSize / 2 - 5);
        Canvas.SetTop(xBtn,  cy - DotSize / 2 - 5);
        string t = type; int idx = index;
        xBtn.MouseLeftButtonDown += (_, e) =>
        {
            if (t == "point") _points.RemoveAt(idx);
            else if (t == "vline") _extraV.RemoveAt(idx);
            else if (t == "hline") _extraH.RemoveAt(idx);
            SaveAndRefresh();
            e.Handled = true;
        };

        HandleCanvas.Children.Add(dot);
        HandleCanvas.Children.Add(center);
        HandleCanvas.Children.Add(xBtn);
        HandleCanvas.Children.Add(hit); // hit 맨 마지막 (최상위)
    }

    // ─── 드래그 ────────────────────────────────────────────────

    private void Handle_Down(object sender, MouseButtonEventArgs e)
    {
        if (_placingType != "none") return; // 배치 모드 중엔 드래그 무시
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

    // ─── 배치 모드 (클릭으로 선/점 추가) ──────────────────────

    private void EnterPlacingMode(string type)
    {
        _placingType = type;
        HandleCanvas.Cursor = Cursors.Cross;

        string hint = type switch
        {
            "point" => "● 교차점을 놓을 위치를 클릭하세요  (Esc: 취소)",
            "vline" => "| 세로선을 놓을 위치를 클릭하세요  (Esc: 취소)",
            "hline" => "— 가로선을 놓을 위치를 클릭하세요  (Esc: 취소)",
            _ => ""
        };
        PlacingHintText.Text = hint;
        PlacingHint.Visibility = Visibility.Visible;
        PreviewCanvas.Visibility = Visibility.Visible;

        // Esc 취소
        KeyDown += PlacingMode_KeyDown;
    }

    private void CancelPlacing()
    {
        _placingType = "none";
        if (HandleCanvas != null) HandleCanvas.Cursor = Cursors.Arrow;
        if (PlacingHint != null) PlacingHint.Visibility = Visibility.Collapsed;
        if (PreviewCanvas != null) PreviewCanvas.Children.Clear();
        KeyDown -= PlacingMode_KeyDown;
    }

    private void PlacingMode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) CancelPlacing();
    }

    // HandleCanvas 마우스 이동 → 미리보기 선 표시
    private void HandleCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_placingType == "none") return;

        PreviewCanvas.Children.Clear();
        var pos = e.GetPosition(this);
        var screen = GetScreenRect();
        var pen = new SolidColorBrush(Color.FromArgb(200, 255, 220, 50));

        switch (_placingType)
        {
            case "vline":
            case "point":
                PreviewCanvas.Children.Add(new Line { X1 = pos.X, Y1 = 0, X2 = pos.X, Y2 = screen.Height, Stroke = pen, StrokeThickness = 2 });
                break;
        }
        switch (_placingType)
        {
            case "hline":
            case "point":
                PreviewCanvas.Children.Add(new Line { X1 = 0, Y1 = pos.Y, X2 = screen.Width, Y2 = pos.Y, Stroke = pen, StrokeThickness = 2 });
                break;
        }
    }

    // HandleCanvas 클릭 → 배치
    private void HandleCanvas_Click(object sender, MouseButtonEventArgs e)
    {
        if (_placingType == "none") return;

        var pos = e.GetPosition(this);
        switch (_placingType)
        {
            case "point":
                _points.Add(new SplitPoint { X = pos.X, Y = pos.Y });
                break;
            case "vline":
                _extraV.Add(pos.X);
                break;
            case "hline":
                _extraH.Add(pos.Y);
                break;
        }

        CancelPlacing();
        SaveAndRefresh();
        e.Handled = true;
    }

    // ─── 저장 ──────────────────────────────────────────────────

    private void SaveAndRefresh()
    {
        ConfigStore.Current.SplitPoints = _points.Select(p => new SplitPoint { X = p.X, Y = p.Y }).ToList();
        ConfigStore.Current.ExtraVLines = new List<double>(_extraV);
        ConfigStore.Current.ExtraHLines = new List<double>(_extraH);
        ConfigStore.SaveLayout();
        RefreshUI();
    }

    // ─── Zone 바 ───────────────────────────────────────────────

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

    // ─── 버튼 ──────────────────────────────────────────────────

    private void BtnToggleMode_Click(object sender, RoutedEventArgs e)  => AppState.ToggleDesktopZone();
    private void BtnEnterEdit_Click(object sender, RoutedEventArgs e)   => AppState.SetMode(ZoneViewMode.Edit);
    private void BtnDone_Click(object sender, RoutedEventArgs e)        => AppState.SetMode(ZoneViewMode.Zone);

    private void BtnAddPoint_Click(object sender, RoutedEventArgs e) => EnterPlacingMode("point");
    private void BtnAddHLine_Click(object sender, RoutedEventArgs e) => EnterPlacingMode("hline");
    private void BtnAddVLine_Click(object sender, RoutedEventArgs e) => EnterPlacingMode("vline");

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DimBackground == null) return;
        ConfigStore.Current.OverlayOpacity = e.NewValue;
        ApplyOpacity();
        ConfigStore.Save();
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        AppState.ModeChanged -= OnModeChanged;
        base.OnClosed(e);
    }
}
