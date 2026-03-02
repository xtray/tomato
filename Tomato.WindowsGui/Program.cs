using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using Tomato.WindowsCore;

namespace Tomato.WindowsGui;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal static class AppIconProvider
{
    private static Icon? _cached;

    public static Icon? GetAppIcon()
    {
        _cached ??= Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        return _cached;
    }
}

internal sealed class MainForm : Form
{
    private readonly PomodoroEngine _engine = new();
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 1000 };
    private FloatingFocusForm? _floatingForm;

    private readonly List<WinTask> _tasks = [];
    private Guid? _sessionTaskId;

    private int _workMinutes = 25;
    private int _shortBreakMinutes = 5;
    private int _longBreakMinutes = 15;

    private readonly GradientBackgroundPanel _background = new() { Dock = DockStyle.Fill };

    private readonly ListBox _taskList = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
        IntegralHeight = false,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 76,
        BackColor = Color.White
    };

    private readonly TextBox _newTaskInput = new()
    {
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
    };

    private readonly Label _taskCountBadge = CreateBadgeLabel("0");
    private readonly Label _taskTitleLabel = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 22F, FontStyle.Bold),
        Text = "Select a task",
        ForeColor = UiPalette.TextPrimary
    };

    private readonly Label _taskStatLabel = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 10F, FontStyle.Regular),
        ForeColor = UiPalette.TextSecondary,
        Text = "Pick a task and start focus"
    };

    private readonly Label _phaseLabel = CreatePhaseLabel("Work");

    private readonly TimerRingControl _ringControl = new()
    {
        Dock = DockStyle.Fill,
        MinimumSize = new Size(280, 280)
    };

    private readonly Button _focusButton = CreateActionButton("Focus", true);
    private readonly Button _stopButton = CreateActionButton("Stop", false);
    private readonly Button _resetButton = CreateActionButton("Reset", false);
    private readonly Button _floatButton = CreateActionButton("Float", false);

    public MainForm()
    {
        Text = "Tomato for Windows";
        Icon = AppIconProvider.GetAppIcon();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 600);
        Size = new Size(1080, 680);
        BackColor = UiPalette.Window;

        _taskList.DrawItem += OnDrawTaskItem;
        _taskList.SelectedIndexChanged += (_, _) => RefreshView();

        _newTaskInput.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }
            AddTask();
            e.SuppressKeyPress = true;
        };

        _focusButton.Click += (_, _) => HandleFocusButton();
        _stopButton.Click += (_, _) => StopSession();
        _resetButton.Click += (_, _) => ResetTimer();
        _floatButton.Click += (_, _) => ShowFloatingFocusWindow();
        _floatButton.Enabled = false;

        _tickTimer.Tick += OnTick;

        _background.Controls.Add(BuildLayout());
        Controls.Add(_background);

        FormClosing += (_, _) =>
        {
            if (_floatingForm is { IsDisposed: false })
            {
                _floatingForm.Close();
            }
        };

        _tasks.Add(new WinTask("My First Task"));
        RefreshTaskList();
        _taskList.SelectedIndex = 0;
        RefreshView();
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 16, 18, 16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 470F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var left = BuildTaskCard();
        left.Margin = new Padding(0, 0, 10, 0);
        var right = BuildTimerCard();
        right.Margin = new Padding(10, 0, 0, 0);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        return root;
    }

    private Control BuildTaskCard()
    {
        var card = new GlassCardPanel { Dock = DockStyle.Fill };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(14)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 4,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var taskTitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Text = "Tasks",
            ForeColor = UiPalette.TextPrimary,
            Anchor = AnchorStyles.Left
        };

        var settingsButton = CreateMiniButton("Settings");
        settingsButton.Click += (_, _) => OpenSettings();
        var themeButton = CreateMiniButton("Theme");
        themeButton.Click += (_, _) => _background.IsCoolTheme = !_background.IsCoolTheme;

        var deleteButton = CreateMiniButton("Delete");
        deleteButton.Click += (_, _) => DeleteSelectedTask();

        header.Controls.Add(taskTitle, 0, 0);
        header.Controls.Add(_taskCountBadge, 1, 0);
        header.Controls.Add(themeButton, 2, 0);
        header.Controls.Add(settingsButton, 3, 0);

        var listWrap = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(0, 6, 0, 6),
            Margin = new Padding(0, 10, 0, 10)
        };
        listWrap.Controls.Add(_taskList);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 3,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var inputHost = new Panel
        {
            Dock = DockStyle.Fill,
            MinimumSize = new Size(300, 48),
            Height = 48,
            BackColor = Color.FromArgb(244, 247, 251),
            Padding = new Padding(12, 8, 12, 8),
            Margin = new Padding(0, 0, 10, 0)
        };
        inputHost.Controls.Add(_newTaskInput);
        _newTaskInput.Dock = DockStyle.Fill;
        RoundControl(inputHost, 10);

        var addButton = CreateRoundIconButton("+", true);
        addButton.Click += (_, _) => AddTask();

        footer.Controls.Add(inputHost, 0, 0);
        footer.Controls.Add(addButton, 1, 0);
        footer.Controls.Add(deleteButton, 2, 0);

        layout.Controls.Add(header, 0, 0);
        layout.Controls.Add(listWrap, 0, 1);
        layout.Controls.Add(footer, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildTimerCard()
    {
        var card = new GlassCardPanel { Dock = DockStyle.Fill };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Padding = new Padding(20)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var titleStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        titleStack.Controls.Add(_taskTitleLabel);
        titleStack.Controls.Add(_taskStatLabel);

        var phaseRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        phaseRow.Controls.Add(_phaseLabel);

        var ringHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 6, 0, 12),
            BackColor = Color.Transparent
        };
        ringHost.Controls.Add(_ringControl);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        actionRow.Controls.Add(_focusButton);
        actionRow.Controls.Add(_stopButton);
        actionRow.Controls.Add(_resetButton);
        actionRow.Controls.Add(_floatButton);

        layout.Controls.Add(titleStack, 0, 0);
        layout.Controls.Add(phaseRow, 0, 1);
        layout.Controls.Add(ringHost, 0, 2);
        layout.Controls.Add(actionRow, 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private static Button CreateMiniButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 84,
            Height = 36,
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(242, 246, 250),
            ForeColor = UiPalette.TextSecondary,
            Margin = new Padding(6, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        RoundControl(button, 10);
        return button;
    }

    private static Button CreateRoundIconButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 40,
            Height = 40,
            AutoSize = false,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? UiPalette.Primary : Color.FromArgb(242, 246, 250),
            ForeColor = primary ? Color.White : UiPalette.TextSecondary,
            Margin = new Padding(0, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderSize = 0;
        RoundControl(button, 12);
        return button;
    }

    private static Button CreateActionButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Width = 118,
            Height = 54,
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? UiPalette.Primary : Color.FromArgb(240, 244, 248),
            ForeColor = primary ? Color.White : UiPalette.TextPrimary,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(10, 6, 10, 6),
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderSize = 0;
        RoundControl(button, 14);
        return button;
    }

    private void HandleFocusButton()
    {
        if (_engine.Snapshot.IsRunning)
        {
            ShowFloatingFocusWindow();
            return;
        }

        StartFocus();
    }

    private static Label CreateBadgeLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Text = text,
            ForeColor = UiPalette.Primary,
            BackColor = Color.FromArgb(249, 235, 232),
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(8, 0, 0, 0)
        };
    }

    private static Label CreatePhaseLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Text = text,
            ForeColor = UiPalette.Primary,
            BackColor = Color.FromArgb(249, 235, 232),
            Padding = new Padding(10, 6, 10, 6)
        };
    }

    private static void RoundControl(Control control, int radius)
    {
        // Avoid Region clipping on high DPI: WinForms may clip button text when using custom regions.
        _ = control;
        _ = radius;
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void AddTask()
    {
        var title = _newTaskInput.Text.Trim();
        if (string.IsNullOrEmpty(title))
        {
            return;
        }

        _tasks.Add(new WinTask(title));
        _newTaskInput.Text = string.Empty;
        RefreshTaskList();
        _taskList.SelectedIndex = _tasks.Count - 1;
        RefreshView();
    }

    private void DeleteSelectedTask()
    {
        if (_taskList.SelectedIndex < 0 || _taskList.SelectedIndex >= _tasks.Count)
        {
            return;
        }

        var selected = _tasks[_taskList.SelectedIndex];
        if (_sessionTaskId == selected.Id)
        {
            StopSession();
        }

        _tasks.RemoveAt(_taskList.SelectedIndex);
        RefreshTaskList();
        if (_tasks.Count > 0)
        {
            _taskList.SelectedIndex = Math.Min(_taskList.SelectedIndex, _tasks.Count - 1);
        }
        RefreshView();
    }

    private void RefreshTaskList()
    {
        var selectedId = GetSelectedTask()?.Id;
        _taskList.BeginUpdate();
        _taskList.Items.Clear();
        foreach (var task in _tasks)
        {
            _taskList.Items.Add(task);
        }
        _taskList.EndUpdate();

        if (selectedId.HasValue)
        {
            var index = _tasks.FindIndex(t => t.Id == selectedId.Value);
            if (index >= 0)
            {
                _taskList.SelectedIndex = index;
            }
        }
    }

    private void StartFocus()
    {
        var selected = GetSelectedTask();
        if (selected is null)
        {
            MessageBox.Show(this, "Please select a task first.", "Tomato", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _sessionTaskId = selected.Id;
        _engine.StartFocusSession(_workMinutes, _shortBreakMinutes, _longBreakMinutes);
        _tickTimer.Start();
        ShowFloatingFocusWindow();
        RefreshView();
    }

    private void StopSession()
    {
        _tickTimer.Stop();
        _engine.StopSession();
        _sessionTaskId = null;
        if (_floatingForm is { Visible: true })
        {
            RestoreMainWindow();
        }
        RefreshView();
    }

    private void ResetTimer()
    {
        _tickTimer.Stop();
        _engine.ResetToWorkReady();
        _sessionTaskId = null;
        if (_floatingForm is { Visible: true })
        {
            RestoreMainWindow();
        }
        RefreshView();
    }

    private void ShowFloatingFocusWindow()
    {
        if (_floatingForm is null || _floatingForm.IsDisposed)
        {
            _floatingForm = new FloatingFocusForm(
                onBackToMain: RestoreMainWindow,
                onStop: StopSession,
                onReset: ResetTimer
            );
        }

        UpdateFloatingWindow();
        PositionFloatingWindow(_floatingForm);

        Hide();
        _floatingForm.Show();
        _floatingForm.BringToFront();
    }

    private void RestoreMainWindow()
    {
        if (_floatingForm is { IsDisposed: false })
        {
            _floatingForm.Hide();
        }

        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private static void PositionFloatingWindow(Form floatingForm)
    {
        var screen = Screen.FromPoint(Cursor.Position);
        var area = screen.WorkingArea;
        var x = area.Right - floatingForm.Width - 20;
        var y = area.Top + 20;
        floatingForm.Location = new Point(x, y);
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_workMinutes, _shortBreakMinutes, _longBreakMinutes);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _workMinutes = dialog.WorkMinutes;
        _shortBreakMinutes = dialog.ShortBreakMinutes;
        _longBreakMinutes = dialog.LongBreakMinutes;

        if (!_engine.Snapshot.IsRunning)
        {
            _engine.ResetToWorkReady();
        }
        RefreshView();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var before = _engine.Snapshot;
        _engine.Tick();
        var after = _engine.Snapshot;

        if (before.Phase == PomodoroPhase.Work &&
            (after.Phase == PomodoroPhase.ShortBreak || after.Phase == PomodoroPhase.LongBreak))
        {
            IncrementCompletedPomodoroForSessionTask();
            RefreshTaskList();
        }

        if (before.IsRunning &&
            !after.IsRunning &&
            (before.Phase == PomodoroPhase.ShortBreak || before.Phase == PomodoroPhase.LongBreak) &&
            after.Phase == PomodoroPhase.Work)
        {
            _tickTimer.Stop();
            _sessionTaskId = null;
            var owner = _floatingForm is { Visible: true } ? (IWin32Window)_floatingForm : this;
            MessageBox.Show(owner, "Session completed.", "Tomato", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        RefreshView();
    }

    private void IncrementCompletedPomodoroForSessionTask()
    {
        if (!_sessionTaskId.HasValue)
        {
            return;
        }

        var task = _tasks.FirstOrDefault(t => t.Id == _sessionTaskId.Value);
        if (task is null)
        {
            return;
        }
        task.CompletedPomodoros += 1;
    }

    private WinTask? GetSelectedTask()
    {
        if (_taskList.SelectedIndex < 0 || _taskList.SelectedIndex >= _tasks.Count)
        {
            return null;
        }
        return _tasks[_taskList.SelectedIndex];
    }

    private WinTask? GetDisplayTask()
    {
        if (_sessionTaskId.HasValue)
        {
            return _tasks.FirstOrDefault(t => t.Id == _sessionTaskId.Value);
        }
        return GetSelectedTask();
    }

    private void RefreshView()
    {
        _taskCountBadge.Text = _tasks.Count.ToString();

        var snapshot = _engine.Snapshot;
        var selectedTask = GetSelectedTask();
        var displayTask = GetDisplayTask();

        _taskTitleLabel.Text = displayTask?.Title ?? "Select a task";
        _taskStatLabel.Text = displayTask is null
            ? "Pick a task and start focus"
            : $"{displayTask.CompletedPomodoros}x completed";

        var isIdle = snapshot.Phase == PomodoroPhase.Idle;
        var isRunning = snapshot.IsRunning;

        var phaseText = snapshot.Phase switch
        {
            PomodoroPhase.Work => "Work",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => "Ready"
        };

        var phaseColor = GetPhaseColor(snapshot.Phase);

        _phaseLabel.Text = phaseText;
        _phaseLabel.ForeColor = phaseColor;
        _phaseLabel.BackColor = Color.FromArgb(249, 235, 232);

        var totalSeconds = isIdle ? _workMinutes * 60 : Math.Max(1, snapshot.PhaseTotalSeconds);
        var remainingSeconds = isIdle ? _workMinutes * 60 : Math.Max(0, snapshot.RemainingSeconds);
        var ratio = totalSeconds <= 0 ? 1F : (float)remainingSeconds / totalSeconds;

        _ringControl.RingColor = phaseColor;
        _ringControl.TimeText = FormatTime(remainingSeconds);
        _ringControl.RemainingRatio = ratio;

        _focusButton.Enabled = selectedTask is not null || isRunning;
        _stopButton.Enabled = isRunning;
        _resetButton.Enabled = !isIdle || displayTask is not null;
        _floatButton.Enabled = isRunning;

        UpdateFloatingWindow();
    }

    private static string FormatTime(int totalSeconds)
    {
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private void UpdateFloatingWindow()
    {
        if (_floatingForm is null || _floatingForm.IsDisposed)
        {
            return;
        }

        var snapshot = _engine.Snapshot;
        var displayTask = GetDisplayTask();
        _floatingForm.UpdateState(
            snapshot: snapshot,
            taskTitle: displayTask?.Title ?? "Focus",
            phaseColor: GetPhaseColor(snapshot.Phase),
            fallbackWorkSeconds: _workMinutes * 60
        );
    }

    private static Color GetPhaseColor(PomodoroPhase phase)
    {
        return phase switch
        {
            PomodoroPhase.Work => UiPalette.Primary,
            PomodoroPhase.ShortBreak => UiPalette.ShortBreak,
            PomodoroPhase.LongBreak => UiPalette.LongBreak,
            _ => UiPalette.TextSecondary
        };
    }

    private void OnDrawTaskItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _tasks.Count)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var task = _tasks[e.Index];
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        using var rowPath = CreateRoundedRectPath(
            new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, e.Bounds.Width - 8, e.Bounds.Height - 8),
            10
        );

        using var rowBrush = new SolidBrush(selected ? Color.FromArgb(249, 236, 233) : Color.White);
        e.Graphics.FillPath(rowBrush, rowPath);

        using var titleBrush = new SolidBrush(UiPalette.TextPrimary);
        using var metaBrush = new SolidBrush(UiPalette.TextSecondary);
        using var titleFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        using var metaFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);

        var textX = e.Bounds.X + 16;
        var titleRect = new Rectangle(textX, e.Bounds.Y + 12, e.Bounds.Width - 50, 26);
        var metaRect = new Rectangle(textX, e.Bounds.Y + 40, e.Bounds.Width - 50, 22);
        TextRenderer.DrawText(
            e.Graphics,
            task.Title,
            titleFont,
            titleRect,
            titleBrush.Color,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
        );
        var metaText = task.CompletedPomodoros > 0 ? $"{task.CompletedPomodoros}x completed" : "No pomodoros yet";
        TextRenderer.DrawText(
            e.Graphics,
            metaText,
            metaFont,
            metaRect,
            metaBrush.Color,
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
        );

        if (selected)
        {
            using var dotBrush = new SolidBrush(UiPalette.Primary);
            e.Graphics.FillEllipse(dotBrush, e.Bounds.Right - 20, e.Bounds.Y + (e.Bounds.Height / 2F) - 4, 8, 8);
        }
    }
}

internal sealed class FloatingFocusForm : Form
{
    private readonly Action _onBackToMain;
    private readonly Action _onStop;
    private readonly Action _onReset;
    private bool _dragging;
    private Point _dragStartCursor;
    private Point _dragStartForm;

    private readonly Label _phaseLabel = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        Text = "Work",
        ForeColor = UiPalette.Primary,
        BackColor = Color.FromArgb(249, 235, 232),
        Padding = new Padding(8, 5, 8, 5),
        Margin = new Padding(0, 0, 0, 6)
    };

    private readonly Label _taskLabel = new()
    {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 30,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = UiPalette.TextPrimary,
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(0, 0, 0, 2)
    };

    private readonly TimerRingControl _ringControl = new()
    {
        Dock = DockStyle.Fill,
        MinimumSize = new Size(230, 230),
        TimeFontSize = 17F
    };

    public FloatingFocusForm(Action onBackToMain, Action onStop, Action onReset)
    {
        _onBackToMain = onBackToMain;
        _onStop = onStop;
        _onReset = onReset;

        Text = "Tomato Focus";
        Icon = AppIconProvider.GetAppIcon();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Width = 380;
        Height = 460;
        MinimumSize = new Size(340, 420);
        BackColor = Color.White;

        var card = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var topRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        topRow.Controls.Add(_phaseLabel);

        var backButton = CreateFloatingButton("Back");
        backButton.Click += (_, _) => _onBackToMain();
        var stopButton = CreateFloatingButton("Stop");
        stopButton.Click += (_, _) => _onStop();
        var resetButton = CreateFloatingButton("Reset");
        resetButton.Click += (_, _) => _onReset();

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent
        };
        buttonRow.Controls.Add(stopButton);
        buttonRow.Controls.Add(resetButton);
        buttonRow.Controls.Add(backButton);

        var ringHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };
        ringHost.Controls.Add(_ringControl);

        layout.Controls.Add(topRow, 0, 0);
        layout.Controls.Add(_taskLabel, 0, 1);
        layout.Controls.Add(ringHost, 0, 2);
        layout.Controls.Add(buttonRow, 0, 3);

        card.Controls.Add(layout);
        Controls.Add(card);

        SizeChanged += (_, _) => ApplyRoundedWindowRegion();
        ApplyRoundedWindowRegion();
        EnableDrag(this);
    }

    public void UpdateState(PomodoroSnapshot snapshot, string taskTitle, Color phaseColor, int fallbackWorkSeconds)
    {
        var isIdle = snapshot.Phase == PomodoroPhase.Idle;
        var totalSeconds = isIdle ? fallbackWorkSeconds : Math.Max(1, snapshot.PhaseTotalSeconds);
        var remainingSeconds = isIdle ? fallbackWorkSeconds : Math.Max(0, snapshot.RemainingSeconds);
        var ratio = totalSeconds <= 0 ? 1F : (float)remainingSeconds / totalSeconds;

        _phaseLabel.Text = snapshot.Phase switch
        {
            PomodoroPhase.Work => "Work",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => "Ready"
        };
        _phaseLabel.ForeColor = phaseColor;

        _taskLabel.Text = taskTitle;

        _ringControl.RingColor = phaseColor;
        _ringControl.RemainingRatio = ratio;
        _ringControl.TimeText = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
    }

    private static Button CreateFloatingButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 82,
            Height = 36,
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = UiPalette.TextPrimary,
            Margin = new Padding(0, 0, 6, 0)
        };
    }

    private void ApplyRoundedWindowRegion()
    {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
        {
            return;
        }

        using var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
        var radius = 24;
        var diameter = radius * 2;
        var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        Region?.Dispose();
        Region = new Region(path);
    }

    private void EnableDrag(Control root)
    {
        if (root is not Button)
        {
            root.MouseDown += OnDragMouseDown;
            root.MouseMove += OnDragMouseMove;
            root.MouseUp += OnDragMouseUp;
        }

        foreach (Control child in root.Controls)
        {
            EnableDrag(child);
        }
    }

    private void OnDragMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        _dragStartCursor = Cursor.Position;
        _dragStartForm = Location;
    }

    private void OnDragMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var cursor = Cursor.Position;
        var dx = cursor.X - _dragStartCursor.X;
        var dy = cursor.Y - _dragStartCursor.Y;
        Location = new Point(_dragStartForm.X + dx, _dragStartForm.Y + dy);
    }

    private void OnDragMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = false;
        }
    }
}

internal sealed class SettingsForm : Form
{
    private readonly NumericUpDown _workInput;
    private readonly NumericUpDown _shortBreakInput;
    private readonly NumericUpDown _longBreakInput;

    public int WorkMinutes => (int)_workInput.Value;
    public int ShortBreakMinutes => (int)_shortBreakInput.Value;
    public int LongBreakMinutes => (int)_longBreakInput.Value;

    public SettingsForm(int workMinutes, int shortBreakMinutes, int longBreakMinutes)
    {
        Text = "Settings";
        Icon = AppIconProvider.GetAppIcon();
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 250);
        BackColor = UiPalette.Window;

        _workInput = CreateDurationInput(workMinutes, 1, 60);
        _shortBreakInput = CreateDurationInput(shortBreakMinutes, 1, 30);
        _longBreakInput = CreateDurationInput(longBreakMinutes, 1, 60);

        var card = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12),
            Padding = new Padding(16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateSettingsLabel("Focus Duration"), 0, 0);
        layout.Controls.Add(_workInput, 1, 0);
        layout.Controls.Add(CreateSettingsLabel("Short Break"), 0, 1);
        layout.Controls.Add(_shortBreakInput, 1, 1);
        layout.Controls.Add(CreateSettingsLabel("Long Break"), 0, 2);
        layout.Controls.Add(_longBreakInput, 1, 2);

        var doneButton = CreateDialogPrimaryButton("Done");
        doneButton.Width = 96;
        doneButton.DialogResult = DialogResult.OK;

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent
        };
        buttonRow.Controls.Add(doneButton);

        layout.Controls.Add(buttonRow, 0, 3);
        layout.SetColumnSpan(buttonRow, 2);

        card.Controls.Add(layout);
        Controls.Add(card);
        AcceptButton = doneButton;
    }

    private static Label CreateSettingsLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = UiPalette.TextPrimary,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 0, 10)
        };
    }

    private static Button CreateDialogPrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 100,
            Height = 40,
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiPalette.Primary,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderSize = 0;
        RoundControl(button, 12);
        return button;
    }

    private static void RoundControl(Control control, int radius)
    {
        _ = control;
        _ = radius;
    }

    private static NumericUpDown CreateDurationInput(int value, int min, int max)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Clamp(value, min, max),
            Width = 120,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 0, 8)
        };
    }
}

internal sealed class GradientBackgroundPanel : Panel
{
    private bool _isCoolTheme;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsCoolTheme
    {
        get => _isCoolTheme;
        set
        {
            if (_isCoolTheme == value)
            {
                return;
            }
            _isCoolTheme = value;
            Invalidate();
        }
    }

    public GradientBackgroundPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        var rect = ClientRectangle;
        var startColor = _isCoolTheme ? Color.FromArgb(242, 247, 255) : Color.FromArgb(255, 248, 244);
        var endColor = _isCoolTheme ? Color.FromArgb(232, 240, 252) : Color.FromArgb(239, 244, 250);
        using var gradient = new LinearGradientBrush(
            rect,
            startColor,
            endColor,
            LinearGradientMode.ForwardDiagonal
        );
        e.Graphics.FillRectangle(gradient, rect);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var orb2 = new SolidBrush(_isCoolTheme
            ? Color.FromArgb(58, 127, 176, 255)
            : Color.FromArgb(64, 110, 165, 255));
        e.Graphics.FillEllipse(orb2, -90, rect.Height - 220, 260, 220);
    }
}

internal sealed class GlassCardPanel : Panel
{
    public GlassCardPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var clearBrush = new SolidBrush(GetEffectiveBackgroundColor());
        e.Graphics.FillRectangle(clearBrush, ClientRectangle);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        using var path = CreateRoundedRectPath(rect, 20);

        using var fill = new LinearGradientBrush(
            rect,
            Color.FromArgb(223, 255, 255, 255),
            Color.FromArgb(198, 255, 255, 255),
            LinearGradientMode.ForwardDiagonal
        );
        e.Graphics.FillPath(fill, path);

        using var border = new Pen(Color.FromArgb(175, 255, 255, 255), 1.2F);
        e.Graphics.DrawPath(border, path);
    }

    private Color GetEffectiveBackgroundColor()
    {
        var parent = Parent;
        while (parent is not null)
        {
            if (parent.BackColor.A > 0)
            {
                return parent.BackColor;
            }
            parent = parent.Parent;
        }

        return UiPalette.Window;
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        var arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class TimerRingControl : Control
{
    private float _remainingRatio = 1F;
    private Color _ringColor = UiPalette.Primary;
    private string _timeText = "25:00";
    private float _timeFontSize = 34F;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float RemainingRatio
    {
        get => _remainingRatio;
        set
        {
            _remainingRatio = Math.Clamp(value, 0F, 1F);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public Color RingColor
    {
        get => _ringColor;
        set
        {
            _ringColor = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string TimeText
    {
        get => _timeText;
        set
        {
            _timeText = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float TimeFontSize
    {
        get => _timeFontSize;
        set
        {
            _timeFontSize = Math.Max(12F, value);
            Invalidate();
        }
    }

    public TimerRingControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true
        );
        BackColor = Color.White;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var size = Math.Min(ClientSize.Width, ClientSize.Height) - 36;
        if (size <= 0)
        {
            return;
        }

        var x = (ClientSize.Width - size) / 2F;
        var y = (ClientSize.Height - size) / 2F;
        var outer = new RectangleF(x, y, size, size);

        using var bgBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
        e.Graphics.FillEllipse(bgBrush, outer);

        var ringRect = new RectangleF(x + 22, y + 22, size - 44, size - 44);
        using var trackPen = new Pen(Color.FromArgb(178, 216, 225, 236), 10F);
        e.Graphics.DrawEllipse(trackPen, ringRect);

        using var glowPen = new Pen(Color.FromArgb(70, _ringColor), 14F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var ringPen = new Pen(_ringColor, 10F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var sweep = 360F * _remainingRatio;
        e.Graphics.DrawArc(glowPen, ringRect, -90F, sweep);
        e.Graphics.DrawArc(ringPen, ringRect, -90F, sweep);

        var textRect = new RectangleF(ringRect.X + 20, ringRect.Y + 20, ringRect.Width - 40, ringRect.Height - 40);
        var fittedFontSize = CalculateFittedFontSize(e.Graphics, _timeText, textRect.Size, _timeFontSize);
        using var timeFont = new Font("Consolas", fittedFontSize, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            _timeText,
            timeFont,
            Rectangle.Round(textRect),
            UiPalette.TextPrimary,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding
        );
    }

    private static float CalculateFittedFontSize(Graphics graphics, string text, SizeF bounds, float preferredSize)
    {
        var size = preferredSize;
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.NoWrap
        };

        while (size > 12F)
        {
            using var font = new Font("Consolas", size, FontStyle.Bold);
            var measured = graphics.MeasureString(text, font, bounds, format);
            if (measured.Width <= bounds.Width * 0.98F && measured.Height <= bounds.Height * 0.9F)
            {
                return size;
            }
            size -= 1F;
        }

        return 12F;
    }
}

internal sealed class WinTask
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; }
    public int CompletedPomodoros { get; set; }

    public WinTask(string title)
    {
        Title = title;
    }
}

internal static class UiPalette
{
    public static readonly Color Window = Color.FromArgb(245, 248, 251);
    public static readonly Color Primary = Color.FromArgb(224, 71, 56);
    public static readonly Color ShortBreak = Color.FromArgb(47, 167, 127);
    public static readonly Color LongBreak = Color.FromArgb(59, 122, 219);
    public static readonly Color TextPrimary = Color.FromArgb(36, 44, 53);
    public static readonly Color TextSecondary = Color.FromArgb(93, 104, 117);
}
