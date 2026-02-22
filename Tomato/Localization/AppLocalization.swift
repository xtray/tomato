import Foundation

enum AppLanguage: String, CaseIterable, Codable, Equatable {
    case chinese = "zh-Hans"
    case english = "en"

    var displayName: String {
        switch self {
        case .chinese:
            return "中文"
        case .english:
            return "English"
        }
    }

    static func fallback(locale: Locale = .current) -> AppLanguage {
        let identifier = locale.identifier.lowercased()
        return identifier.hasPrefix("zh") ? .chinese : .english
    }
}

enum LanguagePreferences {
    static let defaultKey = "appLanguage"

    static func load(
        from defaults: UserDefaults = .standard,
        key: String = defaultKey,
        locale: Locale = .current
    ) -> AppLanguage {
        guard let rawValue = defaults.string(forKey: key),
              let language = AppLanguage(rawValue: rawValue) else {
            return AppLanguage.fallback(locale: locale)
        }
        return language
    }

    static func save(_ language: AppLanguage, to defaults: UserDefaults = .standard, key: String = defaultKey) {
        defaults.set(language.rawValue, forKey: key)
    }
}

enum AppText {
    static func string(_ key: String, language: AppLanguage, _ arguments: CVarArg...) -> String {
        let format = localizedFormat(for: key, language: language)
        guard !arguments.isEmpty else { return format }
        return String(format: format, locale: Locale(identifier: language.rawValue), arguments: arguments)
    }

    private static func localizedFormat(for key: String, language: AppLanguage) -> String {
        switch language {
        case .chinese:
            return chineseTable[key] ?? englishTable[key] ?? key
        case .english:
            return englishTable[key] ?? key
        }
    }

    private static let englishTable: [String: String] = [
        "settings.title": "Settings",
        "settings.subtitle": "Tune your focus rhythm",
        "settings.theme": "Theme",
        "settings.language": "Language",
        "settings.duration.focus": "Focus Duration",
        "settings.duration.short_break": "Short Break",
        "settings.duration.long_break": "Long Break",
        "settings.duration.current": "Current: %d min",
        "settings.duration.minutes": "%d min",
        "settings.done": "Done",

        "common.theme": "Theme",
        "common.settings": "Settings",
        "common.stop": "Stop",
        "common.reset": "Reset",
        "common.focus": "Focus",
        "common.float": "Float",

        "task.section_title": "Tasks",
        "task.empty.title": "No tasks yet",
        "task.empty.subtitle": "Add your first task and start a focus session.",
        "task.add.placeholder": "Add new task...",
        "task.completed.count": "%d pomodoros completed",
        "task.select.prompt": "Select a task",
        "task.mark.done": "Mark as Completed",
        "task.mark.undone": "Mark as Incomplete",
        "task.delete": "Delete Task",

        "help.quick_theme": "Quick theme switch",
        "help.settings": "Settings",
        "help.back_to_main": "Back to main window",

        "alert.delete_task.title": "Delete Task?",
        "alert.delete_task.confirm": "Delete",
        "alert.delete_task.cancel": "Cancel",
        "alert.delete_task.message": "Are you sure you want to delete \"%@\"? This action cannot be undone.",

        "timer.phase.work": "Focusing...",
        "timer.phase.short_break": "Short Break",
        "timer.phase.long_break": "Long Break",
        "menu.timer": "Timer",
        "menu.start_focus": "Start Focus",
        "menu.settings": "Settings...",

        "theme.mode.glass_vivid": "Glass Vivid",
        "theme.mode.business_motion": "Business Motion"
    ]

    private static let chineseTable: [String: String] = [
        "settings.title": "设置",
        "settings.subtitle": "调整你的专注节奏",
        "settings.theme": "主题",
        "settings.language": "语言",
        "settings.duration.focus": "专注时长",
        "settings.duration.short_break": "短休息",
        "settings.duration.long_break": "长休息",
        "settings.duration.current": "当前：%d 分钟",
        "settings.duration.minutes": "%d 分钟",
        "settings.done": "完成",

        "common.theme": "主题",
        "common.settings": "设置",
        "common.stop": "停止",
        "common.reset": "重置",
        "common.focus": "专注",
        "common.float": "悬浮",

        "task.section_title": "任务",
        "task.empty.title": "暂无任务",
        "task.empty.subtitle": "添加你的第一个任务并开始一次专注。",
        "task.add.placeholder": "添加新任务...",
        "task.completed.count": "已完成 %d 个番茄钟",
        "task.select.prompt": "请选择一个任务",
        "task.mark.done": "标记完成",
        "task.mark.undone": "标记为未完成",
        "task.delete": "删除任务",

        "help.quick_theme": "快速切换主题",
        "help.settings": "设置",
        "help.back_to_main": "返回主窗口",

        "alert.delete_task.title": "删除任务？",
        "alert.delete_task.confirm": "删除",
        "alert.delete_task.cancel": "取消",
        "alert.delete_task.message": "确认删除任务“%@”吗？此操作无法撤销。",

        "timer.phase.work": "专注中...",
        "timer.phase.short_break": "短休息",
        "timer.phase.long_break": "长休息",
        "menu.timer": "计时器",
        "menu.start_focus": "开始专注",
        "menu.settings": "设置...",

        "theme.mode.glass_vivid": "玻璃炫彩",
        "theme.mode.business_motion": "商务律动"
    ]
}
