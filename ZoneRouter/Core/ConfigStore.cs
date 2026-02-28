using System.IO;
using System.Text.Json;
using System.Windows;

namespace ZoneRouter.Core;

public class ZoneDefinition
{
    public int ZoneId { get; set; }
    public string DisplayName { get; set; } = "";
    public List<string> ProcessNames { get; set; } = new();
}

public class AppConfig
{
    // 다중 선 (세로선 x좌표 목록, 가로선 y좌표 목록)
    public List<double> VerticalLines { get; set; } = new();
    public List<double> HorizontalLines { get; set; } = new();

    // 구버전 호환용 (마이그레이션에 사용)
    public double SplitX { get; set; } = 0;
    public double SplitY { get; set; } = 0;

    public double OverlayOpacity { get; set; } = 0.55;
    public List<ZoneDefinition> Zones { get; set; } = new();
}

public static class ConfigStore
{
    private static readonly string ConfigPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "ZoneRouter", "config.json");

    public static AppConfig Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            else
            {
                Current = new AppConfig();
            }

            // 구버전 SplitX/Y → 새 형식으로 마이그레이션
            if (Current.VerticalLines.Count == 0 && Current.SplitX > 0)
                Current.VerticalLines.Add(Current.SplitX);
            if (Current.HorizontalLines.Count == 0 && Current.SplitY > 0)
                Current.HorizontalLines.Add(Current.SplitY);

            EnsureZones();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Config] Load 오류: {ex.Message}");
            Current = new AppConfig();
            EnsureZones();
        }
    }

    /// <summary>
    /// 현재 선 개수에 맞게 Zone 목록 보장
    /// </summary>
    public static void EnsureZones()
    {
        int cols = Current.VerticalLines.Count + 1;
        int rows = Current.HorizontalLines.Count + 1;
        int total = cols * rows;

        string[] defaultNames = {
            "Zone 1 (좌상)", "Zone 2 (우상)", "Zone 3 (좌하)", "Zone 4 (우하)",
            "Zone 5", "Zone 6", "Zone 7", "Zone 8", "Zone 9"
        };

        var existingIds = Current.Zones.Select(z => z.ZoneId).ToHashSet();
        for (int i = 1; i <= total; i++)
        {
            if (!existingIds.Contains(i))
            {
                string name = i <= defaultNames.Length ? defaultNames[i - 1] : $"Zone {i}";
                Current.Zones.Add(new ZoneDefinition { ZoneId = i, DisplayName = name });
            }
        }
        Current.Zones = Current.Zones.OrderBy(z => z.ZoneId).ToList();
    }

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Config] Save 오류: {ex.Message}");
        }
    }

    public static int? FindZoneForProcess(string processName)
    {
        var zone = Current.Zones.FirstOrDefault(z =>
            z.ProcessNames.Any(p => p.Equals(processName, StringComparison.OrdinalIgnoreCase)));
        return zone?.ZoneId;
    }

    public static void AssignProcessToZone(string processName, int zoneId)
    {
        foreach (var z in Current.Zones)
            z.ProcessNames.RemoveAll(p => p.Equals(processName, StringComparison.OrdinalIgnoreCase));

        var target = Current.Zones.FirstOrDefault(z => z.ZoneId == zoneId);
        if (target != null && !target.ProcessNames.Contains(processName, StringComparer.OrdinalIgnoreCase))
            target.ProcessNames.Add(processName);

        Save();
    }

    public static void SaveLines(List<double> vLines, List<double> hLines)
    {
        Current.VerticalLines = vLines;
        Current.HorizontalLines = hLines;
        EnsureZones();
        Save();
    }
}
