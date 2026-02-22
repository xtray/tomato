import SwiftUI

struct SettingsView: View {
    @ObservedObject var taskStore: TaskStore
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        ZStack {
            GlassBackground(mode: mode)

            GlassCard(mode: mode) {
                VStack(alignment: .leading, spacing: AppTheme.Spacing.md) {
                    HStack {
                        VStack(alignment: .leading, spacing: 4) {
                            Text(AppText.string("settings.title", language: language))
                                .font(.system(size: 24, weight: .bold, design: .rounded))
                            Text(AppText.string("settings.subtitle", language: language))
                                .font(.subheadline)
                                .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
                        }

                        Spacer()

                        Button {
                            dismiss()
                        } label: {
                            Image(systemName: "xmark")
                        }
                        .buttonStyle(SecondaryGlassButtonStyle(mode: mode))
                    }

                    VStack(alignment: .leading, spacing: AppTheme.Spacing.xs) {
                        Text(AppText.string("settings.theme", language: language))
                            .font(.system(size: 14, weight: .semibold))

                        Picker(AppText.string("settings.theme", language: language), selection: themeSelection) {
                            ForEach(ThemeMode.allCases, id: \.self) { option in
                                Text(option.displayName(language: language)).tag(option)
                            }
                        }
                        .pickerStyle(.segmented)
                    }
                    .padding(.horizontal, AppTheme.Spacing.sm)
                    .padding(.vertical, AppTheme.Spacing.sm)
                    .background(AppTheme.Colors.textFieldFill(for: mode))
                    .overlay(
                        RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                            .stroke(AppTheme.Colors.textFieldStroke(for: mode), lineWidth: 0.8)
                    )
                    .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))

                    VStack(alignment: .leading, spacing: AppTheme.Spacing.xs) {
                        Text(AppText.string("settings.language", language: language))
                            .font(.system(size: 14, weight: .semibold))

                        Picker(AppText.string("settings.language", language: language), selection: languageSelection) {
                            ForEach(AppLanguage.allCases, id: \.self) { option in
                                Text(option.displayName).tag(option)
                            }
                        }
                        .pickerStyle(.segmented)
                    }
                    .padding(.horizontal, AppTheme.Spacing.sm)
                    .padding(.vertical, AppTheme.Spacing.sm)
                    .background(AppTheme.Colors.textFieldFill(for: mode))
                    .overlay(
                        RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                            .stroke(AppTheme.Colors.textFieldStroke(for: mode), lineWidth: 0.8)
                    )
                    .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))

                    durationSetting(
                        title: AppText.string("settings.duration.focus", language: language),
                        icon: "bolt.fill",
                        value: $taskStore.workDuration,
                        range: 1...60,
                        color: AppTheme.Colors.tomatoPrimary(for: mode)
                    )

                    durationSetting(
                        title: AppText.string("settings.duration.short_break", language: language),
                        icon: "leaf.fill",
                        value: $taskStore.shortBreakDuration,
                        range: 1...30,
                        color: AppTheme.Colors.accentMint(for: mode)
                    )

                    durationSetting(
                        title: AppText.string("settings.duration.long_break", language: language),
                        icon: "moon.stars.fill",
                        value: $taskStore.longBreakDuration,
                        range: 1...60,
                        color: AppTheme.Colors.phaseLongBreak(for: mode)
                    )

                    HStack {
                        Spacer()
                        Button(AppText.string("settings.done", language: language)) {
                            dismiss()
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                    }
                }
            }
            .padding(AppTheme.Spacing.md)
        }
        .frame(width: 440, height: 500)
    }

    private func durationSetting(title: String, icon: String, value: Binding<Int>, range: ClosedRange<Int>, color: Color) -> some View {
        HStack(spacing: AppTheme.Spacing.sm) {
            ZStack {
                Circle()
                    .fill(color.opacity(0.2))
                    .frame(width: 30, height: 30)
                Image(systemName: icon)
                    .font(.system(size: 12, weight: .semibold))
                    .foregroundStyle(color)
            }

            VStack(alignment: .leading, spacing: 2) {
                Text(title)
                    .font(.system(size: 14, weight: .semibold))
                Text(AppText.string("settings.duration.current", language: language, value.wrappedValue / 60))
                    .font(.caption)
                    .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
            }

            Spacer()

            Picker("", selection: value) {
                ForEach(range, id: \.self) { minute in
                    Text(AppText.string("settings.duration.minutes", language: language, minute)).tag(minute * 60)
                }
            }
            .labelsHidden()
            .frame(width: 120)
            .tint(color)
        }
        .padding(.horizontal, AppTheme.Spacing.sm)
        .padding(.vertical, AppTheme.Spacing.sm)
        .background(AppTheme.Colors.textFieldFill(for: mode))
        .overlay(
            RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                .stroke(AppTheme.Colors.textFieldStroke(for: mode), lineWidth: 0.8)
        )
        .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))
    }

    var mode: ThemeMode {
        taskStore.themeMode
    }

    var language: AppLanguage {
        taskStore.appLanguage
    }

    private var themeSelection: Binding<ThemeMode> {
        Binding(
            get: { taskStore.themeMode },
            set: { newValue in
                guard taskStore.themeMode != newValue else { return }
                // Defer publishing to next run loop to avoid state publishing during view updates.
                DispatchQueue.main.async {
                    taskStore.themeMode = newValue
                }
            }
        )
    }

    private var languageSelection: Binding<AppLanguage> {
        Binding(
            get: { taskStore.appLanguage },
            set: { newValue in
                guard taskStore.appLanguage != newValue else { return }
                DispatchQueue.main.async {
                    taskStore.appLanguage = newValue
                }
            }
        )
    }
}
