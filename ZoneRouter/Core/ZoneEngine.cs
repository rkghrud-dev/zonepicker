using System.Windows;

namespace ZoneRouter.Core;

/// <summary>
/// 점(x, y) 하나로 화면을 4개의 Zone(Rect)으로 분할하는 엔진
/// </summary>
public static class ZoneEngine
{
    /// <summary>
    /// 분할점 기준으로 4개 Zone 계산
    /// ZoneId: 1=좌상, 2=우상, 3=좌하, 4=우하
    /// </summary>
    public static Dictionary<int, Rect> CalcZones(Point splitPoint, Rect screen)
    {
        double x = Math.Clamp(splitPoint.X, screen.Left + 20, screen.Right - 20);
        double y = Math.Clamp(splitPoint.Y, screen.Top + 20, screen.Bottom - 20);

        return new Dictionary<int, Rect>
        {
            [1] = new Rect(screen.Left,  screen.Top, x - screen.Left,       y - screen.Top),
            [2] = new Rect(x,            screen.Top, screen.Right - x,       y - screen.Top),
            [3] = new Rect(screen.Left,  y,          x - screen.Left,        screen.Bottom - y),
            [4] = new Rect(x,            y,          screen.Right - x,       screen.Bottom - y),
        };
    }

    /// <summary>
    /// 화면 중앙을 기본 분할점으로 반환
    /// </summary>
    public static Point DefaultSplitPoint(Rect screen)
        => new(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
}
