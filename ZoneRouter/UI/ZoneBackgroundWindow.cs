using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using ZoneRouter.Core;

namespace ZoneRouter.UI;

/// <summary>
/// 배경화면만 가리는 창 (앱 창들 아래에 위치)
/// z-order: 데스크톱 위, 앱 창들 아래
/// </summary>
public class ZoneBackgroundWindow : Window
{
    #region Win32

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOMOVE     = 0x0002;
    private const uint SWP_NOSIZE     = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int  GWL_EXSTYLE    = -20;
    private const int  WS_EX_TRANSPARENT = 0x00000020;
    private const int  WS_EX_TOOLWINDOW  = 0x00000080;

    #endregion

    public ZoneBackgroundWindow()
    {
        WindowStyle   = WindowStyle.None;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost       = false;
        Background    = new SolidColorBrush(Color.FromRgb(10, 10, 18));
        Width         = SystemParameters.PrimaryScreenWidth;
        Height        = SystemParameters.PrimaryScreenHeight;
        Left = 0; Top = 0;

        Visibility = Visibility.Collapsed; // 처음엔 숨김

        SourceInitialized += (_, _) => ApplyWin32Style();
        Activated         += (_, _) => SendToBottom(); // 혹시 활성화되면 다시 내림

        AppState.ModeChanged += OnModeChanged;
    }

    private void ApplyWin32Style()
    {
        var hwnd = new WindowInteropHelper(this).Handle;

        // 클릭 통과 (마우스 이벤트를 앱 창에 전달)
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);

        SendToBottom();
    }

    private void SendToBottom()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        // 앱 창들 아래, 데스크톱 위에 위치
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private void OnModeChanged(ZoneViewMode mode)
    {
        Visibility = mode == ZoneViewMode.Zone
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (mode == ZoneViewMode.Zone)
            SendToBottom();
    }

    protected override void OnClosed(EventArgs e)
    {
        AppState.ModeChanged -= OnModeChanged;
        base.OnClosed(e);
    }
}
