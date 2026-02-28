using System.Windows.Threading;
using ZoneRouter.UI;

namespace ZoneRouter.Core;

public class WindowMonitor
{
    private readonly DispatcherTimer _timer;
    private HashSet<IntPtr> _knownHandles = new();
    private bool _pickerShowing = false;
    private readonly Queue<WindowInfo> _pendingPicker = new();

    public WindowMonitor(int intervalMs = 400)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(intervalMs) };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        var current = WindowManager.GetVisibleWindows();
        _knownHandles = new HashSet<IntPtr>(current.Select(w => w.Handle));
        Router.ApplySavedRules();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            var current = WindowManager.GetVisibleWindows();
            var currentHandles = new HashSet<IntPtr>(current.Select(w => w.Handle));

            foreach (var win in current)
            {
                if (_knownHandles.Contains(win.Handle)) continue;

                System.Diagnostics.Debug.WriteLine($"[Monitor] 새 창: {win.Title} ({win.ProcessName})");

                // 규칙 있으면 자동 배치
                if (!Router.TryAutoRoute(win))
                {
                    // 규칙 없으면 팝업 큐에 추가
                    _pendingPicker.Enqueue(win);
                }
            }

            Router.SyncZoneWindows();
            _knownHandles = currentHandles;

            // 팝업이 안 떠있으면 다음 것 처리
            if (!_pickerShowing && _pendingPicker.Count > 0)
                ShowNextPicker();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor] 오류: {ex.Message}");
        }
    }

    private void ShowNextPicker()
    {
        if (_pendingPicker.Count == 0) return;
        var win = _pendingPicker.Dequeue();

        _pickerShowing = true;
        var picker = new ZonePickerWindow(win);
        picker.ZoneSelected += (zoneId) =>
        {
            ConfigStore.AssignProcessToZone(win.ProcessName, zoneId);
            Router.AssignWindowToZone(win.Handle, zoneId);
        };
        picker.Closed += (_, _) =>
        {
            _pickerShowing = false;
            // 남은 큐 처리
            if (_pendingPicker.Count > 0) ShowNextPicker();
        };
        picker.Show();
    }
}
