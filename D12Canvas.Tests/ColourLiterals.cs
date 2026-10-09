using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace D12Canvas.Tests;

public sealed record ColourLiteral(string Selector, string Property, string Literal)
{
    public bool IsTokenDeclaration => Property.StartsWith("--", StringComparison.Ordinal);
}

public static partial class ColourLiterals
{
    private static readonly HashSet<string> NamedColours = Enum.GetValues<KnownColor>()
        .Select(Color.FromKnownColor)
        .Where(colour => !colour.IsSystemColor && colour.Name != nameof(Color.Transparent))
        .Select(colour => colour.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<ColourLiteral> In(string css)
    {
        var found = new List<ColourLiteral>();
        var preludes = new Stack<string>();
        var pending = new StringBuilder();

        foreach (var character in CommentFree(css))
        {
            switch (character)
            {
                case '{':
                    preludes.Push(Normalised(pending.ToString()));
                    pending.Clear();
                    break;
                case ';':
                    Collect(pending.ToString(), preludes, found);
                    pending.Clear();
                    break;
                case '}':
                    Collect(pending.ToString(), preludes, found);
                    pending.Clear();
                    if (preludes.Count > 0)
                    {
                        preludes.Pop();
                    }
                    break;
                default:
                    pending.Append(character);
                    break;
            }
        }

        return found;
    }

    public static Dictionary<string, string> LibraryStyleBlocks()
    {
        var libraryDirectory = Path.Combine(RepositoryRoot(), "D12Canvas");
        return Directory
            .EnumerateFiles(libraryDirectory, "*.razor", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(libraryDirectory, path))
            .Select(path =>
                (
                    Name: Path.GetFileName(path),
                    Css: StyleBlocksOfRazorSource(File.ReadAllText(path))
                )
            )
            .Where(component => component.Css.Length > 0)
            .ToDictionary(component => component.Name, component => component.Css);
    }

    private static bool IsBuildOutput(string libraryDirectory, string path)
    {
        var firstSegment = Path.GetRelativePath(libraryDirectory, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return firstSegment is "bin" or "obj";
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "D12Canvas.slnx"))
        )
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "No D12Canvas.slnx above " + AppContext.BaseDirectory
            );
    }

    private static string StyleBlocksOfRazorSource(string razorSource) =>
        string.Join(
                "\n",
                StyleBlock().Matches(razorSource).Select(match => match.Groups["css"].Value)
            )
            .Replace("@@", "@", StringComparison.Ordinal);

    private static void Collect(
        string declaration,
        Stack<string> preludes,
        List<ColourLiteral> found
    )
    {
        var colon = declaration.IndexOf(':');
        if (colon < 0 || preludes.Count == 0)
        {
            return;
        }

        var property = declaration[..colon].Trim();
        var value = QuotedString().Replace(declaration[(colon + 1)..], "\"\"");
        var selector = preludes.Peek();

        foreach (Match match in HexColour().Matches(value))
        {
            found.Add(new ColourLiteral(selector, property, match.Value.ToLowerInvariant()));
        }

        foreach (Match match in ColourFunction().Matches(value))
        {
            found.Add(new ColourLiteral(selector, property, Normalised(match.Value)));
        }

        foreach (Match match in Identifier().Matches(value))
        {
            if (NamedColours.Contains(match.Value))
            {
                found.Add(new ColourLiteral(selector, property, match.Value.ToLowerInvariant()));
            }
        }
    }

    private static string CommentFree(string css) => Comment().Replace(css, " ");

    private static string Normalised(string text) => Whitespace().Replace(text, " ").Trim();

    [GeneratedRegex(@"<style>(?<css>.*?)</style>", RegexOptions.Singleline)]
    private static partial Regex StyleBlock();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex("\"[^\"]*\"|'[^']*'")]
    private static partial Regex QuotedString();

    [GeneratedRegex(@"#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3,4})(?![\w-])")]
    private static partial Regex HexColour();

    [GeneratedRegex(@"(?<![\w-])(?:rgba?|hsla?|hwb|lab|lch|oklab|oklch|color)\([^)]*\)")]
    private static partial Regex ColourFunction();

    [GeneratedRegex(@"(?<![\w#-])[A-Za-z]+(?![\w(-])")]
    private static partial Regex Identifier();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
