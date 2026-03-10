using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsTaskDoubleClickHandlerTests
{
    [Fact]
    public void TaskListDoubleClickHandler_UsesActionResolverAndCanReopenFloatingWindow()
    {
        var projectRoot = FindProjectRoot();
        var programPath = Path.Combine(projectRoot, "Tomato.WindowsGui", "Program.cs");
        var source = File.ReadAllText(programPath);
        var methodBody = ExtractMethodBody(source, "private void OnTaskListMouseDoubleClick(object? sender, MouseEventArgs e)");

        Assert.Contains("ResolveDoubleClickAction", methodBody);
        Assert.Contains("ShowFloatingFocusWindow();", methodBody);
    }

    private static string FindProjectRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "Tomato.xcodeproj", "project.pbxproj")))
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
