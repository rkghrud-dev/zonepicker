using System.Windows;

namespace ZoneRouter.Core;

/// <summary>
/// 규칙 기반으로 창을 어느 Zone에 배치할지 결정하고 실제 이동 수행
/// </summary>
public static class Router
{
    // ZoneId → Zone Rect (픽셀 좌표)
    public static Dictionary<int, Rect> CurrentZones { get; private set; } = new();

    // ZoneId → 해당 Zone에 속한 창 핸들 목록
    public static Dictionary<int, List<IntPtr>> ZoneWindows { get; private set; } = new()
    {
        [1] = new(), [2] = new(), [3] = new(), [4] = new()
    };

    public static void UpdateZones(Dictionary<int, Rect> zones)
    {
        CurrentZones = zones;
    }

    /// <summary>
    /// 규칙에 따라 창을 자동 배치. 규칙 없으면 false 반환
    /// </summary>
    public static bool TryAutoRoute(WindowInfo win)
    {
        int? zoneId = ConfigStore.FindZoneForProcess(win.ProcessName, win.Title);
        if (zoneId == null) return false;

        AssignWindowToZone(win.Handle, zoneId.Value);
        return true;
    }

    /// <summary>
    /// 창을 특정 Zone에 배치 (이동 + 목록 등록)
    /// </summary>
    public static void AssignWindowToZone(IntPtr hWnd, int zoneId)
    {
        if (!CurrentZones.TryGetValue(zoneId, out var rect)) return;

        WindowManager.MoveToZone(hWnd, rect);

        // 기존 Zone 목록에서 제거
        foreach (var list in ZoneWindows.Values)
            list.Remove(hWnd);

        // 새 Zone에 추가
        if (!ZoneWindows.ContainsKey(zoneId))
            ZoneWindows[zoneId] = new List<IntPtr>();

        if (!ZoneWindows[zoneId].Contains(hWnd))
            ZoneWindows[zoneId].Add(hWnd);
    }

    /// <summary>
    /// 프로그램 시작 시 기존 창들에 저장된 규칙 적용
    /// </summary>
    public static void ApplySavedRules()
    {
        if (CurrentZones.Count == 0) return;

        var windows = WindowManager.GetVisibleWindows();
        foreach (var win in windows)
        {
            try { TryAutoRoute(win); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Router] ApplySavedRules 오류: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Zone에 속한 창 목록을 현재 실제 창 목록과 동기화 (닫힌 창 제거)
    /// </summary>
    public static void SyncZoneWindows()
    {
        var visible = WindowManager.GetVisibleWindows();
        var validHandles = new HashSet<IntPtr>(visible.Select(w => w.Handle));

        foreach (var list in ZoneWindows.Values)
            list.RemoveAll(h => !validHandles.Contains(h));
    }
}
