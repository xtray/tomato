using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsMainFormFocusRestoreTests
{
    [Fact]
    public void RestoreMainWindow_RefocusesTaskListAfterReturningFromFloatingWindow()
    {
        var projectRoot = FindProjectRoot();
        var programPath = Path.Combine(projectRoot, "Tomato.WindowsGui", "Program.cs");
        var source = File.ReadAllText(programPath);
        var methodBody = ExtractMethodBody(source, "private void RestoreMainWindow()");

        Assert.Contains(
            "_taskList.Focus();",
            methodBody
        );
        Assert.Contains(
            "Invalidate(true);",
            methodBody
        );
        Assert.Contains(
            "Update();",
            methodBody
        );
    }

    [Fact]
    public void OnTaskListMouseDoubleClick_ReopensFloatingWindowForCurrentSessionTask()
    {
        var projectRoot = FindProjectRoot();
        var programPath = Path.Combine(projectRoot, "Tomato.WindowsGui", "Program.cs");
        var source = File.ReadAllText(programPath);
        var methodBody = ExtractMethodBody(source, "private void OnTaskListMouseDoubleClick(object? sender, MouseEventArgs e)");

        Assert.Contains(
            "WindowsTaskDoubleClickActionResolver.Resolve",
            methodBody
        );
        Assert.Contains(
            "ShowFloatingFocusWindow();",
            methodBody
        );
    }

    [Fact]
    public void OnTaskListMouseDoubleClick_ResumesPausedSessionTaskThroughFocusHandler()
    {
        var projectRoot = FindProjectRoot();
        var programPath = Path.Combine(projectRoot, "Tomato.WindowsGui", "Program.cs");
        var source = File.ReadAllText(programPath);
        var methodBody = ExtractMethodBody(source, "private void OnTaskListMouseDoubleClick(object? sender, MouseEventArgs e)");

        Assert.Contains(
            "WindowsTaskDoubleClickAction.ResumeFloatingFocus",
            methodBody
        );
        Assert.Contains(
            "StartOrResumeFocus();",
            methodBody
        );
    }

    [Fact]
    public void BuildTimerCard_CentersRingInsideDedicatedLayoutHost()
    {
        var projectRoot = FindProjectRoot();
        var programPath = Path.Combine(projectRoot, "Tomato.WindowsGui", "Program.cs");
        var source = File.ReadAllText(programPath);
        var methodBody = ExtractMethodBody(source, "private Control BuildTimerCard()");

        Assert.Contains(
            "var ringLayout = new TableLayoutPanel",
            methodBody
        );
        Assert.Contains(
            "layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));",
            methodBody
        );
        Assert.Contains(
            "var titleStack = new TableLayoutPanel",
            methodBody
        );
        Assert.Contains(
            "_taskTitleLabel.AutoEllipsis = true;",
            methodBody
        );
        Assert.Contains(
            "_taskTitleLabel.AutoSize = false;",
            methodBody
        );
        Assert.Contains(
            "ringLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));",
            methodBody
        );
        Assert.Contains(
            "ringLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));",
            methodBody
        );
        Assert.Contains(
            "ringLayout.Controls.Add(_ringControl, 1, 1);",
            methodBody
        );
    }

    private static string FindProjectRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "Tomato.sln")) ||
                File.Exists(Path.Combine(directory, "Tomato.xcodeproj", "project.pbxproj")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory)!;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }

    private static string ExtractMethodBody(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(signatureIndex >= 0, $"{signature} should exist in Program.cs.");

        var bodyStart = source.IndexOf('{', signatureIndex);
        Assert.True(bodyStart >= 0, $"{signature} should contain a method body.");

        var depth = 0;
        for (var i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(bodyStart, i - bodyStart + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not extract body for {signature}.");
    }
}
