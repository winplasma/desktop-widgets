// WinPlasma.Widgets — Views/BaseWidgetWindow.cs
// Base class for all widget windows. Handles:
// - Transparent, always-behind-other-windows (HWND_BOTTOM) positioning
// - Drag to move (PointerPressed/Moved/Released)
// - Position persistence to config
// - Resize handles (future)
// - Right-click context menu: Remove Widget

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinPlasma.Widgets.Models;

namespace WinPlasma.Widgets.Views;

/// <summary>
/// Base class for all widget windows.
/// Subclasses set their content and call base.Configure() in their constructor.
/// </summary>
public abstract class BaseWidgetWindow : Window
{
    protected readonly WidgetConfig Config;
    private readonly Action<WidgetConfig> _onPositionChanged;

    // Drag state
    private bool _isDragging;
    private Windows.Foundation.Point _dragStartPointer;
    private Windows.Graphics.PointInt32 _dragStartWindow;

    // Win32 constants
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private static readonly IntPtr HWND_BOTTOM = new(1); // Behind all windows
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    protected BaseWidgetWindow(WidgetConfig config, Action<WidgetConfig> onPositionChanged)
    {
        Config = config;
        _onPositionChanged = onPositionChanged;
    }

    /// <summary>Call this at the end of subclass constructor after setting Content.</summary>
    protected void Configure(int defaultWidth, int defaultHeight)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        // Borderless, non-resizable
        var presenter = OverlappedPresenter.Create();
        presenter.IsResizable = false;
        presenter.IsMinimizable = false;
        presenter.IsMaximizable = false;
        presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
        AppWindow.SetPresenter(presenter);

        // Position and size
        var w = Config.Width > 0 ? Config.Width : defaultWidth;
        var h = Config.Height > 0 ? Config.Height : defaultHeight;
        AppWindow.MoveAndResize(new RectInt32(Config.X, Config.Y, w, h));

        // Extended window styles: tool window (no taskbar), non-activating
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE,
            exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        // Send to bottom of Z-order (sits on desktop, behind all other windows)
        SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

        // Full transparency via DWM
        var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        SystemBackdrop = new Microsoft.UI.Xaml.Media.TransparentBackdrop();
    }

    // ── Drag to move ──────────────────────────────────────────────────────────

    /// <summary>Call this from the widget's root element PointerPressed handler.</summary>
    protected void OnDragStart(PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _dragStartPointer = e.GetCurrentPoint(null).Position;
        _dragStartWindow = AppWindow.Position;
        (e.OriginalSource as UIElement)?.CapturePointer(e.Pointer);
    }

    protected void OnDragMove(PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        var current = e.GetCurrentPoint(null).Position;
        var dx = (int)(current.X - _dragStartPointer.X);
        var dy = (int)(current.Y - _dragStartPointer.Y);
        AppWindow.Move(new PointInt32(_dragStartWindow.X + dx, _dragStartWindow.Y + dy));
    }

    protected void OnDragEnd(PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        (e.OriginalSource as UIElement)?.ReleasePointerCapture(e.Pointer);

        // Persist new position
        Config.X = AppWindow.Position.X;
        Config.Y = AppWindow.Position.Y;
        _onPositionChanged(Config);
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("user32.dll")] protected static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] protected static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] protected static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [StructLayout(LayoutKind.Sequential)]
    protected struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }

    [DllImport("dwmapi.dll")] protected static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);
}
