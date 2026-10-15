using D12Canvas.App.Api.Backlog;
using D12Canvas.App.Shared.Suggestions;

namespace D12Canvas.App.Api.Tests;

public sealed class BacklogFolderTests : IDisposable
{
    private const string OnePixelPng =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    private static readonly DateTimeOffset SubmittedAt = new(
        2026,
        10,
        10,
        14,
        30,
        0,
        TimeSpan.Zero
    );

    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"backlog-tests-{Guid.NewGuid():N}"
    );

    private string BacklogDirectory => Path.Combine(_root, ".scratch", "backlog");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private SavedSuggestion Save(string title, string? details = null, string? screenshot = null) =>
        new BacklogFolder(BacklogDirectory).Save(
            new SuggestionSubmission(title, details, screenshot, "/"),
            SubmittedAt
        );

    [Fact]
    public void FirstSuggestionCreatesTheFolderAndIsNumberedOne()
    {
        var saved = Save("Dark minimap");

        Assert.Equal("001-dark-minimap.md", saved.Markdown);
        Assert.Null(saved.Screenshot);
        Assert.True(File.Exists(Path.Combine(BacklogDirectory, "001-dark-minimap.md")));
    }

    [Fact]
    public void NumberIsOnePastTheHighestExistingPrefix()
    {
        Directory.CreateDirectory(BacklogDirectory);
        File.WriteAllText(Path.Combine(BacklogDirectory, "007-older.md"), "");
        File.WriteAllText(Path.Combine(BacklogDirectory, "notes.md"), "");

        var saved = Save("Next one");

        Assert.Equal("008-next-one.md", saved.Markdown);
    }

    [Fact]
    public void ScreenshotIsWrittenBesideTheMarkdownAndLinkedFromIt()
    {
        var saved = Save("Bar overlaps the menu", screenshot: OnePixelPng);

        Assert.Equal("001-bar-overlaps-the-menu.png", saved.Screenshot);
        var image = File.ReadAllBytes(Path.Combine(BacklogDirectory, saved.Screenshot!));
        Assert.Equal([0x89, (byte)'P', (byte)'N', (byte)'G'], image[..4]);
        var markdown = File.ReadAllText(Path.Combine(BacklogDirectory, saved.Markdown));
        Assert.Contains("![Screenshot](001-bar-overlaps-the-menu.png)", markdown);
    }

    [Fact]
    public void MarkdownCarriesTitleTriageStatusDetailsAndWhen()
    {
        var saved = Save("Snap to guides", details: "  Guides should snap too.  ");

        var markdown = File.ReadAllText(Path.Combine(BacklogDirectory, saved.Markdown));
        Assert.StartsWith("# Snap to guides", markdown);
        Assert.Contains("Status: needs-triage", markdown);
        Assert.Contains("Guides should snap too.", markdown);
        Assert.Contains("Suggested 2026-10-10 14:30 from `/`.", markdown);
    }

    [Theory]
    [InlineData("Zoom: faster!", "zoom-faster")]
    [InlineData("  Multi\nline   title ", "multi-line-title")]
    [InlineData("???", "suggestion")]
    [InlineData(
        "A very long title that keeps going well past the point where a file name stays readable",
        "a-very-long-title-that-keeps-going-well-past-the-point-where"
    )]
    public void TitleBecomesAFileSafeSlug(string title, string expectedSlug)
    {
        Assert.Equal(expectedSlug, BacklogFolder.Slug(title));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankTitleIsRefusedAndWritesNothing(string title)
    {
        Assert.Throws<InvalidSuggestionException>(() => Save(title));
        Assert.False(Directory.Exists(BacklogDirectory));
    }

    [Fact]
    public void ScreenshotThatIsNotAnImageIsRefusedAndWritesNothing()
    {
        Assert.Throws<InvalidSuggestionException>(
            () => Save("Bad picture", screenshot: "data:text/html;base64,PGI+aGk8L2I+")
        );
        Assert.False(Directory.Exists(BacklogDirectory));
    }
}
