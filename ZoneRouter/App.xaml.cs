using System.Windows;
using ZoneRouter.Core;
using ZoneRouter.UI;

namespace ZoneRouter;

public partial class App : Application
{
    private WindowMonitor? _monitor;

    private void App_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            ConfigStore.Load();

            var overlay = new OverlayWindow();
            overlay.Show();

            _monitor = new WindowMonitor();
            _monitor.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"시작 오류: {ex.Message}", "ZoneRouter", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _monitor?.Stop();
        base.OnExit(e);
    }
}
