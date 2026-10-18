import SwiftUI

/// A rounded annular segment. Its shape is also the pointer and drop hit area.
struct WheelSector: Shape {
    let index: Int
    let count: Int

    func path(in rect: CGRect) -> Path {
        guard rect.width > 16, rect.height > 16 else { return Path() }
        let center = CGPoint(x: rect.midX, y: rect.midY)
        let outer = min(rect.width, rect.height) / 2 - 8
        let inner = outer * 0.37
        let corner: CGFloat = 12
        let step = 2 * Double.pi / Double(max(count, 1))
        let start = -Double.pi / 2 + Double(index) * step - step / 2 + 0.018
        let end = start + step - 0.036
        let outerInset = Double(corner / outer)
        let innerInset = Double(corner / inner)
        func point(_ radius: CGFloat, _ angle: Double) -> CGPoint {
            CGPoint(x: center.x + radius * cos(angle), y: center.y + radius * sin(angle))
        }
        var path = Path()
        path.move(to: point(outer, start + outerInset))
        path.addArc(center: center, radius: outer, startAngle: .radians(start + outerInset), endAngle: .radians(end - outerInset), clockwise: false)
        path.addQuadCurve(to: point(outer - corner, end), control: point(outer, end))
        path.addLine(to: point(inner + corner, end))
        path.addQuadCurve(to: point(inner, end - innerInset), control: point(inner, end))
        path.addArc(center: center, radius: inner, startAngle: .radians(end - innerInset), endAngle: .radians(start + innerInset), clockwise: true)
        path.addQuadCurve(to: point(inner + corner, start), control: point(inner, start))
        path.addLine(to: point(outer - corner, start))
        path.addQuadCurve(to: point(outer, start + outerInset), control: point(outer, start))
        path.closeSubpath()
        return path
    }
}

#if DEBUG
struct WheelSector_Previews: PreviewProvider {
    static var previews: some View {
        ZStack {
            Circle().fill(Color(nsColor: .windowBackgroundColor))
            ForEach(0..<6) { index in
                WheelSector(index: index, count: 6)
                    .fill(index == 0 ? Color(red: 1, green: 0.31, blue: 0) : Color(nsColor: .controlBackgroundColor))
                    .overlay(WheelSector(index: index, count: 6).stroke(Color.primary.opacity(0.12)))
            }
        }
        .frame(width: 340, height: 340)
        .padding(24)
        .previewDisplayName("Six-segment conversion wheel")
    }
}

#endif
