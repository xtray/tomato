import SwiftUI

struct GlassCard<Content: View>: View {
    let mode: ThemeMode
    let padding: CGFloat
    @ViewBuilder var content: Content

    init(mode: ThemeMode, padding: CGFloat = AppTheme.Spacing.md, @ViewBuilder content: () -> Content) {
        self.mode = mode
        self.padding = padding
        self.content = content()
    }

    var body: some View {
        content
            .padding(padding)
            .background(AppTheme.Backgrounds.cardFill(for: mode))
            .overlay(
                RoundedRectangle(cornerRadius: AppTheme.Radius.card, style: .continuous)
                    .stroke(AppTheme.Backgrounds.cardStroke(for: mode), lineWidth: 1)
            )
            .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.card, style: .continuous))
            .shadow(
                color: AppTheme.Shadow.card(for: mode),
                radius: AppTheme.Shadow.cardRadius(for: mode),
                x: 0,
                y: AppTheme.Shadow.cardY(for: mode)
            )
    }
}

struct GlassTag: View {
    let mode: ThemeMode
    let text: String
    let tint: Color

    var body: some View {
        Text(text)
            .font(.caption.weight(.semibold))
            .foregroundStyle(tint)
            .padding(.horizontal, 8)
            .padding(.vertical, 4)
            .background(AppTheme.Colors.tagBackground(for: mode, tint: tint))
            .clipShape(Capsule())
    }
}

struct PrimaryGlassButtonStyle: ButtonStyle {
    let mode: ThemeMode

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .semibold))
            .padding(.horizontal, 14)
            .padding(.vertical, 9)
            .foregroundStyle(.white)
            .background(
                LinearGradient(
                    colors: [
                        AppTheme.Colors.tomatoSecondary(for: mode),
                        AppTheme.Colors.tomatoPrimary(for: mode)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            )
            .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))
            .overlay(
                RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                    .stroke(Color.white.opacity(0.35), lineWidth: 0.6)
            )
            .scaleEffect(configuration.isPressed ? 0.97 : 1)
            .animation(AppTheme.Motion.pressAnimation(for: mode), value: configuration.isPressed)
    }
}

struct SecondaryGlassButtonStyle: ButtonStyle {
    let mode: ThemeMode

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .medium))
            .padding(.horizontal, 12)
            .padding(.vertical, 8)
            .foregroundStyle(AppTheme.Colors.textPrimary(for: mode))
            .background(AppTheme.Colors.secondaryButtonFill(for: mode, isPressed: configuration.isPressed))
            .overlay(
                RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous)
                    .stroke(AppTheme.Colors.secondaryButtonStroke(for: mode), lineWidth: 0.8)
            )
            .clipShape(RoundedRectangle(cornerRadius: AppTheme.Radius.small, style: .continuous))
            .scaleEffect(configuration.isPressed ? 0.985 : 1)
            .animation(AppTheme.Motion.pressAnimation(for: mode), value: configuration.isPressed)
    }
}

struct GlassBackground: View {
    let mode: ThemeMode
    @State private var drifting = false

    var body: some View {
        ZStack {
            LinearGradient(
                gradient: AppTheme.Backgrounds.mainGradient(for: mode),
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )

            Circle()
                .fill(AppTheme.Colors.orbPrimary(for: mode))
                .frame(width: 320, height: 320)
                .blur(radius: 30)
                .scaleEffect(drifting ? 1.0 + AppTheme.Motion.backgroundScaleDelta(for: mode) : 1.0)
                .offset(
                    x: drifting ? -160 + AppTheme.Motion.backgroundDriftDistance(for: mode) : -160,
                    y: drifting ? -140 - AppTheme.Motion.backgroundDriftDistance(for: mode) * 0.6 : -140
                )

            Circle()
                .fill(AppTheme.Colors.orbSecondary(for: mode))
                .frame(width: 280, height: 280)
                .blur(radius: 40)
                .scaleEffect(drifting ? 1.0 - AppTheme.Motion.backgroundScaleDelta(for: mode) * 0.7 : 1.0)
                .offset(
                    x: drifting ? 180 - AppTheme.Motion.backgroundDriftDistance(for: mode) * 0.8 : 180,
                    y: drifting ? 180 + AppTheme.Motion.backgroundDriftDistance(for: mode) : 180
                )
        }
        .ignoresSafeArea()
        .onAppear {
            drifting = true
        }
        .animation(
            .easeInOut(duration: AppTheme.Motion.backgroundDriftDuration(for: mode))
                .repeatForever(autoreverses: true),
            value: drifting
        )
    }
}
