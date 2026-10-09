using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The bar floats over the board, so it declares the raised values and its colour scheme on its own
// root in the same four blocks the other floating surfaces use.
public class PropertyBarThemeTokensTests : ComponentTestBase
{
    public PropertyBarThemeTokensTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private string BarCss()
    {
        var board = new Board();
        var rectangle = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(200, 200, 100, 60)
        );
        board.AddComponent(rectangle);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.ContainerOf(rectangle.Id));
        return canvas
            .FindAll("style")
            .Select(style => style.InnerHtml)
            .Single(text => text.Contains(".d12-property-bar {"));
    }

    [Theory]
    [InlineData(".d12-property-bar {", "light")]
    [InlineData("@media (prefers-color-scheme: dark)", "dark")]
    [InlineData("[data-d12-theme=light] .d12-property-bar {", "light")]
    [InlineData("[data-d12-theme=dark] .d12-property-bar {", "dark")]
    public void EveryRaisedBlockDeclaresEveryTokenAndItsColorScheme(string marker, string scheme)
    {
        var block = ExtractBlock(BarCss(), marker);

        foreach (var token in ThemeTokens.Append("--d12-shadow"))
        {
            Assert.Contains(token, block);
        }

        Assert.Contains($"color-scheme: {scheme}", block);
    }

    [Fact]
    public void TheStylesheetsHeightAndCellsAreThePlacementsOwn()
    {
        var css = BarCss();

        Assert.Contains(
            $"height: {PropertyBarPlacement.Height}px",
            ExtractEveryBlock(css, ".d12-property-bar {")
        );
        Assert.Contains("flex: 0 0 26px", ExtractBlock(css, ".d12-property-bar-cell {"));
    }

    [Fact]
    public void TheBarDeclaresNothingOnTheGlobalRoot()
    {
        Assert.DoesNotContain(":root", BarCss());
    }

    [Theory]
    [InlineData(".d12-property-bar-hatch-line {")]
    [InlineData(".d12-property-bar-swatch-themed-fill {")]
    [InlineData(".d12-property-bar-number {")]
    public void ItsPartsReadTokensExclusively(string marker)
    {
        var block = ExtractBlock(BarCss(), marker);

        Assert.Contains("var(--d12-", block);
        Assert.DoesNotContain("#", block);
        Assert.DoesNotContain("rgba(", block);
    }
}
