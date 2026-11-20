namespace D12Canvas.Tests.CommentBudget;

public sealed record SourceComment(int StartLine, int EndLine, string Text);

public static class SourceComments
{
    private static readonly string[] SlashExtensions = [".cs", ".js", ".css"];
    private static readonly string[] HashExtensions = [".ps1", ".sh", ".yml", ".yaml"];
    private static readonly string[] XmlExtensions = [".csproj", ".props"];

    public static bool IsScanned(string path)
    {
        var extension = Path.GetExtension(path);
        return extension == ".razor"
            || SlashExtensions.Contains(extension)
            || HashExtensions.Contains(extension)
            || XmlExtensions.Contains(extension);
    }

    public static IReadOnlyList<SourceComment> Find(string path, string source)
    {
        var text = source.Replace("\r\n", "\n");
        var extension = Path.GetExtension(path);
        var raw =
            HashExtensions.Contains(extension) ? FindHash(text)
            : XmlExtensions.Contains(extension) ? FindXml(text)
            : FindSlash(text, razor: extension == ".razor");
        return MergeAdjacentLineComments(raw, text.Split('\n'));
    }

    private static List<SourceComment> FindSlash(string text, bool razor)
    {
        var found = new List<SourceComment>();
        var line = 1;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\n')
            {
                line++;
                i++;
            }
            else if (razor && StartsWith(text, i, "@*"))
                i = TakeDelimited(text, i, "*@", found, ref line);
            else if (razor && StartsWith(text, i, "<!--"))
                i = TakeDelimited(text, i, "-->", found, ref line);
            else if (StartsWith(text, i, "//"))
            {
                if (razor && i > 0 && text[i - 1] == ':')
                {
                    i += 2;
                    continue;
                }
                var end = text.IndexOf('\n', i);
                end = end < 0 ? text.Length : end;
                found.Add(new SourceComment(line, line, text[i..end]));
                i = end;
            }
            else if (StartsWith(text, i, "/*"))
                i = TakeDelimited(text, i, "*/", found, ref line);
            else if (StartsWith(text, i, "\"\"\""))
                i = SkipRawString(text, i, ref line);
            else if (c == '"' || c == '`' || (c == '\'' && !razor))
                i = SkipQuoted(text, i, ref line);
            else
                i++;
        }
        return found;
    }

    private static int TakeDelimited(
        string text,
        int start,
        string close,
        List<SourceComment> found,
        ref int line
    )
    {
        var closeAt = text.IndexOf(close, start + 2, StringComparison.Ordinal);
        var end = closeAt < 0 ? text.Length : closeAt + close.Length;
        var newlines = CountNewlines(text, start, end);
        found.Add(new SourceComment(line, line + newlines, text[start..end]));
        line += newlines;
        return end;
    }

    private static int SkipRawString(string text, int start, ref int line)
    {
        var quotes = 0;
        while (start + quotes < text.Length && text[start + quotes] == '"')
            quotes++;
        var closeAt = text.IndexOf(
            new string('"', quotes),
            start + quotes,
            StringComparison.Ordinal
        );
        var end = closeAt < 0 ? text.Length : closeAt + quotes;
        line += CountNewlines(text, start, end);
        return end;
    }

    private static int SkipQuoted(string text, int start, ref int line)
    {
        var quote = text[start];
        var verbatim = start > 0 && text[start - 1] == '@';
        var j = start + 1;
        while (j < text.Length)
        {
            var c = text[j];
            if (c == '\\' && !verbatim)
            {
                j += 2;
                continue;
            }
            if (c == quote)
            {
                if (verbatim && j + 1 < text.Length && text[j + 1] == quote)
                {
                    j += 2;
                    continue;
                }
                break;
            }
            if (c == '\n' && quote != '`' && !verbatim)
                break;
            j++;
        }
        var end = Math.Min(j, text.Length);
        line += CountNewlines(text, start, end);
        return end + 1;
    }

    private static List<SourceComment> FindHash(string text)
    {
        var found = new List<SourceComment>();
        var lines = text.Split('\n');
        for (var k = 0; k < lines.Length; k++)
        {
            var trimmed = lines[k].TrimStart();
            if (trimmed.StartsWith('#') && !trimmed.StartsWith("#!"))
                found.Add(new SourceComment(k + 1, k + 1, trimmed));
        }
        return found;
    }

    private static List<SourceComment> FindXml(string text)
    {
        var found = new List<SourceComment>();
        var line = 1;
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\n')
            {
                line++;
                i++;
            }
            else if (StartsWith(text, i, "<!--"))
                i = TakeDelimited(text, i, "-->", found, ref line);
            else
                i++;
        }
        return found;
    }

    private static List<SourceComment> MergeAdjacentLineComments(
        List<SourceComment> raw,
        string[] lines
    )
    {
        var merged = new List<SourceComment>();
        foreach (var comment in raw)
        {
            if (
                merged.Count > 0
                && IsWholeLine(comment, lines)
                && IsWholeLine(merged[^1], lines)
                && merged[^1].EndLine == comment.StartLine - 1
                && IsLineComment(comment)
                && IsLineComment(merged[^1])
            )
            {
                var previous = merged[^1];
                merged[^1] = previous with
                {
                    EndLine = comment.EndLine,
                    Text = previous.Text + "\n" + comment.Text,
                };
                continue;
            }
            merged.Add(comment);
        }
        return merged;
    }

    public static bool IsWholeLine(SourceComment comment, string[] lines) =>
        lines[comment.StartLine - 1].TrimStart().StartsWith(comment.Text.TrimStart()[..2]);

    private static bool IsLineComment(SourceComment comment) =>
        comment.Text.StartsWith("//") || comment.Text.StartsWith('#');

    private static bool StartsWith(string text, int index, string value) =>
        string.CompareOrdinal(text, index, value, 0, value.Length) == 0;

    private static int CountNewlines(string text, int start, int end)
    {
        var count = 0;
        for (var k = start; k < end && k < text.Length; k++)
            if (text[k] == '\n')
                count++;
        return count;
    }
}
