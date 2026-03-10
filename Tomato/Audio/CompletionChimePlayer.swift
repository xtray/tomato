import AVFoundation
import Foundation

final class CompletionChimePlayer: NSObject, AVAudioPlayerDelegate {
    static let shared = CompletionChimePlayer()

    private struct ChimeNote {
        let frequency: Double
        let duration: Double
    }

    private let sampleRate = 44_100.0
    private var activePlayers: [AVAudioPlayer] = []

    func play(event: CompletionChimeEvent, volume: Double) {
        let normalizedVolume = CompletionChimePreferences.normalized(volume)
        guard normalizedVolume > 0 else {
            return
        }

        guard let audioData = makeWaveData(for: event, volume: normalizedVolume) else {
            return
        }

        DispatchQueue.main.async {
            do {
                let player = try AVAudioPlayer(data: audioData)
                player.delegate = self
                player.prepareToPlay()
                self.activePlayers.append(player)
                player.play()
            } catch {
                // Best-effort playback only.
            }
        }
    }

    func audioPlayerDidFinishPlaying(_ player: AVAudioPlayer, successfully flag: Bool) {
        activePlayers.removeAll { $0 === player }
    }

    func audioPlayerDecodeErrorDidOccur(_ player: AVAudioPlayer, error: Error?) {
        activePlayers.removeAll { $0 === player }
    }

    private func makeWaveData(for event: CompletionChimeEvent, volume: Double) -> Data? {
        let notes = melody(for: event)
        let pcmData = renderPCM(notes: notes, volume: volume)
        guard !pcmData.isEmpty else {
            return nil
        }

        return makeWaveContainer(for: pcmData)
    }

    private func melody(for event: CompletionChimeEvent) -> [ChimeNote] {
        switch event {
        case .workCompleted:
            return [
                ChimeNote(frequency: 659.25, duration: 0.24),
                ChimeNote(frequency: 783.99, duration: 0.24),
                ChimeNote(frequency: 987.77, duration: 0.38),
                ChimeNote(frequency: 0, duration: 0.10),
                ChimeNote(frequency: 783.99, duration: 0.24),
                ChimeNote(frequency: 1046.50, duration: 0.46),
                ChimeNote(frequency: 1318.51, duration: 0.62),
                ChimeNote(frequency: 0, duration: 0.12),
                ChimeNote(frequency: 987.77, duration: 0.34),
                ChimeNote(frequency: 1174.66, duration: 0.38),
                ChimeNote(frequency: 1318.51, duration: 0.56)
            ]
        case .breakCompleted:
            return [
                ChimeNote(frequency: 523.25, duration: 0.22),
                ChimeNote(frequency: 659.25, duration: 0.22),
                ChimeNote(frequency: 783.99, duration: 0.30),
                ChimeNote(frequency: 0, duration: 0.08),
                ChimeNote(frequency: 880.00, duration: 0.26),
                ChimeNote(frequency: 1046.50, duration: 0.34),
                ChimeNote(frequency: 1174.66, duration: 0.34),
                ChimeNote(frequency: 0, duration: 0.10),
                ChimeNote(frequency: 987.77, duration: 0.28),
                ChimeNote(frequency: 1318.51, duration: 0.42),
                ChimeNote(frequency: 1567.98, duration: 0.56)
            ]
        }
    }

    private func renderPCM(notes: [ChimeNote], volume: Double) -> Data {
        var samples: [Int16] = []
        let amplitude = Int16(Double(Int16.max) * min(max(volume, 0), 1) * 0.35)

        for note in notes {
            let sampleCount = max(Int(note.duration * sampleRate), 1)
            for index in 0..<sampleCount {
                let envelope = amplitudeEnvelope(sampleIndex: index, totalSamples: sampleCount)
                let sampleValue: Int16
                if note.frequency <= 0 {
                    sampleValue = 0
                } else {
                    let time = Double(index) / sampleRate
                    let angle = 2 * Double.pi * note.frequency * time
                    let harmonic = sin(angle) + (0.35 * sin(angle * 2.0))
                    sampleValue = Int16(Double(amplitude) * envelope * harmonic)
                }
                samples.append(sampleValue)
            }
        }

        return samples.withUnsafeBufferPointer { buffer in
            Data(buffer: buffer)
        }
    }

    private func amplitudeEnvelope(sampleIndex: Int, totalSamples: Int) -> Double {
        let attackSamples = max(Int(sampleRate * 0.012), 1)
        let releaseSamples = max(Int(sampleRate * 0.060), 1)

        if sampleIndex < attackSamples {
            return Double(sampleIndex) / Double(attackSamples)
        }

        if sampleIndex >= totalSamples - releaseSamples {
            let remaining = max(totalSamples - sampleIndex, 0)
            return Double(remaining) / Double(releaseSamples)
        }

        return 1.0
    }

    private func makeWaveContainer(for pcmData: Data) -> Data {
        let audioFormat: UInt16 = 1
        let channelCount: UInt16 = 1
        let bitsPerSample: UInt16 = 16
        let byteRate = UInt32(sampleRate) * UInt32(channelCount) * UInt32(bitsPerSample / 8)
        let blockAlign = UInt16(channelCount * (bitsPerSample / 8))
        let dataLength = UInt32(pcmData.count)
        let riffLength = 36 + dataLength

        var data = Data()
        data.append("RIFF".data(using: .ascii)!)
        data.append(littleEndianBytes(riffLength))
        data.append("WAVE".data(using: .ascii)!)
        data.append("fmt ".data(using: .ascii)!)
        data.append(littleEndianBytes(UInt32(16)))
        data.append(littleEndianBytes(audioFormat))
        data.append(littleEndianBytes(channelCount))
        data.append(littleEndianBytes(UInt32(sampleRate)))
        data.append(littleEndianBytes(byteRate))
        data.append(littleEndianBytes(blockAlign))
        data.append(littleEndianBytes(bitsPerSample))
        data.append("data".data(using: .ascii)!)
        data.append(littleEndianBytes(dataLength))
        data.append(pcmData)
        return data
    }

    private func littleEndianBytes<T: FixedWidthInteger>(_ value: T) -> Data {
        var littleEndianValue = value.littleEndian
        return withUnsafeBytes(of: &littleEndianValue) { Data($0) }
    }
}
