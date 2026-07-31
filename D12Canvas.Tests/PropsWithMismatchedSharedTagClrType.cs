using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture solely for D12CanvasOptionsTests' SharedTag-CLR-type-mismatch throw test -
// carries the same SharedTag ("color") and EditorKind (Color) as PanelTestProps.Tint, but a
// different CLR property type (int, not string).
internal sealed record PropsWithMismatchedSharedTagClrType(
    [property: PanelEditable(EditorKind.Color, SharedTag = "color")] int Tint = 0
);
