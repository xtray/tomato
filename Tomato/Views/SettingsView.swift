import SwiftUI

struct SettingsView: View {
    @ObservedObject var taskStore: TaskStore
    @Environment(\.dismiss) private var dismiss
    
    var body: some View {
        VStack(spacing: 20) {
            Text("Settings")
                .font(.title2)
                .fontWeight(.semibold)
            
            Divider()
            
            VStack(alignment: .leading, spacing: 16) {
                durationSetting(
                    title: "Focus Duration",
                    value: $taskStore.workDuration,
                    range: 1...60,
                    color: .red
                )
                
                durationSetting(
                    title: "Short Break",
                    value: $taskStore.shortBreakDuration,
                    range: 1...30,
                    color: .green
                )
                
                durationSetting(
                    title: "Long Break",
                    value: $taskStore.longBreakDuration,
                    range: 1...60,
                    color: .blue
                )
            }
            .padding(.horizontal)
            
            Spacer()
            
            Button("Done") {
                dismiss()
            }
            .buttonStyle(.borderedProminent)
        }
        .padding()
        .frame(width: 320, height: 300)
        .background(Color(NSColor.windowBackgroundColor))
    }
    
    func durationSetting(title: String, value: Binding<Int>, range: ClosedRange<Int>, color: Color) -> some View {
        HStack {
            Circle()
                .fill(color)
                .frame(width: 12, height: 12)
            
            Text(title)
                .frame(width: 100, alignment: .leading)
            
            Picker("", selection: value) {
                ForEach(range, id: \.self) { minute in
                    Text("\(minute) min").tag(minute * 60)
                }
            }
            .labelsHidden()
            .frame(width: 100)
        }
    }
}
