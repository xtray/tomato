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
    private readonly WindowsAppStateStore _stateStore = new(WindowsAppStateStore.DefaultPath());
    private readonly System.Windows.Forms.Timer _tickTimer = new() { Interval = 1000 };
    private FloatingFocusForm? _floatingForm;

    private readonly List<WinTask> _tasks = [];
    private Guid? _sessionTaskId;

    private int _workMinutes = 25;
    private int _shortBreakMinutes = 5;
    private int _longBreakMinutes = 15;
    private Size _floatingWindowSize = new(
        WindowsAppState.DefaultFloatingWindowWidth,
        WindowsAppState.DefaultFloatingWindowHeight
    );

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
        BackColor = Color.FromArgb(250, 252, 254)
    };
    private readonly Panel _taskListWrap = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(250, 252, 254),
        Padding = new Padding(0, 6, 0, 6),
        Margin = new Padding(0, 10, 0, 10)
    };
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;
    private WindowsThemeMode _lastTaskListThemeMode = WindowsThemeMode.WarmVivid;

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
        LoadPersistedState();

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
        _taskListWrap.Controls.Add(_taskList);

        _background.ThemeMode = _themeMode;
        _background.Controls.Add(BuildLayout());
        Controls.Add(_background);
        SizeChanged += (_, _) => ApplyRoundedWindowRegion();
        ApplyRoundedWindowRegion();

        FormClosing += (_, _) =>
        {
            SavePersistedState();
            if (_floatingForm is { IsDisposed: false })
            {
                _floatingForm.Close();
            }
        };

        RefreshTaskList();
        if (_tasks.Count > 0)
        {
            _taskList.SelectedIndex = 0;
        }
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
            _themeMode = WindowsThemeCatalog.Next(_themeMode);
            _background.ThemeMode = _themeMode;
            SavePersistedState();
            RefreshView();
        };

        var deleteButton = CreateMiniButton("Delete");
        deleteButton.Click += (_, _) => DeleteSelectedTask();

        header.Controls.Add(taskTitle, 0, 0);
        header.Controls.Add(_taskCountBadge, 1, 0);
        header.Controls.Add(themeButton, 2, 0);
        header.Controls.Add(settingsButton, 3, 0);

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
        layout.Controls.Add(_taskListWrap, 0, 1);
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
        SavePersistedState();
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
        SavePersistedState();
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
        if (_floatingForm is { IsDisposed: false })
        {
            _floatingForm.Close();
            _floatingForm.Dispose();
            _floatingForm = null;
        }

        if (_floatingForm is null || _floatingForm.IsDisposed)
        {
            _floatingForm = new FloatingFocusForm(
                onBackToMain: RestoreMainWindow,
                onFocusToggle: HandleFocusButton,
                onReset: ResetTimer,
                initialSize: _floatingWindowSize,
                initialThemeMode: _themeMode,
                onResizeCommitted: HandleFloatingWindowResizeCommitted
            );
        }

        _floatingForm.SetThemeMode(_themeMode);
        UpdateFloatingWindow();
        PositionFloatingWindow(_floatingForm);

        Hide();
        _floatingForm.Show();
        _floatingForm.BringToFront();
    }

    private void RestoreMainWindow()
    {
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }

        if (!Visible)
        {
            Show();
        }

        BringToFront();
        Activate();

        if (_floatingForm is { IsDisposed: false })
        {
            _floatingForm.Hide();
        }

        BeginInvoke((Action)(() =>
        {
            if (IsDisposed)
            {
                return;
            }
            RefreshView();
        }));
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
        using var dialog = new SettingsForm(_workMinutes, _shortBreakMinutes, _longBreakMinutes, _themeMode);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _workMinutes = dialog.WorkMinutes;
        _shortBreakMinutes = dialog.ShortBreakMinutes;
        _longBreakMinutes = dialog.LongBreakMinutes;
        SavePersistedState();

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
        SavePersistedState();
    }

    private void LoadPersistedState()
    {
        var state = _stateStore.Load();
        _themeMode = state.ThemeMode;
        _lastTaskListThemeMode = state.ThemeMode;
        _workMinutes = state.WorkMinutes;
        _shortBreakMinutes = state.ShortBreakMinutes;
        _longBreakMinutes = state.LongBreakMinutes;
        _floatingWindowSize = new Size(state.FloatingWindowWidth, state.FloatingWindowHeight);

        _tasks.Clear();
        foreach (var task in state.Tasks)
        {
            _tasks.Add(new WinTask(task.Id, task.Title, task.CompletedPomodoros));
        }
    }

    private void SavePersistedState()
    {
        var state = new WindowsAppState
        {
            ThemeMode = _themeMode,
            WorkMinutes = _workMinutes,
            ShortBreakMinutes = _shortBreakMinutes,
            LongBreakMinutes = _longBreakMinutes,
            FloatingWindowWidth = _floatingWindowSize.Width,
            FloatingWindowHeight = _floatingWindowSize.Height,
            Tasks = _tasks
                .Select(task => new WindowsTaskState
                {
                    Id = task.Id,
                    Title = task.Title,
                    CompletedPomodoros = task.CompletedPomodoros
                })
                .ToList()
        };

        _stateStore.Save(state);
    }

    private void HandleFloatingWindowResizeCommitted(Size size)
    {
        var normalized = new Size(
            Math.Clamp(size.Width, WindowsAppState.MinFloatingWindowWidth, WindowsAppState.MaxFloatingWindowWidth),
            Math.Clamp(size.Height, WindowsAppState.MinFloatingWindowHeight, WindowsAppState.MaxFloatingWindowHeight)
        );

        if (_floatingWindowSize == normalized)
        {
            return;
        }

        _floatingWindowSize = normalized;
        SavePersistedState();
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
        var themeMode = _themeMode;
        var workAccent = GetPhaseColor(PomodoroPhase.Work);
        var taskRowBaseColor = GetTaskRowBaseColor();

        ApplyWindowChromeTheme(themeMode);
        ApplyThemeToButtonTree(this, themeMode);
        if (_floatingForm is { IsDisposed: false })
        {
            _floatingForm.SetThemeMode(themeMode);
        }

        _taskCountBadge.ForeColor = workAccent;
        _taskCountBadge.BackColor = CreateTagBackground(workAccent, themeMode);

        _phaseLabel.Text = phaseText;
        _phaseLabel.ForeColor = phaseColor;
        _phaseLabel.BackColor = CreateTagBackground(phaseColor, themeMode);
        _taskListWrap.BackColor = taskRowBaseColor;
        _taskList.BackColor = taskRowBaseColor;

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
        if (_lastTaskListThemeMode != themeMode)
        {
            _taskList.Invalidate();
            _lastTaskListThemeMode = themeMode;
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
            themeMode: _themeMode
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
        var accents = WindowsThemeCatalog.PhaseAccents(_themeMode);
        var rgb = phase switch
        {
            PomodoroPhase.Work => accents.Work,
            PomodoroPhase.ShortBreak => accents.ShortBreak,
            PomodoroPhase.LongBreak => accents.LongBreak,
            _ => accents.Work
        };

        return Color.FromArgb(rgb.R, rgb.G, rgb.B);
    }

    private static Color ColorFromRgb(RgbAccent accent)
    {
        return Color.FromArgb(accent.R, accent.G, accent.B);
    }

    private void ApplyWindowChromeTheme(WindowsThemeMode themeMode)
    {
        var titleBarBackground = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(229, 237, 246),
            WindowsThemeMode.GreenFocus => Color.FromArgb(225, 239, 230),
            _ => Color.FromArgb(255, 238, 230)
        };
        var buttonForeground = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => UiPalette.TextPrimary,
            WindowsThemeMode.GreenFocus => UiPalette.GreenPrimary,
            _ => UiPalette.Primary
        };
        var minHoverBackground = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(214, 225, 238),
            WindowsThemeMode.GreenFocus => Color.FromArgb(206, 225, 212),
            _ => Color.FromArgb(252, 226, 219)
        };
        var closeHoverBackground = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(233, 206, 205),
            WindowsThemeMode.GreenFocus => Color.FromArgb(213, 230, 219),
            _ => Color.FromArgb(247, 205, 197)
        };
        var pressedBackground = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(198, 211, 226),
            WindowsThemeMode.GreenFocus => Color.FromArgb(188, 211, 195),
            _ => Color.FromArgb(239, 191, 181)
        };

        _titleBar.BackColor = titleBarBackground;
        _windowControlHost.BackColor = titleBarBackground;
        ConfigureWindowControlButton(_minimizeWindowButton, buttonForeground, minHoverBackground, pressedBackground);
        ConfigureWindowControlButton(_closeWindowButton, buttonForeground, closeHoverBackground, pressedBackground);
    }

    private static Color CreateTagBackground(Color accent, WindowsThemeMode themeMode)
    {
        static int Mix(int from, int to, float t) => (int)Math.Clamp(MathF.Round(from + (to - from) * t), 0, 255);

        var strength = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => 0.12F,
            WindowsThemeMode.GreenFocus => 0.16F,
            _ => 0.18F
        };
        return Color.FromArgb(
            255,
            Mix(255, accent.R, strength),
            Mix(255, accent.G, strength),
            Mix(255, accent.B, strength)
        );
    }

    private static void ApplyThemeToButtonTree(Control root, WindowsThemeMode themeMode)
    {
        foreach (Control control in root.Controls)
        {
            if (control is GlassActionButton button)
            {
                button.ThemeMode = themeMode;
            }
            else if (control is GlassCardPanel card)
            {
                card.ThemeMode = themeMode;
            }
            else if (control is TimerRingControl ring)
            {
                ring.ThemeMode = themeMode;
            }

            if (control.HasChildren)
            {
                ApplyThemeToButtonTree(control, themeMode);
            }
        }
    }

    private Color GetTaskRowSelectedColor()
    {
        return _themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(236, 242, 248),
            WindowsThemeMode.GreenFocus => Color.FromArgb(230, 240, 233),
            _ => Color.FromArgb(249, 236, 233)
        };
    }

    private Color GetTaskRowBaseColor()
    {
        return _themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(246, 249, 252),
            WindowsThemeMode.GreenFocus => Color.FromArgb(239, 246, 241),
            _ => Color.FromArgb(250, 252, 254)
        };
    }

    private Color GetTaskSelectionDotColor()
    {
        return ColorFromRgb(WindowsThemeCatalog.PhaseAccents(_themeMode).Work);
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

        var selectedRowColor = GetTaskRowSelectedColor();
        var rowBaseColor = GetTaskRowBaseColor();
        using var rowBrush = new SolidBrush(selected ? selectedRowColor : rowBaseColor);
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
            var accent = GetTaskSelectionDotColor();
            using var dotBrush = new SolidBrush(accent);
            e.Graphics.FillEllipse(dotBrush, e.Bounds.Right - 20, e.Bounds.Y + (e.Bounds.Height / 2F) - 4, 8, 8);
        }
    }
}

internal sealed class FloatingFocusForm : Form
{
    private const int ResizeCornerHitSize = 22;

    private readonly Action _onBackToMain;
    private readonly Action _onFocusToggle;
    private readonly Action _onReset;
    private readonly Action<Size>? _onResizeCommitted;
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;
    private bool _dragging;
    private bool _resizingFromBottomLeft;
    private bool _didResizeDuringDrag;
    private Point _dragStartCursor;
    private Point _dragStartForm;
    private Size _dragStartSize;
    private Point _resizeStartCursor;
    private Rectangle _resizeStartBounds;

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
        Height = 28,
        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
        ForeColor = UiPalette.TextPrimary,
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(0, 0, 0, 1)
    };

    private readonly TimerRingControl _ringControl = new()
    {
        Dock = DockStyle.Fill,
        MinimumSize = new Size(206, 206),
        TimeFontSize = 16.5F,
        TrackThickness = 12F,
        RingThickness = 12F,
        GlowThickness = 16F
    };

    private readonly Button _focusButton = CreateFloatingIconButton("⏸", primary: true, width: 78, height: 58, fontSize: 20F);
    private readonly Button _resetButton = CreateFloatingIconButton("↺", primary: false, width: 72, height: 58, fontSize: 24F);
    private readonly Button _backButton = CreateFloatingIconButton("←", primary: false, width: 60, height: 60, fontSize: 18F);

    public FloatingFocusForm(
        Action onBackToMain,
        Action onFocusToggle,
        Action onReset,
        Size initialSize,
        WindowsThemeMode initialThemeMode,
        Action<Size>? onResizeCommitted = null
    )
    {
        _onBackToMain = onBackToMain;
        _onFocusToggle = onFocusToggle;
        _onReset = onReset;
        _onResizeCommitted = onResizeCommitted;

        var initialWidth = Math.Clamp(
            initialSize.Width,
            WindowsAppState.MinFloatingWindowWidth,
            WindowsAppState.MaxFloatingWindowWidth
        );
        var initialHeight = Math.Clamp(
            initialSize.Height,
            WindowsAppState.MinFloatingWindowHeight,
            WindowsAppState.MaxFloatingWindowHeight
        );

        Text = "Tomato Focus";
        Icon = AppIconProvider.GetAppIcon();
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Width = initialWidth;
        Height = initialHeight;
        MinimumSize = new Size(WindowsAppState.MinFloatingWindowWidth, WindowsAppState.MinFloatingWindowHeight);
        BackColor = Color.FromArgb(242, 247, 252);
        Opacity = 0.90D;

        var card = new GlassCardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
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
        _phaseLabel.Margin = new Padding(0, 0, 0, 4);
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
        _focusButton.Margin = new Padding(0, 0, 12, 0);
        _resetButton.Margin = new Padding(0, 0, 0, 0);
        buttonRow.Controls.Add(_focusButton, 1, 0);
        buttonRow.Controls.Add(_resetButton, 2, 0);

        var ringHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 6, 0, 6)
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
        SetThemeMode(initialThemeMode);
        EnableDrag(this);
        DpiChanged += OnDpiChanged;
        MouseCaptureChanged += OnFloatingMouseCaptureChanged;
    }

    public void UpdateState(
        PomodoroSnapshot snapshot,
        string taskTitle,
        Color phaseColor,
        int fallbackWorkSeconds,
        bool hasSessionTask,
        WindowsThemeMode themeMode
    )
    {
        var isIdle = snapshot.Phase == PomodoroPhase.Idle;
        var totalSeconds = isIdle ? fallbackWorkSeconds : Math.Max(1, snapshot.PhaseTotalSeconds);
        var remainingSeconds = isIdle ? fallbackWorkSeconds : Math.Max(0, snapshot.RemainingSeconds);
        var ratio = totalSeconds <= 0 ? 1F : (float)remainingSeconds / totalSeconds;

        SetThemeMode(themeMode);

        _phaseLabel.Text = snapshot.Phase switch
        {
            PomodoroPhase.Work => "Focusing...",
            PomodoroPhase.ShortBreak => "Short Break",
            PomodoroPhase.LongBreak => "Long Break",
            _ => "Ready"
        };
        _phaseLabel.ForeColor = phaseColor;
        _phaseLabel.BackColor = CreateTagBackground(phaseColor, themeMode);
        _focusButton.Text = snapshot.IsRunning
            ? "⏸"
            : "▶";

        _taskLabel.Text = taskTitle;

        _ringControl.RingColor = phaseColor;
        _ringControl.RemainingRatio = ratio;
        _ringControl.TimeText = $"{remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
    }

    public void SetThemeMode(WindowsThemeMode themeMode)
    {
        if (_themeMode == themeMode)
        {
            return;
        }

        _themeMode = themeMode;
        ApplyThemeToButtonTree(this, themeMode);
        BackColor = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(242, 247, 252),
            WindowsThemeMode.GreenFocus => Color.FromArgb(233, 242, 236),
            _ => Color.FromArgb(252, 244, 240)
        };
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

    private static Color CreateTagBackground(Color accent, WindowsThemeMode themeMode)
    {
        static int Mix(int from, int to, float t) => (int)Math.Clamp(MathF.Round(from + (to - from) * t), 0, 255);

        var strength = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => 0.12F,
            WindowsThemeMode.GreenFocus => 0.16F,
            _ => 0.18F
        };
        return Color.FromArgb(
            255,
            Mix(255, accent.R, strength),
            Mix(255, accent.G, strength),
            Mix(255, accent.B, strength)
        );
    }

    private static void ApplyThemeToButtonTree(Control root, WindowsThemeMode themeMode)
    {
        foreach (Control control in root.Controls)
        {
            if (control is GlassActionButton button)
            {
                button.ThemeMode = themeMode;
            }
            else if (control is GlassCardPanel card)
            {
                card.ThemeMode = themeMode;
            }
            else if (control is TimerRingControl ring)
            {
                ring.ThemeMode = themeMode;
            }

            if (control.HasChildren)
            {
                ApplyThemeToButtonTree(control, themeMode);
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

        if (IsBottomLeftResizeHotZone(sender, e.Location))
        {
            _resizingFromBottomLeft = true;
            _didResizeDuringDrag = false;
            _resizeStartCursor = Cursor.Position;
            _resizeStartBounds = Bounds;
            _dragging = false;
            BeginDragCapture();
            Cursor = Cursors.SizeNESW;
            return;
        }

        _dragging = true;
        _resizingFromBottomLeft = false;
        _dragStartCursor = Cursor.Position;
        _dragStartForm = Location;
        _dragStartSize = Size;
        BeginDragCapture();
    }

    private void OnDragMouseMove(object? sender, MouseEventArgs e)
    {
        if (_resizingFromBottomLeft)
        {
            ResizeFromBottomLeftCorner();
            return;
        }

        if (!_dragging)
        {
            UpdateResizeCursor(sender, e.Location);
            return;
        }

        var cursor = Cursor.Position;
        var dx = cursor.X - _dragStartCursor.X;
        var dy = cursor.Y - _dragStartCursor.Y;
        var newLocation = new Point(_dragStartForm.X + dx, _dragStartForm.Y + dy);
        if (Size != _dragStartSize)
        {
            SetBounds(newLocation.X, newLocation.Y, _dragStartSize.Width, _dragStartSize.Height);
            return;
        }

        Location = newLocation;
    }

    private void OnDragMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            EndDragInteraction(sender, e.Location, releaseCapture: true);
        }
    }

    private bool IsBottomLeftResizeHotZone(object? sender, Point sourceLocation)
    {
        if (sender is not Control source)
        {
            return false;
        }

        var formPoint = PointToClient(source.PointToScreen(sourceLocation));
        return formPoint.X <= ResizeCornerHitSize &&
               formPoint.Y >= ClientSize.Height - ResizeCornerHitSize;
    }

    private void UpdateResizeCursor(object? sender, Point sourceLocation)
    {
        Cursor = IsBottomLeftResizeHotZone(sender, sourceLocation)
            ? Cursors.SizeNESW
            : Cursors.Default;
    }

    private void ResizeFromBottomLeftCorner()
    {
        var cursor = Cursor.Position;
        var dx = cursor.X - _resizeStartCursor.X;
        var dy = cursor.Y - _resizeStartCursor.Y;

        var right = _resizeStartBounds.Right;
        var newWidth = Math.Max(MinimumSize.Width, _resizeStartBounds.Width - dx);
        var newHeight = Math.Max(MinimumSize.Height, _resizeStartBounds.Height + dy);
        var newX = right - newWidth;

        if (newWidth != Width || newHeight != Height)
        {
            _didResizeDuringDrag = true;
        }
        SetBounds(newX, _resizeStartBounds.Y, newWidth, newHeight);
    }

    private void BeginDragCapture()
    {
        if (!Capture)
        {
            Capture = true;
        }
    }

    private void EndDragInteraction(object? sender, Point sourceLocation, bool releaseCapture)
    {
        var shouldCommitResize = _resizingFromBottomLeft && _didResizeDuringDrag;
        _resizingFromBottomLeft = false;
        _dragging = false;
        _didResizeDuringDrag = false;

        if (releaseCapture && Capture)
        {
            Capture = false;
        }

        if (shouldCommitResize)
        {
            _onResizeCommitted?.Invoke(Size);
        }

        if (sender is Control)
        {
            UpdateResizeCursor(sender, sourceLocation);
            return;
        }

        UpdateResizeCursor(this, PointToClient(Cursor.Position));
    }

    private void OnFloatingMouseCaptureChanged(object? sender, EventArgs e)
    {
        if (Capture || (!_dragging && !_resizingFromBottomLeft))
        {
            return;
        }

        EndDragInteraction(this, PointToClient(Cursor.Position), releaseCapture: false);
    }

    private void OnDpiChanged(object? sender, DpiChangedEventArgs e)
    {
        if (_dragging)
        {
            _dragStartCursor = Cursor.Position;
            _dragStartForm = Location;
            _dragStartSize = Size;
        }

        if (_resizingFromBottomLeft)
        {
            _resizeStartCursor = Cursor.Position;
            _resizeStartBounds = Bounds;
        }

        ApplyRoundedWindowRegion();
        Invalidate(true);
        Update();
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

    public SettingsForm(int workMinutes, int shortBreakMinutes, int longBreakMinutes, WindowsThemeMode themeMode)
    {
        Text = "Settings";
        Icon = AppIconProvider.GetAppIcon();
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 250);
        BackColor = themeMode switch
        {
            WindowsThemeMode.BusinessMotion => Color.FromArgb(242, 247, 252),
            WindowsThemeMode.GreenFocus => Color.FromArgb(233, 242, 236),
            _ => UiPalette.Window
        };

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

        var doneButton = CreateDialogPrimaryButton("Done", themeMode);
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

    private static Button CreateDialogPrimaryButton(string text, WindowsThemeMode themeMode)
    {
        var button = new GlassActionButton(true)
        {
            Text = text,
            Width = 100,
            Height = 40,
            AutoSize = false,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ThemeMode = themeMode,
            TextAlign = ContentAlignment.MiddleCenter
        };
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
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public WindowsThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value)
            {
                return;
            }
            _themeMode = value;
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
        var (startColor, endColor, orbColor) = _themeMode switch
        {
            WindowsThemeMode.BusinessMotion => (
                Color.FromArgb(242, 247, 255),
                Color.FromArgb(232, 240, 252),
                Color.FromArgb(58, 127, 176, 255)
            ),
            WindowsThemeMode.GreenFocus => (
                Color.FromArgb(239, 247, 242),
                Color.FromArgb(224, 238, 229),
                Color.FromArgb(64, 73, 170, 118)
            ),
            _ => (
                Color.FromArgb(255, 247, 242),
                Color.FromArgb(255, 236, 228),
                Color.FromArgb(72, 241, 120, 106)
            )
        };
        using var gradient = new LinearGradientBrush(
            rect,
            startColor,
            endColor,
            LinearGradientMode.ForwardDiagonal
        );
        e.Graphics.FillRectangle(gradient, rect);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var orb2 = new SolidBrush(orbColor);
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
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;
    private bool _useVividSecondaryAccent;
    private bool _isHovered;
    private bool _isPressed;
    private int _cornerRadius = 12;
    private bool _useGlyphOutlineCentering;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public WindowsThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value)
            {
                return;
            }

            _themeMode = value;
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

        var secondaryTextColor = _themeMode == WindowsThemeMode.WarmVivid && _useVividSecondaryAccent
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
            if (_themeMode == WindowsThemeMode.WarmVivid && _useVividSecondaryAccent && !_isPrimary)
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
            var end = _themeMode switch
            {
                WindowsThemeMode.BusinessMotion => UiPalette.BusinessPrimary,
                WindowsThemeMode.GreenFocus => UiPalette.GreenPrimary,
                _ => UiPalette.Primary
            };
            var start = _themeMode switch
            {
                WindowsThemeMode.BusinessMotion => ShiftColor(end, 0.16F),
                WindowsThemeMode.GreenFocus => ShiftColor(end, 0.2F),
                _ => Color.FromArgb(236, 96, 82)
            };
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
                var shadowColor = end;
                using var shadowBrush = new SolidBrush(Color.FromArgb(_isHovered ? 52 : 42, shadowColor));
                graphics.FillPath(shadowBrush, shadowPath);
            }

            using var gradient = new LinearGradientBrush(rect, start, end, LinearGradientMode.ForwardDiagonal);
            using var border = new Pen(Color.FromArgb(140, 255, 255, 255), 1F);
            graphics.FillPath(gradient, path);
            graphics.DrawPath(border, path);
            return;
        }

        if (_themeMode == WindowsThemeMode.WarmVivid && _useVividSecondaryAccent)
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

        var (secondaryFill, secondaryBorder) = _themeMode switch
        {
            WindowsThemeMode.BusinessMotion => (
                _isPressed ? Color.FromArgb(229, 235, 241) : (_isHovered ? Color.FromArgb(235, 241, 247) : Color.FromArgb(240, 245, 250)),
                _isPressed ? Color.FromArgb(180, 188, 201, 216) : Color.FromArgb(184, 197, 210, 224)
            ),
            WindowsThemeMode.GreenFocus => (
                _isPressed ? Color.FromArgb(225, 236, 229) : (_isHovered ? Color.FromArgb(231, 241, 235) : Color.FromArgb(236, 246, 240)),
                _isPressed ? Color.FromArgb(176, 192, 182, 205) : Color.FromArgb(184, 198, 188, 212)
            ),
            _ => (
                _isPressed ? Color.FromArgb(230, 236, 244) : (_isHovered ? Color.FromArgb(236, 242, 249) : Color.FromArgb(241, 245, 250)),
                _isPressed ? Color.FromArgb(182, 200, 214, 228) : Color.FromArgb(182, 208, 220, 232)
            )
        };

        using var fillBrush = new SolidBrush(secondaryFill);
        using var borderPen = new Pen(secondaryBorder, 1F);
        graphics.FillPath(fillBrush, path);
        graphics.DrawPath(borderPen, path);
    }

    private Color ResolveDisabledTextColor()
    {
        if (_themeMode == WindowsThemeMode.WarmVivid && _useVividSecondaryAccent && !_isPrimary)
        {
            return Color.FromArgb(178, 121, 110);
        }

        if (_themeMode == WindowsThemeMode.GreenFocus)
        {
            return Color.FromArgb(142, 154, 146);
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
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public WindowsThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value)
            {
                return;
            }

            _themeMode = value;
            Invalidate();
        }
    }

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
        var (start, end, borderColor) = _themeMode switch
        {
            WindowsThemeMode.BusinessMotion => (
                Color.FromArgb(223, 248, 252, 255),
                Color.FromArgb(198, 246, 250, 255),
                Color.FromArgb(175, 241, 248, 255)
            ),
            WindowsThemeMode.GreenFocus => (
                Color.FromArgb(223, 236, 248, 239),
                Color.FromArgb(198, 230, 244, 234),
                Color.FromArgb(175, 212, 231, 218)
            ),
            _ => (
                Color.FromArgb(223, 255, 255, 255),
                Color.FromArgb(198, 255, 255, 255),
                Color.FromArgb(175, 255, 255, 255)
            )
        };

        using var fill = new LinearGradientBrush(
            rect,
            start,
            end,
            LinearGradientMode.ForwardDiagonal
        );
        e.Graphics.FillPath(fill, path);

        using var border = new Pen(borderColor, 1.2F);
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
    private WindowsThemeMode _themeMode = WindowsThemeMode.WarmVivid;

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public WindowsThemeMode ThemeMode
    {
        get => _themeMode;
        set
        {
            if (_themeMode == value)
            {
                return;
            }

            _themeMode = value;
            BackColor = value switch
            {
                WindowsThemeMode.GreenFocus => Color.FromArgb(236, 246, 240),
                WindowsThemeMode.BusinessMotion => Color.FromArgb(246, 249, 252),
                _ => Color.White
            };
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
        var (ringBgColor, ringTrackColor) = _themeMode switch
        {
            WindowsThemeMode.GreenFocus => (Color.FromArgb(76, 233, 244, 236), Color.FromArgb(176, 185, 214, 194)),
            WindowsThemeMode.BusinessMotion => (Color.FromArgb(60, 255, 255, 255), Color.FromArgb(178, 216, 225, 236)),
            _ => (Color.FromArgb(60, 255, 255, 255), Color.FromArgb(178, 216, 225, 236))
        };

        using var bgBrush = new SolidBrush(ringBgColor);
        e.Graphics.FillEllipse(bgBrush, outer);

        var ringRect = new RectangleF(x + 22, y + 22, size - 44, size - 44);
        using var trackPen = new Pen(ringTrackColor, _trackThickness);
        e.Graphics.DrawEllipse(trackPen, ringRect);

        using var glowPen = new Pen(Color.FromArgb(70, _ringColor), _glowThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var ringPen = new Pen(_ringColor, _ringThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var arc = TimerRingGeometry.DescribeCountdownArc(_remainingRatio);
        e.Graphics.DrawArc(glowPen, ringRect, arc.StartAngle, arc.SweepAngle);
        e.Graphics.DrawArc(ringPen, ringRect, arc.StartAngle, arc.SweepAngle);

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
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; }
    public int CompletedPomodoros { get; set; }

    public WinTask(string title)
        : this(Guid.NewGuid(), title, 0)
    {
    }

    public WinTask(Guid id, string title, int completedPomodoros)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Title = title;
        CompletedPomodoros = Math.Max(0, completedPomodoros);
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
    public static readonly Color GreenPrimary = Color.FromArgb(49, 126, 93);
    public static readonly Color GreenShortBreak = Color.FromArgb(74, 168, 120);
    public static readonly Color GreenLongBreak = Color.FromArgb(112, 196, 146);
    public static readonly Color TextPrimary = Color.FromArgb(36, 44, 53);
    public static readonly Color TextSecondary = Color.FromArgb(93, 104, 117);
}
