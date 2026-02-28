using System.Windows;
using System.Windows.Controls;

namespace ZoneRouter.UI;

public partial class ZonePickerWindow : Window
{
    public event Action<int>? ZoneSelected;

    public ZonePickerWindow()
    {
        InitializeComponent();

        // Zone 이름 버튼에 반영
        var names = Core.ConfigStore.Current.ZoneNames;
        if (names.TryGetValue(1, out var n1)) BtnZone1.Content = n1;
        if (names.TryGetValue(2, out var n2)) BtnZone2.Content = n2;
        if (names.TryGetValue(3, out var n3)) BtnZone3.Content = n3;
        if (names.TryGetValue(4, out var n4)) BtnZone4.Content = n4;
    }

    private void ZoneBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int zoneId))
        {
            ZoneSelected?.Invoke(zoneId);
            DialogResult = true;
            Close();
        }
    }
}
