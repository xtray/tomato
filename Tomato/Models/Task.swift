import Foundation

struct PomodoroTask: Identifiable, Codable, Equatable, Hashable {
    var id: UUID
    var title: String
    var completedPomodoros: Int
    var isCompleted: Bool
    
    init(id: UUID = UUID(), title: String, completedPomodoros: Int = 0, isCompleted: Bool = false) {
        self.id = id
        self.title = title
        self.completedPomodoros = completedPomodoros
        self.isCompleted = isCompleted
    }
}
