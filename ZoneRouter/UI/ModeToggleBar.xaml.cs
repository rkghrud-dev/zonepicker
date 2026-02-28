using System.Windows;
using System.Windows.Input;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class ModeToggleBar : Window
{
    private readonly OverlayWindow _overlay;

    public ModeToggleBar(OverlayWindow overlay)
    {
        InitializeComponent();
        _overlay = overlay;

        // 화면 우하단에 배치
        Loaded += (_, _) =>
        {
            Left = SystemParameters.PrimaryScreenWidth - Width - 20;
            Top = SystemParameters.PrimaryScreenHeight - Height - 60;
            OpacitySlider.Value = ConfigStore.Current.OverlayOpacity;
        };

        AppState.ModeChanged += OnModeChanged;
    }

    private void OnModeChanged(bool isZoneMode)
    {
        BtnToggle.Content = isZoneMode ? "존 모드 ON" : "바탕화면 모드";
        BtnToggle.Background = isZoneMode
            ? System.Windows.Media.Brushes.DodgerBlue
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(76, 175, 80));
        OpacitySlider.IsEnabled = isZoneMode;
    }

    private void BtnToggle_Click(object sender, RoutedEventArgs e)
        => AppState.Toggle();

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_overlay == null) return;
        ConfigStore.Current.OverlayOpacity = e.NewValue;
        _overlay.ApplyOpacity();
        ConfigStore.Save();
    }

    private void DragHandle_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
        => Application.Current.Shutdown();

    protected override void OnClosed(EventArgs e)
    {
        AppState.ModeChanged -= OnModeChanged;
        base.OnClosed(e);
    }
}
