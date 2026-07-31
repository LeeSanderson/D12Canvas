using D12Canvas.Panel;

namespace D12Canvas.Tests;

// A minimal fixture solely for D12CanvasOptionsTests' SharedTag-EditorKind-mismatch throw test -
// carries the same SharedTag ("color") and CLR type (string) as PanelTestProps.Tint, but a
// different EditorKind (Text, not Color).
internal sealed record PropsWithMismatchedSharedTagKind(
    [property: PanelEditable(EditorKind.Text, SharedTag = "color")] string Tint = ""
);
