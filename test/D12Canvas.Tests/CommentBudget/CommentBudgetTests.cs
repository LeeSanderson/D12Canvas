using System.Text.Json;

namespace D12Canvas.Tests.CommentBudget;

public class CommentBudgetTests
{
    private const string BudgetFileName = "comment-budget.json";
    private const string TightenVariable = "D12_TIGHTEN_COMMENT_BUDGET";

    private static readonly string[] ScannedRoots = ["lib", "app", "test", ".github"];
    private static readonly string[] ScannedRootFiles = ["Directory.Build.props"];
    private static readonly string[] SkippedDirectories = ["bin", "obj", "node_modules"];
    private static readonly string[] SkippedPathFragments = ["/wwwroot/lib/"];

    [Fact]
    public void NoSourceFileHasMoreCommentsThanItsBudgetAllows()
    {
        var repoRoot = FindRepoRoot();
        var budget = ReadBudget(repoRoot);
        var actual = CountComments(repoRoot);

        var overBudget = actual
            .Where(entry => entry.Value > budget.GetValueOrDefault(entry.Key))
            .Select(entry =>
                $"{entry.Key}: {entry.Value} comments, budget {budget.GetValueOrDefault(entry.Key)}"
            )
            .ToList();

        Assert.True(
            overBudget.Count == 0,
            "Comments are banned in source files. Remove these comments; a decision belongs in "
                + "docs/adr, a workaround belongs in an issue under .scratch:\n"
                + string.Join("\n", overBudget)
        );
    }

    [Fact]
    public void TheBudgetShrinksAsCommentsAreDeleted()
    {
        var repoRoot = FindRepoRoot();
        var budget = ReadBudget(repoRoot);
        var actual = CountComments(repoRoot);

        var slack = budget
            .Where(entry => actual.GetValueOrDefault(entry.Key) < entry.Value)
            .ToDictionary(entry => entry.Key, entry => actual.GetValueOrDefault(entry.Key));

        if (slack.Count > 0 && Environment.GetEnvironmentVariable(TightenVariable) == "1")
        {
            WriteBudget(repoRoot, Tighten(budget, slack));
            return;
        }

        Assert.True(
            slack.Count == 0,
            $"{BudgetFileName} allows more comments than these files have. Lower each entry to "
                + $"its new count (remove it at 0), or rerun the tests with {TightenVariable}=1 "
                + "to rewrite the budget:\n"
                + string.Join("\n", slack.Select(entry => $"{entry.Key}: {entry.Value}"))
        );
    }

    private static SortedDictionary<string, int> Tighten(
        SortedDictionary<string, int> budget,
        Dictionary<string, int> slack
    )
    {
        var tightened = new SortedDictionary<string, int>(budget, StringComparer.Ordinal);
        foreach (var (file, count) in slack)
        {
            if (count == 0)
                tightened.Remove(file);
            else
                tightened[file] = count;
        }
        return tightened;
    }

    private static Dictionary<string, int> CountComments(string repoRoot) =>
        ScannedFiles(repoRoot)
            .Select(path =>
                (
                    Relative: Path.GetRelativePath(repoRoot, path).Replace('\\', '/'),
                    Count: SourceComments.Find(path, File.ReadAllText(path)).Count
                )
            )
            .Where(file => file.Count > 0)
            .ToDictionary(file => file.Relative, file => file.Count);

    private static IEnumerable<string> ScannedFiles(string repoRoot)
    {
        foreach (var file in ScannedRootFiles)
            yield return Path.Combine(repoRoot, file);
        foreach (var root in ScannedRoots)
        {
            var directory = Path.Combine(repoRoot, root);
            if (!Directory.Exists(directory))
                continue;
            foreach (var path in Walk(directory))
                yield return path;
        }
    }

    private static IEnumerable<string> Walk(string directory)
    {
        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var normalised = file.Replace('\\', '/');
            if (
                SourceComments.IsScanned(file)
                && !SkippedPathFragments.Any(fragment => normalised.Contains(fragment))
            )
                yield return file;
        }
        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (SkippedDirectories.Contains(Path.GetFileName(child)))
                continue;
            foreach (var file in Walk(child))
                yield return file;
        }
    }

    private static SortedDictionary<string, int> ReadBudget(string repoRoot)
    {
        var path = Path.Combine(repoRoot, BudgetFileName);
        var entries = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path))!;
        return new SortedDictionary<string, int>(entries, StringComparer.Ordinal);
    }

    private static void WriteBudget(string repoRoot, SortedDictionary<string, int> budget)
    {
        var json = JsonSerializer.Serialize(
            budget,
            new JsonSerializerOptions { WriteIndented = true }
        );
        File.WriteAllText(Path.Combine(repoRoot, BudgetFileName), json + "\n");
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "D12Canvas.slnx"))
        )
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new InvalidOperationException(
                "D12Canvas.slnx not found above the test binaries."
            );
    }
}
