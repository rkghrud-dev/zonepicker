using System.IO;
using System.Text.Json;

namespace ZoneRouter.Core;

public class SplitPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class ZoneDefinition
{
    public int ZoneId { get; set; }
    public string DisplayName { get; set; } = "";
    public List<string> ProcessNames { get; set; } = new();
}

public class AppConfig
{
    // 교차점 목록 (각 점 = 세로선 x + 가로선 y)
    public List<SplitPoint> SplitPoints { get; set; } = new();
    // 단독 세로선 (x만)
    public List<double> ExtraVLines { get; set; } = new();
    // 단독 가로선 (y만)
    public List<double> ExtraHLines { get; set; } = new();

    public double OverlayOpacity { get; set; } = 0.12;
    public List<ZoneDefinition> Zones { get; set; } = new();

    // 마이그레이션용
    public double SplitX { get; set; } = 0;
    public double SplitY { get; set; } = 0;
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

            // 구버전 마이그레이션: SplitX/Y → SplitPoints
            if (Current.SplitPoints.Count == 0 && Current.SplitX > 0 && Current.SplitY > 0)
                Current.SplitPoints.Add(new SplitPoint { X = Current.SplitX, Y = Current.SplitY });

            EnsureZones();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Config] Load 오류: {ex.Message}");
            Current = new AppConfig();
            EnsureZones();
        }
    }

    public static void EnsureZones()
    {
        int vCount = Current.SplitPoints.Count + Current.ExtraVLines.Count;
        int hCount = Current.SplitPoints.Count + Current.ExtraHLines.Count;
        int total = (vCount + 1) * (hCount + 1);
        total = Math.Max(total, 1);

        string[] defaultNames = {
            "Zone 1", "Zone 2", "Zone 3", "Zone 4",
            "Zone 5", "Zone 6", "Zone 7", "Zone 8", "Zone 9"
        };

        var existingIds = Current.Zones.Select(z => z.ZoneId).ToHashSet();
        for (int i = 1; i <= total; i++)
            if (!existingIds.Contains(i))
                Current.Zones.Add(new ZoneDefinition
                {
                    ZoneId = i,
                    DisplayName = i <= defaultNames.Length ? defaultNames[i - 1] : $"Zone {i}"
                });

        Current.Zones = Current.Zones.OrderBy(z => z.ZoneId).ToList();
    }

    public static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Config] Save 오류: {ex.Message}"); }
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

    public static void SaveLayout()
    {
        EnsureZones();
        Save();
    }
}
