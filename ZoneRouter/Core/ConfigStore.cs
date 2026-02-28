using System.IO;
using System.Text.Json;
using System.Windows;

namespace ZoneRouter.Core;

public class AppConfig
{
    public double SplitX { get; set; } = 0;
    public double SplitY { get; set; } = 0;
    public List<RoutingRule> Rules { get; set; } = new();
    public Dictionary<int, string> ZoneNames { get; set; } = new()
    {
        [1] = "Zone 1", [2] = "Zone 2", [3] = "Zone 3", [4] = "Zone 4"
    };
}

public class RoutingRule
{
    public string ProcessName { get; set; } = "";
    public string TitleContains { get; set; } = "";
    public int ZoneId { get; set; }
}

/// <summary>
/// JSON 설정 저장/로드
/// </summary>
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
            if (!File.Exists(ConfigPath))
            {
                Current = new AppConfig();
                return;
            }
            var json = File.ReadAllText(ConfigPath);
            Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Config] Load 오류: {ex.Message}");
            Current = new AppConfig();
        }
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

    public static void AddOrUpdateRule(string processName, int zoneId, string titleContains = "")
    {
        var existing = Current.Rules.FirstOrDefault(r =>
            r.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.ZoneId = zoneId;
            existing.TitleContains = titleContains;
        }
        else
        {
            Current.Rules.Add(new RoutingRule
            {
                ProcessName = processName,
                TitleContains = titleContains,
                ZoneId = zoneId
            });
        }
        Save();
    }

    public static int? FindZoneForProcess(string processName, string windowTitle)
    {
        var rule = Current.Rules.FirstOrDefault(r =>
            r.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(r.TitleContains) ||
             windowTitle.Contains(r.TitleContains, StringComparison.OrdinalIgnoreCase)));

        return rule?.ZoneId;
    }

    public static void SaveSplitPoint(Point p)
    {
        Current.SplitX = p.X;
        Current.SplitY = p.Y;
        Save();
    }
}
