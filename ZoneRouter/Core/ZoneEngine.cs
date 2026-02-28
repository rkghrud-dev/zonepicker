using System.Windows;

namespace ZoneRouter.Core;

public static class ZoneEngine
{
    /// <summary>
    /// 교차점 + 단독선으로 Zone 계산
    /// </summary>
    public static Dictionary<int, Rect> CalcZones(
        List<SplitPoint> points, List<double> extraV, List<double> extraH, Rect screen)
    {
        var vLines = points.Select(p => p.X).Concat(extraV).ToList();
        var hLines = points.Select(p => p.Y).Concat(extraH).ToList();

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
}
