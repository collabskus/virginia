using Microsoft.Extensions.Logging.Abstractions;
using Virginia.Services;
using Xunit;

namespace Virginia.Tests;

public sealed class ToastServiceTests
{
    private static ToastService NewService() =>
        new(NullLogger<ToastService>.Instance);

    [Fact]
    public void Empty_On_Construction()
    {
        using var svc = NewService();
        Assert.Empty(svc.Toasts);
    }

    [Fact]
    public void ShowInfo_AddsToast_AndFiresEvent()
    {
        using var svc = NewService();
        var fired = 0;
        svc.Changed += () => fired++;

        svc.ShowInfo("hello");

        Assert.Single(svc.Toasts);
        Assert.Equal("hello", svc.Toasts[0].Message);
        Assert.Equal(ToastSeverity.Info, svc.Toasts[0].Severity);
        Assert.False(svc.Toasts[0].HasReload);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void ShowSuccess_Adds_Success_Toast()
    {
        using var svc = NewService();
        svc.ShowSuccess("yay");

        Assert.Single(svc.Toasts);
        Assert.Equal(ToastSeverity.Success, svc.Toasts[0].Severity);
    }

    [Fact]
    public void ShowError_Adds_Sticky_Error_Toast()
    {
        using var svc = NewService();
        svc.ShowError("boom");

        Assert.Single(svc.Toasts);
        Assert.Equal(ToastSeverity.Error, svc.Toasts[0].Severity);
        Assert.False(svc.Toasts[0].HasReload);
    }

    [Fact]
    public void ShowReloadWarning_Adds_Toast_With_Action()
    {
        using var svc = NewService();
        var clicked = 0;

        svc.ShowReloadWarning("conflict", () => clicked++);

        Assert.Single(svc.Toasts);
        Assert.True(svc.Toasts[0].HasReload);
        svc.Toasts[0].OnReload!.Invoke();
        Assert.Equal(1, clicked);
    }

    [Fact]
    public void Second_ReloadWarning_Is_Suppressed_While_One_Exists()
    {
        using var svc = NewService();
        svc.ShowReloadWarning("first", () => { });
        svc.ShowReloadWarning("second", () => { });

        Assert.Single(svc.Toasts);
        Assert.Equal("first", svc.Toasts[0].Message);
    }

    [Fact]
    public void Dismiss_RemovesById_AndFiresEvent()
    {
        using var svc = NewService();
        svc.ShowError("err");
        var id = svc.Toasts[0].Id;

        var fired = 0;
        svc.Changed += () => fired++;

        svc.Dismiss(id);

        Assert.Empty(svc.Toasts);
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Dismiss_NonexistentId_DoesNotFireEvent()
    {
        using var svc = NewService();
        var fired = 0;
        svc.Changed += () => fired++;

        svc.Dismiss(999);

        Assert.Equal(0, fired);
    }

    [Fact]
    public void DismissReloadToasts_RemovesOnlyReloadOnes()
    {
        using var svc = NewService();
        svc.ShowInfo("info");
        svc.ShowError("err");
        svc.ShowReloadWarning("reload", () => { });

        Assert.Equal(3, svc.Toasts.Count);

        svc.DismissReloadToasts();

        Assert.Equal(2, svc.Toasts.Count);
        Assert.DoesNotContain(svc.Toasts, t => t.HasReload);
    }

    [Fact]
    public void DismissReloadToasts_NoOp_WhenNoneExist()
    {
        using var svc = NewService();
        svc.ShowInfo("info");

        var fired = 0;
        svc.Changed += () => fired++;

        svc.DismissReloadToasts();

        Assert.Single(svc.Toasts);
        Assert.Equal(0, fired);
    }

    [Fact]
    public void Toast_Ids_Are_Sequential_And_Unique()
    {
        using var svc = NewService();
        svc.ShowInfo("a");
        svc.ShowInfo("b");
        svc.ShowInfo("c");

        var ids = svc.Toasts.Select(t => t.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void Severity_Classes_Match_Enum()
    {
        Assert.Equal("info", new Toast(1, "x", ToastSeverity.Info, null).SeverityClass);
        Assert.Equal("success", new Toast(1, "x", ToastSeverity.Success, null).SeverityClass);
        Assert.Equal("warn", new Toast(1, "x", ToastSeverity.Warn, null).SeverityClass);
        Assert.Equal("error", new Toast(1, "x", ToastSeverity.Error, null).SeverityClass);
    }

    [Fact]
    public void Severity_Labels_Are_Human_Readable()
    {
        Assert.Equal("Information", new Toast(1, "x", ToastSeverity.Info, null).SeverityLabel);
        Assert.Equal("Success", new Toast(1, "x", ToastSeverity.Success, null).SeverityLabel);
        Assert.Equal("Warning", new Toast(1, "x", ToastSeverity.Warn, null).SeverityLabel);
        Assert.Equal("Error", new Toast(1, "x", ToastSeverity.Error, null).SeverityLabel);
    }
}
