using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class WindowPositionTests
{
    // 1920x1080 主屏（无负坐标多屏）
    private const int ScreenLeft = 0;
    private const int ScreenTop = 0;
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    [Fact]
    public void ClampToVisible_KeepsVisibleState()
    {
        var state = new WindowState { X = 100, Y = 100, Width = 900, Height = 700 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ClampToVisible_ResetsMinimizedOffscreenCoordinates()
    {
        // -32000 是 Windows 最小化窗口的经典坐标（当前 settings.json 中的坏状态）
        var state = new WindowState { X = -32000, Y = -32000, Width = 237, Height = 39 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight);

        Assert.Equal(100, result.X);
        Assert.Equal(100, result.Y);
        Assert.Equal(WindowPosition.DefaultWidth, result.Width);
        Assert.Equal(WindowPosition.DefaultHeight, result.Height);
    }

    [Fact]
    public void ClampToVisible_ResetsFullyOffscreenRight()
    {
        var state = new WindowState { X = 2000, Y = 100, Width = 900, Height = 700 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight);

        Assert.Equal(WindowPosition.DefaultWidth, result.Width);
        Assert.Equal(100, result.X);
    }

    [Fact]
    public void ClampToVisible_ResetsTinySize()
    {
        var state = new WindowState { X = 50, Y = 50, Width = 10, Height = 10 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight);

        Assert.Equal(WindowPosition.DefaultWidth, result.Width);
        Assert.Equal(WindowPosition.DefaultHeight, result.Height);
    }

    [Fact]
    public void ClampToVisible_KeepsWindowSpanningTwoScreens()
    {
        // 双屏：虚拟屏 0-3840，窗口跨主屏/副屏边界（重叠 800px）
        var state = new WindowState { X = 1900, Y = 100, Width = 800, Height = 600 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, 3840, ScreenHeight);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ClampToVisible_NegativeVirtualOrigin_KeepsWindowOnLeftScreen()
    {
        // 主屏为右屏（虚拟原点为负）：窗口位于左屏（负坐标区）
        const int left = -1920;
        const int top = 0;
        const int width = 3840;
        const int height = 1080;
        var state = new WindowState { X = -1800, Y = 100, Width = 800, Height = 600 };

        var result = WindowPosition.ClampToVisible(state, left, top, width, height);

        Assert.Equal(state, result);
    }

    [Fact]
    public void ClampToVisible_ZeroSizeFallsBackToDefaults()
    {
        var state = new WindowState { X = 100, Y = 100, Width = 0, Height = 0 };

        var result = WindowPosition.ClampToVisible(state, ScreenLeft, ScreenTop, ScreenWidth, ScreenHeight);

        Assert.Equal(WindowPosition.DefaultWidth, result.Width);
        Assert.Equal(WindowPosition.DefaultHeight, result.Height);
    }
}
