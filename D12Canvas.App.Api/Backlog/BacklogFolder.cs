using System.Text;
using System.Text.RegularExpressions;
using D12Canvas.App.Shared.Suggestions;

namespace D12Canvas.App.Api.Backlog;

// Each suggestion is NNN-slug.md, plus NNN-slug.<ext> beside it when a screenshot came with it.
// NNN is one past the highest number any entry in the folder already starts with, so numbers keep
// rising after a file is deleted or renamed by hand.
public sealed partial class BacklogFolder(string directory)
{
    public const int MaxTitleLength = 120;
    private const int MaxSlugLength = 60;

    private static readonly Dictionary<string, string> ScreenshotExtensions = new()
    {
        ["image/png"] = "png",
        ["image/jpeg"] = "jpg",
        ["image/gif"] = "gif",
        ["image/webp"] = "webp",
    };

    private readonly Lock _gate = new();

    public string Directory { get; } = directory;

    public SavedSuggestion Save(SuggestionSubmission submission, DateTimeOffset submittedAt)
    {
        var title = Whitespace().Replace(submission.Title ?? "", " ").Trim();
        if (title.Length == 0)
        {
            throw new InvalidSuggestionException("A suggestion needs a title.");
        }

        if (title.Length > MaxTitleLength)
        {
            throw new InvalidSuggestionException(
                $"Keep the title to {MaxTitleLength} characters or fewer."
            );
        }

        (byte[] Bytes, string Extension)? screenshot = submission.Screenshot
            is { Length: > 0 } dataUrl
            ? DecodeScreenshot(dataUrl)
            : null;

        lock (_gate)
        {
            System.IO.Directory.CreateDirectory(Directory);
            var baseName = $"{NextNumber():D3}-{Slug(title)}";
            var screenshotName = screenshot is null
                ? null
                : $"{baseName}.{screenshot.Value.Extension}";
            var markdownName = $"{baseName}.md";

            if (screenshot is { } image)
            {
                WriteNew(screenshotName!, image.Bytes);
            }

            WriteNew(
                markdownName,
                Encoding.UTF8.GetBytes(
                    Markdown(
                        title,
                        submission.Details,
                        screenshotName,
                        submission.PagePath,
                        submittedAt
                    )
                )
            );

            return new SavedSuggestion(markdownName, screenshotName);
        }
    }

    public static string Slug(string title)
    {
        var slug = NonSlugRun().Replace(title.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > MaxSlugLength)
        {
            var cut = slug[..MaxSlugLength];
            var endsOnWord = slug[MaxSlugLength] == '-';
            var lastBreak = cut.LastIndexOf('-');
            slug = (endsOnWord || lastBreak <= 0 ? cut : cut[..lastBreak]).TrimEnd('-');
        }

        return slug.Length == 0 ? "suggestion" : slug;
    }

    private int NextNumber()
    {
        var highest = System
            .IO.Directory.EnumerateFileSystemEntries(Directory)
            .Select(path => NumberPrefix().Match(Path.GetFileName(path)))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();
        return highest + 1;
    }

    private void WriteNew(string fileName, byte[] bytes)
    {
        using var stream = new FileStream(
            Path.Combine(Directory, fileName),
            FileMode.CreateNew,
            FileAccess.Write
        );
        stream.Write(bytes);
    }

    private static (byte[] Bytes, string Extension) DecodeScreenshot(string dataUrl)
    {
        var match = ImageDataUrl().Match(dataUrl);
        if (
            !match.Success
            || !ScreenshotExtensions.TryGetValue(match.Groups[1].Value, out var extension)
        )
        {
            throw new InvalidSuggestionException(
                "The screenshot must be a PNG, JPEG, GIF or WebP image."
            );
        }

        try
        {
            return (Convert.FromBase64String(match.Groups[2].Value), extension);
        }
        catch (FormatException)
        {
            throw new InvalidSuggestionException("The screenshot could not be read.");
        }
    }

    private static string Markdown(
        string title,
        string? details,
        string? screenshotName,
        string? pagePath,
        DateTimeOffset submittedAt
    )
    {
        var markdown = new StringBuilder();
        markdown.AppendLine($"# {title}");
        markdown.AppendLine();
        markdown.AppendLine("Status: needs-triage");
        markdown.AppendLine();
        markdown.AppendLine(
            string.IsNullOrWhiteSpace(details) ? "_No details given._" : details.Trim()
        );

        if (screenshotName is not null)
        {
            markdown.AppendLine();
            markdown.AppendLine($"![Screenshot]({screenshotName})");
        }

        markdown.AppendLine();
        var page = string.IsNullOrWhiteSpace(pagePath) ? "" : $" from `{pagePath}`";
        markdown.AppendLine($"Suggested {submittedAt:yyyy-MM-dd HH:mm}{page}.");
        return markdown.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugRun();

    [GeneratedRegex(@"^(\d+)-")]
    private static partial Regex NumberPrefix();

    [GeneratedRegex(@"^data:([a-z/+.-]+);base64,(.*)$", RegexOptions.Singleline)]
    private static partial Regex ImageDataUrl();
}

public sealed class InvalidSuggestionException(string message) : Exception(message);
