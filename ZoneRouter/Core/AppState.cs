namespace ZoneRouter.Core;

/// <summary>
/// 앱 전체 상태 관리 (존 모드 / 바탕화면 모드)
/// </summary>
public static class AppState
{
    public static bool IsZoneMode { get; private set; } = true;

    public static event Action<bool>? ModeChanged;

    public static void SetMode(bool zoneMode)
    {
        IsZoneMode = zoneMode;
        ModeChanged?.Invoke(zoneMode);
    }

    public static void Toggle() => SetMode(!IsZoneMode);
}
