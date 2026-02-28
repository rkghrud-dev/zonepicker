using System.Windows;
using System.Windows.Controls;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

public partial class ZonePickerWindow : Window
{
    public event Action<int>? ZoneSelected;

    public ZonePickerWindow(WindowInfo win)
    {
        InitializeComponent();

        // 앱 정보 표시
        AppInfoLabel.Text = $"📦 {win.ProcessName}  |  {(win.Title.Length > 40 ? win.Title[..40] + "…" : win.Title)}";

        // Zone 버튼 이름 + 현재 배정 앱 표시
        var buttons = new[] { BtnZone1, BtnZone2, BtnZone3, BtnZone4 };
        foreach (var btn in buttons)
        {
            int zoneId = int.Parse(btn.Tag!.ToString()!);
            var def = ConfigStore.Current.Zones.FirstOrDefault(z => z.ZoneId == zoneId);
            if (def == null) continue;

            string apps = def.ProcessNames.Count > 0
                ? "\n(" + string.Join(", ", def.ProcessNames) + ")"
                : "\n(비어있음)";

            btn.Content = def.DisplayName + apps;
        }
    }

    private void ZoneBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int zoneId))
        {
            ZoneSelected?.Invoke(zoneId);
            Close();
        }
    }
}
