using System.Globalization;

namespace Tomato.WindowsCore;

public static class WindowsUiText
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["task.section_title"] = "Tasks",
        ["task.select.prompt"] = "Select a task",
        ["task.select.subtitle"] = "Pick a task and start focus",
        ["task.completed.count"] = "{0} pomodoros completed",
        ["task.completed.none"] = "No pomodoros yet",
        ["task.mark.done"] = "Mark as Completed",
        ["task.mark.undone"] = "Mark as Incomplete",
        ["task.delete.current"] = "Delete Task",
        ["task.add.placeholder"] = "Add a task",
        ["settings.open"] = "Settings",
        ["theme.switch"] = "Theme",
        ["common.delete"] = "Delete",
        ["common.focus"] = "Focus",
        ["common.pause"] = "Pause",
        ["common.run"] = "Run",
        ["common.reset"] = "Reset",
        ["common.float"] = "Float",
        ["timer.phase.work"] = "Focusing...",
        ["timer.phase.short_break"] = "Short Break",
        ["timer.phase.long_break"] = "Long Break",
        ["timer.phase.ready"] = "Ready",
        ["timer.status.paused"] = "Paused",
        ["floating.task.fallback"] = "Focus",
        ["alert.select_task.title"] = "Tomato",
        ["alert.select_task.message"] = "Please select a task first.",
        ["alert.delete_task.title"] = "Delete Task",
        ["alert.delete_task.message"] = "Delete task \"{0}\"? This action cannot be undone.",
        ["alert.session_completed.title"] = "Tomato",
        ["alert.session_completed.message"] = "Session completed.",
        ["settings.title"] = "Settings",
        ["settings.duration.focus"] = "Focus Duration",
        ["settings.duration.short_break"] = "Short Break",
        ["settings.duration.long_break"] = "Long Break",
        ["settings.opacity"] = "Floating Opacity (%)",
        ["settings.chime.enabled"] = "Play completion chimes",
        ["settings.chime.volume"] = "Chime Volume (%)",
        ["settings.language"] = "Language",
        ["settings.done"] = "Done",
        ["language.chinese"] = "中文",
        ["language.english"] = "English"
    };

    private static readonly IReadOnlyDictionary<string, string> Chinese = new Dictionary<string, string>
    {
        ["task.section_title"] = "任务",
        ["task.select.prompt"] = "请选择任务",
        ["task.select.subtitle"] = "选择一个任务并开始专注",
        ["task.completed.count"] = "已完成 {0} 个番茄钟",
        ["task.completed.none"] = "尚未完成番茄",
        ["task.mark.done"] = "标记完成",
        ["task.mark.undone"] = "标记未完成",
        ["task.delete.current"] = "删除任务",
        ["task.add.placeholder"] = "添加任务",
        ["settings.open"] = "设置",
        ["theme.switch"] = "主题",
        ["common.delete"] = "删除",
        ["common.focus"] = "专注",
        ["common.pause"] = "暂停",
        ["common.run"] = "继续",
        ["common.reset"] = "重置",
        ["common.float"] = "浮窗",
        ["timer.phase.work"] = "专注中...",
        ["timer.phase.short_break"] = "短休息",
        ["timer.phase.long_break"] = "长休息",
        ["timer.phase.ready"] = "准备开始",
        ["timer.status.paused"] = "已暂停",
        ["floating.task.fallback"] = "专注",
        ["alert.select_task.title"] = "番茄钟",
        ["alert.select_task.message"] = "请先选择一个任务。",
        ["alert.delete_task.title"] = "删除任务",
        ["alert.delete_task.message"] = "确定删除任务“{0}”？此操作无法撤销。",
        ["alert.session_completed.title"] = "番茄钟",
        ["alert.session_completed.message"] = "本轮已完成。",
        ["settings.title"] = "设置",
        ["settings.duration.focus"] = "专注时长",
        ["settings.duration.short_break"] = "短休息",
        ["settings.duration.long_break"] = "长休息",
        ["settings.opacity"] = "浮窗透明度（%）",
        ["settings.chime.enabled"] = "播放完成提示音",
        ["settings.chime.volume"] = "提示音音量（%）",
        ["settings.language"] = "语言",
        ["settings.done"] = "完成",
        ["language.chinese"] = "中文",
        ["language.english"] = "English"
    };

    public static string Get(string key, WindowsAppLanguage language, params object[] args)
    {
        var table = language == WindowsAppLanguage.Chinese ? Chinese : English;
        if (!table.TryGetValue(key, out var format))
        {
            format = key;
        }

        if (args.Length == 0)
        {
            return format;
        }

        var culture = language == WindowsAppLanguage.Chinese
            ? CultureInfo.GetCultureInfo("zh-CN")
            : CultureInfo.GetCultureInfo("en-US");
        return string.Format(culture, format, args);
    }
}
