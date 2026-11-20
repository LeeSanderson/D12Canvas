namespace D12Canvas;

public enum GlyphPaint
{
    Stroke,
    Fill,
    AccentStroke,
    AccentFill,
}

public sealed record GlyphPart(string Data, GlyphPaint Paint = GlyphPaint.Stroke);

public sealed record Glyph
{
    public Glyph(params IEnumerable<GlyphPart> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var copied = parts.ToArray();
        if (copied.Length == 0)
        {
            throw new ArgumentException("A glyph needs at least one part.", nameof(parts));
        }

        if (copied.Any(part => part is null || string.IsNullOrWhiteSpace(part.Data)))
        {
            throw new ArgumentException("Every glyph part needs path data.", nameof(parts));
        }

        Parts = Array.AsReadOnly(copied);
    }

    public IReadOnlyList<GlyphPart> Parts { get; }

    public bool Equals(Glyph? other) => other is not null && Parts.SequenceEqual(other.Parts);

    public override int GetHashCode() =>
        Parts.Aggregate(0, (hash, part) => HashCode.Combine(hash, part));
}
