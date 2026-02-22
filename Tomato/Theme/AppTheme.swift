import SwiftUI
import Foundation

enum ThemeMode: String, CaseIterable, Codable, Equatable {
    case glassVivid
    case businessMotion

    var displayName: String {
        switch self {
        case .glassVivid:
            return "Glass Vivid"
        case .businessMotion:
            return "Business Motion"
        }
    }
}

enum ThemePreferences {
    static let defaultKey = "themeMode"

    static func load(from defaults: UserDefaults = .standard, key: String = defaultKey) -> ThemeMode {
        guard let rawValue = defaults.string(forKey: key),
              let mode = ThemeMode(rawValue: rawValue) else {
            return .glassVivid
        }
        return mode
    }

    static func save(_ mode: ThemeMode, to defaults: UserDefaults = .standard, key: String = defaultKey) {
        defaults.set(mode.rawValue, forKey: key)
    }
}

enum AppTheme {
    enum Colors {
        static let tomatoPrimary = Color(red: 0.88, green: 0.23, blue: 0.20)
        static let tomatoSecondary = Color(red: 0.97, green: 0.45, blue: 0.31)
        static let accentMint = Color(red: 0.21, green: 0.69, blue: 0.62)
        static let businessPrimary = Color(red: 0.20, green: 0.25, blue: 0.31)
        static let businessSecondary = Color(red: 0.33, green: 0.40, blue: 0.48)
        static let businessAccent = Color(red: 0.32, green: 0.54, blue: 0.58)

        static let textPrimary = Color.primary
        static let textSecondary = Color.secondary

        static let ringTrack = Color.white.opacity(0.32)
        static let ringGlow = Color.white.opacity(0.48)

        static let phaseWork = tomatoPrimary
        static let phaseShortBreak = accentMint
        static let phaseLongBreak = Color(red: 0.20, green: 0.52, blue: 0.95)

        static func textPrimary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return textPrimary
            case .businessMotion:
                return Color(red: 0.16, green: 0.20, blue: 0.25)
            }
        }

        static func textSecondary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return textSecondary
            case .businessMotion:
                return Color(red: 0.39, green: 0.44, blue: 0.50)
            }
        }

        static func tomatoPrimary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return tomatoPrimary
            case .businessMotion:
                return businessPrimary
            }
        }

        static func tomatoSecondary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return tomatoSecondary
            case .businessMotion:
                return businessSecondary
            }
        }

        static func accentMint(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return accentMint
            case .businessMotion:
                return businessAccent
            }
        }

        static func ringTrack(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return Color.white.opacity(0.32)
            case .businessMotion:
                return Color.white.opacity(0.26)
            }
        }

        static func ringGlow(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return ringGlow
            case .businessMotion:
                return Color.white.opacity(0.34)
            }
        }

        static func textFieldFill(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return Color.white.opacity(0.45)
            case .businessMotion:
                return Color.white.opacity(0.56)
            }
        }

        static func textFieldStroke(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return Color.white.opacity(0.55)
            case .businessMotion:
                return Color.white.opacity(0.70)
            }
        }

        static func tagBackground(for mode: ThemeMode, tint: Color) -> Color {
            switch mode {
            case .glassVivid:
                return tint.opacity(0.15)
            case .businessMotion:
                return tint.opacity(0.18)
            }
        }

        static func secondaryButtonFill(for mode: ThemeMode, isPressed: Bool) -> Color {
            switch mode {
            case .glassVivid:
                return Color.white.opacity(isPressed ? 0.28 : 0.38)
            case .businessMotion:
                return Color.white.opacity(isPressed ? 0.42 : 0.52)
            }
        }

        static func secondaryButtonStroke(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return Color.white.opacity(0.45)
            case .businessMotion:
                return Color.white.opacity(0.62)
            }
        }

        static func selectionFill(for mode: ThemeMode) -> Color {
            tomatoPrimary(for: mode).opacity(mode == .glassVivid ? 0.14 : 0.12)
        }

        static func orbPrimary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return tomatoPrimary.opacity(0.18)
            case .businessMotion:
                return businessPrimary.opacity(0.19)
            }
        }

        static func orbSecondary(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return accentMint.opacity(0.16)
            case .businessMotion:
                return businessAccent.opacity(0.15)
            }
        }

        static func phaseWork(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return phaseWork
            case .businessMotion:
                return businessPrimary
            }
        }

        static func phaseShortBreak(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return phaseShortBreak
            case .businessMotion:
                return businessAccent
            }
        }

        static func phaseLongBreak(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return phaseLongBreak
            case .businessMotion:
                return Color(red: 0.24, green: 0.37, blue: 0.57)
            }
        }
    }

    enum Backgrounds {
        static let mainGradient = Gradient(stops: [
            .init(color: Color(red: 1.00, green: 0.97, blue: 0.96), location: 0.0),
            .init(color: Color(red: 0.95, green: 0.96, blue: 0.99), location: 0.48),
            .init(color: Color(red: 0.98, green: 0.99, blue: 1.00), location: 1.0)
        ])

        static let cardFill = LinearGradient(
            colors: [
                Color.white.opacity(0.68),
                Color.white.opacity(0.52)
            ],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )

        static let cardStroke = LinearGradient(
            colors: [
                Color.white.opacity(0.8),
                Color.white.opacity(0.35)
            ],
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )

        static func mainGradient(for mode: ThemeMode) -> Gradient {
            switch mode {
            case .glassVivid:
                return mainGradient
            case .businessMotion:
                return Gradient(stops: [
                    .init(color: Color(red: 0.95, green: 0.96, blue: 0.97), location: 0.0),
                    .init(color: Color(red: 0.91, green: 0.93, blue: 0.95), location: 0.5),
                    .init(color: Color(red: 0.96, green: 0.97, blue: 0.98), location: 1.0)
                ])
            }
        }

        static func cardFill(for mode: ThemeMode) -> LinearGradient {
            switch mode {
            case .glassVivid:
                return cardFill
            case .businessMotion:
                return LinearGradient(
                    colors: [
                        Color.white.opacity(0.72),
                        Color.white.opacity(0.58)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            }
        }

        static func cardStroke(for mode: ThemeMode) -> LinearGradient {
            switch mode {
            case .glassVivid:
                return cardStroke
            case .businessMotion:
                return LinearGradient(
                    colors: [
                        Color.white.opacity(0.92),
                        Color.white.opacity(0.44)
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
            }
        }
    }

    enum Radius {
        static let card: CGFloat = 20
        static let small: CGFloat = 12
    }

    enum Shadow {
        static let card = Color.black.opacity(0.12)
        static let cardRadius: CGFloat = 16
        static let cardY: CGFloat = 8

        static func card(for mode: ThemeMode) -> Color {
            switch mode {
            case .glassVivid:
                return Color.black.opacity(0.12)
            case .businessMotion:
                return Color.black.opacity(0.16)
            }
        }

        static func cardRadius(for mode: ThemeMode) -> CGFloat {
            switch mode {
            case .glassVivid:
                return 16
            case .businessMotion:
                return 20
            }
        }

        static func cardY(for mode: ThemeMode) -> CGFloat {
            switch mode {
            case .glassVivid:
                return 8
            case .businessMotion:
                return 10
            }
        }
    }

    enum Spacing {
        static let xs: CGFloat = 6
        static let sm: CGFloat = 10
        static let md: CGFloat = 16
        static let lg: CGFloat = 24
    }

    enum Typography {
        static let heroTimer = Font.system(size: 46, weight: .light, design: .monospaced)
        static let sectionTitle = Font.system(size: 18, weight: .semibold)
    }

    enum Motion {
        static func pressAnimation(for mode: ThemeMode) -> Animation {
            switch mode {
            case .glassVivid:
                return .easeOut(duration: 0.12)
            case .businessMotion:
                return .spring(response: 0.25, dampingFraction: 0.78)
            }
        }

        static func backgroundDriftDuration(for mode: ThemeMode) -> Double {
            switch mode {
            case .glassVivid:
                return 10
            case .businessMotion:
                return 6
            }
        }

        static func backgroundDriftDistance(for mode: ThemeMode) -> CGFloat {
            switch mode {
            case .glassVivid:
                return 20
            case .businessMotion:
                return 34
            }
        }

        static func backgroundScaleDelta(for mode: ThemeMode) -> CGFloat {
            switch mode {
            case .glassVivid:
                return 0.02
            case .businessMotion:
                return 0.05
            }
        }
    }
}

extension TimerPhase {
    var themedColor: Color {
        themedColor(for: .glassVivid)
    }

    func themedColor(for mode: ThemeMode) -> Color {
        switch self {
        case .work:
            return AppTheme.Colors.phaseWork(for: mode)
        case .shortBreak:
            return AppTheme.Colors.phaseShortBreak(for: mode)
        case .longBreak:
            return AppTheme.Colors.phaseLongBreak(for: mode)
        }
    }
}
