namespace ZoneRouter.Core;

public enum ZoneViewMode
{
    Desktop,  // 오버레이 숨김
    Zone,     // 존 모드 (흰색 투명 오버레이 + 존 바)
    Edit      // 설정 모드 (바탕화면 완전 가림 + 핸들 편집)
}

public static class AppState
{
    public static ZoneViewMode Mode { get; private set; } = ZoneViewMode.Zone;
    public static event Action<ZoneViewMode>? ModeChanged;

    public static void SetMode(ZoneViewMode mode)
    {
        Mode = mode;
        ModeChanged?.Invoke(mode);
    }

    public static void ToggleDesktopZone()
        => SetMode(Mode == ZoneViewMode.Desktop ? ZoneViewMode.Zone : ZoneViewMode.Desktop);

    // 하위 호환용
    public static bool IsZoneMode => Mode != ZoneViewMode.Desktop;
}
