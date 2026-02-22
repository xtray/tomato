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
                            Text("Settings")
                                .font(.system(size: 24, weight: .bold, design: .rounded))
                            Text("Tune your focus rhythm")
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
                        Text("Theme")
                            .font(.system(size: 14, weight: .semibold))

                        Picker("Theme", selection: $taskStore.themeMode) {
                            ForEach(ThemeMode.allCases, id: \.self) { option in
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
                        title: "Focus Duration",
                        icon: "bolt.fill",
                        value: $taskStore.workDuration,
                        range: 1...60,
                        color: AppTheme.Colors.tomatoPrimary(for: mode)
                    )

                    durationSetting(
                        title: "Short Break",
                        icon: "leaf.fill",
                        value: $taskStore.shortBreakDuration,
                        range: 1...30,
                        color: AppTheme.Colors.accentMint(for: mode)
                    )

                    durationSetting(
                        title: "Long Break",
                        icon: "moon.stars.fill",
                        value: $taskStore.longBreakDuration,
                        range: 1...60,
                        color: AppTheme.Colors.phaseLongBreak(for: mode)
                    )

                    HStack {
                        Spacer()
                        Button("Done") {
                            dismiss()
                        }
                        .buttonStyle(PrimaryGlassButtonStyle(mode: mode))
                    }
                }
            }
            .padding(AppTheme.Spacing.md)
        }
        .frame(width: 420, height: 360)
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
                Text("Current: \(value.wrappedValue / 60) min")
                    .font(.caption)
                    .foregroundStyle(AppTheme.Colors.textSecondary(for: mode))
            }

            Spacer()

            Picker("", selection: value) {
                ForEach(range, id: \.self) { minute in
                    Text("\(minute) min").tag(minute * 60)
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
}
