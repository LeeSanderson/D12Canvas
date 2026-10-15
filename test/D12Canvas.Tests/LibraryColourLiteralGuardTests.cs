using Xunit;

namespace D12Canvas.Tests;

public class LibraryColourLiteralGuardTests
{
    private static readonly (string File, string Selector, string Literal)[] FixedSignalColours =
    [
        ("ComponentContainer.razor", ".port", "#ffffff"),
        ("ComponentContainer.razor", ".port.port-focused", "#f39c12"),
        ("DiagramCanvas.razor", ".floating-endpoint", "#f39c12"),
        ("DiagramCanvas.razor", ".floating-endpoint", "#ffffff"),
    ];

    [Fact]
    public void EveryColourLiteralInTheLibrarysStyleBlocksIsATokenDeclarationOrAFixedSignalColour()
    {
        var styledComponents = ColourLiterals.LibraryStyleBlocks();

        Assert.Contains("DiagramCanvas.razor", styledComponents.Keys);
        Assert.Contains("ComponentContainer.razor", styledComponents.Keys);
        Assert.Contains("PropertyBar.razor", styledComponents.Keys);
        Assert.Contains("PropertyPanel.razor", styledComponents.Keys);
        Assert.Contains("Palette.razor", styledComponents.Keys);
        Assert.Contains("Minimap.razor", styledComponents.Keys);
        Assert.Contains("ContextMenu.razor", styledComponents.Keys);

        var strays = styledComponents
            .SelectMany(component => StrayLiterals(component.Key, component.Value))
            .ToList();

        Assert.True(strays.Count == 0, "Stray colour literals:\n" + string.Join("\n", strays));
    }

    [Fact]
    public void TheGuardCatchesALiteralAddedOutsideATokenDeclaration()
    {
        var canvasCss = ColourLiterals.LibraryStyleBlocks()["DiagramCanvas.razor"];
        var withStrays =
            canvasCss
            + "\n.selection-bounding-box { border: 1px solid #2f80ed; }"
            + "\n.marquee-select { background: rgba(47, 128, 237, 0.08); }"
            + "\n.edge-line { stroke: black; }";

        var strays = StrayLiterals("DiagramCanvas.razor", withStrays);

        Assert.Equal(
            [
                "DiagramCanvas.razor .selection-bounding-box border: #2f80ed",
                "DiagramCanvas.razor .marquee-select background: rgba(47, 128, 237, 0.08)",
                "DiagramCanvas.razor .edge-line stroke: black",
            ],
            strays
        );
    }

    [Fact]
    public void EveryFixedSignalColourIsStillInUse()
    {
        var styledComponents = ColourLiterals.LibraryStyleBlocks();

        foreach (var (file, selector, literal) in FixedSignalColours)
        {
            Assert.Contains(
                ColourLiterals.In(styledComponents[file]),
                found => found.Selector == selector && found.Literal == literal
            );
        }
    }

    [Fact]
    public void TheScannerSeparatesTokenDeclarationsFromLiteralsAndIgnoresNonColours()
    {
        var found = ColourLiterals.In(
            """
            .root { --d12-x: #ABCDEF; color: var(--d12-x); }
            @media (prefers-color-scheme: dark) { .root { --d12-x: hsl(0 0% 10%); } }
            .a { fill: url(#d12-hatch); stroke: currentColor; background: transparent; }
            .b { font: inherit; white-space: nowrap; font-family: "Segoe UI", sans-serif; }
            .c { & .d { outline: 1px solid White; } }
            """
        );

        Assert.Equal(
            [
                new ColourLiteral(".root", "--d12-x", "#abcdef"),
                new ColourLiteral(".root", "--d12-x", "hsl(0 0% 10%)"),
                new ColourLiteral("& .d", "outline", "white"),
            ],
            found
        );
    }

    private static IEnumerable<string> StrayLiterals(string file, string css) =>
        ColourLiterals
            .In(css)
            .Where(found => !found.IsTokenDeclaration)
            .Where(found => !FixedSignalColours.Contains((file, found.Selector, found.Literal)))
            .Select(found => $"{file} {found.Selector} {found.Property}: {found.Literal}");
}
