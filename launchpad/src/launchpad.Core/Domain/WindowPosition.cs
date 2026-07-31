using Launchpad.Core.Models;

namespace Launchpad.Core.Domain;

/// <summary>
/// Pure window-position sanitizer. Guards against minimized-offscreen coordinates
/// (-32000 from minimized windows) and degenerate sizes persisted on close, which
/// would otherwise restore the window outside the visible desktop.
/// </summary>
public static class WindowPosition
{
    public const uint DefaultWidth = 800;
    public const uint DefaultHeight = 600;

    private const int MinWidth = 200;
    private const int MinHeight = 100;
    private const int DefaultX = 100;
    private const int DefaultY = 100;

    public static WindowState ClampToVisible(
        WindowState state,
        int virtualLeft,
        int virtualTop,
        int virtualWidth,
        int virtualHeight,
        int minVisible = 100)
    {
        if (state.Width < MinWidth || state.Height < MinHeight)
        {
            return Default();
        }

        var overlapX = Math.Min(state.X + (int)state.Width, virtualLeft + virtualWidth) - Math.Max(state.X, virtualLeft);
        var overlapY = Math.Min(state.Y + (int)state.Height, virtualTop + virtualHeight) - Math.Max(state.Y, virtualTop);
        if (overlapX < minVisible || overlapY < minVisible)
        {
            return Default();
        }

        return state;
    }

    private static WindowState Default() => new()
    {
        X = DefaultX,
        Y = DefaultY,
        Width = DefaultWidth,
        Height = DefaultHeight,
    };
}
