using System.Text.Json;

namespace Tomato.WindowsCore;

public sealed class WindowsTaskState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; init; } = string.Empty;
    public int CompletedPomodoros { get; init; }
}

public sealed class WindowsAppState
{
    public const int DefaultWorkMinutes = 25;
    public const int DefaultShortBreakMinutes = 5;
    public const int DefaultLongBreakMinutes = 15;
    public const int DefaultFloatingWindowWidth = 340;
    public const int DefaultFloatingWindowHeight = 408;
    public const int MinFloatingWindowWidth = 320;
    public const int MinFloatingWindowHeight = 374;
    public const int MaxFloatingWindowWidth = 1280;
    public const int MaxFloatingWindowHeight = 900;

    public WindowsThemeMode ThemeMode { get; init; } = WindowsThemeMode.WarmVivid;
    public int WorkMinutes { get; init; } = DefaultWorkMinutes;
    public int ShortBreakMinutes { get; init; } = DefaultShortBreakMinutes;
    public int LongBreakMinutes { get; init; } = DefaultLongBreakMinutes;
    public int FloatingWindowWidth { get; init; } = DefaultFloatingWindowWidth;
    public int FloatingWindowHeight { get; init; } = DefaultFloatingWindowHeight;
    public List<WindowsTaskState> Tasks { get; init; } = [];

    public static WindowsAppState Default => new();

    public WindowsAppState Normalized()
    {
        return new WindowsAppState
        {
            ThemeMode = Enum.IsDefined(ThemeMode) ? ThemeMode : WindowsThemeMode.WarmVivid,
            WorkMinutes = ClampDuration(WorkMinutes, min: 1, max: 60, fallback: DefaultWorkMinutes),
            ShortBreakMinutes = ClampDuration(ShortBreakMinutes, min: 1, max: 30, fallback: DefaultShortBreakMinutes),
            LongBreakMinutes = ClampDuration(LongBreakMinutes, min: 1, max: 60, fallback: DefaultLongBreakMinutes),
            FloatingWindowWidth = ClampDimension(
                FloatingWindowWidth,
                min: MinFloatingWindowWidth,
                max: MaxFloatingWindowWidth,
                fallback: DefaultFloatingWindowWidth
            ),
            FloatingWindowHeight = ClampDimension(
                FloatingWindowHeight,
                min: MinFloatingWindowHeight,
                max: MaxFloatingWindowHeight,
                fallback: DefaultFloatingWindowHeight
            ),
            Tasks = (Tasks ?? [])
                .Where(task => task is not null)
                .Select(task => new WindowsTaskState
                {
                    Id = task.Id == Guid.Empty ? Guid.NewGuid() : task.Id,
                    Title = (task.Title ?? string.Empty).Trim(),
                    CompletedPomodoros = Math.Max(0, task.CompletedPomodoros)
                })
                .Where(task => !string.IsNullOrEmpty(task.Title))
                .ToList()
        };
    }

    private static int ClampDuration(int value, int min, int max, int fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }

    private static int ClampDimension(int value, int min, int max, int fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }
}

public sealed class WindowsAppStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _path;

    public WindowsAppStateStore(string path)
    {
        _path = path;
    }

    public static string DefaultPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Tomato", "state.json");
    }

    public WindowsAppState Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return WindowsAppState.Default;
            }

            var json = File.ReadAllText(_path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return WindowsAppState.Default;
            }

            var state = JsonSerializer.Deserialize<WindowsAppState>(json, SerializerOptions);
            return (state ?? WindowsAppState.Default).Normalized();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return WindowsAppState.Default;
        }
    }

    public void Save(WindowsAppState state)
    {
        var normalized = (state ?? WindowsAppState.Default).Normalized();
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(normalized, SerializerOptions);
        File.WriteAllText(_path, json);
    }
}
