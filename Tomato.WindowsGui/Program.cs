using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
    private const int MainWindowCornerRadius = 24;

    private readonly PomodoroEngine _engine = new();
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 1000 };
    private FloatingFocusForm? _floatingForm;

    private readonly List<WinTask> _tasks = [];
    private Guid? _sessionTaskId;

    private int _workMinutes = 25;
    private int _shortBreakMinutes = 5;
    private int _longBreakMinutes = 15;

    private readonly GradientBackgroundPanel _background = new() { Dock = DockStyle.Fill };
    private readonly Panel _titleBar = new()
    {
        Dock = DockStyle.Fill,
        Height = 40,
        BackColor = Color.Transparent,
        Padding = new Padding(0, 4, 10, 4),
        Margin = Padding.Empty
    };
    private readonly FlowLayoutPanel _windowControlHost = new()
    {
        Dock = DockStyle.Right,
        AutoSize = true,
        WrapContents = false,
        FlowDirection = FlowDirection.LeftToRight,
        BackColor = Color.Transparent,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly Button _minimizeWindowButton = CreateWindowControlButton("—");
    private readonly Button _closeWindowButton = CreateWindowControlButton("×");
    private bool _draggingMainWindow;
    private Point _mainDragStartCursor;
    private Point _mainDragStartForm;

    private readonly BufferedListBox _taskList = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
        IntegralHeight = false,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 76,
        BackColor = Color.White
    };
    private bool _lastTaskListCoolTheme;

    private readonly TextBox _newTaskInput = new()
    {
        BorderStyle = BorderStyle.None,
        Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
    };

    private readonly RoundedTagLabel _taskCountBadge = CreateBadgeLabel("0");
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

    private readonly RoundedTagLabel _phaseLabel = CreatePhaseLabel("Focusing...");

    private readonly TimerRingControl _ringControl = new()
    {
        Dock = DockStyle.Fill,
        MinimumSize = new Size(280, 280)
    };

    private readonly Button _focusButton = CreateActionButton("Focus", true);
    private readonly Button _resetButton = CreateActionButton("Reset", false);
    private readonly Button _floatButton = CreateActionButton("Float", false);

    public MainForm()
    {
        Text = string.Empty;
        Icon = AppIconProvider.GetAppIcon();
        FormBorderStyle = FormBorderStyle.None;
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
        _resetButton.Click += (_, _) => ResetTimer();
        _floatButton.Click += (_, _) => ShowFloatingFocusWindow();
        _floatButton.Enabled = false;
        _minimizeWindowButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
        _closeWindowButton.Click += (_, _) => Close();
        _closeWindowButton.Margin = new Padding(6, 0, 0, 0);

        _tickTimer.Tick += OnTick;

        _background.Controls.Add(BuildLayout());
        Controls.Add(_background);
        SizeChanged += (_, _) => ApplyRoundedWindowRegion();
        ApplyRoundedWindowRegion();

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
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleBar = BuildTitleBar();
        var content = BuildContentLayout();
        content.Margin = Padding.Empty;

        root.Controls.Add(titleBar, 0, 0);
        root.Controls.Add(content, 0, 1);
        return root;
    }

    private Control BuildTitleBar()
    {
        _titleBar.Controls.Clear();
        _windowControlHost.Controls.Clear();
        _windowControlHost.Controls.Add(_minimizeWindowButton);
        _windowControlHost.Controls.Add(_closeWindowButton);
        _titleBar.Controls.Add(_windowControlHost);
        EnableMainWindowDrag(_titleBar);
        return _titleBar;
    }

    private Control BuildContentLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(18, 16, 18, 16)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 510F));
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
        themeButton.Click += (_, _) =>
        {
            _background.IsCoolTheme = !_background.IsCoolTheme;
            RefreshView();
        };

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
        return new GlassActionButton(false)
        {
            UseVividSecondaryAccent = true,
            Text = text,
            Width = 84,
            Height = 36,
            AutoSize = false,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            Margin = new Padding(6, 0, 0, 0)
        };
    }

    private static Button CreateRoundIconButton(string text, bool primary)
    {
        return new GlassActionButton(primary)
        {
            Text = text,
            Width = 40,
            Height = 40,
            AutoSize = false,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            Margin = new Padding(0, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Button CreateActionButton(string text, bool primary)
    {
        return new GlassActionButton(primary)
        {
            UseVividSecondaryAccent = !primary,
            Text = text,
            Width = 124,
            Height = 52,
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(12, 8, 12, 8),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Button CreateWindowControlButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 36,
            Height = 30,
            AutoSize = false,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = UiPalette.TextPrimary,
            Font = new Font("Segoe UI", 11F, FontStyle.Regular),
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter,
            TabStop = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 232, 227);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(238, 219, 213);
        return button;
    }

    private static void ConfigureWindowControlButton(Button button, Color foreground, Color hoverBackground, Color pressedBackground)
    {
        button.ForeColor = foreground;
        button.FlatAppearance.MouseOverBackColor = hoverBackground;
        button.FlatAppearance.MouseDownBackColor = pressedBackground;
    }

    private void HandleFocusButton()
    {
        if (_engine.Snapshot.IsRunning)
        {
            PauseSession();
            return;
        }

        StartOrResumeFocus();
    }

    private static RoundedTagLabel CreateBadgeLabel(string text)
    {
        return new RoundedTagLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Text = text,
            ForeColor = UiPalette.Primary,
            BackColor = Color.FromArgb(249, 235, 232),
            Padding = new Padding(8, 5, 8, 5),
            Margin = new Padding(8, 0, 0, 0),
            CornerRadius = 10,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static RoundedTagLabel CreatePhaseLabel(string text)
    {
        return new RoundedTagLabel
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Text = text,
            ForeColor = UiPalette.Primary,
            BackColor = Color.FromArgb(249, 235, 232),
            Padding = new Padding(10, 6, 10, 6),
            CornerRadius = 10,
            TextAlign = ContentAlignment.MiddleCenter
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

    private void EnableMainWindowDrag(Control root)
    {
        if (root is not Button)
        {
            root.MouseDown += OnMainWindowDragMouseDown;
            root.MouseMove += OnMainWindowDragMouseMove;
            root.MouseUp += OnMainWindowDragMouseUp;
        }

        foreach (Control child in root.Controls)
        {
            EnableMainWindowDrag(child);
        }
    }

    private void OnMainWindowDragMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _draggingMainWindow = true;
        _mainDragStartCursor = Cursor.Position;
        _mainDragStartForm = Location;
    }

    private void OnMainWindowDragMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_draggingMainWindow)
        {
            return;
        }

        var cursor = Cursor.Position;
        var dx = cursor.X - _mainDragStartCursor.X;
        var dy = cursor.Y - _mainDragStartCursor.Y;
        Location = new Point(_mainDragStartForm.X + dx, _mainDragStartForm.Y + dy);
    }

    private void OnMainWindowDragMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _draggingMainWindow = false;
        }
    }

    private void ApplyRoundedWindowRegion()
    {
        if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
        {
            return;
        }

        using var path = new GraphicsPath();
        var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
        var radius = MainWindowCornerRadius;
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
            ResetTimer();
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

    private void StartOrResumeFocus()
    {
        var snapshot = _engine.Snapshot;
        var hasResumableSession = !snapshot.IsRunning &&
                                  snapshot.Phase != PomodoroPhase.Idle &&
                                  _sessionTaskId.HasValue;
        if (hasResumableSession)
        {
            _engine.StartFocusSession(_workMinutes, _shortBreakMinutes, _longBreakMinutes);
            _tickTimer.Start();
            ShowFloatingFocusWindow();
            RefreshView();
            return;
        }

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

    private void PauseSession()
    {
        _tickTimer.Stop();
        _engine.StopSession();
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
                onFocusToggle: HandleFocusButton,
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

        RefreshView();
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

        if (_engine.Snapshot.Phase == PomodoroPhase.Idle)
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
            if (_floatingForm is { Visible: true })
            {
                RestoreMainWindow();
            }
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
        var hasResumableSession = !isRunning && !isIdle && _sessionTaskId.HasValue;

        var phaseText = snapshot.Phase switch
        {
            PomodoroPhase.Work => "Focusing...",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => "Ready"
        };

        var phaseColor = GetPhaseColor(snapshot.Phase);
        var useBusinessTheme = _background.IsCoolTheme;
        var workAccent = GetPhaseColor(PomodoroPhase.Work);

        ApplyWindowChromeTheme(useBusinessTheme);
        ApplyThemeToButtonTree(this, useBusinessTheme);
        if (_floatingForm is { IsDisposed: false })
        {
            _floatingForm.SetBusinessTheme(useBusinessTheme);
        }

        _taskCountBadge.ForeColor = workAccent;
        _taskCountBadge.BackColor = CreateTagBackground(workAccent, useBusinessTheme);

        _phaseLabel.Text = phaseText;
        _phaseLabel.ForeColor = phaseColor;
        _phaseLabel.BackColor = CreateTagBackground(phaseColor, useBusinessTheme);

        var totalSeconds = isIdle ? _workMinutes * 60 : Math.Max(1, snapshot.PhaseTotalSeconds);
        var remainingSeconds = isIdle ? _workMinutes * 60 : Math.Max(0, snapshot.RemainingSeconds);
        var ratio = totalSeconds <= 0 ? 1F : (float)remainingSeconds / totalSeconds;

        _ringControl.RingColor = phaseColor;
        _ringControl.TimeText = FormatTime(remainingSeconds);
        _ringControl.RemainingRatio = ratio;

        _focusButton.Text = ResolveFocusButtonText(snapshot, _sessionTaskId.HasValue);
        _focusButton.Enabled = selectedTask is not null || isRunning || hasResumableSession;
        _resetButton.Enabled = !isIdle || displayTask is not null;
        _floatButton.Enabled = isRunning || hasResumableSession;
        if (_lastTaskListCoolTheme != useBusinessTheme)
        {
            _taskList.Invalidate();
            _lastTaskListCoolTheme = useBusinessTheme;
        }

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
            fallbackWorkSeconds: _workMinutes * 60,
            hasSessionTask: _sessionTaskId.HasValue,
            useBusinessTheme: _background.IsCoolTheme
        );
    }

    private static string ResolveFocusButtonText(PomodoroSnapshot snapshot, bool hasSessionTask)
    {
        if (snapshot.IsRunning)
        {
            return "Pause";
        }

        if (hasSessionTask && snapshot.Phase != PomodoroPhase.Idle)
        {
            return "Run";
        }

        return "Focus";
    }

    private Color GetPhaseColor(PomodoroPhase phase)
    {
        var business = _background.IsCoolTheme;
        return phase switch
        {
            PomodoroPhase.Work => business ? UiPalette.BusinessPrimary : UiPalette.Primary,
            PomodoroPhase.ShortBreak => business ? UiPalette.BusinessAccent : UiPalette.ShortBreak,
            PomodoroPhase.LongBreak => business ? UiPalette.BusinessLongBreak : UiPalette.LongBreak,
            _ => business ? UiPalette.BusinessPrimary : UiPalette.Primary
        };
    }

    private void ApplyWindowChromeTheme(bool useBusinessTheme)
    {
        var titleBarBackground = useBusinessTheme
            ? Color.FromArgb(229, 237, 246)
            : Color.FromArgb(255, 238, 230);
        var buttonForeground = useBusinessTheme
            ? UiPalette.TextPrimary
            : UiPalette.Primary;
        var minHoverBackground = useBusinessTheme
            ? Color.FromArgb(214, 225, 238)
            : Color.FromArgb(252, 226, 219);
        var closeHoverBackground = useBusinessTheme
            ? Color.FromArgb(233, 206, 205)
            : Color.FromArgb(247, 205, 197);
        var pressedBackground = useBusinessTheme
            ? Color.FromArgb(198, 211, 226)
            : Color.FromArgb(239, 191, 181);

        _titleBar.BackColor = titleBarBackground;
        _windowControlHost.BackColor = titleBarBackground;
        ConfigureWindowControlButton(_minimizeWindowButton, buttonForeground, minHoverBackground, pressedBackground);
        ConfigureWindowControlButton(_closeWindowButton, buttonForeground, closeHoverBackground, pressedBackground);
    }

    private static Color CreateTagBackground(Color accent, bool useBusinessTheme)
    {
        static int Mix(int from, int to, float t) => (int)Math.Clamp(MathF.Round(from + (to - from) * t), 0, 255);

        var strength = useBusinessTheme ? 0.12F : 0.18F;
        return Color.FromArgb(
            255,
            Mix(255, accent.R, strength),
            Mix(255, accent.G, strength),
            Mix(255, accent.B, strength)
        );
    }

    private static void ApplyThemeToButtonTree(Control root, bool useBusinessTheme)
    {
        foreach (Control control in root.Controls)
        {
            if (control is GlassActionButton button)
            {
                button.UseBusinessTheme = useBusinessTheme;
            }

            if (control.HasChildren)
            {
                ApplyThemeToButtonTree(control, useBusinessTheme);
            }
        }
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

        var selectedRowColor = _background.IsCoolTheme
            ? Color.FromArgb(236, 242, 248)
            : Color.FromArgb(249, 236, 233);
        using var rowBrush = new SolidBrush(selected ? selectedRowColor : Color.White);
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
            var accent = _background.IsCoolTheme ? UiPalette.BusinessPrimary : UiPalette.Primary;
            using var dotBrush = new SolidBrush(accent);
            e.Graphics.FillEllipse(dotBrush, e.Bounds.Right - 20, e.Bounds.Y + (e.Bounds.Height / 2F) - 4, 8, 8);
        }
    }
}

internal sealed class FloatingFocusForm : Form
{
    private readonly Action _onBackToMain;
    private readonly Action _onFocusToggle;
    private readonly Action _onReset;
    private bool _useBusinessTheme;
    private bool _dragging;
    private Point _dragStartCursor;
    private Point _dragStartForm;

    private readonly RoundedTagLabel _phaseLabel = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        Text = "Focusing...",
        ForeColor = UiPalette.Primary,
        BackColor = Color.FromArgb(210, 249, 235, 232),
        Padding = new Padding(8, 5, 8, 5),
        Margin = new Padding(0, 0, 0, 6),
        CornerRadius = 10,
        TextAlign = ContentAlignment.MiddleCenter
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
        TimeFontSize = 17F,
        TrackThickness = 12F,
        RingThickness = 12F,
        GlowThickness = 16F
    };

    private readonly Button _focusButton = CreateFloatingIconButton("⏸", primary: true, width: 82, height: 62, fontSize: 22F);
    private readonly Button _resetButton = CreateFloatingIconButton("↺", primary: false, width: 76, height: 62, fontSize: 26F);
    private readonly Button _backButton = CreateFloatingIconButton("←", primary: false, width: 64, height: 64, fontSize: 20F);

    public FloatingFocusForm(Action onBackToMain, Action onFocusToggle, Action onReset)
    {
        _onBackToMain = onBackToMain;
        _onFocusToggle = onFocusToggle;
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
        BackColor = Color.FromArgb(242, 247, 252);
        Opacity = 0.94D;

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

        var topRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        topRow.ColumnCount = 3;
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        topRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topRow.Controls.Add(_phaseLabel, 0, 0);
        topRow.Controls.Add(_backButton, 2, 0);
        _phaseLabel.Margin = new Padding(0, 0, 0, 6);
        _backButton.Margin = new Padding(0, 0, 0, 0);

        _backButton.Click += (_, _) => _onBackToMain();
        _focusButton.Click += (_, _) => _onFocusToggle();
        _resetButton.Click += (_, _) => _onReset();

        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        buttonRow.ColumnCount = 4;
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _focusButton.Margin = new Padding(0, 0, 16, 0);
        _resetButton.Margin = new Padding(0, 0, 0, 0);
        buttonRow.Controls.Add(_focusButton, 1, 0);
        buttonRow.Controls.Add(_resetButton, 2, 0);

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

    public void UpdateState(
        PomodoroSnapshot snapshot,
        string taskTitle,
        Color phaseColor,
        int fallbackWorkSeconds,
        bool hasSessionTask,
        bool useBusinessTheme
    )
    {
        var isIdle = snapshot.Phase == PomodoroPhase.Idle;
        var totalSeconds = isIdle ? fallbackWorkSeconds : Math.Max(1, snapshot.PhaseTotalSeconds);
        var remainingSeconds = isIdle ? fallbackWorkSeconds : Math.Max(0, snapshot.RemainingSeconds);
        var ratio = totalSeconds <= 0 ? 1F : (float)remainingSeconds / totalSeconds;

        SetBusinessTheme(useBusinessTheme);

        _phaseLabel.Text = snapshot.Phase switch
        {
            PomodoroPhase.Work => "Focusing...",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => "Ready"
        };
        _phaseLabel.ForeColor = phaseColor;
        _phaseLabel.BackColor = CreateTagBackground(phaseColor, useBusinessTheme);
        _focusButton.Text = snapshot.IsRunning
            ? "⏸"
            : "▶";

        _taskLabel.Text = taskTitle;

        _ringControl.RingColor = phaseColor;
        _ringControl.RemainingRatio = ratio;
        _ringControl.TimeText = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
    }

    public void SetBusinessTheme(bool useBusinessTheme)
    {
        if (_useBusinessTheme == useBusinessTheme)
        {
            return;
        }

        _useBusinessTheme = useBusinessTheme;
        ApplyThemeToButtonTree(this, useBusinessTheme);
        Invalidate(true);
    }

    private static Button CreateFloatingIconButton(string text, bool primary, int width, int height, float fontSize)
    {
        return new GlassActionButton(primary)
        {
            UseVividSecondaryAccent = !primary,
            UseGlyphOutlineCentering = true,
            Text = text,
            Width = width,
            Height = height,
            AutoSize = false,
            Font = new Font("Segoe UI Symbol", fontSize, FontStyle.Bold),
            CornerRadius = Math.Min(width, height) / 2,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static Color CreateTagBackground(Color accent, bool useBusinessTheme)
    {
        static int Mix(int from, int to, float t) => (int)Math.Clamp(MathF.Round(from + (to - from) * t), 0, 255);

        var strength = useBusinessTheme ? 0.12F : 0.18F;
        return Color.FromArgb(
            255,
            Mix(255, accent.R, strength),
            Mix(255, accent.G, strength),
            Mix(255, accent.B, strength)
        );
    }

    private static void ApplyThemeToButtonTree(Control root, bool useBusinessTheme)
    {
        foreach (Control control in root.Controls)
        {
            if (control is GlassActionButton button)
            {
                button.UseBusinessTheme = useBusinessTheme;
            }

            if (control.HasChildren)
            {
                ApplyThemeToButtonTree(control, useBusinessTheme);
            }
        }
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
        var startColor = _isCoolTheme ? Color.FromArgb(242, 247, 255) : Color.FromArgb(255, 247, 242);
        var endColor = _isCoolTheme ? Color.FromArgb(232, 240, 252) : Color.FromArgb(255, 236, 228);
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
            : Color.FromArgb(72, 241, 120, 106));
        e.Graphics.FillEllipse(orb2, -90, rect.Height - 220, 260, 220);
    }
}

internal sealed class RoundedTagLabel : Label
{
    private int _cornerRadius = 10;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            var next = Math.Max(2, value);
            if (_cornerRadius == next)
            {
                return;
            }

            _cornerRadius = next;
            Invalidate();
        }
    }

    public RoundedTagLabel()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var textToMeasure = string.IsNullOrEmpty(Text) ? " " : Text;
        var measured = TextRenderer.MeasureText(
            textToMeasure,
            Font,
            new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.SingleLine
        );

        return new Size(
            measured.Width + Padding.Horizontal + 4,
            measured.Height + Padding.Vertical + 2
        );
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintParentBackground(e.Graphics);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        var radius = Math.Clamp(_cornerRadius, 2, Math.Min(rect.Width, rect.Height) / 2);
        using var path = CreateRoundedRectPath(rect, radius);
        using var fill = new SolidBrush(BackColor);
        e.Graphics.FillPath(fill, path);

        var textRect = new Rectangle(
            rect.X + Padding.Left,
            rect.Y + Padding.Top,
            rect.Width - Padding.Horizontal,
            rect.Height - Padding.Vertical
        );
        if (textRect.Width <= 0 || textRect.Height <= 0)
        {
            textRect = rect;
        }

        var flags = ResolveTextFlags(TextAlign) |
                    TextFormatFlags.SingleLine;
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, flags);
    }

    private void PaintParentBackground(Graphics graphics)
    {
        if (Parent is null)
        {
            using var fallbackBrush = new SolidBrush(UiPalette.Window);
            graphics.FillRectangle(fallbackBrush, ClientRectangle);
            return;
        }

        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(-Left, -Top);
            var parentBounds = new Rectangle(Point.Empty, Parent.ClientSize);
            var paintArgs = new PaintEventArgs(graphics, parentBounds);
            InvokePaintBackground(Parent, paintArgs);
            InvokePaint(Parent, paintArgs);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static TextFormatFlags ResolveTextFlags(ContentAlignment alignment)
    {
        return alignment switch
        {
            ContentAlignment.TopLeft => TextFormatFlags.Left | TextFormatFlags.Top,
            ContentAlignment.TopCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
            ContentAlignment.TopRight => TextFormatFlags.Right | TextFormatFlags.Top,
            ContentAlignment.MiddleLeft => TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
            ContentAlignment.MiddleRight => TextFormatFlags.Right | TextFormatFlags.VerticalCenter,
            ContentAlignment.BottomLeft => TextFormatFlags.Left | TextFormatFlags.Bottom,
            ContentAlignment.BottomCenter => TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom,
            ContentAlignment.BottomRight => TextFormatFlags.Right | TextFormatFlags.Bottom,
            _ => TextFormatFlags.Left | TextFormatFlags.Top
        };
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

internal sealed class BufferedListBox : ListBox
{
    public BufferedListBox()
    {
        SetStyle(
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint,
            true
        );
        UpdateStyles();
    }
}

internal sealed class GlassActionButton : Button
{
    private readonly bool _isPrimary;
    private bool _useBusinessTheme;
    private bool _useVividSecondaryAccent;
    private bool _isHovered;
    private bool _isPressed;
    private int _cornerRadius = 12;
    private bool _useGlyphOutlineCentering;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool UseBusinessTheme
    {
        get => _useBusinessTheme;
        set
        {
            if (_useBusinessTheme == value)
            {
                return;
            }

            _useBusinessTheme = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            var next = Math.Max(4, value);
            if (_cornerRadius == next)
            {
                return;
            }

            _cornerRadius = next;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool UseVividSecondaryAccent
    {
        get => _useVividSecondaryAccent;
        set
        {
            if (_useVividSecondaryAccent == value)
            {
                return;
            }

            _useVividSecondaryAccent = value;
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool UseGlyphOutlineCentering
    {
        get => _useGlyphOutlineCentering;
        set
        {
            if (_useGlyphOutlineCentering == value)
            {
                return;
            }

            _useGlyphOutlineCentering = value;
            Invalidate();
        }
    }

    public GlassActionButton(bool isPrimary)
    {
        _isPrimary = isPrimary;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.UserPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true
        );
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintParentBackground(e.Graphics);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        var radius = Math.Clamp(_cornerRadius, 4, Math.Min(rect.Width, rect.Height) / 2);
        using var path = CreateRoundedRectPath(rect, radius);
        PaintBackground(e.Graphics, rect, path, radius);

        var secondaryTextColor = !_useBusinessTheme && _useVividSecondaryAccent
            ? UiPalette.Primary
            : UiPalette.TextPrimary;
        var textColor = Enabled
            ? (_isPrimary ? Color.White : secondaryTextColor)
            : ResolveDisabledTextColor();

        if (_useGlyphOutlineCentering)
        {
            DrawCenteredGlyphOutline(e.Graphics, rect, textColor);
        }
        else
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis
            );
        }
    }

    private void PaintBackground(Graphics graphics, Rectangle rect, GraphicsPath path, int radius)
    {
        if (!Enabled)
        {
            if (!_useBusinessTheme && _useVividSecondaryAccent && !_isPrimary)
            {
                using var vividDisabledFill = new SolidBrush(Color.FromArgb(251, 238, 234));
                using var vividDisabledBorder = new Pen(Color.FromArgb(208, 153, 145), 1F);
                graphics.FillPath(vividDisabledFill, path);
                graphics.DrawPath(vividDisabledBorder, path);
            }
            else
            {
                using var fill = new SolidBrush(Color.FromArgb(234, 239, 245));
                graphics.FillPath(fill, path);
            }
            return;
        }

        if (_isPrimary)
        {
            var end = _useBusinessTheme ? UiPalette.BusinessPrimary : UiPalette.Primary;
            var start = _useBusinessTheme ? ShiftColor(end, 0.16F) : Color.FromArgb(236, 96, 82);
            if (_isPressed)
            {
                start = ShiftColor(start, -0.1F);
                end = ShiftColor(end, -0.12F);
            }
            else if (_isHovered)
            {
                start = ShiftColor(start, 0.08F);
                end = ShiftColor(end, 0.06F);
            }

            if (!_isPressed)
            {
                var shadowRect = rect;
                shadowRect.Offset(0, 2);
                using var shadowPath = CreateRoundedRectPath(shadowRect, radius);
                var shadowColor = _useBusinessTheme ? UiPalette.BusinessPrimary : UiPalette.Primary;
                using var shadowBrush = new SolidBrush(Color.FromArgb(_isHovered ? 52 : 42, shadowColor));
                graphics.FillPath(shadowBrush, shadowPath);
            }

            using var gradient = new LinearGradientBrush(rect, start, end, LinearGradientMode.ForwardDiagonal);
            using var border = new Pen(Color.FromArgb(140, 255, 255, 255), 1F);
            graphics.FillPath(gradient, path);
            graphics.DrawPath(border, path);
            return;
        }

        if (!_useBusinessTheme && _useVividSecondaryAccent)
        {
            var accentFill = _isPressed
                ? Color.FromArgb(245, 223, 218)
                : (_isHovered ? Color.FromArgb(250, 231, 226) : Color.FromArgb(252, 237, 233));
            var accentBorder = _isPressed
                ? Color.FromArgb(198, 121, 113)
                : (_isHovered ? Color.FromArgb(212, 132, 123) : Color.FromArgb(221, 147, 138));
            using var accentFillBrush = new SolidBrush(accentFill);
            using var accentBorderPen = new Pen(accentBorder, 1F);
            graphics.FillPath(accentFillBrush, path);
            graphics.DrawPath(accentBorderPen, path);
            return;
        }

        var secondaryFill = _useBusinessTheme
            ? (_isPressed
                ? Color.FromArgb(229, 235, 241)
                : (_isHovered ? Color.FromArgb(235, 241, 247) : Color.FromArgb(240, 245, 250)))
            : (_isPressed
                ? Color.FromArgb(230, 236, 244)
                : (_isHovered ? Color.FromArgb(236, 242, 249) : Color.FromArgb(241, 245, 250)));
        var secondaryBorder = _useBusinessTheme
            ? (_isPressed ? Color.FromArgb(180, 188, 201, 216) : Color.FromArgb(184, 197, 210, 224))
            : (_isPressed ? Color.FromArgb(182, 200, 214, 228) : Color.FromArgb(182, 208, 220, 232));

        using var fillBrush = new SolidBrush(secondaryFill);
        using var borderPen = new Pen(secondaryBorder, 1F);
        graphics.FillPath(fillBrush, path);
        graphics.DrawPath(borderPen, path);
    }

    private Color ResolveDisabledTextColor()
    {
        if (!_useBusinessTheme && _useVividSecondaryAccent && !_isPrimary)
        {
            return Color.FromArgb(178, 121, 110);
        }

        return Color.FromArgb(155, 164, 176);
    }

    private void DrawCenteredGlyphOutline(Graphics graphics, Rectangle rect, Color textColor)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.None,
            FormatFlags = StringFormatFlags.NoClip
        };

        var emSize = graphics.DpiY * Font.SizeInPoints / 72F;
        using var glyphPath = new GraphicsPath();
        glyphPath.AddString(Text, Font.FontFamily, (int)Font.Style, emSize, Point.Empty, format);
        var bounds = glyphPath.GetBounds();
        if (bounds.Width <= 0F || bounds.Height <= 0F)
        {
            TextRenderer.DrawText(
                graphics,
                Text,
                Font,
                rect,
                textColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine
            );
            return;
        }

        var offsetX = rect.Left + (rect.Width - bounds.Width) / 2F - bounds.X;
        var offsetY = rect.Top + (rect.Height - bounds.Height) / 2F - bounds.Y;
        using var matrix = new Matrix();
        matrix.Translate(offsetX, offsetY);
        glyphPath.Transform(matrix);

        using var glyphBrush = new SolidBrush(textColor);
        graphics.FillPath(glyphBrush, glyphPath);
    }

    private void PaintParentBackground(Graphics graphics)
    {
        if (Parent is null)
        {
            using var fallbackBrush = new SolidBrush(UiPalette.Window);
            graphics.FillRectangle(fallbackBrush, ClientRectangle);
            return;
        }

        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(-Left, -Top);
            var parentBounds = new Rectangle(Point.Empty, Parent.ClientSize);
            var paintArgs = new PaintEventArgs(graphics, parentBounds);
            InvokePaintBackground(Parent, paintArgs);
            InvokePaint(Parent, paintArgs);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private static Color ShiftColor(Color color, float offset)
    {
        static int Clamp(float value) => (int)Math.Clamp(MathF.Round(value), 0, 255);

        var delta = offset * 255F;
        return Color.FromArgb(
            color.A,
            Clamp(color.R + delta),
            Clamp(color.G + delta),
            Clamp(color.B + delta)
        );
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

internal sealed class GlassCardPanel : Panel
{
    public GlassCardPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PaintParentBackground(e.Graphics);

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

    private void PaintParentBackground(Graphics graphics)
    {
        if (Parent is null)
        {
            using var fallbackBrush = new SolidBrush(UiPalette.Window);
            graphics.FillRectangle(fallbackBrush, ClientRectangle);
            return;
        }

        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(-Left, -Top);
            var parentBounds = new Rectangle(Point.Empty, Parent.ClientSize);
            var paintArgs = new PaintEventArgs(graphics, parentBounds);
            InvokePaintBackground(Parent, paintArgs);
            InvokePaint(Parent, paintArgs);
        }
        finally
        {
            graphics.Restore(state);
        }
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
    private float _trackThickness = 10F;
    private float _ringThickness = 10F;
    private float _glowThickness = 14F;

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float TrackThickness
    {
        get => _trackThickness;
        set
        {
            _trackThickness = Math.Clamp(value, 2F, 28F);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float RingThickness
    {
        get => _ringThickness;
        set
        {
            _ringThickness = Math.Clamp(value, 2F, 28F);
            Invalidate();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public float GlowThickness
    {
        get => _glowThickness;
        set
        {
            _glowThickness = Math.Clamp(value, 2F, 36F);
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
        using var trackPen = new Pen(Color.FromArgb(178, 216, 225, 236), _trackThickness);
        e.Graphics.DrawEllipse(trackPen, ringRect);

        using var glowPen = new Pen(Color.FromArgb(70, _ringColor), _glowThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var ringPen = new Pen(_ringColor, _ringThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
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
    public static readonly Color BusinessPrimary = Color.FromArgb(51, 64, 79);
    public static readonly Color BusinessAccent = Color.FromArgb(82, 138, 148);
    public static readonly Color BusinessLongBreak = Color.FromArgb(61, 94, 145);
    public static readonly Color TextPrimary = Color.FromArgb(36, 44, 53);
    public static readonly Color TextSecondary = Color.FromArgb(93, 104, 117);
}
