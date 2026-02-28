using System.Windows;

namespace ZoneRouter.Core;

public static class Router
{
    public static Dictionary<int, Rect> CurrentZones { get; private set; } = new();
    public static Dictionary<int, List<IntPtr>> ZoneWindows { get; private set; } = new();

    public static void UpdateZones(Dictionary<int, Rect> zones)
    {
        CurrentZones = zones;
        // 새 Zone에 빈 목록 보장
        foreach (var id in zones.Keys)
            if (!ZoneWindows.ContainsKey(id)) ZoneWindows[id] = new();
    }

    public static bool TryAutoRoute(WindowInfo win)
    {
        int? zoneId = ConfigStore.FindZoneForProcess(win.ProcessName);
        if (zoneId == null) return false;
        AssignWindowToZone(win.Handle, zoneId.Value);
        return true;
    }

    public static void AssignWindowToZone(IntPtr hWnd, int zoneId)
    {
        if (!CurrentZones.TryGetValue(zoneId, out var rect)) return;
        WindowManager.MoveToZone(hWnd, rect);

        foreach (var list in ZoneWindows.Values) list.Remove(hWnd);
        if (!ZoneWindows.ContainsKey(zoneId)) ZoneWindows[zoneId] = new();
        if (!ZoneWindows[zoneId].Contains(hWnd)) ZoneWindows[zoneId].Add(hWnd);
    }

    public static void ApplySavedRules()
    {
        if (CurrentZones.Count == 0) return;
        foreach (var win in WindowManager.GetVisibleWindows())
        {
            try { TryAutoRoute(win); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Router] 오류: {ex.Message}"); }
        }
    }

    public static void SyncZoneWindows()
    {
        var visible = WindowManager.GetVisibleWindows();
        var valid = new HashSet<IntPtr>(visible.Select(w => w.Handle));
        foreach (var list in ZoneWindows.Values) list.RemoveAll(h => !valid.Contains(h));
    }
}
