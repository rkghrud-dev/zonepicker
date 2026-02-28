using System.Windows;
using System.Windows.Threading;

namespace ZoneRouter.Core;

/// <summary>
/// 폴링 방식으로 새 창 감지 (MVP1)
/// 새 창 발견 시 NewWindowDetected 이벤트 발생
/// </summary>
public class WindowMonitor
{
    private readonly DispatcherTimer _timer;
    private HashSet<IntPtr> _knownHandles = new();

    public event Action<WindowInfo>? NewWindowDetected;

    public WindowMonitor(int intervalMs = 400)
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(intervalMs)
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        // 현재 창 목록을 기준점으로 저장
        var current = WindowManager.GetVisibleWindows();
        _knownHandles = new HashSet<IntPtr>(current.Select(w => w.Handle));

        // 저장된 규칙 즉시 적용
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

            // 새로 생긴 창 감지
            foreach (var win in current)
            {
                if (!_knownHandles.Contains(win.Handle))
                {
                    System.Diagnostics.Debug.WriteLine($"[Monitor] 새 창: {win.Title} ({win.ProcessName})");

                    // 규칙 자동 적용 시도 (MVP1)
                    // 규칙 없으면 무시 (MVP2에서 팝업 추가)
                    if (!Router.TryAutoRoute(win))
                    {
                        System.Diagnostics.Debug.WriteLine($"[Monitor] 규칙 없음: {win.ProcessName}");
                    }

                    NewWindowDetected?.Invoke(win);
                }
            }

            // Zone 창 목록 동기화
            Router.SyncZoneWindows();

            _knownHandles = currentHandles;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Monitor] 오류: {ex.Message}");
        }
    }
}
