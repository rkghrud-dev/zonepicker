using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace ZoneRouter.Core;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; } = "";
    public string ProcessName { get; set; } = "";
    public int ProcessId { get; set; }
}

/// <summary>
/// Win32 API 기반 창 이동/리사이즈/전면화
/// </summary>
public static class WindowManager
{
    #region Win32 P/Invoke

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern long GetWindowLong(IntPtr hWnd, int nIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private const int SW_RESTORE = 9;
    private const int SW_SHOWNOACTIVATE = 4;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int GWL_STYLE = -16;
    private const long WS_VISIBLE = 0x10000000L;
    private const long WS_CHILD = 0x40000000L;
    private const long WS_EX_TOOLWINDOW = 0x00000080L;
    private const int GWL_EXSTYLE = -20;

    #endregion

    /// <summary>
    /// 화면에 보이는 일반 창 목록 반환
    /// </summary>
    public static List<WindowInfo> GetVisibleWindows()
    {
        var list = new List<WindowInfo>();

        EnumWindows((hWnd, _) =>
        {
            try
            {
                if (!IsWindowVisible(hWnd)) return true;

                int len = GetWindowTextLength(hWnd);
                if (len == 0) return true;

                long style = GetWindowLong(hWnd, GWL_STYLE);
                if ((style & WS_CHILD) != 0) return true;

                long exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString();
                if (string.IsNullOrWhiteSpace(title)) return true;

                GetWindowThreadProcessId(hWnd, out uint pid);
                string procName = "";
                try { procName = Process.GetProcessById((int)pid).ProcessName; }
                catch { }

                // ZoneRouter 자신은 제외
                if (procName.Equals("ZoneRouter", StringComparison.OrdinalIgnoreCase)) return true;

                list.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = procName,
                    ProcessId = (int)pid
                });
            }
            catch { }
            return true;
        }, IntPtr.Zero);

        return list;
    }

    /// <summary>
    /// 창을 지정 Zone(Rect)으로 이동 및 리사이즈 (픽셀 좌표)
    /// </summary>
    public static bool MoveToZone(IntPtr hWnd, Rect zoneRect)
    {
        try
        {
            ShowWindow(hWnd, SW_RESTORE);
            bool result = SetWindowPos(hWnd, IntPtr.Zero,
                (int)zoneRect.Left, (int)zoneRect.Top,
                (int)zoneRect.Width, (int)zoneRect.Height,
                SWP_NOZORDER | SWP_SHOWWINDOW);

            if (!result)
                Debug.WriteLine($"[WM] SetWindowPos 실패: {hWnd}");
            return result;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WM] MoveToZone 오류: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 창 전면화 (포커스)
    /// </summary>
    public static void BringToFront(IntPtr hWnd)
    {
        try
        {
            ShowWindow(hWnd, SW_RESTORE);
            SetForegroundWindow(hWnd);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WM] BringToFront 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 현재 포그라운드 창 반환
    /// </summary>
    public static IntPtr GetForeground() => GetForegroundWindow();

    /// <summary>
    /// DIP(WPF 논리 픽셀) → 물리 픽셀 변환
    /// WPF 좌표와 Win32 픽셀 좌표가 DPI 배율에서 다를 수 있음
    /// </summary>
    public static Rect DipToPixel(Rect dip, double dpiScaleX, double dpiScaleY)
    {
        return new Rect(
            dip.X * dpiScaleX,
            dip.Y * dpiScaleY,
            dip.Width * dpiScaleX,
            dip.Height * dpiScaleY);
    }
}
