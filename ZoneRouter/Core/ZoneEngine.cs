using System.Windows;

namespace ZoneRouter.Core;

public static class ZoneEngine
{
    /// <summary>
    /// 세로선(x) 목록 + 가로선(y) 목록으로 Zone 계산
    /// ZoneId: 좌상단부터 행 우선 순서 (1, 2, 3... )
    /// </summary>
    public static Dictionary<int, Rect> CalcZones(List<double> vLines, List<double> hLines, Rect screen)
    {
        var xs = new List<double> { screen.Left };
        xs.AddRange(vLines.Select(x => Math.Clamp(x, screen.Left + 20, screen.Right - 20)).OrderBy(x => x));
        xs.Add(screen.Right);

        var ys = new List<double> { screen.Top };
        ys.AddRange(hLines.Select(y => Math.Clamp(y, screen.Top + 20, screen.Bottom - 20)).OrderBy(y => y));
        ys.Add(screen.Bottom);

        var zones = new Dictionary<int, Rect>();
        int id = 1;
        for (int row = 0; row < ys.Count - 1; row++)
            for (int col = 0; col < xs.Count - 1; col++)
                zones[id++] = new Rect(xs[col], ys[row], xs[col + 1] - xs[col], ys[row + 1] - ys[row]);

        return zones;
    }

    public static Point DefaultSplitPoint(Rect screen)
        => new(screen.Left + screen.Width / 2, screen.Top + screen.Height / 2);
}
